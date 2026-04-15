using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Payment;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Utilities;
using Amazon.Runtime.Internal.Util;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using PhoneNumbers;
using System;
using System.Collections.Generic;
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
        public MembershipRepository(DataContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
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


        public async Task<MembershipDashboardDto> GetMembershipDashboardAsync(string membershipuserId)
        {
            var membership = await _context.MemberShips
                .AsNoTracking()
                .Where(m => m.UserId == membershipuserId)
                .Select(m => new MembershipInfoDto
                {
                    UserId = m.UserId,
                    MembershipId = m.Id,
                    Name = m.Name,
                    MembershipType = m.MemberShipType.Name,
                    Status = m.IsActive ? "Active" : "Inactive",
                    DateCreated = m.OnBoardedOn,
                    Location = m.Location,
                    LeaderName = m.MembershipLeaderFirstName + " " + m.MembershipLeaderLastName
                })
                .FirstOrDefaultAsync();

            if (membership == null)
                throw new BaseException(ExceptionType.NOTFOUND, "Membership not found");

            var membershipId = membership.MembershipId;

            var farmStats = await
  (
      from fc in _context.FarmerCooperatives.AsNoTracking()
      where fc.CooperativeId == membershipId

      join f in _context.Farmers.AsNoTracking()
          on fc.FarmerId equals f.Id

      join fr in _context.Farms.AsNoTracking()
          on f.Id equals fr.FarmerId into farmJoin
      from fr in farmJoin.DefaultIfEmpty()

      group new { f, fr } by 1 into g
      select new
      {
          TotalFarmers = g.Select(x => x.f.Id).Distinct().Count(),

          TotalVerifiedFarmers = g
              .Select(x => new { x.f.Id, x.f.Level })
              .Distinct()
              .Count(x => x.Level != FarmerLevel.Level1),

          TotalUnverifiedFarmers = g
              .Select(x => new { x.f.Id, x.f.Level })
              .Distinct()
              .Count(x => x.Level == FarmerLevel.Level1),

          TotalFarms = g.Count(x => x.fr != null),

          TotalFarmSize = g.Sum(x => x.fr != null
              ? (double?)x.fr.FarmSizeInSqMetre
              : 0) ?? 0,

          Completed = g.Count(x => x.fr != null && x.fr.MappingStatus == MappingStatus.Verified),

          InProgress = g.Count(x => x.fr != null && x.fr.MappingStatus == MappingStatus.Mapped),

          NotStarted = g.Count(x => x.fr != null && x.fr.MappingStatus == MappingStatus.UnMapped)
      }
  )
  .FirstOrDefaultAsync();

            farmStats ??= new
            {
                TotalFarmers = 0,
                TotalVerifiedFarmers = 0,
                TotalUnverifiedFarmers = 0,
                TotalFarms = 0,
                TotalFarmSize = 0.0,
                Completed = 0,
                InProgress = 0,
                NotStarted = 0
            };
            var totalAgents = await _context.MemberShipAgents
                .AsNoTracking()
                .Where(x => x.MemberShipId == membershipId)
                .Select(x => x.AgentId)
                .Distinct()
                .CountAsync();

            var farmerLinkChart = await _context.FarmerCooperatives
                .AsNoTracking()
                .Where(fc => fc.CooperativeId == membershipId)
                .GroupBy(fc => new
                {
                    fc.JoinedOn.Year,
                    fc.JoinedOn.Month
                })
                .Select(g => new MonthlyFarmerLinkDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    FarmerCount = g.Count()
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            var recentActivities = await _context.ActivityLogs
                .AsNoTracking()
                .Where(a => a.PerformedByUserId == membership.UserId)
                .OrderByDescending(a => a.CreatedOn)
                .Take(5)
                .Select(a => new ActivityDto
                {
                    Description = a.ActivityName,
                    PerformedBy = a.PerformedByUserId,
                    Date = a.CreatedOn
                })
                .ToListAsync();

         
            // -----------------------------
            var inputDisbursementChart = await
            (
                from fc in _context.FarmerCooperatives.AsNoTracking()
                where fc.CooperativeId == membershipId

                join ia in _context.InputAllocations.AsNoTracking()
                    on fc.FarmerId equals ia.FarmerId

                group ia by new
                {
                    ia.AllocatedDate.Year,
                    ia.AllocatedDate.Month
                }
                into g

                select new MonthlyInputDisbursementDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    InputCount = g.Count()
                }
            )
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync();

            return new MembershipDashboardDto
            {
                Membership = membership,

                Summary = new MemberSummaryDto
                {
                    TotalAgents = totalAgents,
                    TotalFarmers = farmStats.TotalFarmers,
                    TotalVerifiedFarmers = farmStats.TotalVerifiedFarmers,
                    TotalUnverifiedFarmers = farmStats.TotalUnverifiedFarmers,
                    TotalFarms = farmStats.TotalFarms,
                    TotalFarmSize = farmStats.TotalFarmSize,
                    FarmsMapped = farmStats.Completed,
                    MappingProgressPercentage = farmStats.TotalFarms == 0
        ? 0
        : (int)Math.Round((double)farmStats.Completed / farmStats.TotalFarms * 100)
                },

                MappingProgress = new FarmMappingProgressDto
                {
                    TotalFarms = farmStats.TotalFarms,
                    Completed = farmStats.Completed,
                    InProgress = farmStats.InProgress,
                    NotStarted = farmStats.NotStarted
                },

                FarmerLinkHistogram = farmerLinkChart,

                FarmerInputDisbursedLinkHistogram = inputDisbursementChart,

                RecentActivities = recentActivities,

                CanAddFarmer = membership.Status == "Active"
            };
        }

        /// <summary>
        /// TODO: REVERT TO THIS IF FAILED
        /// </summary>
        /// <param name="agentUserId"></param>
        /// <returns></returns>

        //public async Task<MembershipDashboardDto> GetMembershipDashboardAsync(string membershipuserId)
        //{

        //    var membership = await _context.MemberShips
        //        .Where(m => m.UserId == membershipuserId)
        //        .Select(m => new MembershipInfoDto
        //        {
        //            UserId=m.UserId,
        //            MembershipId = m.Id,
        //            Name = m.Name,
        //            MembershipType = m.MemberShipType.Name,
        //            Status = m.IsActive ? "Active" : "Inactive",
        //            DateCreated = m.OnBoardedOn,
        //            Location = m.Location
        //        })
        //        .FirstOrDefaultAsync();

        //    if (membership == null)
        //        throw new BaseException(ExceptionType.NOTFOUND, "Membership not found");

        //    // 2️⃣ Farmer IDs Linked to Membership
        //    var farmerQuery = _context.FarmerCooperatives
        //        .Where(fc => fc.CooperativeId == membership.MembershipId)
        //        .Select(fc => fc.FarmerId)
        //        .Distinct();

        //    var totalFarmers = await farmerQuery.CountAsync();

        //    // 3️⃣ Agent Overview
        //    var membershipQuery = _context.MemberShipAgents
        //       .Where(fc => fc.MemberShipId == membership.MembershipId)
        //       .Select(fc => fc.AgentId)
        //       .Distinct();

        //    var totalAgents = await membershipQuery.CountAsync();

        //    // 4️⃣ Farm Overview
        //    var farmQuery = _context.Farms
        //        .Where(f => farmerQuery.Contains(f.FarmerId));

        //    var totalFarms = await farmQuery.CountAsync();

        //    var farmSizeTotal = await farmQuery
        //        .SumAsync(f => (double?)f.FarmSizeInSqMetre) ?? 0;

        //    // 5️⃣ Mapping Progress
        //    var mappingStats = await farmQuery
        //        .GroupBy(f => f.MappingStatus)
        //        .Select(g => new
        //        {
        //            Status = g.Key,
        //            Count = g.Count()
        //        })
        //        .ToListAsync();

        //    var completed = mappingStats
        //        .FirstOrDefault(x => x.Status == MappingStatus.Verified)?.Count ?? 0;

        //    var inProgress = mappingStats
        //        .FirstOrDefault(x => x.Status == MappingStatus.Mapped)?.Count ?? 0;

        //    var notStarted = mappingStats
        //        .FirstOrDefault(x => x.Status == MappingStatus.UnMapped)?.Count ?? 0;

        //    var mappingPercentage = totalFarms == 0
        //        ? 0
        //        : (int)Math.Round((double)completed / totalFarms * 100);

        //    var farmerLinkChart = await _context.FarmerCooperatives
        //        .Where(fc => fc.CooperativeId == membership.MembershipId)
        //        .GroupBy(fc => new
        //        {
        //            fc.JoinedOn.Year,
        //            fc.JoinedOn.Month
        //        })
        //        .Select(g => new MonthlyFarmerLinkDto
        //        {
        //            Year = g.Key.Year,
        //            Month = g.Key.Month,
        //            FarmerCount = g.Count()
        //        })
        //        .OrderBy(x => x.Year)
        //        .ThenBy(x => x.Month)
        //        .ToListAsync();

        //    var recentActivities = await _context.ActivityLogs
        //        .Where(a => a.PerformedByUserId == membership.UserId)
        //        .OrderByDescending(a => a.CreatedOn)
        //        .Take(5)
        //        .Select(a => new ActivityDto
        //        {
        //            Description = a.ActivityName,
        //            PerformedBy = a.PerformedByUserId,
        //            Date = a.CreatedOn
        //        })
        //        .ToListAsync();

        //    var result= new MembershipDashboardDto
        //    {
        //        Membership = membership,

        //        Summary = new MemberSummaryDto
        //        {
        //            TotalAgents = totalAgents,
        //            TotalFarmers = totalFarmers,
        //            TotalFarms = totalFarms,
        //            TotalFarmSize = farmSizeTotal,
        //            FarmsMapped = completed,
        //            MappingProgressPercentage = mappingPercentage
        //        },

        //        MappingProgress = new FarmMappingProgressDto
        //        {
        //            TotalFarms = totalFarms,
        //            Completed = completed,
        //            InProgress = inProgress,
        //            NotStarted = notStarted
        //        },

        //        FarmerLinkHistogram = farmerLinkChart,

        //        RecentActivities = recentActivities,

        //        CanAddFarmer = membership.Status == "Active"
        //    };


        //    return result;
        //}


        /** public async Task<MembershipDashboardDto> GetMembershipDashboardAsync(long membershipId)
         {
             //  Membership basic info
             var membership = await _context.MemberShips
                 .Where(c => c.Id == membershipId)
                 .Select(c => new MembershipInfoDto
                 {
                     MembershipId = c.Id,
                     Name = c.Name,
                     MembershipType = c.MemberShipType.Name,
                     Status = "Active",//c. ? "Active" : "Inactive",
                     DateCreated = c.OnBoardedOn,
                     Location = c.State
                 })
                 .FirstOrDefaultAsync();

             if (membership == null)
                 throw new BaseException(ExceptionType.NOTFOUND, "Membership not found");

             // Farmers under this membership
             var farmerIds = await _context.FarmerCooperatives
                 .Where(fc => fc.CooperativeId == membershipId)
                 .Select(fc => fc.FarmerId)
                 .Distinct()
                 .ToListAsync();

             var totalFarmers = farmerIds.Count;

             // 3️⃣ Farms under those farmers
             var farmPlots = await _context.Farms
                 .Where(p => farmerIds.Contains(p.FarmerId))
                 .Select(p => new { p.Id, p.MappingStatus })
                 .ToListAsync();

             var totalFarms = farmPlots.Count;

             var completed = farmPlots.Count(p => p.MappingStatus == MappingStatus.Completed);
             var inProgress = farmPlots.Count(p => p.MappingStatus == MappingStatus.InProgress);
             var notStarted = farmPlots.Count(p => p.MappingStatus == MappingStatus.NotStarted);

             var farmsMapped = completed;

             var mappingPercentage = totalFarms == 0
                 ? 0
                 : (int)Math.Round((double)farmsMapped / totalFarms * 100);

             // 4️⃣ Build DTO
             return new MembershipDashboardDto
             {
                 Membership = membership,

                 Summary = new FarmerSummaryDto
                 {
                     TotalFarmers = totalFarmers,
                     TotalFarms = totalFarms,
                     FarmsMapped = farmsMapped,
                     MappingProgressPercentage = mappingPercentage
                 },

                 MappingProgress = new FarmMappingProgressDto
                 {
                     TotalFarms = totalFarms,
                     Completed = completed,
                     InProgress = inProgress,
                     NotStarted = notStarted
                 }
             };
         }
       **/
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
                entity.CreatedOn= DateTimeOffset.Now;
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

        //public async Task<(bool status, MemberShipModel data)> CreateUpdateMembership(MemberShipModel data)
        //{
        //    try
        //    {
        //        var existingCooperative = await _context.MemberShips
        //            .FirstOrDefaultAsync(x => x.UserId == data.UserId);

        //        var isModified = false;
        //        if (existingCooperative != null)
        //        {
        //            if (data.IsLeaderAFarmer)
        //            {
        //                existingCooperative.IsLeaderAFarmer = data.IsLeaderAFarmer;
        //            }
        //            if (!string.IsNullOrWhiteSpace(data.Name))
        //                existingCooperative.Name = data.Name;

        //            if (!string.IsNullOrWhiteSpace(data.Location))
        //                existingCooperative.Location = data.Location;

        //            if (!string.IsNullOrWhiteSpace(data.State))
        //                existingCooperative.State = data.State;

        //            if (!string.IsNullOrWhiteSpace(data.Lga))
        //                existingCooperative.Lga = data.Lga;

        //            if (!string.IsNullOrWhiteSpace(data.ContactPerson))
        //                existingCooperative.ContactPerson = data.ContactPerson;


        //            if (!string.IsNullOrWhiteSpace(data.Ward))
        //                existingCooperative.Ward = data.Ward;

        //            if (!string.IsNullOrWhiteSpace(data.Email))
        //                existingCooperative.Email = data.Email;

        //            if (!string.IsNullOrWhiteSpace(data.PhoneNumber))
        //                existingCooperative.PhoneNumber = data.PhoneNumber;

        //            if (!string.IsNullOrWhiteSpace(data.MembershipLeaderFirstName))
        //                existingCooperative.MembershipLeaderFirstName = data.MembershipLeaderFirstName;

        //            if (!string.IsNullOrWhiteSpace(data.MembershipLeaderGender))
        //                existingCooperative.MembershipLeaderGender = data.MembershipLeaderGender;

        //            if (!string.IsNullOrWhiteSpace(data.MembershipLeaderMiddleName))
        //                existingCooperative.MembershipLeaderMiddleName = data.MembershipLeaderMiddleName;

        //            if (!string.IsNullOrWhiteSpace(data.MembershipLeaderLastName))
        //                existingCooperative.MembershipLeaderLastName = data.MembershipLeaderLastName;


        //            if (data.MemberShipTypeId != 0)
        //            {
        //                existingCooperative.MemberShipTypeId = data.MemberShipTypeId;
        //            }

        //            if (data.OrganizationId.HasValue)
        //            {
        //                bool exists = await _context.OrganizationMemberships.AnyAsync(x =>
        //                    x.MembershipId == existingCooperative.Id &&
        //                    x.OrganizationId == data.OrganizationId.Value);

        //                if (!exists)
        //                {
        //                    await _context.OrganizationMemberships.AddAsync(new OrganizationMembership
        //                    {
        //                        MembershipId = existingCooperative.Id,
        //                        OrganizationId = data.OrganizationId.Value,
        //                        AssignedByUserId = data.OnboardedById,
        //                        AssignedOn = DateTimeOffset.Now,
        //                        IsActive=true,
        //                    });
        //                }
        //            }


        //            //if (!string.IsNullOrWhiteSpace(data.IsAssignedToAgentId))
        //            //    existingCooperative.IsAssignedToAgentId = data.IsAssignedToAgentId;

        //            isModified = await AssignAgentToMembership(data, existingCooperative, isModified);
        //            _context.MemberShips.Update(existingCooperative);
        //            await _context.SaveChangesAsync();

        //            return (true, existingCooperative.Map());
        //        }

        //        var entity = data.Map();

        //        await _context.MemberShips.AddAsync(entity);
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
        //            $"Could not save agent details: {ex.Message}");
        //    }
        //}
        public async Task<(bool status, MemberShipModel data)> CreateUpdateMembership(MemberShipModel data)
        {
            try
            {
                var existingCooperative = await _context.MemberShips
                    .FirstOrDefaultAsync(x => x.UserId == data.UserId);

                bool isNew = existingCooperative == null;

                MemberShip entity;

                if (isNew)
                {
                    entity = data.Map();
                    await _context.MemberShips.AddAsync(entity);
                }
                else
                {
                    entity = existingCooperative;

                    if (data.IsLeaderAFarmer)
                        entity.IsLeaderAFarmer = data.IsLeaderAFarmer;

                    if (!string.IsNullOrWhiteSpace(data.Name))
                        entity.Name = data.Name;

                    if (!string.IsNullOrWhiteSpace(data.Location))
                        entity.Location = data.Location;

                    if (!string.IsNullOrWhiteSpace(data.State))
                        entity.State = data.State;

                    if (!string.IsNullOrWhiteSpace(data.Lga))
                        entity.Lga = data.Lga;

                    if (!string.IsNullOrWhiteSpace(data.ContactPerson))
                        entity.ContactPerson = data.ContactPerson;

                    if (!string.IsNullOrWhiteSpace(data.Ward))
                        entity.Ward = data.Ward;

                    if (!string.IsNullOrWhiteSpace(data.Email))
                        entity.Email = data.Email;

                    if (!string.IsNullOrWhiteSpace(data.PhoneNumber))
                        entity.PhoneNumber = data.PhoneNumber;

                    if (!string.IsNullOrWhiteSpace(data.MembershipLeaderFirstName))
                        entity.MembershipLeaderFirstName = data.MembershipLeaderFirstName;

                    if (!string.IsNullOrWhiteSpace(data.MembershipLeaderGender))
                        entity.MembershipLeaderGender = data.MembershipLeaderGender;

                    if (!string.IsNullOrWhiteSpace(data.MembershipLeaderMiddleName))
                        entity.MembershipLeaderMiddleName = data.MembershipLeaderMiddleName;

                    if (!string.IsNullOrWhiteSpace(data.MembershipLeaderLastName))
                        entity.MembershipLeaderLastName = data.MembershipLeaderLastName;

                    if (data.MemberShipTypeId != 0)
                        entity.MemberShipTypeId = data.MemberShipTypeId;
                }

                await _context.SaveChangesAsync();

                var membershipId = entity.Id;

                if (data.OrganizationId.HasValue)
                {
                    var exists = await _context.OrganizationMemberships
                        .AnyAsync(x =>
                            x.MembershipId == membershipId &&
                            x.OrganizationId == data.OrganizationId.Value);

                    if (!exists)
                    {
                        await _context.OrganizationMemberships.AddAsync(new OrganizationMembership
                        {
                            MembershipId = membershipId,
                            OrganizationId = data.OrganizationId.Value,
                            AssignedByUserId = data.OnboardedById,
                            AssignedOn = DateTimeOffset.UtcNow,
                            IsActive = true
                        });

                        await _context.SaveChangesAsync();
                    }
                }

                var isModified = false;
                isModified = await AssignAgentToMembership(data, entity, isModified);

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
                    $"Could not save membership details: {ex.Message}");
            }
        }
        private async Task<bool> AssignAgentToMembership(MemberShipModel data, MemberShip existingCooperative, bool isModified)
        {
            if (data.AgentIds.Count > 0)
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
                        IsActive=true,

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

            var entity =data.Map();

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
        public async Task<PagedListResponse<List<AgentMembershipDto>>> GetAgentsByMembership(
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

            var pagedQuery = baseQuery
                .OrderByDescending(a => a.OnBoardedOn)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize);

            var result = await (
                from a in pagedQuery
                join af in _context.FarmerAgents
                    on a.Id equals af.AgentId into farmerGroup
                from fg in farmerGroup.DefaultIfEmpty()
                group fg by new
                {
                    a.Id,
                    a.UserId,
                    a.Name,
                    a.PhoneNumber,
                    a.IsActive,
                    a.OnBoardedOn,
                    a.State,
                    a.Lga
                }
                into g
                select new AgentMembershipDto
                {
                    Id = g.Key.Id,
                    AgentUserId = g.Key.UserId,
                    AgentName = g.Key.Name,
                    PhoneNumber = g.Key.PhoneNumber,
                    TotalAssignedFarmers = g.Count(x => x != null),
                    Status = g.Key.IsActive ? "Active" : "Inactive",
                    DateAdded = g.Key.OnBoardedOn,
                    State = g.Key.State,
                    Lga = g.Key.Lga
                }
            ).ToListAsync();

            //// Add Serial Numbers
            //int serialStart = (query.PageNumber - 1) * query.PageSize;

            ////for (int i = 0; i < result.Count; i++)
            ////    result[i].SN = serialStart + i + 1;

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
        public async Task<List<MemberShipResponseModel>> GetCooperativeByAgent(string agentId, ListQueryParams query)
        {
            // Base query: cooperatives assigned to this agent via join table
            var cooperativesQuery = from f in _context.MemberShips
                            join fa in _context.MemberShipAgents on f.Id equals fa.MemberShipId
                            join a in _context.Agents on fa.AgentId equals a.Id
                            where a.UserId == agentId
                            select f;
            //var cooperativesQuery = _context.MemberShips
            // .AsNoTracking()
            // .Where(s =>
            //// s.IsAssignedToAgentId == agentId || 
            // s.OnboardedById == agentId);


            // Filter by organization code
            //if (!string.IsNullOrWhiteSpace(query.organizationCode))
            //{
            //    cooperativesQuery = cooperativesQuery.Where(s => s.OrganizationCode == query.organizationCode);
            //}

            var cooperatives = await cooperativesQuery
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
                    Tier=EnumExtensions.GetDisplayName(s.Tier),
                    MembershipType= s.MemberShipType.Name,
                    MembershipLeaderFirstName=s.MembershipLeaderLastName,
                    MembershipLeaderGender=s.MembershipLeaderGender ,
                    MembershipLeaderLastName=s.MembershipLeaderLastName,
                    MembershipLeaderMiddleName=s.MembershipLeaderMiddleName,
                    Ward=s.Ward,
                    MemberShipTypeId=s.MemberShipTypeId,
                    // Count farmers via FarmerCooperatives
                    TotalFarmers = _context.FarmerCooperatives.Count(fc => fc.CooperativeId == s.Id)
                })
                .ToListAsync();

            return cooperatives.Any() ? cooperatives : new List<MemberShipResponseModel>();
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
            var agent= await _context.Agents.Where(c => c.UserId == agentUserId).FirstOrDefaultAsync();
            // Get all farmers in this cooperative via FarmerCooperatives
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
                // Check if the assignment already exists
                var alreadyAssigned = await _context.FarmerAgents
                    .AnyAsync(fa => fa.FarmerId == farmer.Id && fa.AgentId == agent.Id);

                if (!alreadyAssigned)
                {
                    // Assign agent
                    await _context.FarmerAgents.AddAsync(new FarmerAgent
                    {
                        FarmerId = farmer.Id,
                        AgentId = agent.Id,
                        AssignedOn = DateTimeOffset.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();

            // Return mapped farmers
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

