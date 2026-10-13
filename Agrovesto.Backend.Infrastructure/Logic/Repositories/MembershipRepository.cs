using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Input;
using Agrovesto.Backend.Core.Models.Payment;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.Logic.Services;
using Agrovesto.Backend.Infrastructure.Utilities;
using Amazon.Runtime.Internal.Util;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using PhoneNumbers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories
{
    public class MembershipRepository : IMembershipRepository
    {
        private readonly DataContext _context;
        private readonly IDistributedCache _cache;
        private readonly ILogger<MembershipRepository> _logger;
        private const int DefaultMembershipTypeId = 1;
        public MembershipRepository(DataContext context,
            IDistributedCache cache,
            ILogger<MembershipRepository> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

       
       
        public async Task DeactivateUser(
            string membershipUserId,
            string participantUserId,
            MembershipParticipantType participantType,
            string deactivatedByUserId,
            string? reason)
        {
            var membership = await _context.MemberShips
                .Where(m => m.UserId == membershipUserId)
                .Select(m => new { m.Id })
                .FirstOrDefaultAsync();

            if (membership == null)
                throw new BaseException(ExceptionType.NOTFOUND, "Membership not found");

            switch (participantType)
            {
                case MembershipParticipantType.Farmer:
                    {
                        var farmer = await _context.Farmers
                            .Where(f => f.UserId == participantUserId)
                            .Select(f => new { f.Id })
                            .FirstOrDefaultAsync();

                        if (farmer == null)
                            throw new BaseException(ExceptionType.NOTFOUND, "Farmer not found");

                        var farmerCooperative = await _context.FarmerCooperatives
                            .Where(fc => fc.CooperativeId == membership.Id
                                      && fc.FarmerId == farmer.Id
                                      && fc.IsActive)
                            .FirstOrDefaultAsync();

                        if (farmerCooperative == null)
                            throw new BaseException(ExceptionType.NOTFOUND, "Farmer is not an active member of this membership");

                        farmerCooperative.IsActive = false;
                        farmerCooperative.DeactivationReason = reason;
                        farmerCooperative.DeactivatedByUserId = deactivatedByUserId;
                        
                        farmerCooperative.DeactivatedOn = DateTimeOffset.Now;
                        break;
                    }

                case MembershipParticipantType.Agent:
                    {
                        var agent = await _context.Agents
                            .Where(a => a.UserId == participantUserId)
                            .Select(a => new { a.Id })
                            .FirstOrDefaultAsync();

                        if (agent == null)
                            throw new BaseException(ExceptionType.NOTFOUND, "Agent not found");

                        var membershipAgent = await _context.MemberShipAgents
                            .Where(ma => ma.MemberShipId == membership.Id
                                      && ma.AgentId == agent.Id
                                      && ma.IsActive)
                            .FirstOrDefaultAsync();

                        if (membershipAgent == null)
                            throw new BaseException(ExceptionType.NOTFOUND, "Agent is not actively assigned to this membership");

                        membershipAgent.IsActive = false;
                        membershipAgent.DeactivationReason = reason;
                        membershipAgent.DeactivatedByUserId = deactivatedByUserId;
                        // Note: MemberShipAgent has no DeactivatedOn field (unlike FarmerCooperative) — nothing to set here.
                        break;
                    }

                default:
                    throw new ArgumentOutOfRangeException(nameof(participantType));
            }

            await _context.SaveChangesAsync();
        }
        public async Task<MembershipResponse> CreateMembershipAsync(
        CreateMembershipDto dto,
       OnBoardingArgs args
            )
        {
            // ── Validate businesses exist ─────────────────────────────
            var business = await _context.Businesses
                .Where(b => b.BusinessCode == args.businessCode && b.IsActive).FirstOrDefaultAsync()
                ;

            if (business == null)
                throw new ValidationException(
                    $"Business not found: {args.businessCode}");

            // ── Validate MemberShipType belongs to one of these businesses ─
            var membershipType = await _context.MemberShipTypes
                .FirstOrDefaultAsync(t =>
                    t.Id == dto.MemberShipTypeId &&
                    t.BusinessCode == args.businessCode);

            if (membershipType is null)
                throw new ValidationException(
                    "MembershipType not found for the given business.");

            // ── Validate organization if provided ─────────────────────
            Organization? organization = null;
            if (!string.IsNullOrWhiteSpace(args.organizationCode))
            {
                organization = await _context.Organizations
                    .FirstOrDefaultAsync(o =>
                        o.BusinessCode == args.organizationCode && o.IsActive);

                if (organization is null)
                    throw new ValidationException(
                        $"Organization '{args.organizationCode}' not found.");
            }

            // ── Validate agent if provided ────────────────────────────
            if (args.AgentIds.Any())
            {
                var agentExists = await _context.Agents
                    .AnyAsync(a => args.AgentIds.Contains(a.Id) && a.IsActive);

                if (!agentExists)
                    throw new ValidationException(
                        $"Agent {args.AgentIds} not found.");
            }
            dto.SignupMethod = !string.IsNullOrWhiteSpace(dto.Email)
? SignupMethod.Email
: SignupMethod.PhoneNumber;
            await using var transaction = await _context.Database
                .BeginTransactionAsync(IsolationLevel.ReadCommitted);
            try
            {
                var stubUser = new User
                {
                    UserType = "MemberShip",
                    IsActive = false,          // inactive until leader is assigned
                    IsLeaderProfileComplete = false,
                    CreatedOn = DateTimeOffset.UtcNow,
                    CreatedBy = args.UserId,
                    BusinessCode = args.businessCode, // primary business
                    DateOfBirth = dto.DateOfBirth,
                    SignupMethod = dto.SignupMethod
                };
                await _context.Users.AddAsync(stubUser);

                var membership = new MemberShip
                {
                    Name = dto.Name,
                    State = dto.State,
                    Lga = dto.Lga,
                    Ward = dto.Ward,
                    Location = dto.Location,
                    ContactPerson = dto.ContactPerson,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    UserId = stubUser.Id,
                    BelongToBusinessCode = args.businessCode, // primary
                    MemberShipTypeId = dto.MemberShipTypeId,
                    OnboardedById = args.UserId,
                    TierLevel = Tier.Tier1,
                    OnBoardedOn = DateTimeOffset.UtcNow,
                    IsActive = true,
                    RegistrationNumber= dto.RegistrationNumber
                };
                await _context.MemberShips.AddAsync(membership);


                await _context.UserBusinesses.AddAsync(new UserBusiness
                {
                    UserId = stubUser.Id,
                    BusinessId = business.BusinessId,
                    IsDefault = business.BusinessCode == args.businessCode,
                    IsActive = true,
                    JoinedOn = DateTimeOffset.UtcNow,
                    InvitedBy = args.UserId,
                });


                if (organization is not null)
                {
                    await _context.UserOrganizations.AddAsync(new UserOrganization
                    {
                        UserId = stubUser.Id,
                        OrganizationCode = organization.OrganizationCode,
                        IsActive = true,
                    });

                    await _context.OrganizationMemberships.AddAsync(new OrganizationMembership
                    {
                        OrganizationId = organization.OrganizationId,
                        MemberShip = membership,  // EF resolves FK
                        AssignedByUserId = args.UserId,
                        IsActive = true,
                        AssignedOn = DateTimeOffset.UtcNow,
                    });
                }

                if (args.AgentIds.Any())
                {
                    await _context.MemberShipAgents.AddAsync(new MemberShipAgent
                    {
                        AgentId = args.AgentIds.FirstOrDefault().Value,
                        MemberShip = membership,  // EF resolves FK
                        OnboardedByUserId = args.UserId,
                        IsActive = true,
                        AssignedOn = DateTimeOffset.UtcNow,
                    });

                    membership.HasbeenAssignedAgent = true;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Membership {Name} created with stub user {UserId}",
                    membership.Name, stubUser.Id);

                return MapToResponse(membership, stubUser, business, organization);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

        }
        public async Task<MembershipResponse> AssignLeaderAsync(
        AssignLeaderDto dto,
        string updatedById)
        {
            // ── Load membership with its current stub user ─────────────
            var membership = await _context.MemberShips
                .Include(m => m.MemberShipType)
                .FirstOrDefaultAsync(m => m.Id == dto.MembershipId && m.IsActive);

            if (membership is null)
                throw new NotFoundException("Membership not found.");

            await using var transaction = await _context.Database
                .BeginTransactionAsync(IsolationLevel.ReadCommitted);
            try
            {
                User leader;

                if (!string.IsNullOrWhiteSpace(dto.ExistingUserId))
                {
                    // ── Path A: link an existing platform user as leader ──
                    leader = await _context.Users.FindAsync(dto.ExistingUserId)
                        ?? throw new NotFoundException(
                            $"User '{dto.ExistingUserId}' not found.");

                    // If membership already had a different stub user, deactivate it
                    if (membership.UserId != dto.ExistingUserId)
                    {
                        var oldStub = await _context.Users.FindAsync(membership.UserId);
                        if (oldStub is not null && !oldStub.IsLeaderProfileComplete)
                        {
                            // It was a stub — safe to deactivate
                            oldStub.IsActive = false;
                            oldStub.ModifiedBy = updatedById;
                            oldStub.DateModified = DateTimeOffset.UtcNow;
                        }
                    }

                    membership.UserId = leader.Id;
                }
                else
                {
                    // ── Path B: update the existing stub user ─────────────
                    leader = await _context.Users.FindAsync(membership.UserId)
                        ?? throw new NotFoundException(
                            "Stub user for this membership not found.");
                }

                // ── Update User with leader details ────────────────────
                leader.Name = $"{dto.FirstName} {dto.LastName}".Trim();
                leader.PhoneNumber = dto.PhoneNumber;
                leader.Email = dto.Email ?? leader.Email;
                leader.IsActive = true;
                leader.IsLeaderProfileComplete = true;
                leader.ModifiedBy = updatedById;
                leader.DateModified = DateTimeOffset.UtcNow;

                // ── Update Membership leader fields ────────────────────
                membership.MembershipLeaderFirstName = dto.FirstName;
                membership.MembershipLeaderMiddleName = dto.MiddleName;
                membership.MembershipLeaderLastName = dto.LastName;
                membership.MembershipLeaderGender = dto.Gender;
                membership.IsLeaderAFarmer = dto.IsLeaderAFarmer;
                membership.PhoneNumber = dto.PhoneNumber;
                membership.Email = dto.Email ?? membership.Email;
                membership.UserId = leader.Id;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Leader '{Name}' assigned to membership {MembershipId}",
                    leader.Name, membership.Id);

                var businesses = await _context.UserBusinesses
                    .Include(ub => ub.Business)
                    .Where(ub => ub.UserId == leader.Id && ub.IsActive)
                    .Select(ub => ub.Business.BusinessCode)
                    .ToListAsync();

                return MapToResponse(membership, leader, null, null,
                    businessCodes: businesses);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static MembershipResponse MapToResponse(
            MemberShip membership,
            User user,
            Business? businesses,
            Organization? org,
            List<string>? businessCodes = null)
        {
            return new MembershipResponse
            {
                Id = membership.Id,
                Name = membership.Name,
                UserId = user.Id,
                IsLeaderProfileComplete = user.IsLeaderProfileComplete,
                State = membership.State,
                Location = membership.Location,
                BusinessCode = businesses.BusinessCode,
                HasbeenAssignedAgent = membership.HasbeenAssignedAgent,
            };
        }

        public IQueryable<Farmer> BuildFarmerQuery(FarmerFilter filter)
        {
            var query = _context.Farmers.AsNoTracking()
                .Where(f =>
                    _context.FarmerCooperatives.Any(fc =>
                        fc.FarmerId == f.Id &&
                        fc.CooperativeId == filter.CooperativeId));

            if (!string.IsNullOrWhiteSpace(filter.BusinessCode))
            {
                query = query.Where(f => f.BelongToBusinessCode == filter.BusinessCode);
            }

            if (!string.IsNullOrWhiteSpace(filter.State))
            {
                query = query.Where(f => f.State == filter.State);
            }

            if (filter.Id.HasValue && filter.Id > 0)
            {
                query = query.Where(f => f.Id == filter.Id);
            }

            if (filter.StartDate.HasValue || filter.EndDate.HasValue)
            {
                var start = filter.StartDate?.Date ?? DateTime.MinValue.Date;
                var end = (filter.EndDate?.Date ?? DateTime.UtcNow.Date).AddDays(1); // exclusive upper bound

                query = query.Where(f => f.OnBoardedOn >= start && f.OnBoardedOn < end);
            }

            if (!string.IsNullOrWhiteSpace(filter.OrganizationCode))
            {
                query = query.Where(f =>
                    _context.FarmerOrganizations.Any(fo =>
                        fo.FarmerId == f.Id &&
                        fo.OrganizationCode == filter.OrganizationCode));
            }

            return query;
        }
        public async Task<MemberShipModel> AssignMembershipToAgents(AssignmentArgs args, long membershipId)
        {
            if (args.AgentIds == null || !args.AgentIds.Any())
                return null;

            // Check if the farmer exists
            var memberShip = await _context.MemberShips
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == membershipId);

            if (memberShip == null)
                return null;

            // Get existing assignments for this farmer
            var existingAssignments = await _context.MemberShipAgents
                .Where(fa => fa.MemberShipId == membershipId && args.AgentIds.Contains(fa.AgentId))
                .Select(fa => fa.AgentId)
                .ToListAsync();

            // Filter out agents that are already assigned

            var newAgentIds = args.AgentIds
    .Where(x => x.HasValue)
    .Select(x => x.Value)
    .Except(existingAssignments)
    .ToList();

            // Create new assignments
            if (newAgentIds.Any())
            {
                var newAssignments = newAgentIds.Select(agentId => new MemberShipAgent
                {
                    MemberShipId = membershipId,
                    AgentId = agentId,
                    AssignedOn = DateTimeOffset.UtcNow,

                }).ToList();

                await _context.MemberShipAgents.AddRangeAsync(newAssignments);
                await _context.SaveChangesAsync();
            }

            // Reload the farmer with updated data
            var updatedMembership = await _context.MemberShips
                .FirstOrDefaultAsync(f => f.Id == membershipId);

            return updatedMembership.Map();
        }
        private static readonly SubscriptionStatus[] DisbursedOrBeyondStatuses =
   {
    SubscriptionStatus.Disbursed,
    SubscriptionStatus.PartiallyDisbursed,
    SubscriptionStatus.Active,
    SubscriptionStatus.PendingRepayment,
    SubscriptionStatus.Completed,
    SubscriptionStatus.Defaulted
};

        // Disbursed but not yet at a terminal state (Completed/Defaulted) — currently being repaid.
        private static readonly SubscriptionStatus[] ActiveFinancingStatuses =
        {
    SubscriptionStatus.Disbursed,
    SubscriptionStatus.PartiallyDisbursed,
    SubscriptionStatus.Active,
    SubscriptionStatus.PendingRepayment
};

        public async Task<MembershipDashboardDto> GetMembershipDetailAsync(
            string membershipUserId,
            string organizationCode = null)
        {
            var membership = await _context.MemberShips
                .AsNoTracking()
                .Where(m => m.UserId == membershipUserId)
                .Join(_context.Users.AsNoTracking(),
                      m => m.UserId,
                      u => u.Id,
                      (m, u) => new { m, u })
                .GroupJoin(_context.OrganizationMemberships.AsNoTracking()
                        .Where(om => om.IsActive
                                  && (organizationCode == null || om.Organization.OrganizationCode == organizationCode)),
                    x => x.m.Id,
                    om => om.MembershipId,
                    (x, omGroup) => new { x.m, x.u, omGroup })
                .SelectMany(
                    x => x.omGroup
                        .OrderByDescending(om => om.AssignedOn)
                        .Take(1)
                        .DefaultIfEmpty(),
                    (x, om) => new { x.m, x.u, om })
                .Select(x => new MembershipInfoDto
                {
                    UserId = x.m.UserId,
                    MembershipId = x.m.Id,
                    Name = x.m.Name,
                    MembershipType = x.m.MemberShipType.Name,
                    Status = x.m.IsActive,
                    DateCreated = x.om != null ? x.om.AssignedOn : x.m.OnBoardedOn,
                    Location = x.m.Location,
                    LeaderName = x.u.Name,
                    PhoneNumber = x.u.PhoneNumber,
                    Email = x.u.Email
                })
                .FirstOrDefaultAsync();

            if (membership == null)
                throw new BaseException(ExceptionType.NOTFOUND, "Membership not found");

            var filter = new FarmerFilter
            {
                CooperativeId = membership.MembershipId,
                OrganizationCode = organizationCode
            };

            var baseQuery = BuildFarmerQuery(filter);

            var farmerStats = await baseQuery
                .GroupBy(f => 1)
                .Select(g => new
                {
                    TotalFarmers = g.Count(),
                    TotalVerifiedFarmers = g.Count(x => x.Level != FarmerLevel.Level1),
                    TotalUnverifiedFarmers = g.Count(x => x.Level == FarmerLevel.Level1)
                })
                .FirstOrDefaultAsync();

            var farmerIdsQuery = baseQuery.Select(f => f.Id);

            var farmStats = await _context.Farms
                .AsNoTracking()
                .Where(f => farmerIdsQuery.Contains(f.FarmerId))
                .GroupBy(f => 1)
                .Select(g => new
                {
                    TotalFarms = g.Count(),
                    TotalFarmSize = g.Sum(x => (double?)x.FarmSizeInSqMetre) ?? 0,
                    FarmsMapped = g.Count(x => x.MappingStatus == MappingStatus.Mapped)
                })
                .FirstOrDefaultAsync();

            var totalAgents = await _context.MemberShipAgents
                .AsNoTracking()
                .Where(x => x.MemberShipId == membership.MembershipId)
                .Select(x => x.AgentId)
                .Distinct()
                .CountAsync();

            // Farmers currently active in this cooperative — needed to also pull in Individual-type
            // subscriptions issued directly to this membership's farmers (not just MemberShip-type
            // subscriptions issued to the coop as a whole).
            var farmerUserIdsQuery = _context.FarmerCooperatives
                .AsNoTracking()
                .Where(fc => fc.CooperativeId == membership.MembershipId && fc.IsActive)
                .Join(_context.Farmers.AsNoTracking(),
                      fc => fc.FarmerId,
                      f => f.Id,
                      (fc, f) => f.UserId);

            // BUG FIX: previously only counted MemberShip-type subscriptions belonging to the coop itself.
            // Now also includes Individual-type subscriptions belonging to this membership's active farmers —
            // financing given directly to a member farmer should still count toward this membership's totals.
            var financingQuery = _context.InputPackageSubscriptions
                .AsNoTracking()
                .Where(s =>
                    ((s.SubscriberType == InputSubScriptionType.MemberShip && s.SubscriberUserId == membership.UserId) ||
                     (s.SubscriberType == InputSubScriptionType.Individual && farmerUserIdsQuery.Contains(s.SubscriberUserId)))
                    && (organizationCode == null || s.Organization.OrganizationCode == organizationCode));

            // Total value of everything actually disbursed to this membership (disbursed-or-beyond, same
            // convention used elsewhere — excludes pending/approved/rejected subscriptions that never paid out).
            var totalFinancedValue = await financingQuery
                .Where(s => DisbursedOrBeyondStatuses.Contains(s.Status))
                .SumAsync(s => (decimal?)s.TotalFinancingValue) ?? 0m;

            // Count of subscriptions currently disbursed and being repaid — excludes Completed/Defaulted
            // (terminal) and everything pre-disbursement.
            var activeFinancing = await financingQuery
                .Where(s => ActiveFinancingStatuses.Contains(s.Status))
                .CountAsync();

            var totalFarms = farmStats?.TotalFarms ?? 0;
            var farmsMapped = farmStats?.FarmsMapped ?? 0;

            return new MembershipDashboardDto
            {
                Membership = membership,
                Summary = new MemberSummaryDto
                {
                    TotalAgents = totalAgents,
                    TotalFarmers = farmerStats?.TotalFarmers ?? 0,
                    TotalVerifiedFarmers = farmerStats?.TotalVerifiedFarmers ?? 0,
                    TotalUnverifiedFarmers = farmerStats?.TotalUnverifiedFarmers ?? 0,
                    TotalFarms = totalFarms,
                    TotalFarmSize = farmStats?.TotalFarmSize ?? 0,
                    FarmsMapped = farmsMapped,
                    MappingProgressPercentage = totalFarms == 0
                        ? 0
                        : (int)Math.Round((double)farmsMapped / totalFarms * 100),
                    TotalFinancedValue = totalFinancedValue,
                    ActiveFinancing = activeFinancing
                }
            };
        }
        public async Task<PagedListResponse<List<MembershipAgentDto>>> GetMembershipAgentsAsync(
    string membershipUserId,
    ListQueryParams query)
        {
            query ??= new ListQueryParams();
            if (query.PageNumber <= 0) query.PageNumber = 1;
            if (query.PageSize <= 0 || query.PageSize > 10) query.PageSize = 10;

            var membership = await _context.MemberShips
                .AsNoTracking()
                .Where(m => m.UserId == membershipUserId)
                .Select(m => new { m.Id })
                .FirstOrDefaultAsync();

            if (membership == null)
                throw new BaseException(ExceptionType.NOTFOUND, "Membership not found");

            var membershipId = membership.Id;

            // Farmers who belong to THIS cooperative (active membership in the cooperative).
            var farmerIdsInThisCooperative = _context.FarmerCooperatives
                .AsNoTracking()
                .Where(fc => fc.CooperativeId == membershipId && fc.IsActive)
                .Select(fc => fc.FarmerId);

            var baseQuery = _context.MemberShipAgents
                .AsNoTracking()
                .Where(ma => ma.MemberShipId == membershipId)
                .Join(_context.Agents.AsNoTracking(),
                      ma => ma.AgentId,
                      a => a.Id,
                      (ma, a) => new { ma, a });

            var totalCount = await baseQuery.CountAsync();

            if (totalCount == 0)
            {
                return new PagedListResponse<List<MembershipAgentDto>>
                {
                    Data = new List<MembershipAgentDto>(),
                    Count = 0,
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize
                };
            }

            var agents = await baseQuery
                .OrderByDescending(x => x.ma.AssignedOn)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(x => new MembershipAgentDto
                {
                    AgentId= x.a.Id,
                    UserId = x.a.UserId,
                    AgentName = x.a.Name,
                    Location = x.a.Location,
                    // Only farmers assigned to this agent AND belonging to this specific membership/cooperative —
                    // not the agent's full farmer count across every cooperative they serve.
                    TotalFarmersAssignedToAgents = _context.FarmerAgents
                        .AsNoTracking()
                        .Where(fa => fa.AgentId == x.a.Id
                                  && fa.IsActive
                                  && farmerIdsInThisCooperative.Contains(fa.FarmerId))
                        .Count(),
                    // Status reflects the agent's assignment to THIS membership, not the agent's global IsActive flag.
                    Status = x.ma.IsActive
                })
                .ToListAsync();

            return new PagedListResponse<List<MembershipAgentDto>>
            {
                Data = agents,
                Count = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };
        }
        public async Task<PagedListResponse<List<MembershipFarmerDto>>> GetMembershipFarmersAsync(
        string membershipUserId,
        ListQueryParams query)
        {
            query ??= new ListQueryParams();
            if (query.PageNumber <= 0) query.PageNumber = 1;
            if (query.PageSize <= 0 || query.PageSize > 10) query.PageSize = 10;

            var membership = await _context.MemberShips
                .AsNoTracking()
                .Where(m => m.UserId == membershipUserId)
                .Select(m => new { m.Id })
                .FirstOrDefaultAsync();

            if (membership == null)
                throw new BaseException(ExceptionType.NOTFOUND, "Membership not found");

            var membershipId = membership.Id;

            var baseQuery = _context.FarmerCooperatives
                .AsNoTracking()
                .Where(fc => fc.CooperativeId == membershipId)
                .Join(_context.Farmers.AsNoTracking(),
                      fc => fc.FarmerId,
                      f => f.Id,
                      (fc, f) => new { fc, f })
                .Join(_context.Users.AsNoTracking(),
                      x => x.f.UserId,
                      u => u.Id,
                      (x, u) => new { x.fc, x.f, u });

            var totalCount = await baseQuery.CountAsync();

            if (totalCount == 0)
            {
                return new PagedListResponse<List<MembershipFarmerDto>>
                {
                    Data = new List<MembershipFarmerDto>(),
                    Count = 0,
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize
                };
            }

            // Level stays as the raw enum here — GetDisplayName reads a [Display] attribute via reflection
            // and can't be translated to SQL, so it's applied later, after this query is materialized.
            var pagedFarmers = await baseQuery
                .OrderByDescending(x => x.fc.JoinedOn)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(x => new
                {
                    x.f.Id,
                    FarmerUserId = x.f.UserId,
                    FarmerName = x.u.Name,
                    x.f.Level,
                    // Status reflects the farmer's membership in THIS cooperative, not any global farmer status
                    // (the Farmer entity has no IsActive field of its own).
                    Status = x.fc.IsActive
                })
                .ToListAsync();

            var farmerIds = pagedFarmers.Select(f => f.Id).ToList();

            // Only farms that have actually been mapped (regardless of verification status) count toward
            // TotalFarms/LandSize — MappingStatus.UnMapped means no polygon/size data has been captured yet.
            var farmStats = await _context.Farms
                .AsNoTracking()
                .Where(f => farmerIds.Contains(f.FarmerId)
                         && !f.IsDeleted
                         && f.MappingStatus != MappingStatus.UnMapped)
                .GroupBy(f => f.FarmerId)
                .Select(g => new
                {
                    FarmerId = g.Key,
                    TotalFarms = g.Count(),
                    TotalLandSizeInHectares = g.Sum(x => (double?)x.SizeInHectares) ?? 0
                })
                .ToDictionaryAsync(x => x.FarmerId, x => x);

            var result = pagedFarmers.Select(f =>
            {
                var stats = farmStats.GetValueOrDefault(f.Id);

                return new MembershipFarmerDto
                {
                    FarmerId=f.Id,
                    UserId = f.FarmerUserId,
                    FarmerName = f.FarmerName,
                    Level = EnumExtensions.GetDisplayName(f.Level),
                    TotalFarms = stats?.TotalFarms ?? 0,
                    LandSize = stats?.TotalLandSizeInHectares ?? 0,
                    Status = f.Status
                };
            }).ToList();

            return new PagedListResponse<List<MembershipFarmerDto>>
            {
                Data = result,
                Count = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };
        }

        public async Task<PagedListResponse<List<MembershipFinancingBreakdownDto>>> GetMembershipFinancingBreakdownAsync(
       string membershipUserId,
       ListQueryParams query)
        {
            query ??= new ListQueryParams();
            if (query.PageNumber <= 0) query.PageNumber = 1;
            if (query.PageSize <= 0 || query.PageSize > 10) query.PageSize = 10;

            var membership = await _context.MemberShips
                .AsNoTracking()
                .Where(m => m.UserId == membershipUserId)
                .Select(m => new { m.Id, m.UserId })
                .FirstOrDefaultAsync();

            if (membership == null)
                throw new BaseException(ExceptionType.NOTFOUND, "Membership not found");

            // For a membership-type subscription, "Members" is the number of farmers currently in this
            // cooperative — not tied to any individual subscription — so it's computed once and applied
            // identically to every row in the list.
            var totalFarmers = await _context.FarmerCooperatives
                .AsNoTracking()
                .Where(fc => fc.CooperativeId == membership.Id && fc.IsActive)
                .Select(fc => fc.FarmerId)
                .Distinct()
                .CountAsync();

            var baseQuery = _context.InputPackageSubscriptions
                .AsNoTracking()
                .Where(s => s.SubscriberUserId == membership.UserId
                         && s.SubscriberType == InputSubScriptionType.MemberShip);

            var totalCount = await baseQuery.CountAsync();

            if (totalCount == 0)
            {
                return new PagedListResponse<List<MembershipFinancingBreakdownDto>>
                {
                    Data = new List<MembershipFinancingBreakdownDto>(),
                    Count = 0,
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize
                };
            }

            // Status/PackageStructure stay as raw enums here — GetDisplayName can't be translated to SQL,
            // so it's applied later, after this query is materialized.
            var pagedSubscriptions = await baseQuery
                .OrderByDescending(s => s.SubscribedOn)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(s => new
                {
                    s.Id,
                    PackageName = s.InputPackage.Name,
                    PackageStructure = s.InputPackage.PackageStructure,
                    s.TotalFinancingValue,
                    s.RepaymentValue,
                    s.Status
                })
                .ToListAsync();

            var subscriptionIds = pagedSubscriptions.Select(s => s.Id).ToList();

            var paidPerSubscription = await _context.InputPackageSubscriptionPayments
                .AsNoTracking()
                .Where(p => subscriptionIds.Contains(p.SubscriptionId))
                .GroupBy(p => p.SubscriptionId)
                .Select(g => new
                {
                    SubscriptionId = g.Key,
                    TotalPaid = g.Sum(p => (decimal?)p.AmountPaid) ?? 0m
                })
                .ToDictionaryAsync(x => x.SubscriptionId, x => x.TotalPaid);

            var result = pagedSubscriptions.Select(s =>
            {
                var totalPaid = paidPerSubscription.GetValueOrDefault(s.Id);
                var outstanding = s.RepaymentValue - totalPaid;

                return new MembershipFinancingBreakdownDto
                {
                    PackageName = s.PackageName,
                    Structure = EnumExtensions.GetDisplayName(s.PackageStructure),
                    Members = totalFarmers,
                    FinancedValue = s.TotalFinancingValue,
                    OutstandingRepayments = outstanding < 0 ? 0 : outstanding,
                    Status = EnumExtensions.GetDisplayName(s.Status)
                };
            }).ToList();

            return new PagedListResponse<List<MembershipFinancingBreakdownDto>>
            {
                Data = result,
                Count = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };
        }
        public async Task<MemberShipTypeModel?> GetMemberShipTypeById(long id)
        {
            var type = await _context.MemberShipTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (type == null)
                return null;

            var response = type.Map();
            return response;
        }


        public async Task<List<MemberShipTypeModel>> GetMembershipTypes(BusinessRegistrationArgs args)
        {
            if (args == null || string.IsNullOrWhiteSpace(args.businessCode))
                return new List<MemberShipTypeModel>();

            return await _context.MemberShipTypes
                .AsNoTracking()
                .Where(m => m.BusinessCode == args.businessCode)
                .Select(x => new MemberShipTypeModel
                {
                    Id = x.Id,
                    Name = x.Name ?? string.Empty,
                })
                .ToListAsync();
        }
        public async Task<(bool status, MemberShipTypeModel data)> CreateUpdateMemberShipType(MemberShipTypeModel data, BusinessRegistrationArgs args)
        {
            try
            {
                var existingType = await _context.MemberShipTypes
                    .FirstOrDefaultAsync(x => x.Id == data.Id);

                if (existingType != null)
                {
                    if (!string.IsNullOrWhiteSpace(data.Name))
                        existingType.Name = data.Name;

                    if (!string.IsNullOrWhiteSpace(data.Description))
                        existingType.Description = data.Description;

                    existingType.UpdatedOn = DateTimeOffset.Now;
                    existingType.UpdatedBy = args.UserId;
                    _context.MemberShipTypes.Update(existingType);
                    await _context.SaveChangesAsync();

                    return (true, existingType.Map());
                }

                var entity = data.Map();
                entity.CreatedBy = args.UserId;
                entity.BusinessCode = args.businessCode;
                entity.CreatedOn = DateTimeOffset.Now;
                await _context.MemberShipTypes.AddAsync(entity);
                await _context.SaveChangesAsync();

                return (true, entity.Map());
            }
            catch (BaseException ex)
            {
                throw new BaseException(
                    Core.Enums.ExceptionType.OPERATION_FAILED,
                    ex.Message);
            }
            catch (Exception ex)
            {
                throw new NotSuccessfulException(
                    $"Could not save agent details: {ex.Message}");
            }
        }

        public async Task<ProfileChange<MemberShipModel>> UpsertMembershipAsync(MemberShipModel data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.UserId))
                throw new BaseException(ExceptionType.BAD_REQUEST, "User id is required to save membership details.");

            var entity = await _context.MemberShips.FirstOrDefaultAsync(x => x.UserId == data.UserId);
            var isNew = entity == null;
            var changed = new List<string>();

            if (isNew)
            {
                // The default type applies ONLY on create; on update 0 means "not provided".
                if (data.MemberShipTypeId == 0) data.MemberShipTypeId = DefaultMembershipTypeId;

                entity = data.Map();
                entity.BelongToBusinessCode = data.BelongToBusinessCode;
                _context.MemberShips.Add(entity);
            }
            else
            {
                // IsLeaderAFarmer is a plain bool, so "false" can't be told apart from "not sent".
                // It can therefore only be turned ON here. Make the DTO property bool? if it must be reversible.
                if (data.IsLeaderAFarmer && !entity.IsLeaderAFarmer)
                {
                    entity.IsLeaderAFarmer = true;
                    changed.Add(nameof(MemberShipModel.IsLeaderAFarmer));
                }

                PatchHelper.Patch(changed, nameof(MemberShipModel.Name), data.Name, entity.Name, v => entity.Name = v);
                PatchHelper.Patch(changed, nameof(MemberShipModel.Location), data.Location, entity.Location, v => entity.Location = v);
                PatchHelper.Patch(changed, nameof(MemberShipModel.State), data.State, entity.State, v => entity.State = v);
                PatchHelper.Patch(changed, nameof(MemberShipModel.Lga), data.Lga, entity.Lga, v => entity.Lga = v);
                PatchHelper.Patch(changed, nameof(MemberShipModel.Ward), data.Ward, entity.Ward, v => entity.Ward = v);
                PatchHelper.Patch(changed, nameof(MemberShipModel.ContactPerson), data.ContactPerson, entity.ContactPerson, v => entity.ContactPerson = v);
                PatchHelper.Patch(changed, nameof(MemberShipModel.Email), data.Email, entity.Email, v => entity.Email = v);
                PatchHelper.Patch(changed, nameof(MemberShipModel.PhoneNumber), data.PhoneNumber, entity.PhoneNumber, v => entity.PhoneNumber = v);
                PatchHelper.Patch(changed, nameof(MemberShipModel.MembershipLeaderFirstName), data.MembershipLeaderFirstName, entity.MembershipLeaderFirstName, v => entity.MembershipLeaderFirstName = v);
                PatchHelper.Patch(changed, nameof(MemberShipModel.MembershipLeaderMiddleName), data.MembershipLeaderMiddleName, entity.MembershipLeaderMiddleName, v => entity.MembershipLeaderMiddleName = v);
                PatchHelper.Patch(changed, nameof(MemberShipModel.MembershipLeaderLastName), data.MembershipLeaderLastName, entity.MembershipLeaderLastName, v => entity.MembershipLeaderLastName = v);
                PatchHelper.Patch(changed, nameof(MemberShipModel.MembershipLeaderGender), data.MembershipLeaderGender, entity.MembershipLeaderGender, v => entity.MembershipLeaderGender = v);
                PatchHelper.Patch(changed, nameof(MemberShipModel.RegistrationNumber), data.RegistrationNumber, entity.RegistrationNumber, v => entity.RegistrationNumber = v);

                if (data.MemberShipTypeId != 0 && data.MemberShipTypeId != entity.MemberShipTypeId)
                {
                    entity.MemberShipTypeId = data.MemberShipTypeId;
                    changed.Add(nameof(MemberShipModel.MemberShipTypeId));
                    PatchHelper.Patch(changed, nameof(MemberShipModel.MemberShipTypeId), data.MemberShipTypeId, entity.MemberShipTypeId, v => entity.MemberShipTypeId = v);

                }
            }

            await _context.SaveChangesAsync(); // assigns entity.Id for new memberships

            if (data.OrganizationId.HasValue && data.OrganizationId.Value > 0)
            {
                var orgId = data.OrganizationId.Value;
                var linked = await _context.OrganizationMemberships
                    .AnyAsync(x => x.MembershipId == entity.Id && x.OrganizationId == orgId);

                if (!linked)
                {
                    _context.OrganizationMemberships.Add(new OrganizationMembership
                    {
                        MembershipId = entity.Id,
                        OrganizationId = orgId,
                        AssignedByUserId = data.OnboardedById,
                        AssignedOn = DateTimeOffset.UtcNow,
                        IsActive = true
                    });
                    changed.Add(nameof(MemberShipModel.OrganizationId));
                }
            }

            // NIN/BVN are NOT copied to the user here any more: UpdateUserProfileAsync owns that (with its checks).

            // Existing helper (unchanged, not shown in your snippet)
            await AssignAgentToMembership(data, entity, changed.Count > 0);

            await _context.SaveChangesAsync();

            return new ProfileChange<MemberShipModel>(entity.Map(), changed, isNew);
        }

        // Backwards-compatible wrapper so existing create flows keep compiling.
        public async Task<(bool status, MemberShipModel data)> CreateUpdateMembership(MemberShipModel data)
        {
            var result = await UpsertMembershipAsync(data);
            return (true, result.Data);
        }
        //public async Task<(bool status, MemberShipModel data)> CreateUpdateMembership(MemberShipModel data)
        //{
        //    try
        //    {
        //        var existingCooperative = await _context.MemberShips
        //            .FirstOrDefaultAsync(x => x.UserId == data.UserId);

        //        bool isNew = existingCooperative == null;

        //        MemberShip entity;

        //        if (isNew)
        //        {
        //            entity = data.Map();
        //            await _context.MemberShips.AddAsync(entity);
        //        }
        //        else
        //        {
        //            entity = existingCooperative;

        //            if (data.IsLeaderAFarmer)
        //                entity.IsLeaderAFarmer = data.IsLeaderAFarmer;

        //            if (!string.IsNullOrWhiteSpace(data.Name))
        //                entity.Name = data.Name;

        //            if (!string.IsNullOrWhiteSpace(data.Location))
        //                entity.Location = data.Location;

        //            if (!string.IsNullOrWhiteSpace(data.State))
        //                entity.State = data.State;

        //            if (!string.IsNullOrWhiteSpace(data.Lga))
        //                entity.Lga = data.Lga;

        //            if (!string.IsNullOrWhiteSpace(data.ContactPerson))
        //                entity.ContactPerson = data.ContactPerson;

        //            if (!string.IsNullOrWhiteSpace(data.Ward))
        //                entity.Ward = data.Ward;

        //            if (!string.IsNullOrWhiteSpace(data.Email))
        //                entity.Email = data.Email;

        //            if (!string.IsNullOrWhiteSpace(data.PhoneNumber))
        //                entity.PhoneNumber = data.PhoneNumber;

        //            if (!string.IsNullOrWhiteSpace(data.MembershipLeaderFirstName))
        //                entity.MembershipLeaderFirstName = data.MembershipLeaderFirstName;

        //            if (!string.IsNullOrWhiteSpace(data.MembershipLeaderGender))
        //                entity.MembershipLeaderGender = data.MembershipLeaderGender;

        //            if (!string.IsNullOrWhiteSpace(data.MembershipLeaderMiddleName))
        //                entity.MembershipLeaderMiddleName = data.MembershipLeaderMiddleName;

        //            if (!string.IsNullOrWhiteSpace(data.MembershipLeaderLastName))
        //                entity.MembershipLeaderLastName = data.MembershipLeaderLastName;

        //            if (data.MemberShipTypeId != 0)
        //                entity.MemberShipTypeId = data.MemberShipTypeId;
        //        }

        //        await _context.SaveChangesAsync();

        //        var membershipId = entity.Id;

        //        if (data.OrganizationId.HasValue)
        //        {
        //            if (data.OrganizationId.Value > 0)
        //            {
        //                var exists = await _context.OrganizationMemberships
        //                    .AnyAsync(x =>
        //                        x.MembershipId == membershipId &&
        //                        x.OrganizationId == data.OrganizationId.Value);

        //                if (!exists)
        //                {
        //                    await _context.OrganizationMemberships.AddAsync(new OrganizationMembership
        //                    {
        //                        MembershipId = membershipId,
        //                        OrganizationId = data.OrganizationId.Value,
        //                        AssignedByUserId = data.OnboardedById,
        //                        AssignedOn = DateTimeOffset.UtcNow,
        //                        IsActive = true
        //                    });

        //                    await _context.SaveChangesAsync();
        //                }
        //            }

        //        }
        //        var isModified = false;
        //        if (!string.IsNullOrWhiteSpace(data.NIN) || !string.IsNullOrWhiteSpace(data.BVN))
        //        {

        //            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == entity.UserId);
        //            if (user != null)
        //            {
        //                if (!string.IsNullOrWhiteSpace(data.NIN) && data.NIN != user.NIN)
        //                {
        //                    user.NIN = data.NIN;
        //                    _context.Users.Update(user);
        //                    isModified = true;
        //                }

        //                if (!string.IsNullOrWhiteSpace(data.BVN) && data.BVN != user.BVN)
        //                {
        //                    user.BVN = data.BVN;
        //                    _context.Users.Update(user);
        //                    isModified = true;
        //                }
        //            }
        //        }


        //        isModified = await AssignAgentToMembership(data, entity, isModified);

        //        await _context.SaveChangesAsync();

        //        return (true, entity.Map());
        //    }
        //    catch (BaseException ex)
        //    {
        //        throw new BaseException(
        //            Core.Enums.ExceptionType.OPERATION_FAILED,
        //            ex.Message);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new NotSuccessfulException(
        //            $"Could not save membership details: {ex.Message}");
        //    }
        //}
        private async Task<bool> AssignAgentToMembership(MemberShipModel data, MemberShip existingCooperative, bool isModified)
        {
            if (data.AgentIds != null && data.AgentIds.Any())
            {
                var existingAgents = _context.MemberShipAgents
                    .Where(fa => fa.MemberShipId == existingCooperative.Id);

                _context.MemberShipAgents.RemoveRange(existingAgents);

                foreach (var agentId in data.AgentIds.Distinct())
                {
                    await _context.MemberShipAgents.AddAsync(new MemberShipAgent
                    {
                        MemberShipId = existingCooperative.Id,
                        AgentId = agentId,
                        AssignedOn = DateTimeOffset.Now,
                        IsActive = true,
                    });
                }

                isModified = true;
                existingCooperative.HasbeenAssignedAgent = true;
            }

            return isModified;
        }
        public async Task<(bool status, MemberShipModel data)> CreateCooperative(MemberShipModel data)
        {
            var existingCooperative = await _context.MemberShips
                 .FirstOrDefaultAsync(x => x.UserId == data.UserId);

            if (existingCooperative != null)
            {
                return (true, existingCooperative.Map());
            }

            var entity = data.Map();

            await _context.AddAsync(entity);
            await _context.SaveChangesAsync();

            return (true, entity.Map());
        }

        public async Task<MemberShipModel> GetMembershipDetailByUserId(string userId)
        {
            var cooperative = await _context.MemberShips.Where(x => x.UserId == userId).FirstOrDefaultAsync();
            if (cooperative == null)
            {
                return null;
            }
            return cooperative.Map();
        }
        public async Task<PagedListResponse<List<AgentMembershipDto>>>
            GetAgentsByMembership(
     long membershipId,
     ListQueryParams query)
        {
            query ??= new ListQueryParams();
            if (query.PageNumber <= 0) query.PageNumber = 1;
            if (query.PageSize <= 0) query.PageSize = 10;

            var baseQuery =
                from a in _context.Agents
                join ma in _context.MemberShipAgents on a.Id equals ma.AgentId
                where ma.MemberShipId == membershipId
                select new
                {
                    a.Id,
                    a.UserId,
                    a.Name,
                    a.PhoneNumber,
                    a.IsActive,
                    a.OnBoardedOn,
                    a.State,
                    a.Lga,
                    a.BelongToBusinessCode
                };

            if (!string.IsNullOrWhiteSpace(query.businessCode))
                baseQuery = baseQuery.Where(a => a.BelongToBusinessCode == query.businessCode);

            if (query.startDate.HasValue && query.endDate.HasValue)
            {
                baseQuery = baseQuery.Where(a =>
                    a.OnBoardedOn >= query.startDate &&
                    a.OnBoardedOn <= query.endDate);
            }

            if (!string.IsNullOrWhiteSpace(query.State))
                baseQuery = baseQuery.Where(a => a.State == query.State);

            if (query.Id > 0)
                baseQuery = baseQuery.Where(a => a.Id == query.Id);

            var totalCount = await baseQuery.CountAsync();

            if (totalCount == 0)
            {
                return new PagedListResponse<List<AgentMembershipDto>>
                {
                    Data = new List<AgentMembershipDto>(),
                    Count = 0,
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize
                };
            }

            // Page the agents first — no join yet
            var pagedAgents = await baseQuery
                .OrderByDescending(a => a.OnBoardedOn)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            var agentIds = pagedAgents.Select(a => a.Id).ToList();

            // One grouped query, keyed only by AgentId — cheap and simple to translate
            var farmerCounts = await _context.FarmerAgents
                .AsNoTracking()
                .Where(af => agentIds.Contains(af.AgentId))
                .GroupBy(af => af.AgentId)
                .Select(g => new { AgentId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.AgentId, x => x.Count);

            var result = pagedAgents.Select(a => new AgentMembershipDto
            {
                Id = a.Id,
                AgentUserId = a.UserId,
                AgentName = a.Name,
                PhoneNumber = a.PhoneNumber,
                TotalAssignedFarmers = farmerCounts.GetValueOrDefault(a.Id),
                Status = a.IsActive ? "Active" : "Inactive",
                DateAdded = a.OnBoardedOn,
                State = a.State,
                Lga = a.Lga
            }).ToList();

            return new PagedListResponse<List<AgentMembershipDto>>
            {
                Data = result,
                Count = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };
        }
        public async Task<AgentOverviewDto> GetAgentOverview(ListQueryParams args)
        {


            var agent = await _context.Agents
                .Where(a => a.Id == args.Id && a.BelongToBusinessCode == args.businessCode)
                .Select(a => new
                {
                    a.Id,
                    a.Name,
                    a.PhoneNumber,
                    a.Email,
                    a.UserId,
                    //   a.CommunityName,
                    a.State,
                    a.IsActive,
                    a.OnBoardedOn
                })
                .FirstOrDefaultAsync();

            if (agent == null)
                throw new BaseException(ExceptionType.NOTFOUND, "Agent not found");

            // Farmers under the agent
            var farmersQuery = _context.FarmerAgents
                .Where(x => x.AgentId == args.Id);

            var farmerIds = await farmersQuery
                .Select(x => x.FarmerId)
                .Distinct()
                .ToListAsync();

            var totalFarmers = farmerIds.Count;

            // Farms under those farmers
            var farms = await _context.Farms
                .Where(f => farmerIds.Contains(f.FarmerId))
                .Select(f => new AgentFarmDto
                {
                    FarmId = f.Id,
                    FarmName = f.Name,
                    FarmSize = f.FarmSize,
                    Location = f.FarmLocation
                })
                .ToListAsync();

            var totalFarmSize = farms.Sum(x => x.FarmSize);
            var totalFarms = farms.Count;
            // Memberships under agent
            var totalMemberships = await _context.MemberShipAgents
                .Where(x => x.AgentId == args.Id)
                .Select(x => x.MemberShipId)
                .Distinct()
                .CountAsync();

            return new AgentOverviewDto
            {
                AgentId = agent.Id,
                Status = agent.IsActive ? "Active" : "Inactive",
                DateRegistered = agent.OnBoardedOn,

                FullName = agent.Name,
                PhoneNumber = agent.PhoneNumber,
                Email = agent.Email,
                AgentUserId = agent.UserId,

                // CommunityName = agent.CommunityName,
                State = agent.State,

                TotalMemberships = totalMemberships,
                TotalFarmers = totalFarmers,
                TotalFarmSize = totalFarmSize,

                Farms = totalFarmers
            };
        }
        public async Task<MemberShipModel?> GetCooperativeDetailById(long id)
        {
            var cooperative = await _context.MemberShips
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (cooperative == null)
                return null;

            // Fetch wallet sequentially (safe)
            //var wallet = await _context.Wallets
            //    .AsNoTracking()
            //    .FirstOrDefaultAsync(w => w.UserId == cooperative.UserId);

            // Fetch farmers belonging to this cooperative via FarmerCooperatives
            var farmers = await (
                from f in _context.Farmers.AsNoTracking()
                join fc in _context.FarmerCooperatives.AsNoTracking()
                    on f.Id equals fc.FarmerId
                where fc.CooperativeId == cooperative.Id
                select f
            ).ToListAsync();

            // Map results
            var response = cooperative.Map();
            //response.Wallet = wallet?.Map();
            response.Farmers = farmers?.Select(f => f.Map()).ToList() ?? new List<FarmerModel>();

            return response;
        }


        public async Task<List<MemberShipModel>> GetCooperatives() //restricted to agrovesto
        {

            var cooperative = await _context.MemberShips.ToListAsync();
            if (cooperative.Count > 0)
            {
                return cooperative.Select(x => x.Map()).OrderByDescending(x => x.OnBoardedOn).ToList();
            }
            return null;
        }

        public async Task<List<MemberShipModel>> GetCooperativesByBusinessCode(string businessCode, ListQueryParams query)
        {
            var cooperatives = _context.MemberShips
                .Where(x => x.BelongToBusinessCode == businessCode)
                .AsQueryable();
            //if (!string.IsNullOrEmpty(query.organizationCode))
            //{
            //    cooperatives = cooperatives.Where(x => x.OrganizationCode == query.organizationCode);
            //}

            var result = await cooperatives
       .OrderByDescending(s => s.OnBoardedOn)
       .Skip(query.PageNumber)
       .Take(query.PageSize)
        .Select(s => s.Map()).ToListAsync();


            if (result.Count > 0)
            {
                return result;
            }
            return null;
        }
        public async Task<PagedListResponse<List<MemberShipResponseModel>>> GetCooperativeByAgent(string agentId, ListQueryParams query)
        {
            query ??= new ListQueryParams();
            if (query.PageNumber <= 0) query.PageNumber = 1;
            if (query.PageSize <= 0) query.PageSize = 10;

            var cooperativesQuery = from f in _context.MemberShips
                                    join fa in _context.MemberShipAgents on f.Id equals fa.MemberShipId
                                    join a in _context.Agents on fa.AgentId equals a.Id
                                    where a.UserId == agentId
                                    select f;

            if (!string.IsNullOrWhiteSpace(query.organizationCode))
            {
                cooperativesQuery = from f in cooperativesQuery
                                    join om in _context.OrganizationMemberships on f.Id equals om.MembershipId
                                    join o in _context.Organizations on om.OrganizationId equals o.OrganizationId
                                    where o.OrganizationCode == query.organizationCode
                                    select f;
            }


            if (!string.IsNullOrWhiteSpace(query.SearchParam))
            {
                var pattern = $"%{query.SearchParam.Trim()}%";
                cooperativesQuery = cooperativesQuery.Where(f =>
                    EF.Functions.Like(f.Name, pattern) ||
                    EF.Functions.Like(f.Location, pattern) ||
                    EF.Functions.Like(f.State, pattern) ||
                    EF.Functions.Like(f.Lga, pattern) ||
                    EF.Functions.Like(f.Ward, pattern) ||
                    EF.Functions.Like(f.ContactPerson, pattern) ||
                    EF.Functions.Like(f.PhoneNumber, pattern) ||
                    EF.Functions.Like(f.MembershipLeaderFirstName, pattern) ||
                    EF.Functions.Like(f.MembershipLeaderLastName, pattern));
            }

            cooperativesQuery = cooperativesQuery.Distinct();

            var totalCount = await cooperativesQuery.CountAsync();
            if (totalCount == 0)
            {
                return new PagedListResponse<List<MemberShipResponseModel>>
                {
                    Data = new List<MemberShipResponseModel>(),
                    Count = 0,
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize
                };
            }

            var skip = (query.PageNumber - 1) * query.PageSize;

            var pageIds = await cooperativesQuery
                .OrderByDescending(s => s.OnBoardedOn)
                .Skip(skip)
                .Take(query.PageSize)
                .Select(s => s.Id)
                .ToListAsync();

            var farmerCounts = await _context.FarmerCooperatives
                .Where(fc => pageIds.Contains(fc.CooperativeId))
                .GroupBy(fc => fc.CooperativeId)
                .Select(g => new { CooperativeId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CooperativeId, x => x.Count);

            var cooperatives = await _context.MemberShips
                .Where(s => pageIds.Contains(s.Id))
                .OrderByDescending(s => s.OnBoardedOn)
                .Select(s => new MemberShipResponseModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Location = s.Location,
                    State = s.State,
                    Lga = s.Lga,
                    OnBoardedOn = s.OnBoardedOn,
                    BelongToBusinessCode = s.BelongToBusinessCode,
                    ContactPerson = s.ContactPerson,
                    Email = s.Email,
                    PhoneNumber = s.PhoneNumber,
                    UserId = s.UserId,
                    IsLeaderAFarmer = s.IsLeaderAFarmer,
                    Tier = EnumExtensions.GetDisplayName(s.TierLevel),
                    MembershipType = s.MemberShipType.Name,
                    MembershipLeaderFirstName = s.MembershipLeaderLastName,
                    MembershipLeaderGender = s.MembershipLeaderGender,
                    MembershipLeaderLastName = s.MembershipLeaderLastName,
                    MembershipLeaderMiddleName = s.MembershipLeaderMiddleName,
                    Ward = s.Ward,
                    MemberShipTypeId = s.MemberShipTypeId
                })
                .ToListAsync();

            foreach (var c in cooperatives)
                c.TotalFarmers = farmerCounts.GetValueOrDefault(c.Id);

            return new PagedListResponse<List<MemberShipResponseModel>>
            {
                Data = cooperatives,
                Count = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };
        }


        public async Task<AgentModel> AssignSupervisorToAgent(string agentId, string supervisorId)
        {
            var agent = await _context.Agents.Where(c => c.UserId == agentId).FirstOrDefaultAsync();
            if (agent == null) return null;
            agent.SupervisorUserId = supervisorId;

            await _context.SaveChangesAsync();

            return agent.Map();
        }

        public async Task<List<FarmerModel>> AssignFarmerToAgent(string agentUserId, long cooperativeId)
        {
            var agent = await _context.Agents.Where(c => c.UserId == agentUserId).FirstOrDefaultAsync();

            var farmers = await (
                from f in _context.Farmers.AsNoTracking()
                join fc in _context.FarmerCooperatives.AsNoTracking()
                    on f.Id equals fc.FarmerId
                where fc.CooperativeId == cooperativeId
                select f
            ).ToListAsync();

            if (!farmers.Any())
                return new List<FarmerModel>();

            foreach (var farmer in farmers)
            {

                var alreadyAssigned = await _context.FarmerAgents
                    .AnyAsync(fa => fa.FarmerId == farmer.Id && fa.AgentId == agent.Id);

                if (!alreadyAssigned)
                {

                    await _context.FarmerAgents.AddAsync(new FarmerAgent
                    {
                        FarmerId = farmer.Id,
                        AgentId = agent.Id,
                        AssignedOn = DateTimeOffset.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();


            return farmers.Select(f => f.Map()).ToList();
        }
        public async Task<List<MemberShipModel>> AssignMemberShipToAgent(string agentUserId, List<long> membershipIds)
        {
            var agent = await _context.Agents
                .FirstOrDefaultAsync(a => a.UserId == agentUserId);

            if (agent == null)
                throw new BaseException(ExceptionType.NOTFOUND, "Agent not found");

            var existingAssignments = await _context.MemberShipAgents
                .Where(ma => ma.AgentId == agent.Id && membershipIds.Contains(ma.MemberShipId))
                .Select(ma => ma.MemberShipId)
                .ToListAsync();

            var newAssignments = membershipIds
                .Where(id => !existingAssignments.Contains(id))
                .Select(id => new MemberShipAgent
                {
                    MemberShipId = id,
                    AgentId = agent.Id,
                    AssignedOn = DateTimeOffset.UtcNow
                }).ToList();

            if (newAssignments.Any())
            {
                await _context.MemberShipAgents.AddRangeAsync(newAssignments);
                await _context.SaveChangesAsync();
            }

            var memberships = await _context.MemberShips
                .Where(m => membershipIds.Contains(m.Id))
                .ToListAsync();

            return memberships.Select(m => m.Map()).ToList();
        }
        public async Task<AgentModel> ViewAgentAssignedToCooperative(long cooperativeId)
        {
            var agent = await (
        from ms in _context.MemberShips
        join msa in _context.MemberShipAgents
            on ms.Id equals msa.MemberShipId
        join a in _context.Agents
            on msa.AgentId equals a.Id
        where ms.Id == cooperativeId
        select a
    ).AsNoTracking()
     .FirstOrDefaultAsync();

            return agent?.Map();

        }

        public async Task<List<AgentModel>> AgentsBySupervisor(string supervisorId, ListQueryParams query)
        {
            var agentsQuery = _context.Agents
                .AsNoTracking()
                .Where(s => s.SupervisorUserId == supervisorId);

            //if (!string.IsNullOrWhiteSpace(query.organizationCode))
            //{
            //    agentsQuery = agentsQuery.Where(s => s.OrganizationCode == query.organizationCode);
            //}

            var agents = await agentsQuery
                .OrderByDescending(s => s.OnBoardedOn)
                //.Skip((query.PageNumber - 1) * query.PageSize)
                //.Take(query.PageSize)
                .Select(s => new AgentModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Location = s.Location,
                    State = s.State,
                    Lga = s.Lga,
                    OnBoardedOn = s.OnBoardedOn,
                    BelongToBusinessCode = s.BelongToBusinessCode,
                    Email = s.Email,
                    PhoneNumber = s.PhoneNumber,
                    // OrganizationCode = s.OrganizationCode,
                    Ward = s.Ward,
                    SupervisorUserId = s.SupervisorUserId,
                    UserId = s.UserId
                })
                .ToListAsync();

            return agents.Any() ? agents : null;
        }


    }
}

