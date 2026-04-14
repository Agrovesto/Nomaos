using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Payment;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.Utilities;
using Amazon.Runtime;
using Amazon.Runtime.Internal.Util;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories.V2
{
    public interface IOrganizationRepository
    {
        Task<OrganizationLocationModel> CreateLocationAsync(OrganizationLocationModel model);
        Task<(bool status, FarmerModel data)> CreateOrUpdateFarmer(FarmerModel data);
        Task<OrganizationRoleModel> CreateRoleAsync(OrganizationRoleModel model);
        Task<PagedListResponse<List<OrganizationAgentActivityLog>>> GetAgentActivityLogs(ListQueryParams args);
        Task<List<OrganizationAgentLocationResponseModel>> GetAgentAssignedLocations(ListQueryParams query);
        Task<PagedListResponse<List<OrganizationAgentInputAllocationModel>>> GetAgentInputAllocations(ListQueryParams args);
        Task<PagedListResponse<List<LocationAgentDetailResponse>>> GetAgentsByLocationAsync(ListQueryParams args);
        Task<PagedListResponse<List<OrganizationAgentSubscriberModel>>> GetAgentSubscribers(ListQueryParams args);
        Task<PagedListResponse<List<OrganizationAgentTransactionHistory>>> GetAgentTransactionHistory(ListQueryParams args);
        Task<List<OrganizationMemberShipResponseModel>> GetCooperativesByOrganization(string organizationCode, ListQueryParams query);
        Task<OrganizationFarmerDetailResponse> GetFarmerDetailsAsync(ListQueryParams query);
        Task<PagedListResponse<List<LocationActivityLogResponse>>> GetLocationActivityLogsAsync(ListQueryParams args);
        Task<List<OrganizationLocationResponseModel>> GetLocationAsync(ListQueryParams query);
        Task<OrganizationLocationModel> GetLocationByIAsync(long Id);
        Task<PagedListResponse<List<LocationInventoryBreakdownResponse>>> GetLocationInventoryBreakdownAsync(ListQueryParams args);
        Task<OrganizationAgentDetailResponse> GetOrganizationAgentDetail(ListQueryParams args);
        Task<OrganizationAgentPerformanceSummary> GetOrganizationAgentPerformanceSummary(ListQueryParams args);
        Task<PagedListResponse<List<OrganizationAgentResponse>>> GetOrganizationAgents(ListQueryParams args);
        Task<OrganizationDashboardResponse> GetOrganizationDashboardAsync(ListQueryParams args);
        Task<OrganizationFarmerDashboardResponse> GetOrganizationFarmerDashboardAsync(ListQueryParams args);
        Task<OrganizationFarmerSalesSummary> GetOrganizationFarmerSalesSummary(ListQueryParams args);
        Task<PagedListResponse<List<OrganizationFarmerResponse>>> GetOrganizationFarmersAsync(ListQueryParams args);
        Task<OrganizationMembershipDashboardResponse> GetOrganizationMembershipDashboard(ListQueryParams query);
        Task<OrganizationRoleModel> GetRoleByIAsync(long Id);
        Task<List<OrganizationRoleModel>> GetRolesAsync(ListQueryParams query);
        Task<bool> ReassignAgentToLocation(OrganizationAgentArgs data);
        Task<OrganizationLocationModel> UpdateLocationAsync(long locationId, OrganizationLocationModel model);
    }
    public class OrganizationRepository : IOrganizationRepository
    {
        private readonly DataContext _context;

        private readonly IDistributedCache _cache;
        public OrganizationRepository(DataContext context, IDistributedCache cache)
        {
            _context = context;

            _cache = cache;
        }
        #region agentviews
        //Rework this view to wire real data
        public async Task<PagedListResponse<List<OrganizationAgentActivityLog>>>
    GetAgentActivityLogs(
       ListQueryParams args)
        {
            await Task.Delay(50); // simulate async

            var now = DateTimeOffset.Now;

            var data = new List<OrganizationAgentActivityLog>
    {
        new OrganizationAgentActivityLog
        {
            Id = 1,
            ActivityType = "Login",
            ActivityName = "Agent logged into system",
            TimeStamp = now.AddHours(-1)
        },
        new OrganizationAgentActivityLog
        {
            Id = 2,
            ActivityType = "Assignment",
            ActivityName = "Assigned to Location 8",
            TimeStamp = now.AddHours(-5)
        },
        new OrganizationAgentActivityLog
        {
            Id = 3,
            ActivityType = "Distribution",
            ActivityName = "Distributed fertilizer to farmer",
            TimeStamp = now.AddDays(-1)
        },
        new OrganizationAgentActivityLog
        {
            Id = 4,
            ActivityType = "Update",
            ActivityName = "Updated farmer subscription",
            TimeStamp = now.AddDays(-2)
        },
        new OrganizationAgentActivityLog
        {
            Id = 5,
            ActivityType = "Login",
            ActivityName = "Agent logged into mobile app",
            TimeStamp = now.AddDays(-3)
        }
    };

            //if (!string.IsNullOrWhiteSpace(activityType))
            //{
            //    data = data
            //        .Where(x => x.ActivityType.Equals(activityType, StringComparison.OrdinalIgnoreCase))
            //        .ToList();
            //}

            if (args.startDate.HasValue)
            {
                data = data
                    .Where(x => x.TimeStamp >= args.startDate.Value)
                    .ToList();
            }

            if (args.endDate.HasValue)
            {
                data = data
                    .Where(x => x.TimeStamp <= args.endDate.Value)
                    .ToList();
            }

            var totalCount = data.Count;

            var pagedData = data
                .OrderByDescending(x => x.TimeStamp)
                .Skip((args.PageNumber - 1) * args.PageSize)
                .Take(args.PageSize)
                .ToList();

            return new PagedListResponse<List<OrganizationAgentActivityLog>>
            {
                PageNumber = args.PageNumber,
                PageSize = args.PageSize,
                Count = totalCount,
                Data = pagedData
            };
        }
        public async Task<PagedListResponse<List<OrganizationAgentTransactionHistory>>>
    GetAgentTransactionHistory(ListQueryParams args)
        {
            await Task.Delay(50); // simulate async

            var factor = (args.Id % 4) + 1;

            var data = new List<OrganizationAgentTransactionHistory>
    {
        new OrganizationAgentTransactionHistory
        {
            Id = 1,
            TransactionType = "Allocation",
            FarmerName = $"Farmer {args.Id}-A",
            FarmerUserId = $"FRM{args.Id}01",
            InputType = "Fertilizer",
            Quantity = 50 * factor,
            QuantityType = "Bags",
            Amount = 25000 * factor,
            Status = "Completed"
        },
        new OrganizationAgentTransactionHistory
        {
            Id = 2,
            TransactionType = "Distribution",
            FarmerName = $"Farmer {args.Id}-B",
            FarmerUserId = $"FRM{args.Id}02",
            InputType = "Seeds",
            Quantity = 30 * factor,
            QuantityType = "Kg",
            Amount = 12000 * factor,
            Status = factor % 2 == 0 ? "Pending" : "Completed"
        },
        new OrganizationAgentTransactionHistory
        {
            Id = 3,
            TransactionType = "Return",
            FarmerName = $"Farmer {args.Id}-C",
            FarmerUserId = $"FRM{args.Id}03",
            InputType = "Pesticide",
            Quantity = 10 * factor,
            QuantityType = "Litres",
            Amount = 5000 * factor,
            Status = "Failed"
        },
        new OrganizationAgentTransactionHistory
        {
            Id = 4,
            TransactionType = "Distribution",
            FarmerName = $"Farmer {args.Id}-D",
            FarmerUserId = $"FRM{args.Id}04",
            InputType = "Fertilizer",
            Quantity = 20 * factor,
            QuantityType = "Bags",
            Amount = 10000 * factor,
            Status = "Completed"
        }
    };

            // ✅ Filtering
            //if (args.Status.HasValue)
            //{
            //    data = data
            //        .Where(x => x.Status.Equals(args.Status.Value, StringComparison.OrdinalIgnoreCase))
            //        .ToList();
            //}

            //if (!string.IsNullOrWhiteSpace(transactionType))
            //{
            //    data = data
            //        .Where(x => x.TransactionType.Equals(transactionType, StringComparison.OrdinalIgnoreCase))
            //        .ToList();
            //}

            var totalCount = data.Count;

            var pagedData = data
                .OrderByDescending(x => x.Id) // mimic real-world sorting (latest first)
                .Skip((args.PageNumber - 1) * args.PageSize)
                .Take(args.PageSize)
                .ToList();

            return new PagedListResponse<List<OrganizationAgentTransactionHistory>>
            {
                PageNumber = args.PageNumber,
                PageSize = args.PageSize,
                Count = totalCount,
                Data = pagedData
            };
        }
        public async Task<OrganizationAgentPerformanceSummary> GetOrganizationAgentPerformanceSummary(ListQueryParams args)
        {
            var agent = await _context.Agents
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == args.Id);

            if (agent == null)
                return null;

            var orgAgents = await _context.OrganizationAgents
                .AsNoTracking()
                .Where(x => x.AgentId == args.Id && x.IsActive)
                .ToListAsync();

            if (!orgAgents.Any())
                return null;

            var orgAgentIds = orgAgents.Select(x => x.Id).ToList();
            var subscribers = 0; //replace when there is data.

            var inputs = 0;//replace when there is data

            var totalEquityPaid = 0;// replace when there is data
            var productSold = 0;//replace when there is data
            return new OrganizationAgentPerformanceSummary
            {
                ProductSold = productSold,
                InputDisbursed = inputs,
                Subscribers = subscribers,
                TotalEquityPaid = totalEquityPaid,
            };
        }
        public async Task<PagedListResponse<List<OrganizationAgentSubscriberModel>>> GetAgentSubscribers(ListQueryParams args)
        {
            await Task.Delay(50); // simulate async

            await Task.Delay(50);

            var factor = (args.Id % 3) + 1;

            var subscribers = new List<OrganizationAgentSubscriberModel>();
    //{
    //    new OrganizationAgentSubscriberModel
    //    {
    //        Id = 1,
    //        FarmerName = $"Farmer {args.Id }-A",
    //        FarmerUserId = $"FRM{args.Id }01",
    //        InputPackage = "Maize Package",
    //        InputPackageId = 101,
    //        EquityPaid = 10000 * factor,
    //        Status = true
    //    },
    //    new OrganizationAgentSubscriberModel
    //    {
    //        Id = 2,
    //        FarmerName = $"Farmer {args.Id }-B",
    //        FarmerUserId = $"FRM{args.Id }02",
    //        InputPackage = "Rice Package",
    //        InputPackageId = 102,
    //        EquityPaid = 15000 * factor,
    //        Status = factor % 2 == 0
    //    },
    //    new OrganizationAgentSubscriberModel
    //    {
    //        Id = 3,
    //        FarmerName = $"Farmer {args.Id }-C",
    //        FarmerUserId = $"FRM{args.Id }03",
    //        InputPackage = "Cassava Package",
    //        InputPackageId = 103,
    //        EquityPaid = 8000 * factor,
    //        Status = true
    //    }
    //};

            if (!subscribers.Any())
                return null;
            if (args.Status.HasValue)
            {
                subscribers = subscribers.Where(x => x.Status == args.Status.Value).ToList();
            }

            var totalCount = subscribers.Count;

            var pagedData = subscribers
                .Skip((args.PageNumber - 1) * args.PageSize)
                .Take(args.PageSize)
                .ToList();

            return new PagedListResponse<List<OrganizationAgentSubscriberModel>>
            {
                PageNumber = args.PageNumber,
                PageSize = args.PageSize,
                Count = totalCount,
                Data = pagedData
            };
        }
        public async Task<PagedListResponse<List<OrganizationAgentInputAllocationModel>>>
    GetAgentInputAllocations(ListQueryParams args)
        {
            await Task.Delay(50); // simulate async

            var baseValue = args.Id % 5 + 1; // vary data per agent

            var data = new List<OrganizationAgentInputAllocationModel>();
            //{
            //    new OrganizationAgentInputAllocationModel
            //    {
            //        Id = 1,
            //        InputName = "Fertilizer",
            //        Allocated = 100 * baseValue,
            //        Distributed = 60 * baseValue,
            //    },
            //    new OrganizationAgentInputAllocationModel
            //    {
            //        Id = 2,
            //        InputName = "Seeds",
            //        Allocated = 80 * baseValue,
            //        Distributed = 30 * baseValue,
            //    },
            //    new OrganizationAgentInputAllocationModel
            //    {
            //        Id = 3,
            //        InputName = "Pesticides",
            //        Allocated = 50 * baseValue,
            //        Distributed = 20 * baseValue,
            //    },
            //    new OrganizationAgentInputAllocationModel
            //    {
            //        Id = 4,
            //        InputName = "Herbicides",
            //        Allocated = 30 * baseValue,
            //        Distributed = 10 * baseValue,
            //    }
            //};
            if (!data.Any())
                return null;
            data.ForEach(x => x.Remaining = x.Allocated - x.Distributed);

            int pageNumber = args.PageNumber > 0 ? args.PageNumber : 1;
            int pageSize = args.PageSize > 0 ? args.PageSize : 10;

            int totalCount = data.Count;

            var pagedData = data
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedListResponse<List<OrganizationAgentInputAllocationModel>>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Count = totalCount,
                Data = pagedData
            };
        }

        #endregion agentviews

        public async Task<OrganizationDashboardResponse> GetOrganizationDashboardAsync(ListQueryParams args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            if (args.Id <= 0)
                throw new ValidationException("Invalid Organization");

            var organizationId = args.Id;

            var result = await _context.Database
                .SqlQueryRaw<OrganizationDashboardResponse>(
        @"
SELECT
    (SELECT COUNT(DISTINCT AgentId)
     FROM OrganizationAgents
     WHERE OrganizationId = {0} AND IsActive = 1) AS TotalAgents,

    (SELECT COUNT(*)
     FROM OrganizationLocations
     WHERE OrganizationId = {0} AND IsActive = 1) AS TotalActiveLocations,

    CAST(0 AS decimal(18,2)) AS TotalEquityPaid,
 CAST(0 AS decimal(18,2)) AS TotalInputDisbursed,
    CAST(0 AS decimal(18,2)) AS TotalProduceSold
", organizationId)
                .FirstOrDefaultAsync();

            return result ?? new OrganizationDashboardResponse();
        }
        public async Task<PagedListResponse<List<LocationAgentDetailResponse>>> GetAgentsByLocationAsync(ListQueryParams args)
        {
            var query = _context.OrganizationAgents
                .AsNoTracking()
                .Where(x => x.LocationId == args.Id && x.IsActive);

            var totalCount = await query.CountAsync();

            var pagedOrgAgents = await query
                .OrderByDescending(x => x.Id)
                .Skip((args.PageNumber - 1) * args.PageSize)
                .Take(args.PageSize)
                .ToListAsync();

            if (!pagedOrgAgents.Any())
            {
                return new PagedListResponse<List<LocationAgentDetailResponse>>
                {
                    PageNumber = args.PageNumber,
                    PageSize = args.PageSize,
                    Count = totalCount,
                    Data = new List<LocationAgentDetailResponse>()
                };
            }

            var orgAgentIds = pagedOrgAgents.Select(x => x.Id).ToList();
            var agentIds = pagedOrgAgents.Select(x => x.AgentId).ToList();

            var agents = await _context.Agents
                .AsNoTracking()
                .Where(x => agentIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            var roles = await _context.OrganizationAgentRoles
                .AsNoTracking()
                .Where(x => orgAgentIds.Contains(x.OrganizationAgentId))
                .GroupBy(x => x.OrganizationAgentId)
                .Select(g => new
                {
                    OrganizationAgentId = g.Key,
                    RoleId = g.Select(x => x.RoleId).FirstOrDefault()
                })
                .ToListAsync();

            var roleIds = roles.Select(x => x.RoleId).ToList();

            var roleDict = await _context.OrganizationRoles
                .AsNoTracking()
                .Where(r => roleIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r.RoleName);

            //// MOCK: Subscribers
            //var subscriberDict = pagedOrgAgents.ToDictionary(
            //    x => x.Id,
            //    x => new Random(x.Id.GetHashCode()).Next(5, 50)
            //);

            //// MOCK: Disbursed
            //var disbursedDict = pagedOrgAgents.ToDictionary(
            //    x => x.Id,
            //    x => (double)new Random(x.Id.GetHashCode()).Next(100, 1000)
            //);

            var result = pagedOrgAgents.Select(oa =>
            {
                var agent = agents.GetValueOrDefault(oa.AgentId);

                var roleEntry = roles.FirstOrDefault(r => r.OrganizationAgentId == oa.Id);

                var roleName = roleEntry != null && roleDict.ContainsKey(roleEntry.RoleId)
                    ? roleDict[roleEntry.RoleId]
                    : "N/A";

                return new LocationAgentDetailResponse
                {
                    AgentId = agent?.Id ?? 0,
                    AgentName = agent?.Name,
                    AgentRole = roleName,
                    TotalSubscribers = 0,//subscriberDict.GetValueOrDefault(oa.Id, 0),
                    TotalInputDisbursed = 0,//disbursedDict.GetValueOrDefault(oa.Id, 0),
                    IsActive = oa.IsActive
                };
            }).ToList();

            return new PagedListResponse<List<LocationAgentDetailResponse>>
            {
                PageNumber = args.PageNumber,
                PageSize = args.PageSize,
                Count = totalCount,
                Data = result
            };
        }

        public async Task<PagedListResponse<List<LocationInventoryBreakdownResponse>>> GetLocationInventoryBreakdownAsync(ListQueryParams args)
        {
            var pageSize = args.PageSize <= 0 ? 10 : args.PageSize;
            var pageNumber = args.PageNumber <= 0 ? 1 : args.PageNumber;

            var location = await _context.OrganizationLocations
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == args.Id);

            if (location == null)
                throw new NotFoundException("Location not found");

            // MOCK: Input types (since no DB yet)
            var mockInputs = new List<LocationInventoryBreakdownResponse>();
            //{
            //    new LocationInventoryBreakdownResponse
            //    {
            //        InputId = 1,
            //        InputName = "Fertilizer",
            //        TotalStock = location.StockLevel * 0.5
            //    },
            //    new LocationInventoryBreakdownResponse
            //    {
            //        InputId = 2,
            //        InputName = "Seeds",
            //        TotalStock = location.StockLevel * 0.3
            //    },
            //    new LocationInventoryBreakdownResponse
            //    {
            //        InputId = 3,
            //        InputName = "Pesticides",
            //        TotalStock = location.StockLevel * 0.2
            //    }
            //};
            if (!mockInputs.Any())
                return null;

            // MOCK: Allocation per input (simulate assigned to agents)
            var allocationDict = mockInputs.ToDictionary(
                x => x.InputId,
                x => new Random((int)x.InputId * (int)location.Id).Next(10, 100)
            );

            //  Calculate allocation + available balance
            mockInputs.ForEach(x =>
            {
                x.QuantityAllocated = allocationDict.GetValueOrDefault(x.InputId, 0);

                //  Available = TotalStock - Allocated
                x.AvailableBalance = x.TotalStock - x.QuantityAllocated;

                // Prevent negative (just in case mock overshoots)
                if (x.AvailableBalance < 0)
                    x.AvailableBalance = 0;
            });

            var totalCount = mockInputs.Count;

            var pagedData = mockInputs
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedListResponse<List<LocationInventoryBreakdownResponse>>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Count = totalCount,
                Data = pagedData
            };
        }

        public async Task<PagedListResponse<List<LocationActivityLogResponse>>> GetLocationActivityLogsAsync(ListQueryParams args)
        {
            var pageSize = args.PageSize <= 0 ? 10 : args.PageSize;
            var pageNumber = args.PageNumber <= 0 ? 1 : args.PageNumber;

            var location = await _context.OrganizationLocations
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == args.Id);

            if (location == null)
                throw new NotFoundException("Location not found");

            //  MOCK: Activity logs
            var mockLogs = new List<LocationActivityLogResponse>();
            //{
            //    new LocationActivityLogResponse
            //    {
            //        Id = 1,
            //        ActivityType = "Agent Assignment",
            //        Name = "Agent Assigned",
            //        Description = "Agent John Doe was assigned to this location",
            //        TimeStamp = DateTimeOffset.Now.AddDays(-1)
            //    },
            //    new LocationActivityLogResponse
            //    {
            //        Id = 2,
            //        ActivityType = "Input Disbursement",
            //        Name = "Fertilizer Distributed",
            //        Description = "50 units of fertilizer disbursed by Agent Jane",
            //        TimeStamp = DateTimeOffset.Now.AddHours(-10)
            //    },
            //    new LocationActivityLogResponse
            //    {
            //        Id = 3,
            //        ActivityType = "Stock Update",
            //        Name = "Inventory Updated",
            //        Description = "Stock level updated after new supply",
            //        TimeStamp = DateTimeOffset.Now.AddHours(-5)
            //    },
            //    new LocationActivityLogResponse
            //    {
            //        Id = 4,
            //        ActivityType = "Agent Update",
            //        Name = "Agent Reassigned",
            //        Description = "Agent moved to another location",
            //        TimeStamp = DateTimeOffset.Now.AddHours(-2)
            //    }
            //};
            if (!mockLogs.Any())
                return null;

            // Optional filtering (by activity type if passed in query.Search)
            //if (!string.IsNullOrWhiteSpace(args.Search))
            //{
            //    mockLogs = mockLogs
            //        .Where(x => x.ActivityType.Contains(args.Search, StringComparison.OrdinalIgnoreCase) ||
            //                    x.Name.Contains(args.Search, StringComparison.OrdinalIgnoreCase))
            //        .ToList();
            //}

            // 📄 Pagination
            var totalCount = mockLogs.Count;

            var pagedData = mockLogs
                .OrderByDescending(x => x.TimeStamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedListResponse<List<LocationActivityLogResponse>>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Count = totalCount,
                Data = pagedData
            };
        }
        public async Task<bool> ReassignAgentToLocation(OrganizationAgentArgs data)
        {
            var existingAgent = await _context.Agents
                .FirstOrDefaultAsync(x => x.UserId == data.UserId);

            if (existingAgent == null)
                throw new Exception("Agent not found");

            if (!data.OrganizationId.HasValue)
                throw new Exception("OrganizationId is required");

            var existingAssignment = await _context.OrganizationAgents
                .FirstOrDefaultAsync(x =>
                    x.AgentId == existingAgent.Id &&
                    x.OrganizationId == data.OrganizationId &&
                    x.IsActive);

            if (existingAssignment == null)
            {
                await _context.OrganizationAgents.AddAsync(new OrganizationAgent
                {
                    AgentId = existingAgent.Id,
                    OrganizationId = data.OrganizationId.Value,
                    AssignedByUserId = data.OnboardedById,
                    AssignedOn = DateTimeOffset.Now,
                    LocationId = data.LocationId,
                    IsActive = true
                });
            }
            else
            {
                existingAssignment.LocationId = data.LocationId;
                existingAssignment.AssignedOn = DateTimeOffset.Now;
                existingAssignment.AssignedByUserId = data.OnboardedById;

                _context.OrganizationAgents.Update(existingAssignment);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<OrganizationAgentDetailResponse> GetOrganizationAgentDetail(ListQueryParams args)
        {
            var agent = await _context.Agents
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == args.UserId);

            if (agent == null)
                return null;

            var orgAgents = await _context.OrganizationAgents
                .AsNoTracking()
                .Where(x => x.AgentId == agent.Id && x.IsActive)
                .ToListAsync();

            if (orgAgents.Count == 0)
                return null;

            var orgAgentIds = orgAgents.Select(x => x.Id).ToList();

            var locationIds = orgAgents
                .Where(x => x.LocationId > 0)
                .Select(x => x.LocationId)
                .Distinct()
                .ToList();
            var locations = await (
                from loc in _context.OrganizationLocations.AsNoTracking()
                where locationIds.Contains(loc.Id)

                join lt in _context.LocationTypes.AsNoTracking()
                    on loc.LocationTypeId equals lt.Id into ltJoin
                from lt in ltJoin.DefaultIfEmpty()

                join st in _context.States.AsNoTracking()
                    on loc.StateId equals st.StateId into stJoin
                from st in stJoin.DefaultIfEmpty()

                join lga in _context.LGAs.AsNoTracking()
                    on loc.LgaId equals lga.LgaId into lgaJoin
                from lga in lgaJoin.DefaultIfEmpty()

                select new OrganizationLocationDto
                {
                    LocationId = loc.Id,
                    LocationName = loc.LocationName,

                    LocationType = lt != null ? lt.Type : null,
                    State = st != null ? st.State : null,
                    Lga = lga != null ? lga.Name : null
                }
            )
            .ToListAsync();

            var roles = await (
                from r in _context.OrganizationAgentRoles.AsNoTracking()
                where orgAgentIds.Contains(r.OrganizationAgentId)

                join rr in _context.OrganizationRoles.AsNoTracking()
                    on r.RoleId equals rr.Id

                select new OrganiationRoleDto
                {
                    RoleId = rr.Id,
                    RoleName = rr.RoleName
                }
            )
            .Distinct()
            .ToListAsync();
            return new OrganizationAgentDetailResponse
            {
                AgentId = agent.Id,
                UserId = agent.UserId,
                Name = agent.Name,
                Email = agent.Email,
                PhoneNumber = agent.PhoneNumber,
                State = agent.State,
                Lga = agent.Lga,
                Ward = agent.Ward,
                OnBoardedOn = agent.OnBoardedOn,

                Locations = locations,
                Roles = roles
            };
        }
        public async Task<OrganizationMembershipDashboardResponse> GetOrganizationMembershipDashboard(
       ListQueryParams query)
        {
            var result = await
            (
                from m in _context.MemberShips.AsNoTracking()

                join om in _context.OrganizationMemberships.AsNoTracking()
                    on m.Id equals om.MembershipId

                join o in _context.BusinessOrganizations.AsNoTracking()
                    on om.OrganizationId equals o.OrganizationId

                join fc in _context.FarmerCooperatives.AsNoTracking()
                    on m.Id equals fc.CooperativeId into farmerJoin
                from fc in farmerJoin.DefaultIfEmpty()

                join f in _context.Farmers.AsNoTracking()
                    on fc.FarmerId equals f.Id into farmerDetailJoin
                from f in farmerDetailJoin.DefaultIfEmpty()

                where o.OrganizationCode == query.organizationCode

                group f by 1 into g
                select new OrganizationMembershipDashboardResponse
                {
                    TotalMemberships =
                        (from m2 in _context.MemberShips
                         join om2 in _context.OrganizationMemberships on m2.Id equals om2.MembershipId
                         join o2 in _context.BusinessOrganizations on om2.OrganizationId equals o2.OrganizationId
                         where o2.OrganizationCode == query.organizationCode
                         select m2.Id).Distinct().Count(),

                    TotalFarmers = g.Count(x => x != null),

                    TotalVerifiedFarmers = g.Count(x => x != null && x.Level != FarmerLevel.Level1),

                    TotalUnverifiedFarmers = g.Count(x => x != null && x.Level == FarmerLevel.Level1),
                    TotalInputDisbursed=0,
                    TotalRecoveryAmount=0
                }
            )
            .FirstOrDefaultAsync();

            return result ?? new OrganizationMembershipDashboardResponse();
        }
        public async Task<List<OrganizationMemberShipResponseModel>> GetCooperativesByOrganization(
           string organizationCode,
           ListQueryParams query)
        {
            var baseQuery =
                from m in _context.MemberShips.AsNoTracking()

                join om in _context.OrganizationMemberships.AsNoTracking()
                    on m.Id equals om.MembershipId

                join o in _context.BusinessOrganizations.AsNoTracking()
                    on om.OrganizationId equals o.OrganizationId

                where o.OrganizationCode == organizationCode

                select m;

            var memberships = await baseQuery
                .OrderByDescending(x => x.OnBoardedOn)
                .ToListAsync();

            if (!memberships.Any())
                return new List<OrganizationMemberShipResponseModel>();

            var membershipIds = memberships.Select(x => x.Id).ToList();

            var farmerLinks = await (
                from fc in _context.FarmerCooperatives.AsNoTracking()
                where membershipIds.Contains(fc.CooperativeId)

                join f in _context.Farmers.AsNoTracking()
                    on fc.FarmerId equals f.Id

                select new
                {
                    fc.CooperativeId,
                    f.Id,
                    f.Level
                }
            ).ToListAsync();

            var membershipAgents = await _context.MemberShipAgents
                .AsNoTracking()
                .Where(x => membershipIds.Contains(x.MemberShipId))
                .Select(x => x.MemberShipId)
                .Distinct()
                .ToListAsync();

            var agentSet = membershipAgents.ToHashSet();

            var result = memberships.Select(m =>
            {
                var farmersForMembership = farmerLinks
                    .Where(x => x.CooperativeId == m.Id)
                    .ToList();

                var totalFarmers = farmersForMembership.Count;

                var totalVerifiedFarmers = farmersForMembership
                    .Count(x => x.Level != FarmerLevel.Level1);

                var totalUnverifiedFarmers = totalFarmers - totalVerifiedFarmers;

                var hasAgent = agentSet.Contains(m.Id);

                return new OrganizationMemberShipResponseModel
                {
                    Id = m.Id,
                    Name = m.Name,
                    Location = m.Location,
                    State = m.State,
                    Lga = m.Lga,
                    OnBoardedOn = m.OnBoardedOn,

                    ContactPerson =
                        $"{m.MembershipLeaderLastName} {m.MembershipLeaderFirstName}",

                    PhoneNumber = m.PhoneNumber,
                    UserId = m.UserId,
                    Tier = EnumExtensions.GetDisplayName(m.Tier),
                    MembershipType = m.MemberShipType?.Name,
                    Ward = m.Ward,

                    TotalFarmers = totalFarmers,
                    TotalVerifiedFarmers = totalVerifiedFarmers,
                    TotalUnverifiedFarmers = totalUnverifiedFarmers,
                    TotalInputReceived = 0,
                    RecoveryAmount = 0,

                    HasbeenAssignedAgent = hasAgent
                };
            }).ToList();

            return result;
        }

        /// <summary>
        /// TODO: REVERT if it fails
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        /**public async Task<OrganizationAgentDetailResponse> GetOrganizationAgentDetail(ListQueryParams args)
        {
            var agent = await _context.Agents
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == args.UserId);

            if (agent == null)
                return null;

            var orgAgents = await _context.OrganizationAgents
                .AsNoTracking()
                .Where(x => x.AgentId == agent.Id && x.IsActive)
                .ToListAsync();

            if (!orgAgents.Any())
                return null;

            var orgAgentIds = orgAgents.Select(x => x.Id).ToList();
            var locationIds = orgAgents.Select(x => x.LocationId).Distinct().ToList();

            var locations = await _context.OrganizationLocations
                .AsNoTracking()
                .Where(x => locationIds.Contains(x.Id))
                .Select(x => new OrganizationLocationDto
                {
                    LocationId = x.Id,
                    LocationName = x.LocationName,
                    LocationType = x.LocationType.Type,
                    State = x.State.State,
                    Lga = x.LGA.Name
                })
                .ToListAsync();

            var roles = await _context.OrganizationAgentRoles
                .AsNoTracking()
                .Where(x => orgAgentIds.Contains(x.OrganizationAgentId))
                .Select(x => new OrganiationRoleDto
                {
                    RoleId = x.RoleId,
                    RoleName = x.OrganizationRole.RoleName
                })
                .Distinct()
                .ToListAsync();

            return new OrganizationAgentDetailResponse
            {
                AgentId = agent.Id,
                UserId = agent.UserId,
                Name = agent.Name,
                Email = agent.Email,
                PhoneNumber = agent.PhoneNumber,
                State = agent.State,
                Lga = agent.Lga,
                Ward = agent.Ward,
                OnBoardedOn = agent.OnBoardedOn,
                Locations = locations,
                Roles = roles
            };
        }
        **/
        public async Task<PagedListResponse<List<OrganizationAgentResponse>>> GetOrganizationAgents(ListQueryParams args)
        {
            args.PageNumber = args.PageNumber <= 0 ? 1 : args.PageNumber;
            args.PageSize = args.PageSize <= 0 ? 10 : args.PageSize;

            var baseQuery =
                from oa in _context.OrganizationAgents.AsNoTracking()
                where oa.OrganizationId == args.Id && oa.IsActive

                join a in _context.Agents.AsNoTracking()
                    on oa.AgentId equals a.Id

                group new { oa, a } by new
                {
                    a.Id,
                    a.UserId,
                    a.Name,
                    a.Email,
                    a.PhoneNumber,
                    a.State,
                    a.Lga,
                    a.Ward,
                    a.OnBoardedOn
                }
                into g

                select new OrganizationAgentResponse
                {
                    AgentId = g.Key.Id,
                    UserId = g.Key.UserId,
                    Name = g.Key.Name,
                    Email = g.Key.Email,
                    PhoneNumber = g.Key.PhoneNumber,
                    State = g.Key.State,
                    Lga = g.Key.Lga,
                    Ward = g.Key.Ward,
                    OnBoardedOn = g.Key.OnBoardedOn,

                    LocationCount = g
                        .Where(x => x.oa.LocationId > 0)
                        .Select(x => x.oa.LocationId)
                        .Distinct()
                        .Count(),

                    RoleCount =
                        (from r in _context.OrganizationAgentRoles
                         where g.Select(x => x.oa.Id).Contains(r.OrganizationAgentId)
                         select r.Id).Count(),

                    Subscribers = 0,
                    InputDisbursed = 0,
                    EquityPaid = 0,
                    ProductSold = 0
                };

            var totalCount = await baseQuery.CountAsync();

            var result = await baseQuery
                .OrderByDescending(x => x.AgentId)
                .Skip((args.PageNumber - 1) * args.PageSize)
                .Take(args.PageSize)
                .ToListAsync();

            return new PagedListResponse<List<OrganizationAgentResponse>>
            {
                PageNumber = args.PageNumber,
                PageSize = args.PageSize,
                Count = totalCount,
                Data = result
            };
        }
       
        /// <summary>
        /// TODO: revert if fails
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
      /**  public async Task<PagedListResponse<List<OrganizationAgentResponse>>> GetOrganizationAgents(ListQueryParams args)
        {
            var query =
    from oa in _context.OrganizationAgents.AsNoTracking()
    where oa.OrganizationId == args.Id && oa.IsActive

    group oa by oa.AgentId into g

    select new
    {
        AgentId = g.Key,

        LocationCount = g
            .Where(x => x.LocationId > 0)
            .Select(x => x.LocationId)
            .Distinct()
            .Count(),

        OrganizationAgentIds = g.Select(x => x.Id)
    };
            var totalCount = await query.CountAsync();

            var pagedAgents = await query
                .OrderByDescending(x => x.AgentId)
                .Skip((args.PageNumber - 1) * args.PageSize)
                .Take(args.PageSize)
                .ToListAsync();

            var agentIds = pagedAgents.Select(x => x.AgentId).ToList();

            var agents = await _context.Agents
                .AsNoTracking()
                .Where(x => agentIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            var orgAgentIds = pagedAgents
                .SelectMany(x => x.OrganizationAgentIds)
                .ToList();

            var roleCounts = await _context.OrganizationAgentRoles
                .AsNoTracking()
                .Where(x => orgAgentIds.Contains(x.OrganizationAgentId))
                .GroupBy(x => x.OrganizationAgentId)
                .Select(g => new
                {
                    OrganizationAgentId = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var roleCountDict = roleCounts.ToDictionary(x => x.OrganizationAgentId, x => x.Count);

            var result = pagedAgents.Select(x =>
            {
                var agent = agents.GetValueOrDefault(x.AgentId);

                var totalRoles = x.OrganizationAgentIds
                    .Sum(id => roleCountDict.GetValueOrDefault(id, 0));

                return new OrganizationAgentResponse
                {
                    AgentId = agent?.Id ?? 0,
                    UserId = agent?.UserId,
                    Name = agent?.Name,
                    Email = agent?.Email,
                    PhoneNumber = agent?.PhoneNumber,
                    State = agent?.State,
                    Lga = agent?.Lga,
                    Ward = agent?.Ward,
                    OnBoardedOn = agent?.OnBoardedOn ?? DateTimeOffset.MinValue,

                    LocationCount = x.LocationCount,
                    RoleCount = totalRoles,
                    Subscribers = 0, //TODO:update when you have subscribers 
                    InputDisbursed = 0, //TODO:update when you have data
                    EquityPaid = 0, //TODO:update when you have data
                    ProductSold = 0 //TODO:update when you have data

                };
            }).ToList();

            return new PagedListResponse<List<OrganizationAgentResponse>>
            {
                PageNumber = args.PageNumber,
                PageSize = args.PageSize,
                Count = totalCount,
                Data = result
            };
        }
      **/
        public async Task<OrganizationLocationModel> UpdateLocationAsync(long locationId, OrganizationLocationModel model)
        {
            var existingLocation = await _context.OrganizationLocations
                .FirstOrDefaultAsync(x => x.Id == locationId);

            if (existingLocation == null)
                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "Location not found");

            var duplicate = await _context.OrganizationLocations
                .AnyAsync(x => x.LocationName == model.LocationName
                && x.OrganizationId==model.OrganizationId
                && x.StateId == model.StateId
                && x.LgaId == model.LgaId
                && x.Address == model.Address
                && x.Id != locationId);

            if (duplicate)
                throw new ValidationException("A location with this name already exists");
            if (model.LocationTypeId > 0)
                existingLocation.LocationTypeId = model.LocationTypeId;

            if (model.StateId > 0)
                existingLocation.StateId = model.StateId;

            if (model.LgaId > 0)
                existingLocation.LgaId = model.LgaId;

            if (!string.IsNullOrWhiteSpace(model.Address))
                existingLocation.Address = model.Address;

            if (model.Capacity > 0)
                existingLocation.Capacity = model.Capacity;

            if (!string.IsNullOrWhiteSpace(model.CapacityType))
                existingLocation.CapacityType = model.CapacityType;

            if (!string.IsNullOrWhiteSpace(model.LocationName))
                existingLocation.LocationName = model.LocationName;


            existingLocation.IsActive = model.IsActive;

            _context.OrganizationLocations.Update(existingLocation);
            await _context.SaveChangesAsync();

            return existingLocation.ViewMap();
        }
        public async Task<OrganizationLocationModel> CreateLocationAsync(OrganizationLocationModel model)
        {
            var orgExist = await _context.OrganizationLocations
              .FirstOrDefaultAsync(x => x.LocationName == model.LocationName && x.OrganizationId == model.OrganizationId);

            if (orgExist != null)
            {
                return orgExist.ViewMap();
            }
            OrganizationLocation org = model.Map();
            org.IsActive = true;
            _context.Set<OrganizationLocation>().Add(org);
            await _context.SaveChangesAsync();
            var response = org.ViewMap();
            return response;
        }
        public async Task<List<OrganizationAgentLocationResponseModel>> GetAgentAssignedLocations(ListQueryParams query)
        {
            var agentLocations = await _context.Set<OrganizationAgent>()
                .Where(a => a.AgentId == query.Id)
                .Select(a => a.LocationId)
                .ToListAsync();

            if (!agentLocations.Any())
                return new List<OrganizationAgentLocationResponseModel>();

            var locations = await _context.Set<OrganizationLocation>()
                .Where(l => agentLocations.Contains(l.Id))
                .Include(l => l.LocationType)
                .Include(l => l.State)
                .Include(l => l.LGA)
                .ToListAsync();

            var result = locations.Select(loc => new OrganizationAgentLocationResponseModel
            {
                Id = loc.Id,
                StateId = loc.StateId,
                LgaId = loc.LgaId,
                BusinessCode = loc.BusinessCode,
                OrganizationId = loc.OrganizationId,
                LocationName = loc.LocationName,
                LocationTypeId = loc.LocationTypeId,
                State = loc.State?.State,
                Lga = loc.LGA?.Name,
                LocationType = loc.LocationType?.Type,
                Address = loc.Address,
                IsActive = loc.IsActive,
                Capacity = loc.Capacity,
                Allocation = loc.StockLevel,
                AvailableCapacity = loc.Capacity - loc.StockLevel,
                CapacityType = loc.CapacityType
            }).ToList();

            return result;
        }
        public async Task<List<OrganizationLocationResponseModel>> GetLocationAsync(ListQueryParams query)
        {
            var locations = await _context.Set<OrganizationLocation>()
                .Where(x => x.BusinessCode == query.businessCode && x.OrganizationId == query.Id)
                .Include(x => x.LocationType)
                .Include(x => x.State)
                .Include(x => x.LGA)
                .ToListAsync();

            var locationIds = locations.Select(l => l.Id).ToList();

            var agentCounts = await _context.Set<OrganizationAgent>()
                .Where(a => locationIds.Contains(a.LocationId))
                .GroupBy(a => a.LocationId)
                .Select(g => new { LocationId = g.Key, Count = g.Count() })
                .ToListAsync();

            var result = locations.Select(loc =>
            {
                var assignedCount = agentCounts.FirstOrDefault(ac => ac.LocationId == loc.Id)?.Count ?? 0;

                return new OrganizationLocationResponseModel
                {
                    Id = loc.Id,
                    StateId = loc.StateId,
                    LgaId = loc.LgaId,
                    BusinessCode = loc.BusinessCode,
                    OrganizationId = loc.OrganizationId,
                    LocationName = loc.LocationName,
                    LocationTypeId = loc.LocationTypeId,
                    State = loc.State?.State,
                    Lga = loc.LGA?.Name,
                    LocationType = loc.LocationType?.Type,
                    Address = loc.Address,
                    IsActive = loc.IsActive,
                    Capacity = loc.Capacity,
                    TotalStock = loc.StockLevel,
                    CurrentUsage = loc.Capacity - loc.StockLevel,
                    CapacityType = loc.CapacityType,
                    AssignedAgentCount = assignedCount.ToString()
                };
            }).ToList();

            return result;
        }
        public async Task<OrganizationLocationModel> GetLocationByIAsync(long Id)
        {
            var loc = await _context.Set<OrganizationLocation>()
                 .Include(x => x.LocationType)
                .Include(x => x.State)
                .Include(x => x.LGA)
                .FirstOrDefaultAsync(x => x.Id == Id);
            if (loc == null)
            {
                return null;
            }
            return new OrganizationLocationModel
            {
                Id = loc.Id,
                StateId = loc.StateId,
                LgaId = loc.LgaId,
                BusinessCode = loc.BusinessCode,
                OrganizationId = loc.OrganizationId,
                LocationName = loc.LocationName,
                LocationTypeId = loc.LocationTypeId,
                State = loc.State?.State,
                Lga = loc.LGA?.Name,
                LocationType = loc.LocationType?.Type,
                Address = loc.Address,
                IsActive = loc.IsActive,
                Capacity = loc.Capacity,
                Used = loc.StockLevel,
                Available = loc.Capacity - loc.StockLevel,
                CapacityType = loc.CapacityType
            };
        }

        public async Task<OrganizationRoleModel> CreateRoleAsync(OrganizationRoleModel model)
        {
            var orgExist = await _context.OrganizationRoles
              .FirstOrDefaultAsync(x => x.RoleName == model.RoleName && x.OrganizationId == model.OrganizationId);

            if (orgExist != null)
            {
                return orgExist.ViewMap();
            }
            OrganizationRole org = model.Map();
            org.CreatedOn = DateTimeOffset.Now;
            _context.Set<OrganizationRole>().Add(org);
            await _context.SaveChangesAsync();
            var response = org.ViewMap();
            return response;
        }
        public async Task<List<OrganizationRoleModel>> GetRolesAsync(ListQueryParams query)
        {
            var org = await _context.Set<OrganizationRole>()
                .Where(x => x.BusinessCode == query.businessCode && x.OrganizationId == query.Id).ToListAsync();

            return org.Select(x => x.ViewMap()).ToList();
        }
        public async Task<OrganizationRoleModel> GetRoleByIAsync(long Id)
        {
            var org = await _context.Set<OrganizationRole>()
                .FirstOrDefaultAsync(x => x.Id == Id);
            if (org == null)
            {
                return null;
            }
            return org.ViewMap();
        }

        #region FarmerMgt
        public async Task<(bool status, FarmerModel data)> CreateOrUpdateFarmer(FarmerModel data)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var farmer = await _context.Farmers
                        .FirstOrDefaultAsync(f => f.UserId == data.UserId);

                    if (farmer == null)
                    {
                        farmer = data.Map();
                        await _context.Farmers.AddAsync(farmer);
                    }
                    else
                    {
                        await UpdateFarmerProfileAsync(data.UserId, new FarmerUpdateModel
                        {
                            Name = data.Name,
                            PhoneNumber = data.PhoneNumber,
                            Email = data.Email,
                            Gender = data.Gender,
                            State = data.State,
                            Lga = data.Lga,
                            Location = data.Location,
                            Ward = data.Ward,
                            BVN = data.BVN,
                            NIN = data.NIN,
                            FarmerCategory = data.FarmerCategory,
                            HighestLevelOfEducation = data.HighestLevelOfEducation,
                            YearsInAgric = data.YearsInAgric,
                            DateOfBirth = data.DateOfBirth
                        });
                    }

                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
                    {
                        farmer = await _context.Farmers
                            .FirstOrDefaultAsync(f => f.UserId == data.UserId);
                    }

                    // ===========================
                    // ORGANIZATION
                    // ===========================
                    if (!string.IsNullOrEmpty(data.OrganizationCode))
                    {
                        var exists = await _context.FarmerOrganizations
                            .AnyAsync(x => x.FarmerId == farmer.Id &&
                                           x.OrganizationCode == data.OrganizationCode);

                        if (!exists)
                        {
                            await _context.FarmerOrganizations.AddAsync(new FarmerOrganization
                            {
                                FarmerId = farmer.Id,
                                OrganizationCode = data.OrganizationCode,
                                IsActive=true,
                                OnboardedByUserId = data.OnboardedByUserId
                            });
                        }
                    }

                    // ===========================
                    // COOPERATIVES (BULK SAFE)
                    // ===========================
                    if (data.CooperativeIds != null)
                    {
                        var existing = await _context.FarmerCooperatives
                            .Where(x => x.FarmerId == farmer.Id)
                            .ToListAsync();

                        var existingIds = existing.Select(x => x.CooperativeId).ToHashSet();
                        var incoming = data.CooperativeIds.ToHashSet();

                        var toAdd = incoming.Where(id => !existingIds.Contains(id));
                        var toRemove = existing.Where(e => !incoming.Contains(e.CooperativeId));

                        await _context.FarmerCooperatives.AddRangeAsync(
                            toAdd.Select(id => new FarmerCooperative
                            {
                                FarmerId = farmer.Id,
                                CooperativeId = id,
                                IsActive=true,
                                OnboardedByUserId = data.OnboardedByUserId,
                                JoinedOn = DateTimeOffset.UtcNow
                            }));

                        _context.FarmerCooperatives.RemoveRange(toRemove);
                    }

                    // ===========================
                    // AGENTS (BULK SAFE)
                    // ===========================
                    if (data.AgentIds != null)
                    {
                        var existing = await _context.FarmerAgents
                            .Where(x => x.FarmerId == farmer.Id)
                            .ToListAsync();

                        var existingIds = existing.Select(x => x.AgentId).ToHashSet();
                        var incoming = data.AgentIds.ToHashSet();

                        var toAdd = incoming.Where(id => !existingIds.Contains(id));
                        var toRemove = existing.Where(e => !incoming.Contains(e.AgentId));

                        await _context.FarmerAgents.AddRangeAsync(
                            toAdd.Select(id => new FarmerAgent
                            {
                                FarmerId = farmer.Id,
                                AgentId = id,
                                AssignedByUserId = data.OnboardedByUserId,
                                AssignedOn = DateTimeOffset.UtcNow,
                                IsActive=true,
                            }));

                        _context.FarmerAgents.RemoveRange(toRemove);
                    }

                    // ===========================
                    // VALUE CHAIN
                    // ===========================
                    if (data.SelectedItems != null)
                    {
                        await UpdateFarmerValueChainItems(farmer.Id, data.SelectedItems);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return (true, farmer.Map());
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        private bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            return ex.InnerException?.Message.Contains("IX_Farmers_UserId") == true;
        }
        //public async Task<(bool status, FarmerModel data)> CreateOrUpdateFarmer(FarmerModel data)
        //{
        //    var strategy = _context.Database.CreateExecutionStrategy();

        //    return await strategy.ExecuteAsync(async () =>
        //    {
        //        using var transaction = await _context.Database.BeginTransactionAsync();

        //        try
        //        {
        //            var farmer = await _context.Farmers
        //                .FirstOrDefaultAsync(f => f.UserId == data.UserId);

        //            if (farmer != null)
        //            {
        //                await UpdateFarmerProfileAsync(data.UserId, new FarmerUpdateModel
        //                {
        //                    DateOfBirth = data.DateOfBirth,
        //                    State = data.State,
        //                    Lga = data.Lga,
        //                    Name = data.Name,
        //                    PhoneNumber = data.PhoneNumber,
        //                    Email = data.Email,
        //                    Gender = data.Gender,
        //                    Location = data.Location,
        //                    Ward = data.Ward,
        //                    BVN = data.BVN,
        //                    NIN = data.NIN,
        //                    IsNinVerified = data.IsNinVerified,
        //                    FarmerCategory = data.FarmerCategory,
        //                    FarmerImage = data.FarmerImage,
        //                    HighestLevelOfEducation = data.HighestLevelOfEducation,
        //                    YearsInAgric = data.YearsInAgric
        //                });
        //            }
        //            else
        //            {
        //                farmer = data.Map();
        //                await _context.Farmers.AddAsync(farmer);
        //            }

        //            await _context.SaveChangesAsync();

        //            // ===========================
        //            // ORGANIZATION LINK
        //            // ===========================
        //            if (!string.IsNullOrEmpty(data.OrganizationCode))
        //            {
        //                bool exists = await _context.FarmerOrganizations
        //                    .AnyAsync(x => x.FarmerId == farmer.Id && x.OrganizationCode == data.OrganizationCode);

        //                if (!exists)
        //                {
        //                    await _context.FarmerOrganizations.AddAsync(new FarmerOrganization
        //                    {
        //                        FarmerId = farmer.Id,
        //                        OrganizationCode = data.OrganizationCode,
        //                        OnboardedByUserId = data.OnboardedByUserId
        //                    });
        //                }
        //            }

        //            // ===========================
        //            // COOPERATIVES
        //            // ===========================
        //            if (data.CooperativeIds != null)
        //            {
        //                var existing = await _context.FarmerCooperatives
        //                    .Where(x => x.FarmerId == farmer.Id)
        //                    .ToListAsync();

        //                var incoming = data.CooperativeIds.Distinct().ToList();

        //                var toAdd = incoming
        //                    .Where(id => !existing.Any(e => e.CooperativeId == id))
        //                    .ToList();

        //                var toRemove = existing
        //                    .Where(e => !incoming.Contains(e.CooperativeId))
        //                    .ToList();

        //                foreach (var id in toAdd)
        //                {
        //                    await _context.FarmerCooperatives.AddAsync(new FarmerCooperative
        //                    {
        //                        FarmerId = farmer.Id,
        //                        CooperativeId = id,
        //                        OnboardedByUserId = data.OnboardedByUserId,
        //                        JoinedOn = DateTimeOffset.Now
        //                    });
        //                }

        //                _context.FarmerCooperatives.RemoveRange(toRemove);
        //            }

        //            // ===========================
        //            // AGENTS
        //            // ===========================
        //            if (data.AgentIds != null)
        //            {
        //                var existing = await _context.FarmerAgents
        //                    .Where(x => x.FarmerId == farmer.Id)
        //                    .ToListAsync();

        //                var incoming = data.AgentIds.Distinct().ToList();

        //                var toAdd = incoming
        //                    .Where(id => !existing.Any(e => e.AgentId == id))
        //                    .ToList();

        //                var toRemove = existing
        //                    .Where(e => !incoming.Contains(e.AgentId))
        //                    .ToList();

        //                foreach (var id in toAdd)
        //                {
        //                    await _context.FarmerAgents.AddAsync(new FarmerAgent
        //                    {
        //                        FarmerId = farmer.Id,
        //                        AgentId = id,
        //                        AssignedByUserId = data.OnboardedByUserId,
        //                        AssignedOn = DateTimeOffset.Now
        //                    });
        //                }

        //                _context.FarmerAgents.RemoveRange(toRemove);
        //            }

        //            // ===========================
        //            // VALUE CHAIN
        //            // ===========================
        //            if (data.SelectedItems != null)
        //            {
        //                await UpdateFarmerValueChainItems(farmer.Id, data.SelectedItems);
        //            }

        //            await _context.SaveChangesAsync();
        //            await transaction.CommitAsync();

        //            return (true, farmer.Map());
        //        }
        //        catch
        //        {
        //            await transaction.RollbackAsync();
        //            throw;
        //        }
        //    });
        //}
       
        public async Task<FarmerModel> UpdateFarmerProfileAsync(string userId, Agrovesto.Backend.Core.Models.Businesses.FarmerUpdateModel update)
        {
            if (string.IsNullOrWhiteSpace(userId) || update == null)
                return null;

            var farmer = await _context.Farmers.FirstOrDefaultAsync(f => f.UserId == userId);
            if (farmer == null) return null;

            var isModified = false;

            if (update.Name != null && update.Name != farmer.Name) { farmer.Name = update.Name; isModified = true; }
            if (update.Location != null && update.Location != farmer.Location) { farmer.Location = update.Location; isModified = true; }
            if (update.State != null && update.State != farmer.State) { farmer.State = update.State; isModified = true; }
            if (update.Lga != null && update.Lga != farmer.Lga) { farmer.Lga = update.Lga; isModified = true; }
            if (update.Ward != null && update.Ward != farmer.Ward) { farmer.Ward = update.Ward; isModified = true; }
            if (update.PhoneNumber != null && update.PhoneNumber != farmer.PhoneNumber) { farmer.PhoneNumber = update.PhoneNumber; isModified = true; }
            if (update.Email != null && update.Email != farmer.Email) { farmer.Email = update.Email; isModified = true; }
            if (update.Gender != null && update.Gender != farmer.Gender) { farmer.Gender = update.Gender; isModified = true; }
            if (update.DateOfBirth != null && update.DateOfBirth != farmer.DateOfBirth) { farmer.DateOfBirth = update.DateOfBirth; isModified = true; }
            // Note: entity Farmer does not persist some DTO fields (FarmerImage, OrganizationCode, NIN, BVN, IsNinVerified).
            // Those are stored in other tables (FarmerImages, FarmerOrganizations, SubAccounts) and should be updated via their respective repositories/services.
            if (update.FarmerCategory != null && update.FarmerCategory != farmer.FarmerCategory) { farmer.FarmerCategory = update.FarmerCategory; isModified = true; }
            if (update.YearsInAgric.HasValue && update.YearsInAgric.Value != farmer.YearsInAgric) { farmer.YearsInAgric = update.YearsInAgric.Value; isModified = true; }
            if (update.HighestLevelOfEducation != null && update.HighestLevelOfEducation != farmer.HighestLevelOfEducation) { farmer.HighestLevelOfEducation = update.HighestLevelOfEducation; isModified = true; }
            if (update.SelectedItems != null)
            {
                await UpdateFarmerValueChainItems(farmer.Id, update.SelectedItems);
                isModified = true;
            }
            if (!string.IsNullOrEmpty(update.OrganizationCode))
            {
                bool exists = await _context.FarmerOrganizations.AnyAsync(x =>
                    x.FarmerId == farmer.Id &&
                    x.OrganizationCode == update.OrganizationCode);

                if (!exists)
                {
                    await _context.FarmerOrganizations.AddAsync(new FarmerOrganization
                    {
                        FarmerId = farmer.Id,
                        OrganizationCode = update.OrganizationCode,
                        OnboardedByUserId = farmer.OnboardedByUserId
                    });
                }
            }
            if (update.CooperativeIds != null)
            {
                var existing = _context.FarmerCooperatives.Where(fc => fc.FarmerId == farmer.Id);
                _context.FarmerCooperatives.RemoveRange(existing);
                foreach (var coopId in update.CooperativeIds.Distinct())
                {
                    await _context.FarmerCooperatives.AddAsync(new FarmerCooperative
                    {
                        FarmerId = farmer.Id,
                        CooperativeId = coopId,
                        OnboardedByUserId = farmer.OnboardedByUserId,
                        JoinedOn = DateTimeOffset.Now
                    });
                }
                isModified = true;
            }

            if (update.AgentIds != null)
            {
                var existingAgents = _context.FarmerAgents.Where(fa => fa.FarmerId == farmer.Id);
                _context.FarmerAgents.RemoveRange(existingAgents);
                foreach (var agentId in update.AgentIds.Distinct())
                {
                    await _context.FarmerAgents.AddAsync(new FarmerAgent
                    {
                        FarmerId = farmer.Id,
                        AgentId = agentId,
                        AssignedOn = DateTimeOffset.Now
                    });
                }
                isModified = true;
            }

            if (!string.IsNullOrWhiteSpace(update.NIN) || !string.IsNullOrWhiteSpace(update.BVN))
            {
                var subAccount = await _context.SubAccounts.FirstOrDefaultAsync(s => s.UserId == farmer.UserId);
                if (subAccount != null)
                {
                    if (!string.IsNullOrWhiteSpace(update.NIN) && update.NIN != subAccount.NIN)
                    {
                        subAccount.NIN = update.NIN;
                        _context.SubAccounts.Update(subAccount);
                        isModified = true;
                    }

                    if (!string.IsNullOrWhiteSpace(update.BVN) && update.BVN != subAccount.Bvn)
                    {
                        subAccount.Bvn = update.BVN;
                        _context.SubAccounts.Update(subAccount);
                        isModified = true;
                    }
                }
                else
                {
                    var newSub = new SubAccount
                    {
                        UserId = farmer.UserId,
                        NIN = update.NIN,
                        Bvn = update.BVN,
                        CreatedOn = DateTimeOffset.UtcNow
                    };
                    await _context.SubAccounts.AddAsync(newSub);
                    isModified = true;
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == farmer.UserId);
                if (user != null)
                {
                    if (!string.IsNullOrWhiteSpace(update.NIN) && update.NIN != user.NIN)
                    {
                        user.NIN = update.NIN;
                        _context.Users.Update(user);
                        isModified = true;
                    }

                    if (!string.IsNullOrWhiteSpace(update.BVN) && update.BVN != user.BVN)
                    {
                        user.BVN = update.BVN;
                        _context.Users.Update(user);
                        isModified = true;
                    }
                }
            }

            if (isModified)
            {
                _context.Farmers.Update(farmer);
                await _context.SaveChangesAsync();
            }

            return farmer.Map();
        }

        private async Task UpdateFarmerValueChainItems(long farmerId, List<ValueChainSelectionDto> valueChainSelections)
        {
            var existingItems = await _context.FarmerValueChainItems
                .Where(x => x.FarmerId == farmerId)
                .ToListAsync();

            var incomingItems = valueChainSelections
                .SelectMany(v => v.Items.Select(item => new { v.ValueChainType, ItemName = item }))
                .ToList();

            foreach (var incoming in incomingItems)
            {
                bool exists = existingItems.Any(e =>
                    e.ValueChainType == incoming.ValueChainType &&
                    e.ItemName == incoming.ItemName);

                if (!exists)
                {
                    _context.FarmerValueChainItems.Add(new FarmerValueChainItem
                    {
                        FarmerId = farmerId,
                        ValueChainType = incoming.ValueChainType,
                        ItemName = incoming.ItemName
                    });
                }
            }

            var toRemove = existingItems
                .Where(e => !incomingItems.Any(i =>
                    i.ValueChainType == e.ValueChainType &&
                    i.ItemName == e.ItemName))
                .ToList();

            _context.FarmerValueChainItems.RemoveRange(toRemove);
            await _context.SaveChangesAsync();
        }
        public async Task<PagedListResponse<List<OrganizationFarmerResponse>>> GetOrganizationFarmersAsync(
      ListQueryParams args)
        {
            if (string.IsNullOrWhiteSpace(args.organizationCode))
                throw new ValidationException("Organization code is required");

            args.PageNumber = args.PageNumber <= 0 ? 1 : args.PageNumber;
            args.PageSize = args.PageSize <= 0 ? 10 : args.PageSize;

            var skip = (args.PageNumber - 1) * args.PageSize;

            var completedQuery = from fo in _context.FarmerOrganizations.AsNoTracking()
                                 join f in _context.Farmers.AsNoTracking() on fo.FarmerId equals f.Id
                                 join u in _context.Users.AsNoTracking() on f.UserId equals u.Id
                                 where fo.OrganizationCode == args.organizationCode
                                       && fo.IsActive
                                 select new OrganizationFarmerResponse
                                 {
                                     FarmerId = f.Id,
                                     UserId = u.Id,
                                     Name = f.Name ?? u.Name,
                                     PhoneNumber = u.PhoneNumber,
                                     Email = u.Email,
                                     Gender = f.Gender,
                                     State = f.State,
                                     Lga = f.Lga,
                                     Ward = f.Ward,
                                     Location = f.Location,
                                     FarmerCategory = f.FarmerCategory,
                                     Level = EnumExtensions.GetDisplayName((FarmerLevel)f.Level),
                                     CreatedOn = f.OnBoardedOn == default ? u.CreatedOn : f.OnBoardedOn,
                                     FarmerOnboardingStatus = u.FarmerOnboardingStatus.ToString(),
                                     IsPendingOnboarding = false,
                                     IsActive=fo.IsActive
                                 };

            var pendingQuery = from u in _context.Users.AsNoTracking()
                               where u.UserType == UserType.Farmer.ToString()
                                     && u.FarmerOnboardingStatus != FarmerOnboardingStatus.Completed
                                     && u.CreatedBy == args.UserId
                               select new OrganizationFarmerResponse
                               {
                                   FarmerId = 0,
                                   UserId = u.Id,
                                   Name = u.Name,
                                   PhoneNumber = u.PhoneNumber,
                                   Email = u.Email,
                                   Gender = null,
                                   State = null,
                                   Lga = null,
                                   Ward = null,
                                   Location = null,
                                   FarmerCategory = null,
                                   Level = "Pending",
                                   CreatedOn = u.CreatedOn,
                                   FarmerOnboardingStatus = u.FarmerOnboardingStatus.ToString(),
                                   IsPendingOnboarding = true,
                                   
                               };

            var completedUserIdsQuery =
                from fo in _context.FarmerOrganizations.AsNoTracking()
                join f in _context.Farmers.AsNoTracking() on fo.FarmerId equals f.Id
                join u in _context.Users.AsNoTracking() on f.UserId equals u.Id
                where fo.OrganizationCode == args.organizationCode && fo.IsActive
                select u.Id;

            var pendingUserIdsQuery = _context.Users
                .AsNoTracking()
                .Where(x =>
                    x.UserType == UserType.Farmer.ToString() &&
                    x.FarmerOnboardingStatus != FarmerOnboardingStatus.Completed &&
                    x.CreatedBy == args.UserId)
                .Select(x => x.Id);

            var completedUserIdsDistinct = await completedUserIdsQuery
                .Distinct()
                .ToListAsync();

            var pendingUserIdsDistinct = await pendingUserIdsQuery
                .Distinct()
                .ToListAsync();

            var pendingOnlyCount = pendingUserIdsDistinct
                .Where(id => !completedUserIdsDistinct.Contains(id))
                .Count();

            var totalCount = completedUserIdsDistinct.Count + pendingOnlyCount;

          
            var lightweightUnion = completedQuery
                .Select(x => new { x.UserId, x.CreatedOn })
                .Concat(pendingQuery.Select(x => new { x.UserId, x.CreatedOn }));

            var pageUserIdsWithDate = await lightweightUnion
                .Where(x => !string.IsNullOrWhiteSpace(x.UserId))
                .GroupBy(x => x.UserId)
                .Select(g => new { UserId = g.Key, LatestCreatedOn = g.Max(x => x.CreatedOn) })
                .OrderByDescending(x => x.LatestCreatedOn)
                .Skip(skip)
                .Take(args.PageSize)
                .ToListAsync();

            var pageUserIds = pageUserIdsWithDate.Select(x => x.UserId).ToList();

            var completedPage = pageUserIds.Any()
                ? await completedQuery.Where(x => pageUserIds.Contains(x.UserId)).ToListAsync()
                : new List<OrganizationFarmerResponse>();

            var pendingPage = pageUserIds.Any()
                ? await pendingQuery.Where(x => pageUserIds.Contains(x.UserId)).ToListAsync()
                : new List<OrganizationFarmerResponse>();

            var mergedDict = completedPage.Concat(pendingPage)
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedOn).First());

            var pagedData = pageUserIdsWithDate
                .Select(p => mergedDict.GetValueOrDefault(p.UserId))
                .Where(x => x != null)
                .ToList();
            var farmerIds = pagedData
                .Where(x => x.FarmerId > 0)
                .Select(x => x.FarmerId.Value)
                .Distinct()
                .ToList();

            Dictionary<long, int> agentCounts = new();
            Dictionary<long, int> membershipCounts = new();
            Dictionary<long, double> RepaymentPercentage = new();

            if (farmerIds.Any())
            {
                   agentCounts = await _context.FarmerAgents
                    .AsNoTracking()
                    .Where(x => farmerIds.Contains(x.FarmerId))
                    .GroupBy(x => x.FarmerId)
                    .Select(g => new { g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Key, x => x.Count);

                membershipCounts = await _context.FarmerCooperatives
                    .AsNoTracking()
                    .Where(x => farmerIds.Contains(x.FarmerId))
                    .GroupBy(x => x.FarmerId)
                    .Select(g => new { g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Key, x => x.Count);
                RepaymentPercentage = await _context.Repayments
       .AsNoTracking()
       .Where(x => farmerIds.Contains(x.FarmerId))
       .GroupBy(x => x.FarmerId)
       .Select(g => new
       {
           FarmerId = g.Key,
           Percentage = g.Count(x => x.IsComplete) * 100.0 / g.Count()
       })
       .ToDictionaryAsync(x => x.FarmerId, x => x.Percentage);
             
            }

            var result = pagedData.Select(x => new OrganizationFarmerResponse
            {
                IsActive=x.IsActive,
                FarmerId = x.FarmerId,
                UserId = x.UserId,
                Name = x.Name,
                PhoneNumber = x.PhoneNumber,
                Email = x.Email,
                Gender = x.Gender,
                State = x.State,
                Lga = x.Lga,
                Ward = x.Ward,
                Location = x.Location,
                FarmerCategory = x.FarmerCategory,
                Level = x.Level,
                CreatedOn = x.CreatedOn,
                FarmerOnboardingStatus = x.FarmerOnboardingStatus,
                IsPendingOnboarding = x.IsPendingOnboarding,
                AgentCount = x.FarmerId > 0 && agentCounts.TryGetValue(x.FarmerId.Value, out var a)
                    ? a
                    : 0,
                MembershipCount = x.FarmerId > 0 && membershipCounts.TryGetValue(x.FarmerId.Value, out var m)
                    ? m
                    : 0,
                RepaymentPercentage = x.FarmerId > 0 && RepaymentPercentage.TryGetValue(x.FarmerId.Value, out var r)
                    ? r
                    : 0
            }).ToList();

            return new PagedListResponse<List<OrganizationFarmerResponse>>
            {
                PageNumber = args.PageNumber,
                PageSize = args.PageSize,
                Count = totalCount,
                Data = result
            };
        }
        //public async Task<PagedListResponse<List<OrganizationFarmerResponse>>> GetOrganizationFarmersAsync(
        //    ListQueryParams args)
        //{
        //    if (string.IsNullOrWhiteSpace(args.organizationCode))
        //        throw new ValidationException("Organization code is required");

        //    args.PageNumber = args.PageNumber <= 0 ? 1 : args.PageNumber;
        //    args.PageSize = args.PageSize <= 0 ? 10 : args.PageSize;

        //    var completedQuery =
        //        from fo in _context.FarmerOrganizations
        //        join f in _context.Farmers on fo.FarmerId equals f.Id
        //        join u in _context.Users on f.UserId equals u.Id
        //        where fo.OrganizationCode == args.organizationCode
        //              && fo.IsActive
        //        select new
        //        {
        //            UserId = u.Id,
        //            FarmerId = (long?)f.Id,
        //            FullName = u.Name,
        //            PhoneNumber = u.PhoneNumber,
        //            Email = u.Email,
        //            CreatedOn = u.CreatedOn,
        //            FarmerOnboardingStatus = u.FarmerOnboardingStatus,
        //            Name = f.Name,
        //            Gender = f.Gender,
        //            State = f.State,
        //            Lga = f.Lga,
        //            Ward = f.Ward,
        //            Location = f.Location,
        //            FarmerCategory = f.FarmerCategory,
        //            Level = (int?)f.Level,
        //            FarmerCreatedOn = (DateTimeOffset?)f.OnBoardedOn,
        //            IsPending = false
        //        };

        //    var pendingQuery =
        //        from u in _context.Users
        //        where u.UserType == UserType.Farmer.ToString()
        //              && u.FarmerOnboardingStatus != FarmerOnboardingStatus.Completed
        //              && u.CreatedBy == args.UserId // adjust if needed
        //        select new
        //        {
        //            UserId = u.Id,
        //            FarmerId = (long?)null,
        //            FullName = u.Name,
        //            PhoneNumber = u.PhoneNumber,
        //            Email = u.Email,
        //            CreatedOn = u.CreatedOn,
        //            FarmerOnboardingStatus = u.FarmerOnboardingStatus,
        //            Name = (string)null,
        //            Gender = (string)null,
        //            State = (string)null,
        //            Lga = (string)null,
        //            Ward = (string)null,
        //            Location = (string)null,
        //            FarmerCategory = (string)null,
        //            Level = (int?)null,
        //            FarmerCreatedOn = (DateTimeOffset?)null,
        //            IsPending = true
        //        };

        //    var unifiedQuery = completedQuery.Concat(pendingQuery);

        //    var totalCount = await unifiedQuery.CountAsync();

        //    var pagedData = await unifiedQuery
        //        .OrderByDescending(x => x.FarmerCreatedOn ?? x.CreatedOn)
        //        .Skip((args.PageNumber - 1) * args.PageSize)
        //        .Take(args.PageSize)
        //        .ToListAsync();

        //    var farmerIds = pagedData
        //        .Where(x => x.FarmerId.HasValue)
        //        .Select(x => x.FarmerId.Value)
        //        .ToList();

        //    var agentCounts = await _context.FarmerAgents
        //        .Where(x => farmerIds.Contains(x.FarmerId))
        //        .GroupBy(x => x.FarmerId)
        //        .Select(g => new { g.Key, Count = g.Count() })
        //        .ToDictionaryAsync(x => x.Key, x => x.Count);

        //    var membershipCounts = await _context.FarmerCooperatives
        //        .Where(x => farmerIds.Contains(x.FarmerId))
        //        .GroupBy(x => x.FarmerId)
        //        .Select(g => new { g.Key, Count = g.Count() })
        //        .ToDictionaryAsync(x => x.Key, x => x.Count);

        //    var result = pagedData.Select(x =>
        //    {
        //        var isPending = x.FarmerId == null;

        //        return new OrganizationFarmerResponse
        //        {
        //            FarmerId = x.FarmerId ?? 0,
        //            UserId = x.UserId,
        //            Name = x.Name ?? x.FullName,
        //            PhoneNumber = x.PhoneNumber,
        //            Email = x.Email,
        //            Gender = x.Gender,
        //            State = x.State,
        //            Lga = x.Lga,
        //            Ward = x.Ward,
        //            Location = x.Location,
        //            FarmerCategory = x.FarmerCategory,
        //            Level = x.Level.HasValue
        //                ? EnumExtensions.GetDisplayName((FarmerLevel)x.Level.Value)
        //                : "Pending",
        //            CreatedOn = x.FarmerCreatedOn ?? x.CreatedOn,
        //            AgentCount = x.FarmerId.HasValue && agentCounts.ContainsKey(x.FarmerId.Value)
        //                ? agentCounts[x.FarmerId.Value]
        //                : 0,
        //            MembershipCount = x.FarmerId.HasValue && membershipCounts.ContainsKey(x.FarmerId.Value)
        //                ? membershipCounts[x.FarmerId.Value]
        //                : 0,
        //            FarmerOnboardingStatus = x.FarmerOnboardingStatus.ToString(),
        //            IsPendingOnboarding = isPending
        //        };
        //    }).ToList();

        //    return new PagedListResponse<List<OrganizationFarmerResponse>>
        //    {
        //        PageNumber = args.PageNumber,
        //        PageSize = args.PageSize,
        //        Count = totalCount,
        //        Data = result
        //    };
        //}



        //    public async Task<PagedListResponse<List<OrganizationFarmerResponse>>> GetOrganizationFarmersAsync(
        //ListQueryParams args)
        //    {
        //        if (string.IsNullOrWhiteSpace(args.organizationCode))
        //            throw new ValidationException("Organization code is required");

        //        args.PageNumber = args.PageNumber <= 0 ? 1 : args.PageNumber;
        //        args.PageSize = args.PageSize <= 0 ? 10 : args.PageSize;

        //        var query = from fo in _context.FarmerOrganizations
        //                    join f in _context.Farmers on fo.FarmerId equals f.Id
        //                    where fo.OrganizationCode == args.organizationCode
        //                    select f;

        //        var totalCount = await query.CountAsync();

        //        var farmers = await query
        //            .OrderByDescending(x => x.Id)
        //            .Skip((args.PageNumber - 1) * args.PageSize)
        //            .Take(args.PageSize)
        //            .ToListAsync();

        //        var farmerIds = farmers.Select(f => f.Id).ToList();

        //        var agentCounts = await _context.FarmerAgents
        //            .Where(x => farmerIds.Contains(x.FarmerId))
        //            .GroupBy(x => x.FarmerId)
        //            .Select(g => new
        //            {
        //                FarmerId = g.Key,
        //                Count = g.Count()
        //            })
        //            .ToDictionaryAsync(x => x.FarmerId, x => x.Count);

        //        var membershipCounts = await _context.FarmerCooperatives
        //            .Where(x => farmerIds.Contains(x.FarmerId))
        //            .GroupBy(x => x.FarmerId)
        //            .Select(g => new
        //            {
        //                FarmerId = g.Key,
        //                Count = g.Count()
        //            })
        //            .ToDictionaryAsync(x => x.FarmerId, x => x.Count);

        //        var result = farmers.Select(f => new OrganizationFarmerResponse
        //        {
        //            FarmerId = f.Id,
        //            UserId = f.UserId,
        //            Name = f.Name,
        //            PhoneNumber = f.PhoneNumber,
        //            Email = f.Email,
        //            Gender = f.Gender,
        //            State = f.State,
        //            Lga = f.Lga,
        //            Ward = f.Ward,
        //            Location = f.Location,
        //            FarmerCategory = f.FarmerCategory,
        //            Level = EnumExtensions.GetDisplayName(f.Level),
        //            CreatedOn = f.OnBoardedOn,

        //            AgentCount = agentCounts.ContainsKey(f.Id)
        //                ? agentCounts[f.Id]
        //                : 0,

        //            MembershipCount = membershipCounts.ContainsKey(f.Id)
        //                ? membershipCounts[f.Id]
        //                : 0
        //        }).ToList();
        //        //return new PagedListResponse<List<OrganizationFarmerResponse>>
        //        //{
        //        //    PageNumber = args.PageNumber,
        //        //    PageSize = args.PageSize,
        //        //    Count = totalCount,
        //        //    Data = result
        //        //};
        //        return new PagedListResponse<List<OrganizationFarmerResponse>>
        //        {
        //            PageNumber = args.PageNumber,
        //            PageSize = args.PageSize,
        //            Count = totalCount,

        //            Data = result
        //        };
        //    }

        public async Task<OrganizationFarmerDetailResponse> GetFarmerDetailsAsync(
  ListQueryParams query)
        {
            if (string.IsNullOrWhiteSpace(query.organizationCode))
                throw new ValidationException("Organization code is required");

            // ===========================
            // VALIDATE FARMER IN ORG
            // ===========================
            var farmer = await (from fo in _context.FarmerOrganizations
                                join f in _context.Farmers on fo.FarmerId equals f.Id
                                where fo.OrganizationCode == query.organizationCode
                                      && f.Id == query.Id
                                select f)
                                .FirstOrDefaultAsync();

            if (farmer == null)
                throw new NotFoundException("Farmer not found in this organization");

            var valueChainItems = await _context.FarmerValueChainItems
       .Where(x => x.FarmerId == query.Id)
       .Select(x => new FarmerValueChainItemDto
       {
           ValueChainType = x.ValueChainType,
           ItemName = x.ItemName
       })
       .ToListAsync();

            var farmsQuery = _context.Farms
                .Where(f => f.FarmerId == query.Id);
            var farms = await farmsQuery.ToListAsync();

           
            var agents = await (from fa in _context.FarmerAgents
                                join a in _context.Agents on fa.AgentId equals a.Id
                                where fa.FarmerId == query.Id
                                select new OrganizationFarmerAgentDto
                                {
                                    AgentId = a.Id,
                                    AgentName = a.Name,
                                    AgentUserId = a.UserId,
                                    AssignedOn = fa.AssignedOn,
                                    Active= a.IsActive
                                })
                                .ToListAsync();

            var memberships = await (from fc in _context.FarmerCooperatives
                                     join c in _context.MemberShips on fc.CooperativeId equals c.Id
                                     where fc.FarmerId == query.Id
                                     select new OrganizationFarmerCooperativeDto
                                     {
                                         MembershipId = c.Id,
                                         MembershipName = c.Name,
                                         JoinedOn = fc.JoinedOn,
                                         IsActive=c.IsActive
                                     })
                                     .ToListAsync();

            return new OrganizationFarmerDetailResponse
            {
                FarmerId = farmer.Id,
                UserId = farmer.UserId,
                Name = farmer.Name,
                PhoneNumber = farmer.PhoneNumber,
                Email = farmer.Email,
                Gender = farmer.Gender,
                State = farmer.State,
                Lga = farmer.Lga,
                Ward = farmer.Ward,
                Location = farmer.Location,
                FarmerCategory = farmer.FarmerCategory,
                Level = EnumExtensions.GetDisplayName(farmer.Level),
                CreatedOn = farmer.OnBoardedOn,
                YearsInAgric=farmer.YearsInAgric,
                FarmSize=farms.Sum(x=>x.FarmSize),
                ValueChainItems = valueChainItems,
                Agents = agents,
                Memberships = memberships
            };
        }
        public async Task<OrganizationFarmerDashboardResponse> GetOrganizationFarmerDashboardAsync(ListQueryParams args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            if (string.IsNullOrWhiteSpace(args.organizationCode))
                throw new ValidationException("Invalid Organization");

            var result = await _context.Database
    .SqlQueryRaw<OrganizationFarmerDashboardResponse>(
@"
SELECT
    -- Total Farmers (MATCHES WORKING QUERY)
    (SELECT COUNT(DISTINCT f.Id)
     FROM FarmerOrganizations fo
     INNER JOIN Farmers f ON fo.FarmerId = f.Id
     INNER JOIN AspNetUsers u ON f.UserId = u.Id
     WHERE fo.OrganizationCode = @orgCode
       ) AS TotalFarmers,

    -- Active Farmers
    (SELECT COUNT(DISTINCT f.Id)
     FROM FarmerOrganizations fo
     INNER JOIN Farmers f ON fo.FarmerId = f.Id
     INNER JOIN AspNetUsers u ON f.UserId = u.Id
     WHERE fo.OrganizationCode = @orgCode
       AND fo.IsActive = 1) AS ActiveFarmers,

    0 AS FarmersWithLoans,
    CAST(0 AS decimal(18,2)) AS RecoveryRate
",
    new SqlParameter("@orgCode", args.organizationCode)
)
.FirstOrDefaultAsync();

            return result ?? new OrganizationFarmerDashboardResponse();
        }
        /// <summary>
        /// TODO: REVERT
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        //public async Task<OrganizationFarmerDashboardResponse> GetOrganizationFarmerDashboardAsync(ListQueryParams args)
        //{
        //    if (args == null)
        //        throw new ArgumentNullException(nameof(args));

        //    if (string.IsNullOrWhiteSpace(args.organizationCode))
        //        throw new ValidationException("Invalid Organization");

        //    try
        //    {
        //        var cacheKey = $"organization-farmer-dashboard:{args.organizationCode}";

        //        string cachedData = null;
        //        try
        //        {
        //            if (_cache != null)
        //                cachedData = await _cache.GetStringAsync(cacheKey);
        //        }
        //        catch (Exception ex)
        //        {
        //            // Redis/Distributed cache timeout or connection issue — degrade gracefully
        //            try { Console.WriteLine($"Cache read failed for {cacheKey}: {ex.Message}"); } catch { }
        //            cachedData = null;
        //        }

        //        if (!string.IsNullOrEmpty(cachedData))
        //        {
        //            return JsonSerializer.Deserialize<OrganizationFarmerDashboardResponse>(cachedData);
        //        }
        //        // ✅ Base Query (VERY IMPORTANT: reuse this)
        //        var farmerOrgQuery = _context.FarmerOrganizations
        //            .AsNoTracking()
        //            .Where(x => x.OrganizationCode == args.organizationCode && x.IsActive);

        //        // ✅ TOTAL FARMERS
        //        var totalFarmers = await farmerOrgQuery
        //            .Select(x => x.FarmerId)
        //            .Distinct()
        //            .CountAsync();

        //        // ✅ ACTIVE FARMERS (Completed onboarding)
        //        var activeFarmers = await (
        //            from fo in farmerOrgQuery
        //            join f in _context.Farmers on fo.FarmerId equals f.Id
        //            join u in _context.Users on f.UserId equals u.Id
        //            where u.FarmerOnboardingStatus == FarmerOnboardingStatus.Completed
        //            select f.Id
        //        ).Distinct().CountAsync();

        //        // ✅ FARMERS WITH LOANS
        //        //var farmersWithLoans = await (
        //        //    from fo in farmerOrgQuery
        //        //    join l in _context.InputAllocations on fo.FarmerId equals l.FarmerId
        //        //    where l.IsActive
        //        //    select l.FarmerId
        //        //).Distinct().CountAsync();

        //        //// ✅ RECOVERY RATE
        //        //// Assumption: LoanRepayments table exists
        //        //var totalLoanAmount = await (
        //        //    from fo in farmerOrgQuery
        //        //    join l in _context.InputAllocations on fo.FarmerId equals l.FarmerId
        //        //    where l.IsActive
        //        //    select l.Amount
        //        //).SumAsync() ?? 0;

        //        //var totalRecoveredAmount = await (
        //        //    from fo in farmerOrgQuery
        //        //    join r in _context.Repayments on fo.FarmerId equals r.FarmerId
        //        //    where r.IsSuccessful
        //        //    select r.Amount
        //        //).SumAsync() ?? 0;

        //        //var recoveryRate = totalLoanAmount == 0
        //        //    ? 0
        //        //    : (totalRecoveredAmount / totalLoanAmount) * 100;

        //        var result= new OrganizationFarmerDashboardResponse
        //        {
        //            TotalFarmers = totalFarmers,
        //            ActiveFarmers = activeFarmers,
        //            FarmersWithLoans = 0,//farmersWithLoans,
        //            RecoveryRate = 0//Math.Round(recoveryRate, 2)
        //        };
        //        var cacheOptions = new DistributedCacheEntryOptions
        //        {
        //            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        //        };

        //        var serialized = JsonSerializer.Serialize(result);

        //        try
        //        {
        //            if (_cache != null)
        //                await _cache.SetStringAsync(cacheKey, serialized, cacheOptions);
        //        }
        //        catch (Exception ex)
        //        {
        //            // Swallow cache set exceptions to avoid failing the request when Redis is down
        //            try { Console.WriteLine($"Cache write failed for {cacheKey}: {ex.Message}"); } catch { }
        //        }

        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception("Error retrieving organization farmer dashboard", ex);
        //    }
        //}

        public async Task<OrganizationFarmerSalesSummary> GetOrganizationFarmerSalesSummary(ListQueryParams args)
        {
            var query = _context.FarmerSalesTracker
                .Where(x => x.FarmerUserId == args.UserId)
                .AsNoTracking();

            var summary = await query
                .GroupBy(x => 1)
                .Select(g => new OrganizationFarmerSalesSummary
                {
                    TotalVolumeSold = g.Sum(x => (decimal?)x.QuantitySoldToday) ?? 0,
                    TotalRevenue = g.Sum(x => (decimal?)x.TotalPriceSold) ?? 0,
                    AverageSalesValue = g.Average(x => (decimal?)x.TotalPriceSold) ?? 0
                })
                .FirstOrDefaultAsync();

            return summary ?? new OrganizationFarmerSalesSummary
            {
                TotalVolumeSold = 0,
                TotalRevenue = 0,
                AverageSalesValue = 0
            };
        }

        
        #endregion FarmerMgt
    }
}
