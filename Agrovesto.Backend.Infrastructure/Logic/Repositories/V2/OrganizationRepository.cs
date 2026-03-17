using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories.V2
{
    public interface IOrganizationRepository
    {
        Task<OrganizationLocationModel> CreateLocationAsync(OrganizationLocationModel model);
        Task<OrganizationRoleModel> CreateRoleAsync(OrganizationRoleModel model);
        Task<PagedListResponse<List<OrganizationAgentActivityLog>>> GetAgentActivityLogs(ListQueryParams args);
        Task<List<OrganizationAgentLocationResponseModel>> GetAgentAssignedLocations(ListQueryParams query);
        Task<PagedListResponse<List<OrganizationAgentInputAllocationModel>>> GetAgentInputAllocations(ListQueryParams args);
        Task<PagedListResponse<List<OrganizationAgentSubscriberModel>>> GetAgentSubscribers(ListQueryParams args);
        Task<PagedListResponse<List<OrganizationAgentTransactionHistory>>> GetAgentTransactionHistory(ListQueryParams args);
        Task<List<OrganizationLocationResponseModel>> GetLocationAsync(ListQueryParams query);
        Task<OrganizationLocationModel> GetLocationByIAsync(long Id);
        Task<OrganizationAgentDetailResponse> GetOrganizationAgentDetail(ListQueryParams args);
        Task<OrganizationAgentPerformanceSummary> GetOrganizationAgentPerformanceSummary(ListQueryParams args);
        Task<PagedListResponse<List<OrganizationAgentResponse>>> GetOrganizationAgents(ListQueryParams args);
        Task<OrganizationRoleModel> GetRoleByIAsync(long Id);
        Task<List<OrganizationRoleModel>> GetRolesAsync(ListQueryParams query);
        Task<bool> ReassignAgentToLocation(OrganizationAgentArgs data);
    }
    public class OrganizationRepository : IOrganizationRepository
    {
        private readonly DataContext _context;
        public OrganizationRepository(DataContext context)
        {
            _context = context;
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
            var subscribers = 10; //replace when there is data.

            var inputs = 4320;//replace when there is data

            var totalEquityPaid = 8450000;// replace when there is data
            var productSold = 15900000;//replace when there is data
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

            var subscribers = new List<OrganizationAgentSubscriberModel>
    {
        new OrganizationAgentSubscriberModel
        {
            Id = 1,
            FarmerName = $"Farmer {args.Id }-A",
            FarmerUserId = $"FRM{args.Id }01",
            InputPackage = "Maize Package",
            InputPackageId = 101,
            EquityPaid = 10000 * factor,
            Status = true
        },
        new OrganizationAgentSubscriberModel
        {
            Id = 2,
            FarmerName = $"Farmer {args.Id }-B",
            FarmerUserId = $"FRM{args.Id }02",
            InputPackage = "Rice Package",
            InputPackageId = 102,
            EquityPaid = 15000 * factor,
            Status = factor % 2 == 0
        },
        new OrganizationAgentSubscriberModel
        {
            Id = 3,
            FarmerName = $"Farmer {args.Id }-C",
            FarmerUserId = $"FRM{args.Id }03",
            InputPackage = "Cassava Package",
            InputPackageId = 103,
            EquityPaid = 8000 * factor,
            Status = true
        }
    };


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

            var data = new List<OrganizationAgentInputAllocationModel>
    {
        new OrganizationAgentInputAllocationModel
        {
            Id = 1,
            InputName = "Fertilizer",
            Allocated = 100 * baseValue,
            Distributed = 60 * baseValue,
        },
        new OrganizationAgentInputAllocationModel
        {
            Id = 2,
            InputName = "Seeds",
            Allocated = 80 * baseValue,
            Distributed = 30 * baseValue,
        },
        new OrganizationAgentInputAllocationModel
        {
            Id = 3,
            InputName = "Pesticides",
            Allocated = 50 * baseValue,
            Distributed = 20 * baseValue,
        },
        new OrganizationAgentInputAllocationModel
        {
            Id = 4,
            InputName = "Herbicides",
            Allocated = 30 * baseValue,
            Distributed = 10 * baseValue,
        }
    };

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
        public async Task<PagedListResponse<List<OrganizationAgentResponse>>> GetOrganizationAgents(ListQueryParams args)
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
                    Subscribers=10, //update when you have subscribers 
                    InputDisbursed= 4320, // update when you have data
                    EquityPaid=8900000, // update when you have data
                    ProductSold= 14200 //update when you have data

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
        public async Task<OrganizationLocationModel> CreateLocationAsync(OrganizationLocationModel model)
        {
            var orgExist = await _context.OrganizationLocations
              .FirstOrDefaultAsync(x => x.LocationName == model.LocationName);

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
            var org = await _context.Set<OrganizationLocation>()
                .FirstOrDefaultAsync(x => x.Id == Id);
            if (org == null)
            {
                return null;
            }
            return org.ViewMap();
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
    }
}
