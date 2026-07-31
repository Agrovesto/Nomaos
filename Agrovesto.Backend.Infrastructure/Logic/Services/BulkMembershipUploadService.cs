using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Services
{
    public interface IBulkMembershipUploadService
    {
        Task<BulkUploadResult> ProcessBulkMembershipUploadAsync(List<BulkMembershipRowDto> rows, OnBoardingArgs args);
    }
    public class BulkMembershipUploadService : IBulkMembershipUploadService
    {
        private readonly DataContext _db;
        private readonly ILogger<BulkMembershipUploadService> _logger;
        private const int ChunkSize = 50; // tune per DB capacity

        public BulkMembershipUploadService(
            DataContext db,
            ILogger<BulkMembershipUploadService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<BulkUploadResult> ProcessBulkMembershipUploadAsync(
     List<BulkMembershipRowDto> rows, OnBoardingArgs args)
        {
            var result = new BulkUploadResult { TotalRows = rows.Count };

            var membershipTypeMap = await _db.MemberShipTypes
                .Where(t => t.BusinessCode == args.businessCode)
                .ToDictionaryAsync(t => t.Name.ToLower(), t => t);

            var businessMap = await _db.Businesses
                .Where(b => b.BusinessCode == args.businessCode)
                .ToDictionaryAsync(b => b.BusinessCode, b => b);

            var orgMap = new Dictionary<string, Organization>();
            if (!string.IsNullOrWhiteSpace(args.organizationCode))
            {
                orgMap = await _db.Organizations
                    .Where(o => o.OrganizationCode == args.organizationCode)
                    .ToDictionaryAsync(o => o.OrganizationCode, o => o);
            }

            // ── Pre-fetch existing users by phone (to REUSE, not reject) ──
            var incomingPhones = rows.Select(r => r.LeaderPhoneNumber).ToList();
            var existingUsersByPhone = (await _db.Users
                .Where(u => incomingPhones.Contains(u.PhoneNumber))
                .ToListAsync())
                .ToDictionary(u => u.PhoneNumber, u => u, StringComparer.OrdinalIgnoreCase);

            // ── Pre-fetch existing MemberShips by name (to REUSE, not reject) ──
            var incomingNames = rows.Select(r => r.MembershipName).ToList();
            var existingMembershipsByName = (await _db.MemberShips
                .Where(m => incomingNames.Contains(m.Name) && m.BelongToBusinessCode == args.businessCode)
                .ToListAsync())
                .ToDictionary(m => m.Name, m => m, StringComparer.OrdinalIgnoreCase);

            // ── The ONLY real pre-check: a membership name collision with a
            // DIFFERENT leader than what's in this row. Everything else -
            // same leader re-uploading, same membership already linked to this
            // org, etc. - is treated as idempotent reuse, not a failure.
            // This is intentionally narrow: it's the one case where we genuinely
            // can't tell if this is a duplicate name for a different group, or a
            // leadership change, so it needs a human to confirm rather than us
            // silently merging or silently rejecting.
            foreach (var row in rows)
            {
                if (existingMembershipsByName.TryGetValue(row.MembershipName, out var existingMembership)
                    && !string.Equals(existingMembership.PhoneNumber, row.LeaderPhoneNumber, StringComparison.OrdinalIgnoreCase))
                {
                    result.Errors.Add(new MembershipRowError
                    {
                        RowNumber = row.RowNumber,
                        MembershipName = row.MembershipName,
                        Messages = new List<string>
                {
                    $"Row {row.RowNumber}: membership '{row.MembershipName}' already exists in this business " +
                    $"under a different leader (phone {existingMembership.PhoneNumber}). " +
                    "If this is a leadership change, please update it directly rather than through bulk upload."
                }
                    });
                }
            }

            result.FailureCount = result.Errors.Select(e => e.RowNumber).Distinct().Count();
            var validRows = rows.Where(r =>
                !result.Errors.Any(e => e.RowNumber == r.RowNumber)).ToList();

            var validChunks = validRows
                .Select((row, i) => new { row, i })
                .GroupBy(x => x.i / ChunkSize)
                .Select(g => g.Select(x => x.row).ToList());

            var strategy = _db.Database.CreateExecutionStrategy();

            foreach (var chunk in validChunks)
            {
                var currentChunk = chunk;
                var rowOutcomes = new Dictionary<int, MembershipRowOutcome>();

                await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _db.Database
                        .BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);
                    try
                    {
                        foreach (var row in currentChunk)
                        {
                            var outcome = await ProcessSingleRowAsync(
                                row, membershipTypeMap, businessMap, orgMap,
                                existingUsersByPhone, existingMembershipsByName, args);

                            rowOutcomes[row.RowNumber] = outcome;
                        }

                        await _db.SaveChangesAsync();
                        await transaction.CommitAsync();

                        foreach (var outcome in rowOutcomes.Values)
                        {
                            if (outcome == MembershipRowOutcome.AlreadyExisted)
                                result.AlreadyExisted++;
                            else
                                result.SuccessCount++;
                        }
                    }
                    catch (DuplicateMembershipException ex)
                    {
                        await transaction.RollbackAsync();
                        result.Errors.Add(new MembershipRowError
                        {
                            RowNumber = ex.RowNumber,
                            MembershipName = currentChunk
                                .FirstOrDefault(r => r.RowNumber == ex.RowNumber)?.MembershipName,
                            Messages = new List<string> { ex.Message }
                        });
                        result.FailureCount++;
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex,
                            "Chunk starting at row {Row} failed and was rolled back.",
                            currentChunk[0].RowNumber);

                        foreach (var row in currentChunk)
                        {
                            result.Errors.Add(new MembershipRowError
                            {
                                RowNumber = row.RowNumber,
                                MembershipName = row.MembershipName,
                                Messages = new List<string> { "Chunk failed: " + ex.Message }
                            });
                        }

                        result.FailureCount += currentChunk.Count;
                        throw;
                    }
                });
            }

            return result;
        }

        private async Task<MembershipRowOutcome> ProcessSingleRowAsync(
            BulkMembershipRowDto row,
            Dictionary<string, MemberShipType> typeMap,
            Dictionary<string, Business> businessMap,
            Dictionary<string, Organization> orgMap,
            Dictionary<string, User> existingUsersByPhone,
            Dictionary<string, MemberShip> existingMembershipsByName,
            OnBoardingArgs args)
        {
            var nameParts = (row.LeaderName ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var firstName = nameParts.ElementAtOrDefault(0);
            var middleName = nameParts.Length > 2 ? nameParts.ElementAtOrDefault(1) : null;
            var lastName = nameParts.Length > 1 ? nameParts.Last() : null;

            var membershipType = typeMap[row.MembershipType.ToLower()];
            var business = businessMap[args.businessCode];

            // ── 1. Reuse existing User by phone, or create one ───────
            User user;
            bool isNewUser = !existingUsersByPhone.TryGetValue(row.LeaderPhoneNumber, out user!);

            if (isNewUser)
            {
                user = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"{firstName} {middleName} {lastName}",
                    CreatedOn = DateTimeOffset.Now,
                    CreatedBy = args.UserId,
                    UserType = "MemberShip",
                    BusinessCode = args.businessCode,
                    IsActive = true,
                    UserName = row.LeaderPhoneNumber,
                    Pin = "1234",
                    PhoneNumber = row.LeaderPhoneNumber,
                };
                await _db.Users.AddAsync(user);
                existingUsersByPhone[row.LeaderPhoneNumber] = user;
            }

            // ── 2. Reuse existing MemberShip by name+business, or create one ──
            // (Genuine name/leader conflicts were already filtered out before
            // this chunk started, so any match here is a safe, same-leader reuse.)
            MemberShip membership;
            bool isNewMembership = !existingMembershipsByName.TryGetValue(row.MembershipName, out membership!);

            if (isNewMembership)
            {
                membership = new MemberShip
                {
                    Name = row.MembershipName,
                    State = row.State,
                    Lga = row.LGA,
                    Location = row.Location,
                    MembershipLeaderFirstName = firstName,
                    MembershipLeaderMiddleName = middleName,
                    MembershipLeaderLastName = lastName,
                    PhoneNumber = row.LeaderPhoneNumber,
                    UserId = user.Id,
                    BelongToBusinessCode = args.businessCode,
                    MemberShipTypeId = membershipType.Id,
                    OnboardedById = args.UserId,
                    TierLevel = Tier.None,
                    IsActive = true,
                    OnBoardedOn = DateTimeOffset.UtcNow,
                };
                await _db.MemberShips.AddAsync(membership);

                // Track so later rows in the SAME batch with this name reuse it too
                existingMembershipsByName[row.MembershipName] = membership;
            }

            // ── 3. UserBusiness — only add if not already linked ─────
            bool alreadyInBusiness = !isNewUser && await _db.UserBusinesses
                .AnyAsync(ub => ub.UserId == user.Id && ub.BusinessId == business.BusinessId);

            if (!alreadyInBusiness)
            {
                await _db.UserBusinesses.AddAsync(new UserBusiness
                {
                    UserId = user.Id,
                    BusinessId = business.BusinessId,
                    IsDefault = true,
                    IsActive = true,
                    JoinedOn = DateTimeOffset.UtcNow,
                    InvitedBy = args.UserId,
                });
            }

            // ── 4. Organization links — only add if not already linked ──
            if (!string.IsNullOrWhiteSpace(args.organizationCode)
                && orgMap.TryGetValue(args.organizationCode, out var org))
            {
                bool alreadyInOrgAsUser = !isNewUser && await _db.UserOrganizations
                    .AnyAsync(uo => uo.UserId == user.Id && uo.OrganizationCode == org.OrganizationCode);

                if (!alreadyInOrgAsUser)
                {
                    await _db.UserOrganizations.AddAsync(new UserOrganization
                    {
                        UserId = user.Id,
                        OrganizationCode = org.OrganizationCode,
                        IsActive = true,
                    });
                }

                bool membershipAlreadyInOrg = !isNewMembership && await _db.OrganizationMemberships
                    .AnyAsync(om => om.MemberShip.Id == membership.Id && om.OrganizationId == org.OrganizationId);

                if (!membershipAlreadyInOrg)
                {
                    await _db.OrganizationMemberships.AddAsync(new OrganizationMembership
                    {
                        OrganizationId = org.OrganizationId,
                        MemberShip = membership,
                        AssignedByUserId = args.UserId,
                        IsActive = true,
                        AssignedOn = DateTimeOffset.UtcNow,
                    });
                }
            }

            // ── 5. Agent link ──────────────────────────────────────
            if (args.Id > 0)
            {
                bool alreadyLinkedToAgent = !isNewMembership && await _db.MemberShipAgents
                    .AnyAsync(ma => ma.MemberShip.Id == membership.Id && ma.AgentId == args.Id);

                if (!alreadyLinkedToAgent)
                {
                    await _db.MemberShipAgents.AddAsync(new MemberShipAgent
                    {
                        AgentId = args.Id,
                        MemberShip = membership,
                        OnboardedByUserId = args.UserId,
                        IsActive = true,
                        AssignedOn = DateTimeOffset.UtcNow,
                    });

                    membership.HasbeenAssignedAgent = true;
                }
            }

            return isNewMembership ? MembershipRowOutcome.Created : MembershipRowOutcome.AlreadyExisted;
        }
    }
}
