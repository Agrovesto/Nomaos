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

            // Prefetch lookup tables
            var membershipTypeMap = await _db.MemberShipTypes
                .Where(t => t.BusinessCode == args.businessCode)
                .ToDictionaryAsync(t => t.Name.ToLower(), t => t);

            var businessMap = await _db.Businesses
                .Where(b => b.BusinessCode == args.businessCode)
                .ToDictionaryAsync(b => b.BusinessCode, b => b);

            var orgMap = new Dictionary<string, Organization>();
            var orgCodes = rows
                .Where(r => !string.IsNullOrWhiteSpace(args.organizationCode))
                .Select(r => args.organizationCode)
                .Distinct()
                .ToList();

            if (orgCodes.Any())
            {
                orgMap = await _db.Organizations
                    .Where(o => orgCodes.Contains(o.OrganizationCode))
                    .ToDictionaryAsync(o => o.OrganizationCode, o => o);
            }

            // ── Pre-validate duplicates (2 DB calls, no transactions) ────
            var incomingPhones = rows.Select(r => r.LeaderPhoneNumber).ToList();
            var existingPhones = (await _db.Users
                .Where(u => incomingPhones.Contains(u.PhoneNumber)
                         && u.BusinessCode == args.businessCode)
                .Select(u => u.PhoneNumber)
                .ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var incomingNames = rows.Select(r => r.MembershipName).ToList();
            var existingNames = (await _db.MemberShips
                .Where(m => incomingNames.Contains(m.Name)
                         && m.BelongToBusinessCode == args.businessCode)
                .Select(m => m.Name)
                .ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                if (existingPhones.Contains(row.LeaderPhoneNumber))
                    result.Errors.Add(new MembershipRowError
                    {
                        RowNumber = row.RowNumber,
                        PhoneNumber = row.LeaderPhoneNumber,
                        Messages = new List<string>
                    { $"Row {row.RowNumber}: phone number '{row.LeaderPhoneNumber}' already exists." }
                    });

                if (existingNames.Contains(row.MembershipName))
                    result.Errors.Add(new MembershipRowError
                    {
                        RowNumber = row.RowNumber,
                        MembershipName = row.MembershipName,
                        Messages = new List<string>
                    { $"Row {row.RowNumber}: membership name '{row.MembershipName}' already exists." }
                    });
            }

            result.FailureCount = result.Errors.Count; // ← sync failure count from pre-validation

            // ── Only chunk and process rows that passed ───────────────────
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

                await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _db.Database
                        .BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);
                    try
                    {
                        foreach (var row in currentChunk)
                        {
                            await ProcessSingleRowAsync(
                                row, membershipTypeMap, businessMap, orgMap, args);
                        }

                        await _db.SaveChangesAsync();
                        await transaction.CommitAsync();
                        result.SuccessCount += currentChunk.Count;
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
        private async Task ProcessSingleRowAsync(
            BulkMembershipRowDto row,
            Dictionary<string, MemberShipType> typeMap,
            Dictionary<string, Business> businessMap,
            Dictionary<string, Organization> orgMap, OnBoardingArgs args)
        {

            // ── Split leader name into parts ──────────────────────────
            var nameParts = (row.LeaderName ?? "").Split(' ',
                StringSplitOptions.RemoveEmptyEntries);
            var firstName = nameParts.ElementAtOrDefault(0);
            var middleName = nameParts.Length > 2
                ? nameParts.ElementAtOrDefault(1) : null;
            var lastName = nameParts.Length > 1
                ? nameParts.Last() : null;

            var membershipType = typeMap[row.MembershipType.ToLower()];
            var business = businessMap[args.businessCode];

            // ── 1. Create the User account ────────────────────────────
            var userId = Guid.NewGuid().ToString();
            var user = new User
            {
                Id = userId,
                Name = $"{firstName} {middleName} {lastName}",
                CreatedOn= DateTimeOffset.Now,
                CreatedBy= args.UserId,
                UserType="MemberShip",
                BusinessCode=args.businessCode,
                IsActive=true,
                UserName=row.LeaderPhoneNumber,
                Pin="0000",
                PhoneNumber = row.LeaderPhoneNumber,
                // Email / password not set for bulk-imported members;
                // they complete registration separately via invite flow
            };
            await _db.Users.AddAsync(user);

            // ── 2. Create the Membership record ──────────────────────
            var membership = new MemberShip
            {
                Name = row.MembershipName,
                State = row.State,
                Lga = row.LGA,
                Location = row.Location,
                MembershipLeaderFirstName = firstName,
                MembershipLeaderMiddleName = middleName,
                MembershipLeaderLastName = lastName,
                PhoneNumber = row.LeaderPhoneNumber,
                UserId = userId,
                BelongToBusinessCode = args.businessCode,
                MemberShipTypeId = membershipType.Id,
                OnboardedById = args.UserId,
                TierLevel = Tier.None,
                IsActive = true,
                OnBoardedOn = DateTimeOffset.UtcNow,
            };
            await _db.MemberShips.AddAsync(membership);

            // EF needs the Membership PK before writing junction records;
            // SaveChanges is called at the END of the chunk, so we use
            // a temporary state approach: add all entities and let EF
            // resolve FK relationships via navigation properties.
            // Alternatively, call SaveChanges here and rely on the outer
            // transaction for rollback safety — choose based on your
            // EF setup.

            // ── 3. UserBusiness (REQUIRED — at least one business) ───
            var userBusiness = new UserBusiness
            {
                UserId = userId,
                BusinessId = business.BusinessId,
                IsDefault = true,
                IsActive = true,
                JoinedOn = DateTimeOffset.UtcNow,
                InvitedBy = args.UserId,
            };
            await _db.UserBusinesses.AddAsync(userBusiness);

            // ── 4. Organization links (CONDITIONAL) ──────────────────
            if (!string.IsNullOrWhiteSpace(args.organizationCode)
                && orgMap.TryGetValue(args.organizationCode, out var org))
            {
                var userOrg = new UserOrganization
                {
                    UserId = userId,
                    OrganizationCode = org.OrganizationCode,
                    IsActive = true,
                    //JoinedOn = DateTimeOffset.UtcNow,
                };
                await _db.UserOrganizations.AddAsync(userOrg);

                // OrganizationMembership is written after membership is
                // tracked by EF — use the navigation property so EF
                // resolves the FK automatically
                var orgMembership = new OrganizationMembership
                {
                    OrganizationId = org.OrganizationId,
                    MemberShip = membership, // EF resolves Id
                    AssignedByUserId = args.UserId,
                    IsActive = true,
                    AssignedOn = DateTimeOffset.UtcNow,
                };
                await _db.OrganizationMemberships.AddAsync(orgMembership);
            }

            // ── 5. Agent link (CONDITIONAL) ──────────────────────────
            if (args.Id > 0)
            {
                var membershipAgent = new MemberShipAgent
                {
                    AgentId = args.Id,
                    MemberShip = membership, // EF resolves Id
                    OnboardedByUserId = args.UserId,
                    IsActive = true,
                    AssignedOn = DateTimeOffset.UtcNow,
                };
                await _db.MemberShipAgents.AddAsync(membershipAgent);

                membership.HasbeenAssignedAgent = true;
            }
        }
    }
}
