using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.Utilities;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
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

        #region farmervaluechain

        #endregion farmervaluechain
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

            // Filter by business code
            if (!string.IsNullOrWhiteSpace(businessCode))
            {
                query = query.Where(f => f.BelongToBusinessCode == businessCode);
            }

            // Filter by organization via FarmerOrganizations
            if (!string.IsNullOrWhiteSpace(organizationCode))
            {
                query = from f in query
                        join fo in _context.FarmerOrganizations
                            on f.Id equals fo.FarmerId
                        where fo.OrganizationCode == organizationCode
                        select f;
            }

            // Filter by state
            if (!string.IsNullOrWhiteSpace(state))
            {
                query = query.Where(f => f.State == state);
            }

            // Filter by onboarding date range
            if (startDate.HasValue)
            {
                query = query.Where(f => f.OnBoardedOn >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(f => f.OnBoardedOn <= endDate.Value);
            }

            var farmers = await query.ToListAsync();

            if (farmers == null || !farmers.Any())
            {
                throw new Exception("No record found");
            }

            // Map cooperative names
            var cooperativeIds = farmers.Select(f => f.BelongToCooperativeId).Distinct().ToList();
            var cooperatives = await _context.Cooperatives
                .Where(c => cooperativeIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name);

            // Filter by cooperative name if provided
            if (!string.IsNullOrWhiteSpace(cooperativeName))
            {
                farmers = farmers
                    .Where(f => cooperatives.TryGetValue(f.BelongToCooperativeId, out var name)
                                && name.Equals(cooperativeName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Map results
            return farmers.Select(f =>
            {
                var mapped = f.Map();
                if (cooperatives.TryGetValue(f.BelongToCooperativeId, out var coopName))
                {
                    mapped.CooperativeName = coopName;
                }
                return mapped;
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

            // Calculate percentage changes
            double percentageChange = await CalculateFarmerPercentagePerformance(
                businessCode, organizationCode, currentMonth, previousMonth, currentYear, previousYear);
            double cooperativePercentageChange = await CalculateCooperativePercentagePerformance(
                businessCode, organizationCode, currentMonth, previousMonth, currentYear, previousYear);
            double agentPercentageChange = await CalculateAgentPercentagePerformance(
                businessCode, organizationCode, currentMonth, previousMonth, currentYear, previousYear);

            // Base queries
            var agentsQuery = _context.Agents.Where(a => a.BelongToBusinessCode == businessCode);
            var cooperativesQuery = _context.Cooperatives.Where(c => c.BelongToBusinessCode == businessCode);
            var farmersQuery = _context.Farmers.Where(f => f.BelongToBusinessCode == businessCode);
            var warehousesQuery = _context.Warehouses.Where(w => w.BelongToBusinessCode == businessCode);

            // Filter by organization
            if (!string.IsNullOrEmpty(organizationCode))
            {
                agentsQuery = agentsQuery.Where(a => a.OrganizationCode.Contains(organizationCode));
                cooperativesQuery = cooperativesQuery.Where(c => c.OrganizationCode.Contains(organizationCode));

                // Farmers: join with FarmerOrganizations
                farmersQuery = from f in farmersQuery
                               join fo in _context.FarmerOrganizations
                                   on f.Id equals fo.FarmerId
                               where fo.OrganizationCode == organizationCode
                               select f;

                warehousesQuery = warehousesQuery.Where(w => w.OrganizationCode.Contains(organizationCode));
            }

            // Build metrics
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

        private async Task<double> CalculateFarmerPercentagePerformance(
      string businessCode,
      string? organizationCode,
      int currentMonth,
      int previousMonth,
      int currentYear,
      int previousYear)
        {
            // Base query: farmers in the business
            var farmersQuery = _context.Farmers.Where(f => f.BelongToBusinessCode == businessCode);

            // Filter by organization if provided
            if (!string.IsNullOrEmpty(organizationCode))
            {
                farmersQuery = from farmer in farmersQuery
                               join fo in _context.FarmerOrganizations
                                   on farmer.Id equals fo.FarmerId
                               where fo.OrganizationCode == organizationCode
                               select farmer;
            }

            // Count farmers onboarded in the current month
            var totalFarmersThisMonth = await farmersQuery
                .CountAsync(f => f.OnBoardedOn.Year == currentYear && f.OnBoardedOn.Month == currentMonth);

            // Count farmers onboarded in the previous month
            var totalFarmersLastMonth = await farmersQuery
                .CountAsync(f => f.OnBoardedOn.Year == previousYear && f.OnBoardedOn.Month == previousMonth);

            // Calculate percentage change
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
            // Check if the farmer exists by UserId
            var farmer = await _context.Farmers.FirstOrDefaultAsync(f => f.UserId == data.UserId);

            if (farmer != null)
            {
                // Update basic details if changed
                bool isModified = false;
                if (farmer.Name != data.Name) { farmer.Name = data.Name; isModified = true; }
                if (farmer.PhoneNumber != data.PhoneNumber) { farmer.PhoneNumber = data.PhoneNumber; isModified = true; }
                if (farmer.Email != data.Email) { farmer.Email = data.Email; isModified = true; }

                if (isModified)
                {
                    _context.Farmers.Update(farmer);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                // Create new farmer
                farmer = data.Map();
                await _context.Farmers.AddAsync(farmer);
                await _context.SaveChangesAsync(); // get Farmer.Id
            }

            // Link farmer to organization if not already linked
            if (!string.IsNullOrEmpty(data.OrganizationCode))
            {
                bool alreadyLinked = await _context.FarmerOrganizations
                    .AnyAsync(fo => fo.FarmerId == farmer.Id && fo.OrganizationCode == data.OrganizationCode);

                if (!alreadyLinked)
                {
                    await _context.FarmerOrganizations.AddAsync(new FarmerOrganization
                    {
                        FarmerId = farmer.Id,
                        OrganizationCode = data.OrganizationCode,
                        OnboardedByUserId = data.OnboardedByUserId
                    });
                    await _context.SaveChangesAsync();
                }
            }

            // Update value chain items
            await UpdateFarmerValueChainItems(farmer.Id, data.SelectedItems);

            return (true, farmer.Map());
        }

        /** public async Task<(bool status, FarmerModel data)> CreateFarmer(FarmerModel data)
         {
             // Check if the farmer already exists
             var farmerExist = await _context.Farmers
                 .FirstOrDefaultAsync(x => x.UserId == data.UserId);

             if (farmerExist != null)
             {
                 // Farmer exists → update basic details if they have changed
                 bool isModified = false;

                 if (farmerExist.Name != data.Name)
                 {
                     farmerExist.Name = data.Name;
                     isModified = true;
                 }

                 if (farmerExist.PhoneNumber != data.PhoneNumber)
                 {
                     farmerExist.PhoneNumber = data.PhoneNumber;
                     isModified = true;
                 }

                 if (farmerExist.Email != data.Email)
                 {
                     farmerExist.Email = data.Email;
                     isModified = true;
                 }
                 // Add other properties as needed...

                 if (isModified)
                 {
                     _context.Farmers.Update(farmerExist);
                     await _context.SaveChangesAsync();
                 }

                 // Update the value chain items regardless
                 await UpdateFarmerValueChainItems(farmerExist.Id, data.SelectedItems);

                 return (true, farmerExist.Map());
             }

             // Farmer does not exist → create new
             var entity = data.Map(); // Map FarmerModel → Farmer entity
             await _context.Farmers.AddAsync(entity);
             await _context.SaveChangesAsync(); // Save to get Id

             // Handle value chain items
             await UpdateFarmerValueChainItems(entity.Id, data.SelectedItems);

             return (true, entity.Map());
         }
 **/
        private async Task UpdateFarmerValueChainItems(long farmerId, List<ValueChainSelectionDto> valueChainSelections)
        {
            // Step 1: Get existing items
            var existingItems = await _context.FarmerValueChainItems
                .Where(x => x.FarmerId == farmerId)
                .ToListAsync();

            // Step 2: Flatten incoming items
            var incomingItems = valueChainSelections
                .SelectMany(v => v.Items.Select(item => new { v.ValueChainType, ItemName = item }))
                .ToList();

            // Step 3: Add new items
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

            // Step 4: Remove items not in the new list
            var toRemove = existingItems
                .Where(e => !incomingItems.Any(i =>
                    i.ValueChainType == e.ValueChainType &&
                    i.ItemName == e.ItemName))
                .ToList();

            _context.FarmerValueChainItems.RemoveRange(toRemove);
            await _context.SaveChangesAsync();
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
            // Base query filtered by business code
            var farmersQuery = _context.Farmers.AsQueryable();

            if (!string.IsNullOrEmpty(query.businessCode))
            {
                farmersQuery = farmersQuery.Where(f => f.BelongToBusinessCode == query.businessCode);
            }

            // Filter by organization if provided
            if (!string.IsNullOrEmpty(query.organizationCode))
            {
                farmersQuery = from farmer in farmersQuery
                               join fo in _context.FarmerOrganizations
                                   on farmer.Id equals fo.FarmerId
                               where fo.OrganizationCode == query.organizationCode
                               select farmer;
            }

            // Filter farmers by agent via cooperative assignment
            farmersQuery = from farmer in farmersQuery
                           join cooperative in _context.Cooperatives
                               on farmer.BelongToCooperativeId equals cooperative.Id
                           where cooperative.IsAssignedToAgentId == agentId
                           select farmer;

            // Perform joins for cooperative, images, subaccounts
            var result = await (
                from farmer in farmersQuery

                join cooperative in _context.Cooperatives
                    on farmer.BelongToCooperativeId equals cooperative.Id

                join image in _context.FarmerImages
                    on farmer.UserId equals image.FarmerUserId into imageGroup
                from img in imageGroup.DefaultIfEmpty()

                join subAccount in _context.SubAccounts
                    on farmer.UserId equals subAccount.UserId into subAccountGroup
                from sub in subAccountGroup.DefaultIfEmpty()

                orderby farmer.OnBoardedOn descending

                select new
                {
                    Farmer = farmer,
                    CooperativeName = cooperative.Name,
                    FarmerImageKey = img != null ? img.IMageKey : null,
                    SubAccount = sub
                }
            ).ToListAsync();

            // Return empty list if no records found
            if (result == null || !result.Any())
                return new List<FarmerModel>();

            // Map results
            var mappedFarmers = result.Select(item =>
            {
                var mapped = item.Farmer.Map();
                mapped.CooperativeName = item.CooperativeName;
                mapped.FarmerImage = item.FarmerImageKey;

                if (item.SubAccount != null)
                {
                    mapped.NIN = item.SubAccount.NIN;
                    mapped.BVN = item.SubAccount.Bvn;
                }

                return mapped;
            }).ToList();

            return mappedFarmers;
        }


        public async Task<List<FarmerModel>> GetFarmersByCooperativeId(long cooperativeId, ListQueryParams query)
        {
            // Base query filtered by business code if provided
            var farmersQuery = _context.Farmers.AsQueryable();

            if (!string.IsNullOrEmpty(query.businessCode))
            {
                farmersQuery = farmersQuery.Where(f => f.BelongToBusinessCode == query.businessCode);
            }

            // Filter by organization if provided
            if (!string.IsNullOrEmpty(query.organizationCode))
            {
                farmersQuery = from farmer in farmersQuery
                               join fo in _context.FarmerOrganizations
                                   on farmer.Id equals fo.FarmerId
                               where fo.OrganizationCode == query.organizationCode
                               select farmer;
            }

            // Filter by cooperative
            farmersQuery = farmersQuery.Where(f => f.BelongToCooperativeId == cooperativeId);

            // Perform joins for cooperative, images, subaccounts
            var result = await (
                from farmer in farmersQuery

                join cooperative in _context.Cooperatives
                    on farmer.BelongToCooperativeId equals cooperative.Id

                join image in _context.FarmerImages
                    on farmer.UserId equals image.FarmerUserId into imageGroup
                from img in imageGroup.DefaultIfEmpty()

                join subAccount in _context.SubAccounts
                    on farmer.UserId equals subAccount.UserId into subAccountGroup
                from sub in subAccountGroup.DefaultIfEmpty()

                orderby farmer.OnBoardedOn descending

                select new
                {
                    Farmer = farmer,
                    CooperativeName = cooperative.Name,
                    FarmerImageKey = img != null ? img.IMageKey : null,
                    SubAccount = sub
                }
            ).ToListAsync();

            // Return empty list if no records
            if (result == null || !result.Any())
                return new List<FarmerModel>();

            // Map results
            var mappedFarmers = result.Select(item =>
            {
                var mapped = item.Farmer.Map();
                mapped.CooperativeName = item.CooperativeName;
                mapped.FarmerImage = item.FarmerImageKey;

                if (item.SubAccount != null)
                {
                    mapped.NIN = item.SubAccount.NIN;
                    mapped.BVN = item.SubAccount.Bvn;
                    mapped.IsNinVerified = item.SubAccount.IsNinVerified;
                }

                return mapped;
            }).ToList();

            return mappedFarmers;
        }
        public async Task<List<FarmerModel>> GetFarmersByBusinessCode(string businessCode, ListQueryParams query)
        {
            var farmersQuery = _context.Farmers
                .Where(f => f.BelongToBusinessCode == businessCode)
                .AsQueryable();

            if (!string.IsNullOrEmpty(query.organizationCode))
            {
                farmersQuery = from farmer in farmersQuery
                               join fo in _context.FarmerOrganizations
                                   on farmer.Id equals fo.FarmerId
                               where fo.OrganizationCode == query.organizationCode
                               select farmer;
            }

            var result = await (
                from farmer in farmersQuery

                join cooperative in _context.Cooperatives
                    on farmer.BelongToCooperativeId equals cooperative.Id into cooperativeGroup
                from coop in cooperativeGroup.DefaultIfEmpty() 

                join image in _context.FarmerImages
                    on farmer.UserId equals image.FarmerUserId into imageGroup
                from img in imageGroup.DefaultIfEmpty() 

                join subAccount in _context.SubAccounts
                    on farmer.UserId equals subAccount.UserId into subAccountGroup
                from sub in subAccountGroup.DefaultIfEmpty() 

                orderby farmer.OnBoardedOn descending

                select new
                {
                    Farmer = farmer,
                    Cooperative = coop,
                    FarmerImage = img,
                    SubAccount = sub
                }
            ).ToListAsync();

            var mappedFarmers = result.Select(item =>
            {
//                var valueChainSelections = await (
// from selection in _context.FarmerValueChainItems
// where selection.FarmerId == item.Farmer.Id
// group selection by selection.ValueChainType into g
// select new FarmerValueChainsSelectionDto
// {
//     TypeName = g.Key,
//     Items = g.Select(x => new ValueChainItemDto
//     {
//         ItemId = x.Id,
//         Name = x.ItemName
//     }).ToList()
// }
//).ToListAsync();

                var mapped = item.Farmer.Map();
                //mapped.ValueChainItems= valueChainSelections;
                mapped.CooperativeName = item.Cooperative?.Name;
                mapped.FarmerImage = item.FarmerImage?.IMageKey;
                mapped.NIN = item.SubAccount?.NIN;
                mapped.BVN = item.SubAccount?.Bvn;

                return mapped;
            }).ToList();

            return mappedFarmers;
        }
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

            var query = _context.FarmerImages
        .Where(f => f.BelongToBusinessCode == businessCode);

            if (!string.IsNullOrEmpty(organizationCode))
            {
                query = query.Where(f => f.OrganizationCode.Contains(organizationCode));
            }
            var farmer = await query.FirstOrDefaultAsync();

            
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

                // 🟢 Left join cooperative (farmer might not belong to one)
                join cooperative in _context.Cooperatives
                    on farmer.BelongToCooperativeId equals cooperative.Id into cooperativeGroup
                from coop in cooperativeGroup.DefaultIfEmpty()

                    // 🟢 Left join agent (cooperative might not have an assigned agent)
                join agent in _context.Agents
                    on coop.IsAssignedToAgentId equals agent.UserId into agentGroup
                from agt in agentGroup.DefaultIfEmpty()

                    // 🟢 Left join image
                join image in _context.FarmerImages
                    on farmer.UserId equals image.FarmerUserId into imageGroup
                from img in imageGroup.DefaultIfEmpty()

                    // 🟢 Left join subaccount
                join subAccount in _context.SubAccounts
                    on farmer.UserId equals subAccount.UserId into subAccountGroup
                from sub in subAccountGroup.DefaultIfEmpty()

                select new
                {
                    Farmer = farmer,
                    Cooperative = coop,
                    Agent = agt,
                    FarmerImage = img,
                    SubAccount = sub
                }
            ).FirstOrDefaultAsync();

            if (farmerProfile == null)
                throw new Exception("Farmer not found");

            // 🟢 Farm plots
            var plots = await _context.FarmPlotDetails
                .Where(p => p.FarmerId == farmerProfile.Farmer.Id)
                .ToListAsync();

            // 🟢 Repayments
            var repayments = await _context.Repayments
                .Include(x => x.RepaymentBreakdown)
                .Where(p => p.FarmerUserId == farmerProfile.Farmer.UserId)
                .ToListAsync();

            // 🟢 Visitations
            var visitations = await _context.FarmVisitationActivities
                .Where(v => v.FarmerUserId == userId)
                .OrderByDescending(v => v.CreatedOn)
                .ToListAsync();

            // 🟢 Value chain selections
            var valueChainSelections = await (
    from selection in _context.FarmerValueChainItems
    where selection.FarmerId == farmerProfile.Farmer.Id
    group selection by selection.ValueChainType into g
    select new FarmerValueChainsSelectionDto
    {
        TypeName = g.Key,
        Items = g.Select(x => new ValueChainItemDto
        {
            ItemId = x.Id,
            Name = x.ItemName
        }).ToList()
    }
).ToListAsync();


            // 🟢 Map main farmer details
            var mappedFarmer = farmerProfile.Farmer.Map();
            mappedFarmer.ValueChainItems = valueChainSelections;

            // Add SubAccount details if available
            mappedFarmer.NIN = farmerProfile.SubAccount?.NIN;
            mappedFarmer.BVN = farmerProfile.SubAccount?.Bvn;

            // 🟢 Build final DTO safely
            var result = new FarmerDetailDto
            {
                Farmer = mappedFarmer,
                Cooperative = farmerProfile.Cooperative?.Map(),  // ✅ Optional
                Agent = farmerProfile.Agent?.Map(),              // ✅ Optional
                FarmerImage = farmerProfile.FarmerImage?.Map(),  // ✅ Optional
                PlotDetails = plots.Select(p => p.Map()).ToList(),
                Repayments = repayments.Select(r => r.Map()).ToList(),
                Visitations = visitations.Select(v => v.Map()).ToList()
            };

            return result;
        }

        /** public async Task<FarmerDetailDto> GetFarmerDetails(string userId)
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
                 PlotDetails = plots.Select(p => p.Map()).ToList(),
                 Visitations = visitations.Select(v => v.Map()).ToList()
             };

             return result;

         }
         **/

        public async Task<PagedListResponse<List<FarmerDetailDto>>> GetAllFarmerDetails(
       string businessCode,
       string? organizationCode = null,
       long? cooperativeId = null,
       string? agentUserId = null,
       string? state = null,
       string? sortBy = "Name", // You can add other fields
       bool sortAsc = true)
        {
            var query = _context.Farmers
                .Where(f => f.BelongToBusinessCode == businessCode);

            // Filter by organization via FarmerOrganizations
            if (!string.IsNullOrEmpty(organizationCode))
            {
                query = from f in query
                        join fo in _context.FarmerOrganizations
                            on f.Id equals fo.FarmerId
                        where fo.OrganizationCode == organizationCode
                        select f;
            }

            // Filter by cooperative
            if (cooperativeId.HasValue)
            {
                query = query.Where(f => f.BelongToCooperativeId == cooperativeId.Value);
            }

            // Filter by state
            if (!string.IsNullOrEmpty(state))
            {
                query = query.Where(f => f.State == state);
            }

            // Count total before joins
            int totalCount = await query.CountAsync();

            // Include joins for details
            var farmers = await (
                from farmer in query
                    .OrderByDescending(f => f.OnBoardedOn)

                join coop in _context.Cooperatives
                    on farmer.BelongToCooperativeId equals coop.Id into coopGroup
                from cooperative in coopGroup.DefaultIfEmpty()

                join image in _context.FarmerImages
                    on farmer.UserId equals image.FarmerUserId into imageGroup
                from farmerImage in imageGroup.DefaultIfEmpty()

                join agent in _context.Agents
                    on cooperative.IsAssignedToAgentId equals agent.UserId into agentGroup
                from assignedAgent in agentGroup.DefaultIfEmpty()

                join subAccount in _context.SubAccounts
                    on farmer.UserId equals subAccount.UserId into subAccountGroup
                from sub in subAccountGroup.DefaultIfEmpty()

                select new
                {
                    Farmer = farmer,
                    Cooperative = cooperative,
                    FarmerImage = farmerImage,
                    Agent = assignedAgent,
                    SubAccount = sub
                }
            ).ToListAsync();

            var farmerIds = farmers.Select(f => f.Farmer.Id).ToList();

            // Preload value chain items
            var valueChainItems = await _context.FarmerValueChainItems
                .Where(v => farmerIds.Contains(v.FarmerId))
                .ToListAsync();

            var groupedValueChains = valueChainItems
                .GroupBy(v => v.FarmerId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Final projection
            var result = farmers.Select(f => new FarmerDetailDto
            {
                Farmer = f.Farmer.Map().ApplySubAccount(f.SubAccount),
                Cooperative = f.Cooperative?.Map(),
                FarmerImage = f.FarmerImage?.Map(),
                Agent = f.Agent?.Map(),
                PlotDetails = _context.FarmPlotDetails
                    .Where(p => p.FarmerId == f.Farmer.Id)
                    .Select(x => x.Map())
                    .ToList(),

                Visitations = _context.FarmVisitationActivities
                    .Where(v => v.FarmerUserId == f.Farmer.UserId)
                    .Select(x => x.Map())
                    .ToList(),

                ValueChainItems = groupedValueChains.TryGetValue(f.Farmer.Id, out var items)
                    ? items.Select(x => new FarmerValueChainSelectionDto
                    {
                        TypeName = x.ValueChainType,
                        ItemName = x.ItemName
                    }).ToList()
                    : new List<FarmerValueChainSelectionDto>()
            }).ToList();

            return new PagedListResponse<List<FarmerDetailDto>>
            {
                Data = result,
                Count = totalCount
            };
        }


    }
}

