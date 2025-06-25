using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories
{
    public class FarmerRepository : IFarmerRepository
    {
        private readonly DataContext _context;
        public FarmerRepository(DataContext context)
        {
            _context = context;
        }
        #region report
        public async Task<List<FarmerModel>> FilterFarmerForDownload(
   string? businessCode,
   string? organizationCode,
    string? cooperativeName,
    string? state,
    DateTime? startDate,
    DateTime? endDate)
        {
            var query = _context.Farmers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(businessCode))
            {
                query = query.Where(x => x.BelongToBusinessCode == businessCode);
            }
            if (!string.IsNullOrWhiteSpace(organizationCode))
            {
                query = query.Where(x => x.OrganizationCode == organizationCode);
            }

            if (!string.IsNullOrWhiteSpace(state))
            {
                query = query.Where(x => x.State == state);
            }

            if (startDate.HasValue)
            {
                query = query.Where(x => x.OnBoardedOn >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(x => x.OnBoardedOn <= endDate.Value);
            }

            var farmers = await query.ToListAsync();

            if (farmers == null || !farmers.Any())
            {
                throw new Exception("No record found");
            }

            var cooperativeIds = farmers.Select(f => f.BelongToCooperativeId).Distinct().ToList();
            var cooperatives = await _context.Cooperatives
                .Where(c => cooperativeIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name);

            if (!string.IsNullOrWhiteSpace(cooperativeName))
            {
                farmers = farmers
                    .Where(f => cooperatives.TryGetValue(f.BelongToCooperativeId, out var name)
                                && name.Equals(cooperativeName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return farmers.Select(farmer =>
            {
                var mappedFarmer = farmer.Map();

                if (cooperatives.TryGetValue(farmer.BelongToCooperativeId, out var coopName))
                {
                    mappedFarmer.CooperativeName = coopName;
                }

                return mappedFarmer;
            }).ToList();
        }

        #endregion report
        #region agent
        public async Task<BusinessEntitiesMetrics> GetAgentEntitiesMetricsAsync(string businessCode,

   string? organizationCode,
            string userId)
        {
            var currentMonth = DateTime.UtcNow.Month;
            var previousMonth = DateTime.UtcNow.AddMonths(-1).Month;
            var currentYear = DateTime.UtcNow.Year;
            var previousYear = DateTime.UtcNow.AddMonths(-1).Year;

            double percentageChange = await CalculateFarmerPercentagePerformance(businessCode, organizationCode, currentMonth, previousMonth, currentYear, previousYear);
            double cooperativePercentageChange = await CalculateCooperativePercentagePerformance(businessCode, organizationCode, currentMonth, previousMonth, currentYear, previousYear);
            double agentPercentageChange = await CalculateAgentPercentagePerformance(businessCode, organizationCode, currentMonth, previousMonth, currentYear, previousYear);

            var metrics = new BusinessEntitiesMetrics

            {

                //  TotalCooperative = await _context.Cooperatives.CountAsync(c => c.BelongToBusinessCode == businessCode && c.IsAssignedToAgentId ==),
                TotalFarmer = await _context.Farmers.CountAsync(f => f.BelongToBusinessCode == businessCode && f.OnboardedByUserId == userId),
                PendingFarmUpdate = await _context.FarmPlotDetails.CountAsync(f => f.CoordinateTakenByUserId == userId),

                TotalWarehouse = await _context.Warehouses.CountAsync(w => w.BelongToBusinessCode == businessCode),
                PercentageFarmerPerformance = percentageChange,
                PercentageCooperativePerformance = cooperativePercentageChange,
                PercentageAgentPerformance = agentPercentageChange,
            };

            return metrics;
        }

        #endregion agent


        public async Task<FarmerModel> AssignFarmerToCooperative(long farmerId, long cooperativeId)
        {
            var farmer = await _context.Farmers.Where(c => c.Id == farmerId).FirstOrDefaultAsync();
            if (farmer == null) return null;
            farmer.BelongToCooperativeId = cooperativeId;

            await _context.SaveChangesAsync();

            return farmer.Map();
        }

        public async Task<BusinessEntitiesMetrics> GetBusinessEntitiesMetricsAsyncByAgent(string agentId, ListQueryParams query)
        {

            var farmers = await GetFarmersByAgent(agentId, query);

            var currentMonth = DateTime.UtcNow.Month;
            var previousMonth = DateTime.UtcNow.AddMonths(-1).Month;
            var currentYear = DateTime.UtcNow.Year;
            var previousYear = DateTime.UtcNow.AddMonths(-1).Year;

            double percentageChange = await CalculateFarmerPercentagePerformanceByAgent(agentId, currentMonth, previousMonth, currentYear, previousYear);
            double cooperativePercentageChange = await CalculateCooperativePercentagePerformanceByAgent(agentId, currentMonth, previousMonth, currentYear, previousYear);
            // double salesTrackedPerformance = await CalculateAgentPercentagePerformance(businessCode, currentMonth, previousMonth, currentYear, previousYear);


            var metrics = new BusinessEntitiesMetrics

            {
                TotalCooperative = await _context.Cooperatives.CountAsync(c =>
                 (!string.IsNullOrEmpty(query.organizationCode) && (c.OrganizationCode.Contains(query.organizationCode)))
           && c.BelongToBusinessCode == query.businessCode && c.IsAssignedToAgentId == agentId),
                TotalFarmer = farmers.Count,
                TotalWarehouse = await _context.Warehouses.CountAsync(w =>
                 (!string.IsNullOrEmpty(query.organizationCode) && (w.OrganizationCode.Contains(query.organizationCode)))
           &&
                w.BelongToBusinessCode == query.businessCode && w.ManagedByUserId == agentId),
                PercentageFarmerPerformance = percentageChange,
                PercentageCooperativePerformance = cooperativePercentageChange,
                //PercentageAgentPerformance = salesTrackedPerformance,
            };

            return metrics;
        }



        private async Task<double> CalculateCooperativePercentagePerformanceByAgent(string agentId, int currentMonth, int previousMonth, int currentYear, int previousYear)
        {
            var totalCooperativesThisMonth = await _context.Cooperatives
                .CountAsync(f => f.IsAssignedToAgentId == agentId && f.OnBoardedOn.Year == currentYear && f.OnBoardedOn.Month == currentMonth);

            var totalCooperativesLastMonth = await _context.Cooperatives
                .CountAsync(f => f.IsAssignedToAgentId == agentId && f.OnBoardedOn.Year == previousYear && f.OnBoardedOn.Month == previousMonth);

            double percentageChange = totalCooperativesLastMonth == 0
                ? (totalCooperativesThisMonth > 0 ? 100 : 0)
                : ((double)(totalCooperativesThisMonth - totalCooperativesLastMonth) / totalCooperativesLastMonth) * 100;
            return percentageChange;
        }

        private async Task<double> CalculateFarmerPercentagePerformanceByAgent(string agentId, int currentMonth, int previousMonth, int currentYear, int previousYear)
        {
            var totalFarmersThisMonth = await _context.Farmers
                .CountAsync(f => f.OnboardedByUserId == agentId && f.OnBoardedOn.Year == currentYear && f.OnBoardedOn.Month == currentMonth);

            var totalFarmersLastMonth = await _context.Farmers
                .CountAsync(f => f.OnboardedByUserId == agentId && f.OnBoardedOn.Year == previousYear && f.OnBoardedOn.Month == previousMonth);

            double percentageChange = totalFarmersLastMonth == 0
                ? (totalFarmersThisMonth > 0 ? 100 : 0)
                : ((double)(totalFarmersThisMonth - totalFarmersLastMonth) / totalFarmersLastMonth) * 100;
            return percentageChange;
        }

        public async Task<BusinessEntitiesMetrics> GetBusinessEntitiesMetricsAsync(string businessCode, string? organizationCode)
        {
            var currentMonth = DateTime.UtcNow.Month;
            var previousMonth = DateTime.UtcNow.AddMonths(-1).Month;
            var currentYear = DateTime.UtcNow.Year;
            var previousYear = DateTime.UtcNow.AddMonths(-1).Year;

            double percentageChange = await CalculateFarmerPercentagePerformance(businessCode, organizationCode, currentMonth, previousMonth, currentYear, previousYear);
            double cooperativePercentageChange = await CalculateCooperativePercentagePerformance(businessCode, organizationCode, currentMonth, previousMonth, currentYear, previousYear);
            double agentPercentageChange = await CalculateAgentPercentagePerformance(businessCode, organizationCode, currentMonth, previousMonth, currentYear, previousYear);

            var agentsQuery = _context.Agents.Where(a => a.BelongToBusinessCode == businessCode);
            var cooperativesQuery = _context.Cooperatives.Where(c => c.BelongToBusinessCode == businessCode);
            var farmersQuery = _context.Farmers.Where(f => f.BelongToBusinessCode == businessCode);
            var warehousesQuery = _context.Warehouses.Where(w => w.BelongToBusinessCode == businessCode);

            if (!string.IsNullOrEmpty(organizationCode))
            {
                agentsQuery = agentsQuery.Where(a => a.OrganizationCode.Contains(organizationCode));
                cooperativesQuery = cooperativesQuery.Where(c => c.OrganizationCode.Contains(organizationCode));
                farmersQuery = farmersQuery.Where(f => f.OrganizationCode.Contains(organizationCode));
                warehousesQuery = warehousesQuery.Where(w => w.OrganizationCode.Contains(organizationCode));
            }

            var metrics = new BusinessEntitiesMetrics
            {
                TotalAgents = await agentsQuery.CountAsync(),
                TotalCooperative = await cooperativesQuery.CountAsync(),
                TotalFarmer = await farmersQuery.CountAsync(),
                TotalWarehouse = await warehousesQuery.CountAsync(),

                PercentageFarmerPerformance = percentageChange,
                PercentageCooperativePerformance = cooperativePercentageChange,
                PercentageAgentPerformance = agentPercentageChange,
            };

            return metrics;
        }

        private async Task<double> CalculateAgentPercentagePerformance(string businessCode, string? organizationCode, int currentMonth, int previousMonth, int currentYear, int previousYear)
        {
            var agentsQuery = _context.Agents.Where(f =>
                f.BelongToBusinessCode == businessCode);

            if (!string.IsNullOrEmpty(organizationCode))
            {
                agentsQuery = agentsQuery.Where(f => f.OrganizationCode.Contains(organizationCode));
            }

            var totalAgentsThisMonth = await agentsQuery
                .CountAsync(f => f.OnBoardedOn.Year == currentYear && f.OnBoardedOn.Month == currentMonth);

            var totalAgentsLastMonth = await agentsQuery
                .CountAsync(f => f.OnBoardedOn.Year == previousYear && f.OnBoardedOn.Month == previousMonth);

            double percentageChange = totalAgentsLastMonth == 0
                ? (totalAgentsThisMonth > 0 ? 100 : 0)
                : ((double)(totalAgentsThisMonth - totalAgentsLastMonth) / totalAgentsLastMonth) * 100;

            return percentageChange;
        }

        private async Task<double> CalculateCooperativePercentagePerformance(string businessCode, string? organizationCode, int currentMonth, int previousMonth, int currentYear, int previousYear)
        {
            var cooperativesQuery = _context.Cooperatives.Where(f =>
                f.BelongToBusinessCode == businessCode);

            if (!string.IsNullOrEmpty(organizationCode))
            {
                cooperativesQuery = cooperativesQuery.Where(f => f.OrganizationCode.Contains(organizationCode));
            }

            var totalCooperativesThisMonth = await cooperativesQuery
                .CountAsync(f => f.OnBoardedOn.Year == currentYear && f.OnBoardedOn.Month == currentMonth);

            var totalCooperativesLastMonth = await cooperativesQuery
                .CountAsync(f => f.OnBoardedOn.Year == previousYear && f.OnBoardedOn.Month == previousMonth);

            double percentageChange = totalCooperativesLastMonth == 0
                ? (totalCooperativesThisMonth > 0 ? 100 : 0)
                : ((double)(totalCooperativesThisMonth - totalCooperativesLastMonth) / totalCooperativesLastMonth) * 100;

            return percentageChange;
        }

        private async Task<double> CalculateFarmerPercentagePerformance(string businessCode, string? organizationCode, int currentMonth, int previousMonth, int currentYear, int previousYear)
        {
            var farmersQuery = _context.Farmers.Where(f =>
                f.BelongToBusinessCode == businessCode);

            if (!string.IsNullOrEmpty(organizationCode))
            {
                farmersQuery = farmersQuery.Where(f => f.OrganizationCode.Contains(organizationCode));
            }

            var totalFarmersThisMonth = await farmersQuery
                .CountAsync(f => f.OnBoardedOn.Year == currentYear && f.OnBoardedOn.Month == currentMonth);

            var totalFarmersLastMonth = await farmersQuery
                .CountAsync(f => f.OnBoardedOn.Year == previousYear && f.OnBoardedOn.Month == previousMonth);

            double percentageChange = totalFarmersLastMonth == 0
                ? (totalFarmersThisMonth > 0 ? 100 : 0)
                : ((double)(totalFarmersThisMonth - totalFarmersLastMonth) / totalFarmersLastMonth) * 100;

            return percentageChange;
        }

        public string GetYearOfBirth(int age)
        {
            int currentYear = DateTime.Now.Year;
            int yearOfBirth = currentYear - age;
            return yearOfBirth.ToString();
        }
        public async Task<(bool status, FarmerModel data)> CreateFarmer(FarmerModel data)
        {
            var farmerExist = await _context.Farmers
                .FirstOrDefaultAsync(x => x.UserId == data.UserId);

            if (farmerExist != null)
            {
                // Farmer already exists, return the existing one
                return (true, farmerExist.Map());
            }
            var entity = data.Map();

            await _context.AddAsync(entity);
            await _context.SaveChangesAsync();

            return (true, entity.Map());
        }



        public async Task<FarmerModel> GetFarmerByUserId(string userId)
        {
            var farmers = await _context.Farmers.
               Where(x => x.UserId == userId).FirstOrDefaultAsync();

            if (farmers == null)
            {
                throw new Exception("No record found");
            }

            // Fetch cooperative names for all distinct cooperativeIds in farmers

            var cooperatives = await _context.Cooperatives
                .Where(c => c.Id == farmers.BelongToCooperativeId)
                .ToDictionaryAsync(c => c.Id, c => c.Name);

            // Fetch farmer images

            var farmerImages = await _context.FarmerImages
                .Where(img => img.FarmerUserId == farmers.UserId)
                .ToDictionaryAsync(img => img.FarmerUserId, img => img.IMageKey); // Assuming ImageUrl is the image path



            var mappedFarmer = farmers.Map();

            if (cooperatives.TryGetValue(mappedFarmer.BelongToCooperativeId, out var cooperativeName))
            {
                mappedFarmer.CooperativeName = cooperativeName;
            }

            if (farmerImages.TryGetValue(mappedFarmer.UserId, out var imageUrl))
            {
                mappedFarmer.FarmerImage = imageUrl;
            }

            return mappedFarmer;

        }

        public async Task<FarmerModel> GetFarmerById(long Id)
        {
            var farmer = await _context.Farmers.Where(x => x.Id == Id).FirstOrDefaultAsync();
            if (farmer == null)
            {
                return null;
            }
            return farmer.Map();
        }
        public async Task<List<FarmerModel>> GetFarmers()//restricted to agrovesto
        {
            var farmer = await _context.Farmers.ToListAsync();
            if (farmer.Count > 0)
            {
                return farmer.Select(x => x.Map()).OrderByDescending(x => x.OnBoardedOn).ToList();
            }
            return null;
        }

        public async Task<PagedListResponse<List<FarmerModel>>> FilterFarmers(
  int pageNumber = 1,
  int pageSize = 10,
  DateTimeOffset? startDate = null,
  DateTimeOffset? endDate = null,
  string? location = null)
        {
            //var query = _context.FarmerSalesTracker

            //    .AsQueryable();
            var query = _context.Farmers

           .AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(x => x.OnBoardedOn >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(x => x.OnBoardedOn <= endDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(x => x.State == location);
            }
            if (query.Count() == 0) { return null; }
            var totalCount = await query.CountAsync();


            var pagedResult = await query
                .OrderByDescending(x => x.OnBoardedOn)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
               .ToListAsync();
            return new PagedListResponse<List<FarmerModel>>
            {
                Data = pagedResult.Select(x => x.Map()).ToList(),
                Count = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<List<FarmerModel>> GetFarmersByOnboardedUserId(string userId, ListQueryParams query)
        {

            var farmers = await _context.Farmers
                .AsNoTracking()
                .Where(s
                =>
                //(!string.IsNullOrEmpty(query.Location) && (s.Location.Contains(query.Location) || s.State.Contains(query.Location)
                // || s.Lga.Contains(query.Location))) &&
                s.OnboardedByUserId == userId)
                .OrderByDescending(s => s.OnBoardedOn)
                .Skip(query.PageNumber)
                .Take(query.PageSize)
                 .Select(s => s.Map()).ToListAsync();


            if (farmers.Count > 0)
            {
                return farmers;
            }
            return null;
        }



        public async Task<List<FarmerModel>> GetFarmersByAgent(string agentId, ListQueryParams query)
        {
            //var farmers = await (from f in _context.Farmers
            //                     join c in _context.Cooperatives
            //                     on f.BelongToCooperativeId equals c.Id
            //                     where c.IsAssignedToAgentId == agentId
            //                     select f).ToListAsync();


            //if (farmers.Count > 0)
            //{
            //    return farmers.Select(f => f.Map()).ToList();
            //}


            //return null;

            var farmersQuery = _context.Farmers.AsQueryable();

            farmersQuery = farmersQuery.Where(x => x.BelongToBusinessCode == query.businessCode);

            if (!string.IsNullOrEmpty(query.organizationCode))
            {
                farmersQuery = farmersQuery.Where(x => x.OrganizationCode.Contains(query.organizationCode));
            }

            farmersQuery = from farmer in farmersQuery
                           join cooperative in _context.Cooperatives
                               on farmer.BelongToCooperativeId equals cooperative.Id
                           where cooperative.IsAssignedToAgentId == agentId
                           select farmer;
            var result = await (
                from farmer in farmersQuery
                join cooperative in _context.Cooperatives
                    on farmer.BelongToCooperativeId equals cooperative.Id
                join image in _context.FarmerImages
                    on farmer.UserId equals image.FarmerUserId into imageGroup
                from img in imageGroup.DefaultIfEmpty()
                orderby farmer.OnBoardedOn descending
                select new
                {
                    Farmer = farmer,
                    CooperativeName = cooperative.Name,
                    FarmerImageKey = img != null ? img.IMageKey : null
                }
            ).ToListAsync();

            if (result == null || !result.Any())
            {
                throw new Exception("No record found");
            }

            var mappedFarmers = result.Select(item =>
            {
                var mapped = item.Farmer.Map();
                mapped.CooperativeName = item.CooperativeName;
                mapped.FarmerImage = item.FarmerImageKey;
                return mapped;
            }).ToList();

            return mappedFarmers;

        }


        public async Task<List<FarmerModel>> GetFarmersByCooperativeId(long Id, ListQueryParams query)
        {

            var farmers = await (from a in _context.Farmers
                                 join c in _context.Cooperatives
                                 on a.BelongToCooperativeId equals c.Id
                                 where a.BelongToCooperativeId == Id
                                 select a).ToListAsync();



            if (farmers.Count > 0)
            {
                return farmers.Select(f => f.Map()).OrderByDescending(f => f.OnBoardedOn).ToList();
            }


            return null;
        }
        public async Task<List<FarmerModel>> GetFarmersByBusinessCode(string businessCode, ListQueryParams query)
        {
            var farmersQuery = _context.Farmers.AsQueryable();

            farmersQuery = farmersQuery.Where(x => x.BelongToBusinessCode == businessCode);

            if (!string.IsNullOrEmpty(query.organizationCode))
            {
                farmersQuery = farmersQuery.Where(x => x.OrganizationCode.Contains(query.organizationCode));
            }

            var result = await (
                from farmer in farmersQuery

                join cooperative in _context.Cooperatives
                    on farmer.BelongToCooperativeId equals cooperative.Id

                    into cooperativeGroup
                from coop in cooperativeGroup.DefaultIfEmpty()

                join image in _context.FarmerImages
                    on farmer.UserId equals image.FarmerUserId into imageGroup
                from img in imageGroup.DefaultIfEmpty()

                orderby farmer.OnBoardedOn descending

                select new
                {
                    Farmer = farmer,
                    CooperativeName = coop != null ? coop.Name : null,
                    FarmerImageKey = img != null ? img.IMageKey : null
                }
            ).ToListAsync();

            if (result == null || !result.Any())
            {
                throw new Exception("No record found");
            }

            var mappedFarmers = result.Select(item =>
            {
                var mapped = item.Farmer.Map();
                mapped.CooperativeName = item.CooperativeName;
                mapped.FarmerImage = item.FarmerImageKey;
                return mapped;
            }).ToList();

            return mappedFarmers;
        }

        /** public async Task<List<FarmerModel>> GetFarmersByBusinessCode(string businessCode, ListQueryParams query)
         {
             var farmers = await _context.Farmers
                 .Where(x =>
                  (!string.IsNullOrEmpty(query.organizationCode) && (x.OrganizationCode.Contains(query.organizationCode)))
            &&
                 x.BelongToBusinessCode == businessCode)
                 .OrderByDescending(x => x.OnBoardedOn)
                 .ToListAsync();

             if (farmers == null || !farmers.Any())
             {
                 throw new Exception("No record found");
             }

             // Fetch cooperative names for all distinct cooperativeIds in farmers
             var cooperativeIds = farmers.Select(f => f.BelongToCooperativeId).Distinct().ToList();
             var cooperatives = await _context.Cooperatives
                 .Where(c => cooperativeIds.Contains(c.Id))
                 .ToDictionaryAsync(c => c.Id, c => c.Name);

             // Fetch farmer images
             var farmerUserIds = farmers.Select(f => f.UserId).Distinct().ToList();
             var farmerImages = await _context.FarmerImages
                 .Where(img => farmerUserIds.Contains(img.FarmerUserId))
                 .ToDictionaryAsync(img => img.FarmerUserId, img => img.IMageKey); // Assuming ImageUrl is the image path

             return farmers.Select(farmer =>
             {
                 var mappedFarmer = farmer.Map();

                 if (cooperatives.TryGetValue(farmer.BelongToCooperativeId, out var cooperativeName))
                 {
                     mappedFarmer.CooperativeName = cooperativeName;
                 }

                 if (farmerImages.TryGetValue(farmer.UserId, out var imageUrl))
                 {
                     mappedFarmer.FarmerImage = imageUrl; 
                 }

                 return mappedFarmer;
             }).ToList();
         }

   **/

        public async Task<(bool status, FarmerImagesModel data)> SaveImage(FarmerImagesModel data)
        {
            var entity = data.Map();
            var imageExist = await _context.FarmerImages.Where(x => x.FarmerUserId == data.FarmerUserId
            && x.BelongToBusinessCode == data.BelongToBusinessCode).FirstOrDefaultAsync();


            if (imageExist != null)
            {
                imageExist.IMageKey = data.ImageKey;
                imageExist.ImageName = data.ImageName;

                entity.IMageKey = data.ImageKey;
                entity.ImageName = data.ImageName;
            }
            else
            {

                await _context.AddAsync(entity);
            }



            await _context.SaveChangesAsync();

            return (true, entity.Map());
        }

        public async Task<FarmerImagesModel> GetFarmerImage(string userId, string businessCode, string? organizationCode)
        {
            var farmer = await _context.FarmerImages.Where(x =>
             (!string.IsNullOrEmpty(organizationCode) && (x.OrganizationCode.Contains(organizationCode)))
           && x.FarmerUserId == userId && x.BelongToBusinessCode == businessCode).FirstOrDefaultAsync();
            if (farmer == null)
            {
                return null;
            }
            return farmer.Map();
        }
        public async Task<List<FarmerImagesModel>> GetImagesByBusinessCode(string businessCode, string? organizationCode)
        {
            var query = await _context.FarmerImages
                                 .Where(img => (!string.IsNullOrEmpty(organizationCode) && (img.OrganizationCode.Contains(organizationCode)))
           && img.BelongToBusinessCode == businessCode)
                                 .ToListAsync();

            if (query == null)
            {
                return null;
            }
            return query.Select(x => x.Map()).ToList();
        }


        public async Task<FarmerModel> AssignFarmerToAgent(string agentuserId, long farmerId)
        {
            var farmer = await _context.Farmers.Where(c => c.Id == farmerId).FirstOrDefaultAsync();
            if (farmer == null) return null;
            farmer.OnboardedByUserId = agentuserId;
            await _context.SaveChangesAsync();

            return farmer.Map();
        }

        public async Task<FarmerDetailDto> GetFarmerDetails(string userId)
        {
            var farmerProfile = await (
            from farmer in _context.Farmers
            where farmer.UserId == userId

            join cooperative in _context.Cooperatives
            on farmer.BelongToCooperativeId equals cooperative.Id

            join agent in _context.Agents
            on cooperative.IsAssignedToAgentId equals agent.UserId into agentGroup
            from agent in agentGroup.DefaultIfEmpty()

            join image in _context.FarmerImages
             on farmer.UserId equals image.FarmerUserId into imageGroup
            from img in imageGroup.DefaultIfEmpty()

            select new
            {
                Farmer = farmer,
                Cooperative = cooperative,
                Agent = agent,
                FarmerImage = img
            }).FirstOrDefaultAsync();

            if (farmerProfile == null)
            {
                throw new Exception("Farmer not found");
            }

            // Get farm plots
            var plots = await _context.FarmPlotDetails
                .Where(p => p.FarmerId == farmerProfile.Farmer.Id)
                .ToListAsync();

            // Get visitation activities
            var visitations = await _context.FarmVisitationActivities
                .Where(v => v.FarmerUserId == userId)
                .OrderByDescending(v => v.CreatedOn)
                .ToListAsync();


            var result = new FarmerDetailDto
            {
                Farmer = farmerProfile.Farmer.Map(),
                Cooperative = farmerProfile.Cooperative.Map(),
                Agent = farmerProfile.Agent.Map(),
                FarmerImage = farmerProfile.FarmerImage.Map(),
                PlotDetails = plots.Select(p=>p.Map()).ToList(),
                Visitations = visitations.Select(v=>v.Map()).ToList()
            };

            return result;

        }



    }
}

