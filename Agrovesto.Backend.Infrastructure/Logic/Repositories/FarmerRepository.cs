using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Payment;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.Utilities;
using DocumentFormat.OpenXml.Office2010.Excel;
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
        public async Task<(bool status, string message)> OnFarmerLevelUpdate(string userId, FarmerLevel level)
        {
            var user = await _context.Farmers.Where(x => x.UserId == userId).FirstOrDefaultAsync();
            if (user == null)
            {
                return (false, null);
            }
            user.Level = level;
            await _context.SaveChangesAsync();

            return (true, "Level Updated");
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
            //var cooperativeIds = farmers.Select(f => f.BelongToCooperativeId).Distinct().ToList();
            //var cooperatives = await _context.Cooperatives
            //    .Where(c => cooperativeIds.Contains(c.Id))
            //    .ToDictionaryAsync(c => c.Id, c => c.Name);

            //// Filter by cooperative name if provided
            //if (!string.IsNullOrWhiteSpace(cooperativeName))
            //{
            //    farmers = farmers
            //        .Where(f => cooperatives.TryGetValue(f.BelongToCooperativeId, out var name)
            //                    && name.Equals(cooperativeName, StringComparison.OrdinalIgnoreCase))
            //        .ToList();
            //}

            // Map results
            return farmers.Select(f =>
            {
                var mapped = f.Map();
                //if (cooperatives.TryGetValue(f.BelongToCooperativeId, out var coopName))
                //{
                //    mapped.CooperativeName = coopName;
                //}
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

        public async Task<bool> DeactivateFarmerFromOrganization(AssignmentArgs args, string? message =null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var farmerExists = await _context.Farmers
                    .AnyAsync(f => f.Id == args.Id);

                if (!farmerExists)
                    return false;

                var now = DateTimeOffset.UtcNow;

                var orgFarmer = await _context.FarmerOrganizations
                    .FirstOrDefaultAsync(x => x.FarmerId == args.Id
                                           && x.OrganizationCode == args.organizationCode
                                           && x.IsActive);

                if (orgFarmer == null)
                    return false;

                orgFarmer.IsActive = false;
                orgFarmer.DeactivatedOn = now;
                orgFarmer.DeactivatedByUserId = args.OnboardedByUserId;
                orgFarmer.DeactivationReason = !string.IsNullOrEmpty(message) ? message:  $"Deactivated by user {args.OnboardedByUserId}";


                var farmerAgents = await _context.FarmerAgents
                    .Where(x => x.FarmerId == args.Id
                             && x.OrganizationCode == args.organizationCode
                             && x.IsActive)
                    .ToListAsync();

                foreach (var agent in farmerAgents)
                {
                    agent.IsActive = false;
                    agent.DeactivatedOn = now;
                    orgFarmer.DeactivatedByUserId = args.OnboardedByUserId;
                    orgFarmer.DeactivationReason = !string.IsNullOrEmpty(message) ? message : $"Deactivated by user {args.OnboardedByUserId}";

                }


                var farmerMemberships = await _context.FarmerCooperatives
                    .Where(x => x.FarmerId == args.Id
                             && x.OrganizationCode == args.organizationCode
                             && x.IsActive)
                    .ToListAsync();

                foreach (var membership in farmerMemberships)
                {
                    membership.IsActive = false;
                    membership.DeactivatedOn = now;
                    orgFarmer.DeactivatedByUserId = args.OnboardedByUserId;
                    orgFarmer.DeactivationReason = !string.IsNullOrEmpty(message) ? message : $"Deactivated by user {args.OnboardedByUserId}";

                }


                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<FarmerModel> AssignFarmerToCooperatives(AssignmentArgs args, long farmerId)
        {
            if (args.CooperativeIds == null || !args.CooperativeIds.Any())
                return null;

            // Check if the farmer exists
            var farmer = await _context.Farmers
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == farmerId);

            if (farmer == null)
                return null;

            // Get existing assignments for this farmer
            var existingAssignments = await _context.FarmerCooperatives
                .Where(fa => fa.FarmerId == farmerId && args.CooperativeIds.Contains(fa.CooperativeId))
                .Select(fa => fa.CooperativeId)
                .ToListAsync();

            // Filter out agents that are already assigned
            var newCooperativeIds = args.CooperativeIds
       .Where(x => x.HasValue)
       .Select(x => x.Value)
       .Except(existingAssignments)
       .ToList();

            // Create new assignments
            if (newCooperativeIds.Any())
            {
                var newAssignments = newCooperativeIds.Select(cooperativeId => new FarmerCooperative
                {
                    FarmerId = farmerId,
                    CooperativeId = cooperativeId,
                    OnboardedByUserId = args.OnboardedByUserId,
                    JoinedOn = DateTimeOffset.UtcNow,
                    IsActive=true,
                    OrganizationCode=args.organizationCode
                }).ToList();

                await _context.FarmerCooperatives.AddRangeAsync(newAssignments);
                await _context.SaveChangesAsync();
            }

            // Reload the farmer with updated data
            var updatedFarmer = await _context.Farmers
                .FirstOrDefaultAsync(f => f.Id == farmerId);

            return updatedFarmer.Map();
        }

        public async Task<FarmerModel> AssignFarmerToCooperative(
    long farmerId,
    long cooperativeId,
    string? assignedByUserId = null)
        {
            var farmerExists = await _context.Farmers
                .AnyAsync(f => f.Id == farmerId);

            if (!farmerExists)
                return null;

            var cooperativeExists = await _context.MemberShips
                .AnyAsync(c => c.Id == cooperativeId);

            if (!cooperativeExists)
                return null;

            bool alreadyAssigned = await _context.FarmerCooperatives.AnyAsync(fc =>
                fc.FarmerId == farmerId &&
                fc.CooperativeId == cooperativeId);

            if (!alreadyAssigned)
            {
                await _context.FarmerCooperatives.AddAsync(new FarmerCooperative
                {
                    FarmerId = farmerId,
                    CooperativeId = cooperativeId,
                    OnboardedByUserId = assignedByUserId,
                    JoinedOn = DateTimeOffset.UtcNow
                });

                await _context.SaveChangesAsync();
            }
            var farmer = await _context.Farmers
                .FirstOrDefaultAsync(f => f.Id == farmerId);

            return farmer?.Map();
        }

        public async Task<bool> RemoveFarmerFromCooperative(long farmerId, long cooperativeId)
        {

            var link = await _context.FarmerCooperatives
                .FirstOrDefaultAsync(fc => fc.FarmerId == farmerId && fc.CooperativeId == cooperativeId);

            if (link == null)
                return false;

            _context.FarmerCooperatives.Remove(link);
            await _context.SaveChangesAsync();
            return true;
        }
        /// <summary>
        /// /update farmer profile
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="update"></param>
        /// <returns></returns>
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

        public async Task<BusinessEntitiesMetrics> GetBusinessEntitiesMetricsAsyncByAgent(string agentId, ListQueryParams query)
        {

            var farmers = await GetFarmersByAgent(agentId, query);

            var currentMonth = DateTime.UtcNow.Month;
            var previousMonth = DateTime.UtcNow.AddMonths(-1).Month;
            var currentYear = DateTime.UtcNow.Year;
            var previousYear = DateTime.UtcNow.AddMonths(-1).Year;

            double percentageChange = await CalculateFarmerPercentagePerformanceByAgent(agentId, currentMonth, previousMonth, currentYear, previousYear);
            double cooperativePercentageChange = await CalculateCooperativePercentagePerformanceByAgent(agentId, currentMonth, previousMonth, currentYear, previousYear);
            var metrics = new BusinessEntitiesMetrics
            {
                //TotalCooperative = await _context.MemberShips.CountAsync(c =>
                // (!string.IsNullOrEmpty(query.organizationCode) && (c.OrganizationCode.Contains(query.organizationCode)))
           //&& c.BelongToBusinessCode == query.businessCode
           //  ),
                TotalFarmer = farmers.Count,
                TotalWarehouse = await _context.Warehouses.CountAsync(w =>
                 (!string.IsNullOrEmpty(query.organizationCode) && (w.OrganizationCode.Contains(query.organizationCode)))
           &&
                w.BelongToBusinessCode == query.businessCode && w.ManagedByUserId == agentId),
                PercentageFarmerPerformance = percentageChange,
                PercentageCooperativePerformance = cooperativePercentageChange,
            };

            return metrics;
        }

        private async Task<double> CalculateCooperativePercentagePerformanceByAgent(string agentId, int currentMonth, int previousMonth, int currentYear, int previousYear)
        {
            var totalCooperativesThisMonth = await _context.MemberShips
                .CountAsync(f =>
                f.OnBoardedOn.Year == currentYear && f.OnBoardedOn.Month == currentMonth);

            var totalCooperativesLastMonth = await _context.MemberShips
                .CountAsync(f =>
                f.OnBoardedOn.Year == previousYear && f.OnBoardedOn.Month == previousMonth);

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

            double percentageChange = await CalculateFarmerPercentagePerformance(
                businessCode, organizationCode, currentMonth, previousMonth, currentYear, previousYear);
            double cooperativePercentageChange = await CalculateCooperativePercentagePerformance(
                businessCode, organizationCode, currentMonth, previousMonth, currentYear, previousYear);
            double agentPercentageChange = await CalculateAgentPercentagePerformance(
                businessCode, organizationCode, currentMonth, previousMonth, currentYear, previousYear);

            var agentsQuery = _context.Agents.Where(a => a.BelongToBusinessCode == businessCode);
            var cooperativesQuery = _context.MemberShips.Where(c => c.BelongToBusinessCode == businessCode);
            var farmersQuery = _context.Farmers.Where(f => f.BelongToBusinessCode == businessCode);
            var warehousesQuery = _context.Warehouses.Where(w => w.BelongToBusinessCode == businessCode);

            if (!string.IsNullOrEmpty(organizationCode))
            {
               // agentsQuery = agentsQuery.Where(a => a.OrganizationCode.Contains(organizationCode));
                //cooperativesQuery = cooperativesQuery.Where(c => c.OrganizationCode.Contains(organizationCode));

                farmersQuery = from f in farmersQuery
                               join fo in _context.FarmerOrganizations
                                   on f.Id equals fo.FarmerId
                               where fo.OrganizationCode == organizationCode
                               select f;

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

            //if (!string.IsNullOrEmpty(organizationCode))
            //{
            //    agentsQuery = agentsQuery.Where(f => f.OrganizationCode.Contains(organizationCode));
            //}

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
            var cooperativesQuery = _context.MemberShips.Where(f =>
                f.BelongToBusinessCode == businessCode);

            //if (!string.IsNullOrEmpty(organizationCode))
            //{
            //    cooperativesQuery = cooperativesQuery.Where(f => f.OrganizationCode.Contains(organizationCode));
            //}

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
            var farmersQuery = _context.Farmers.Where(f => f.BelongToBusinessCode == businessCode);

            if (!string.IsNullOrEmpty(organizationCode))
            {
                farmersQuery = from farmer in farmersQuery
                               join fo in _context.FarmerOrganizations
                                   on farmer.Id equals fo.FarmerId
                               where fo.OrganizationCode == organizationCode
                               select farmer;
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
        public async Task<(bool status, FarmerModel data)> CreateFarmerV2(FarmerModel data)
        {
            var farmer = await _context.Farmers
                .FirstOrDefaultAsync(f => f.UserId == data.UserId);

            if (farmer == null)
            {
                farmer = data.Map();
                await _context.Farmers.AddAsync(farmer);
                await _context.SaveChangesAsync();
            }
            else
            {
                farmer.Name = data.Name;
                farmer.PhoneNumber = data.PhoneNumber;
                farmer.Email = data.Email;
                _context.Farmers.Update(farmer);
                await _context.SaveChangesAsync();
            }

            if (!string.IsNullOrEmpty(data.OrganizationCode))
            {
                bool exists = await _context.FarmerOrganizations.AnyAsync(x =>
                    x.FarmerId == farmer.Id &&
                    x.OrganizationCode == data.OrganizationCode);

                if (!exists)
                {
                    await _context.FarmerOrganizations.AddAsync(new FarmerOrganization
                    {
                        FarmerId = farmer.Id,
                        OrganizationCode = data.OrganizationCode,
                        OnboardedByUserId = data.OnboardedByUserId
                    });
                }
            }
            if (data.CooperativeIds?.Any() == true)
            {
                foreach (var coopId in data.CooperativeIds)
                {
                    bool exists = await _context.FarmerCooperatives.AnyAsync(x =>
                        x.FarmerId == farmer.Id && x.CooperativeId == coopId);

                    if (!exists)
                    {
                        await _context.FarmerCooperatives.AddAsync(new FarmerCooperative
                        {
                            FarmerId = farmer.Id,
                            CooperativeId = coopId,
                            OnboardedByUserId = data.OnboardedByUserId
                        });
                    }
                }
            }
            if (data.AgentIds?.Any() == true)
            {
                foreach (var agentId in data.AgentIds)
                {
                    bool exists = await _context.FarmerAgents.AnyAsync(x =>
                        x.FarmerId == farmer.Id && x.AgentId == agentId);

                    if (!exists)
                    {
                        await _context.FarmerAgents.AddAsync(new FarmerAgent
                        {
                            FarmerId = farmer.Id,
                            AgentId = agentId,
                            AssignedByUserId = data.OnboardedByUserId
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();

            await UpdateFarmerValueChainItems(farmer.Id, data.SelectedItems);

            return (true, farmer.Map());
        }

        public async Task<(bool status, FarmerModel data)> CreateFarmer(FarmerModel data)
        {
            var farmer = await _context.Farmers.FirstOrDefaultAsync(f => f.UserId == data.UserId);

            if (farmer != null)
            {
                await UpdateFarmerProfileAsync(data.UserId, new FarmerUpdateModel
                {
                    DateOfBirth = data.DateOfBirth,
                    SelectedItems = data.SelectedItems,
                    State = data.State,
                    AgentIds = data.AgentIds,
                    CooperativeIds = data.CooperativeIds,
                    BVN = data.BVN,
                    Email = data.Email,
                    FarmerCategory = data.FarmerCategory,
                    FarmerImage = data.FarmerImage,
                    Gender = data.Gender,
                    HighestLevelOfEducation = data.HighestLevelOfEducation,
                    IsNinVerified = data.IsNinVerified,
                    Lga = data.Lga,
                    Location = data.Location,
                    Name = data.Name,
                    NIN = data.NIN,
                    OrganizationCode = data.OrganizationCode,
                    PhoneNumber = data.PhoneNumber,
                    Ward = data.Ward,
                    YearsInAgric = data.YearsInAgric
                });

            }
            else
            {
                farmer = data.Map();
                await _context.Farmers.AddAsync(farmer);
                await _context.SaveChangesAsync();
            }

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

            if (data.CooperativeIds != null && data.CooperativeIds.Any())
            {
                foreach (var coopId in data.CooperativeIds.Distinct())
                {
                    bool alreadyLinked = await _context.FarmerCooperatives
                        .AnyAsync(fc => fc.FarmerId == farmer.Id && fc.CooperativeId == coopId);

                    if (!alreadyLinked)
                    {
                        await _context.FarmerCooperatives.AddAsync(new FarmerCooperative
                        {
                            FarmerId = farmer.Id,
                            CooperativeId = coopId,
                            OnboardedByUserId = data.OnboardedByUserId,
                            JoinedOn = DateTimeOffset.Now
                        });
                    }
                }
                await _context.SaveChangesAsync();
            }

            if (data.AgentIds != null && data.AgentIds.Any())
            {
                foreach (var agentId in data.AgentIds.Distinct())
                {
                    bool alreadyLinked = await _context.FarmerAgents
                        .AnyAsync(fa => fa.FarmerId == farmer.Id && fa.AgentId == agentId);

                    if (!alreadyLinked)
                    {
                        await _context.FarmerAgents.AddAsync(new FarmerAgent
                        {
                            FarmerId = farmer.Id,
                            AgentId = agentId,
                            AssignedByUserId = data.OnboardedByUserId,
                            AssignedOn = DateTimeOffset.Now
                        });
                    }
                }
                await _context.SaveChangesAsync();
            }
            if (data.SelectedItems != null && data.SelectedItems.Any())
            {
                await UpdateFarmerValueChainItems(farmer.Id, data.SelectedItems);
            }

            return (true, farmer.Map());
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

        public async Task<FarmerModel> GetFarmerByUserId(string userId)
        {
            var farmer = await _context.Farmers
              .FirstOrDefaultAsync(x => x.UserId == userId);

            if (farmer == null)
                throw new Exception("No record found");

            var cooperativeNames = await (
              from fc in _context.FarmerCooperatives
              join c in _context.MemberShips
                  on fc.CooperativeId equals c.Id
              where fc.FarmerId == farmer.Id
              select c.Name
          ).ToListAsync();

            var farmerImage = await _context.FarmerImages
             .Where(img => img.FarmerUserId == farmer.UserId)
             .Select(img => img.IMageKey)
             .FirstOrDefaultAsync();

            var mappedFarmer = farmer.Map();

            mappedFarmer.CooperativeNames = cooperativeNames;

            mappedFarmer.CooperativeName = cooperativeNames.FirstOrDefault();

            mappedFarmer.FarmerImage = farmerImage;

            return mappedFarmer;
        }

        //public async Task<FarmerModel> GetFarmerByUserId(string userId)
        //{
        //    var farmers = await _context.Farmers.
        //       Where(x => x.UserId == userId).FirstOrDefaultAsync();

        //    if (farmers == null)
        //    {
        //        throw new Exception("No record found");
        //    }

        //    // Fetch cooperative names for all distinct cooperativeIds in farmers

        //    var cooperatives = await _context.Cooperatives
        //        .Where(c => c.Id == farmers.BelongToCooperativeId)
        //        .ToDictionaryAsync(c => c.Id, c => c.Name);

        //    // Fetch farmer images

        //    var farmerImages = await _context.FarmerImages
        //        .Where(img => img.FarmerUserId == farmers.UserId)
        //        .ToDictionaryAsync(img => img.FarmerUserId, img => img.IMageKey); // Assuming ImageUrl is the image path



        //    var mappedFarmer = farmers.Map();

        //    if (cooperatives.TryGetValue(mappedFarmer.BelongToCooperativeId, out var cooperativeName))
        //    {
        //        mappedFarmer.CooperativeName = cooperativeName;
        //    }

        //    if (farmerImages.TryGetValue(mappedFarmer.UserId, out var imageUrl))
        //    {
        //        mappedFarmer.FarmerImage = imageUrl;
        //    }

        //    return mappedFarmer;

        //}

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
        public async Task<bool> DoesPhoneAlreadyExistWithinOrganization(string phonenumber, string organizationCode)
        {
            var query = from farmer in _context.Farmers
                        join fo in _context.FarmerOrganizations
                            on farmer.Id equals fo.FarmerId
                        where farmer.PhoneNumber == phonenumber
                              && fo.OrganizationCode == organizationCode
                        select farmer;
            return await query.AnyAsync();
        }
        public async Task<List<string>> GetAllPhonesForOrganizationAsync(string organizationCode)
        {
            var query =
                from farmer in _context.Farmers
                join fo in _context.FarmerOrganizations
                    on farmer.Id equals fo.FarmerId
                where fo.OrganizationCode == organizationCode
                select farmer.PhoneNumber;

            return await query
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct()
                .ToListAsync();
        }
        public async Task<(int totalFarms, double totalSize)> GetAgentFarmStats(string agentUserId)
        {
            var stats = await (
                from f in _context.Farmers
                join fa in _context.FarmerAgents on f.Id equals fa.FarmerId
                join a in _context.Agents on fa.AgentId equals a.Id
                join farm in _context.Farms on f.Id equals farm.FarmerId
                where a.UserId == agentUserId
                group farm by 1 into g
                select new
                {
                    TotalFarms = g.Count(),
                    TotalSize = g.Sum(x => x.SizeInHectares)
                }
            ).FirstOrDefaultAsync();

            return (
                stats?.TotalFarms ?? 0,
                stats?.TotalSize ?? 0
            );
        }
        /*
         * Total Farms
Total Wallet Balance
Total Input Received
Outstanding Input Debt
Operational Overview (e.g., farms in progress, completed, or active)
         */


        public async Task<FarmerDashboardDto> GetFarmerDashboard(string farmerUserId)
        {
            var farmer = await _context.Farmers
                .FirstOrDefaultAsync(f => f.UserId == farmerUserId);

            if (farmer == null)
                return new FarmerDashboardDto();

            var farmerId = farmer.Id;

            // -------------------- Total Farms --------------------
            var farmsQuery = _context.Farms
                .Where(f => f.FarmerId == farmerId);

            var totalFarms = await farmsQuery.CountAsync();

            // -------------------- Operational Overview --------------------
            var operationalStats = await farmsQuery
                .GroupBy(f => 1)
                .Select(g => new
                {
                    NotStarted = g.Count(x => x.MappingStatus == MappingStatus.UnMapped),
                    InProgress = g.Count(x => x.MappingStatus == MappingStatus.Mapped),
                    Completed = g.Count(x => x.MappingStatus == MappingStatus.Verified)
                })
                .FirstOrDefaultAsync();

            // -------------------- Wallet Balance --------------------
            var walletBalance = await _context.UserWallets
    .Where(w => w.UserId == farmerUserId && !w.IsDeleted)
    .SumAsync(w => (decimal?)w.WalletBalance) ?? 0m;

            // -------------------- Input Financials --------------------
            //var inputStats = await _context.InputTransactions
            //    .Where(i => i.FarmerId == farmerId)
            //    .GroupBy(i => 1)
            //    .Select(g => new
            //    {
            //        TotalReceived = g
            //            .Where(x => x.TransactionType == InputTransactionType.Disbursement)
            //            .Sum(x => (decimal?)x.Amount) ?? 0,

            //        TotalRepayment = g
            //            .Where(x => x.TransactionType == InputTransactionType.Repayment)
            //            .Sum(x => (decimal?)x.Amount) ?? 0
            //    })
            //    .FirstOrDefaultAsync();

            //var totalInputReceived = inputStats?.TotalReceived ?? 0;
            //var totalRepayment = inputStats?.TotalRepayment ?? 0;
            //var outstandingDebt = totalInputReceived - totalRepayment;

            // -------------------- Return Dashboard --------------------
            return new FarmerDashboardDto
            {
                TotalFarms = totalFarms,
                TotalWalletBalance = walletBalance,
                TotalInputReceived = 0,//totalInputReceived,
                OutstandingInputDebt = 0,//outstandingDebt,

                NotMapped = operationalStats?.NotStarted ?? 0,
                InProgressFarms = operationalStats?.InProgress ?? 0,
                CompletedFarms = operationalStats?.Completed ?? 0
            };
        }
        public async Task<AgentDashboardDto> GetAgentDashboardData(string agentUserId)
        {
            var farmerQuery =
                from f in _context.Farmers
                join fa in _context.FarmerAgents on f.Id equals fa.FarmerId
                join a in _context.Agents on fa.AgentId equals a.Id
                where a.UserId == agentUserId
                select f;

            var farmerIds = await farmerQuery.Select(f => f.Id).ToListAsync();

            if (!farmerIds.Any())
            {
                return new AgentDashboardDto();
            }

            var totalFarmers = farmerIds.Count;

            var totalFarmersLinkedToCoops = await _context.FarmerCooperatives
                .Where(fc => farmerIds.Contains(fc.FarmerId))
                .Select(fc => fc.FarmerId)
                .Distinct()
                .CountAsync();

            var farmStats = await _context.Farms
                .Where(f => farmerIds.Contains(f.FarmerId))
                .GroupBy(f => 1)
                .Select(g => new
                {
                    TotalFarms = g.Count(),
                    TotalSize = g.Sum(x => x.SizeInHectares)
                })
                .FirstOrDefaultAsync();

            var totalFarms = farmStats?.TotalFarms ?? 0;
            var totalFarmSize = farmStats?.TotalSize ?? 0;

            var mappedFarmers = await _context.Farms
                .Where(f => farmerIds.Contains(f.FarmerId))
                .Select(f => f.FarmerId)
                .Distinct()
                .CountAsync();

            var totalCooperatives = await (
                from c in _context.MemberShips
                join ac in _context.MemberShipAgents on c.Id equals ac.MemberShipId
                join a in _context.Agents on ac.AgentId equals a.Id
                where a.UserId == agentUserId
                select c.Id
            ).Distinct().CountAsync();

            return new AgentDashboardDto
            {
                TotalCooperatives = totalCooperatives,
                TotalFarmers = totalFarmers,
                TotalFarmSizeHectares = totalFarmSize,
                TotalFarms = totalFarms,

                MappedFarms = mappedFarmers,
                UnmappedFarms = totalFarmers - mappedFarmers,

                FarmersLinkedToCooperatives = totalFarmersLinkedToCoops,
                FarmersNotLinkedToCooperatives = totalFarmers - totalFarmersLinkedToCoops
            };
        }

        public async Task<PagedListResponse<List<AgentMembershipDto>>> GetAgentsByFarmer(
       long farmerId,
       ListQueryParams query)
        {
            query ??= new ListQueryParams();
            if (query.PageNumber <= 0) query.PageNumber = 1;
            if (query.PageSize <= 0) query.PageSize = 10;

            var baseQuery =
                from a in _context.Agents
                join ma in _context.FarmerAgents on a.Id equals ma.AgentId
                where ma.FarmerId == farmerId
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

        public async Task<PagedListResponse<List<FarmerAgentDto>>> GetFarmersByAgent(
     string agentId,
     ListQueryParams query)
        {
            query ??= new ListQueryParams();
            if (query.PageNumber <= 0) query.PageNumber = 1;
            if (query.PageSize <= 0) query.PageSize = 10;

            // Build base query joining FarmerAgents -> Agents to filter by agent's UserId
            var queryable = from f in _context.Farmers
                            join fa in _context.FarmerAgents on f.Id equals fa.FarmerId
                            join a in _context.Agents on fa.AgentId equals a.Id
                            where a.UserId == agentId
                            select f;

            // Apply optional filters
            if (!string.IsNullOrEmpty(query.businessCode))
                queryable = queryable.Where(f => f.BelongToBusinessCode == query.businessCode);

            if (!string.IsNullOrEmpty(query.organizationCode))
                queryable = from f in queryable
                            join fo in _context.FarmerOrganizations on f.Id equals fo.FarmerId
                            where fo.OrganizationCode == query.organizationCode
                            select f;

            if (query.startDate.HasValue)
                queryable = queryable.Where(f => f.OnBoardedOn >= query.startDate.Value);
            if (query.endDate.HasValue)
                queryable = queryable.Where(f => f.OnBoardedOn <= query.endDate.Value);

            if (!string.IsNullOrWhiteSpace(query.State))
                queryable = queryable.Where(f => f.State == query.State);

            if (query.Id > 0)
                queryable = queryable.Where(f => f.Id == query.Id);

            // materialize filtered farmers
            var farmers = await queryable.OrderByDescending(f => f.OnBoardedOn).ToListAsync();

            if (!farmers.Any())
            {
                return new PagedListResponse<List<FarmerAgentDto>> { Data = new List<FarmerAgentDto>(), Count = 0, PageNumber = query.PageNumber, PageSize = query.PageSize };
            }

            var farmerIds = farmers.Select(x => x.Id).ToList();

            // preload cooperatives membership
            var coopLookup = await _context.FarmerCooperatives
                .Where(fc => farmerIds.Contains(fc.FarmerId))
                .GroupBy(fc => fc.FarmerId)
                .ToDictionaryAsync(g => g.Key, g => g.Count() > 0);

            // preload farm mapping existence
            var mappedLookup = await _context.Farms
                .Where(p => farmerIds.Contains(p.FarmerId))
                .GroupBy(p => p.FarmerId)
                .ToDictionaryAsync(g => g.Key, g => g.Count() > 0);

            // preload farm counts and total size (hectares) per farmer
            var farmStatsLookup = await _context.Farms
                .Where(p => farmerIds.Contains(p.FarmerId))
                .GroupBy(p => p.FarmerId)
                .ToDictionaryAsync(g => g.Key, g => new { Count = g.Count(), 
                    TotalSize = g.Sum(x => x.FarmSize) });

            // apply mapping status filter if provided
            if (!string.IsNullOrWhiteSpace(query.MappingStatus))
            {
                bool wantMapped = query.MappingStatus.Equals("Mapped", StringComparison.OrdinalIgnoreCase);
                farmers = farmers.Where(f => mappedLookup.GetValueOrDefault(f.Id, false) == wantMapped).ToList();
            }

            // apply cooperative status filter if provided
            if (!string.IsNullOrWhiteSpace(query.CooperativeStatus))
            {
                bool wantLinked = query.CooperativeStatus.Equals("Linked", StringComparison.OrdinalIgnoreCase);
                farmers = farmers.Where(f => coopLookup.GetValueOrDefault(f.Id, false) == wantLinked).ToList();
            }

            var totalCount = farmers.Count;

            // compute totals over filtered set
            var totalLinked = farmers.Count(f => coopLookup.GetValueOrDefault(f.Id, false));
            var totalMapped = farmers.Count(f => mappedLookup.GetValueOrDefault(f.Id, false));

            // paging
            var skip = (query.PageNumber - 1) * query.PageSize;
            var page = farmers.Skip(skip).Take(query.PageSize).ToList();

            // projection
            var result = page.Select(f => new FarmerAgentDto
            {
                Id = f.Id,
                FarmerUserId = f.UserId,
                FullName = f.Name,
                State = f.State,
                Lga = f.Lga,
                CooperativeStatus = coopLookup.GetValueOrDefault(f.Id, false) ? "Linked" : "Not Linked",
                MappingStatus = mappedLookup.GetValueOrDefault(f.Id, false) ? "Mapped" : "Unmapped",
                DateAdded = f.OnBoardedOn,
                TotalLinkedFarmers = totalLinked,
                TotalMappedFarms = totalMapped,
                // per-farmer farm counts and total size in hectares
                TotalFarms = farmStatsLookup.GetValueOrDefault(f.Id)?.Count ?? 0,
                TotalFarmSizeHectares = farmStatsLookup.GetValueOrDefault(f.Id)?.TotalSize ?? 0,
            }).ToList();

            return new PagedListResponse<List<FarmerAgentDto>>
            {
                Data = result,
                Count = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };
        }
        public async Task<FarmerMembershipOverviewDto> GetFarmerMembershipDetails(string userId)
        {
            var membership = await _context.MemberShips
                .Where(m => m.UserId == userId)
                .Select(m => new
                {
                    m.Id,
                    m.Name,
                    Type = m.MemberShipType.Name,
                    Status = m.IsActive ? "Active" : "NotActive",
                    m.Location,
                    m.OnBoardedOn
                })
                .FirstOrDefaultAsync();

            if (membership == null) return null;

            var numberOfFarmers = await _context.FarmerCooperatives
                .Where(fc => fc.CooperativeId == membership.Id)
                .Select(fc => fc.FarmerId)
                .Distinct()
                .CountAsync();

            var farmStats = await (
                from fc in _context.FarmerCooperatives
                join f in _context.Farms on fc.FarmerId equals f.FarmerId
                where fc.CooperativeId == membership.Id
                group f by fc.CooperativeId into g
                select new
                {
                    TotalFarms = g.Count(),
                    TotalMappedFarms = g.Count(x => x.MappingStatus == MappingStatus.Mapped || x.MappingStatus == MappingStatus.Verified) // adjust if mapping logic differs
                }
            ).FirstOrDefaultAsync();

            var result = new FarmerMembershipOverviewDto
            {
                MembershipId = membership.Id,
                Name = membership.Name,
                Type = membership.Type,
                Status = membership.Status,
                Location = membership.Location,
                DateCreated = membership.OnBoardedOn,
                NumberOfFarmers = numberOfFarmers,
                TotalFarmCount = farmStats?.TotalFarms ?? 0,
                TotalMappedFarms = farmStats?.TotalMappedFarms ?? 0
            };

            return result;
        }
        public async Task<List<FarmerMembershipSummaryDto>> GetMembershipsByFarmer(ListQueryParams args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            if (args.Id <= 0) return new List<FarmerMembershipSummaryDto>();

            int pageNumber = args.PageNumber > 0 ? args.PageNumber : 1;
            int pageSize = args.PageSize > 0 ? args.PageSize : 20;

            var query = from fc in _context.FarmerCooperatives
                        join m in _context.MemberShips on fc.CooperativeId equals m.Id
                        where fc.FarmerId == args.Id
                        select new
                        {
                            m.Id,
                            m.Name,
                            m.MemberShipType,
                            m.IsActive
                        };

            if (!string.IsNullOrWhiteSpace(args.FilterBy))
            {
                var filter = args.FilterBy.Trim().ToLower();
                query = query.Where(m =>
                    m.Name.ToLower().Contains(filter) ||
                    m.MemberShipType.Name.ToLower().Contains(filter)
                );
            }

            var totalCount = await query.CountAsync();

            var memberships = await query
                .OrderBy(m => m.Name) 
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Distinct()
                .ToListAsync();

            if (!memberships.Any())
                return new List<FarmerMembershipSummaryDto>();

            var result = memberships.Select(m => new FarmerMembershipSummaryDto
            {
                MembershipId = m.Id,
                Name = m.Name,
                Type = m.MemberShipType.Name,
                Status = m.IsActive ? "Active" : "NotActive"
            }).ToList();

            return result;
        }
        public async Task<PagedListResponse<List<FarmerMembershipDto>>> GetFarmersByMembership(
    long cooperativeId,
    ListQueryParams query)
        {
            query ??= new ListQueryParams();
            if (query.PageNumber <= 0) query.PageNumber = 1;
            if (query.PageSize <= 0) query.PageSize = 10;

            var baseQuery =
                from f in _context.Farmers
                join fc in _context.FarmerCooperatives on f.Id equals fc.FarmerId
                join c in _context.MemberShips on fc.CooperativeId equals c.Id
                where c.Id == cooperativeId
                select f;

            if (!string.IsNullOrWhiteSpace(query.businessCode))
                baseQuery = baseQuery.Where(f => f.BelongToBusinessCode == query.businessCode);

            if (!string.IsNullOrWhiteSpace(query.organizationCode))
            {
                baseQuery =
                    from f in baseQuery
                    join fo in _context.FarmerOrganizations on f.Id equals fo.FarmerId
                    where fo.OrganizationCode == query.organizationCode
                    select f;
            }

            if (query.startDate.HasValue && query.endDate.HasValue)
            {
                baseQuery = baseQuery.Where(f =>
                    f.OnBoardedOn >= query.startDate.Value &&
                    f.OnBoardedOn <= query.endDate.Value);
            }
            else if (query.startDate.HasValue)
            {
                var date = query.startDate.Value.Date;
                baseQuery = baseQuery.Where(f => f.OnBoardedOn.Date == date);
            }

            if (!string.IsNullOrWhiteSpace(query.State))
               baseQuery = baseQuery.Where(f => f.State == query.State);
                //baseQuery = baseQuery.Where(f => f.State.Contains(query.State));

            if (query.Id > 0)
                baseQuery = baseQuery.Where(f => f.Id == query.Id);

            var totalCount = await baseQuery.CountAsync();

            var farmers = await baseQuery
                .OrderByDescending(f => f.OnBoardedOn)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            if (!farmers.Any())
            {
                return new PagedListResponse<List<FarmerMembershipDto>>
                {
                    Data = new List<FarmerMembershipDto>(),
                    Count = 0,
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize
                };
            }

            var farmerIds = farmers.Select(f => f.Id).ToList();

            var farmsLookup = await _context.Farms
                .Where(p => farmerIds.Contains(p.FarmerId))
                .GroupBy(p => p.FarmerId)
                .ToDictionaryAsync(g => g.Key, g => new
                {
                    TotalFarms = g.Count(),
                    LandSize = g.Sum(x => x.FarmSize)
                });

            var result = farmers.Select(f =>
            {
                farmsLookup.TryGetValue(f.Id, out var farmData);

                return new FarmerMembershipDto
                {
                    Id = f.Id,
                    FarmerUserId = f.UserId,
                    FullName = f.Name,
                    DateAdded = f.OnBoardedOn,
                    State = f.State,
                    Lga = f.Lga,
                    Level = EnumExtensions.GetDisplayName(f.Level),
                    TotalFarms = farmData?.TotalFarms ?? 0,
                    LandSize = farmData?.LandSize ?? 0,
                    MappingStatus = (farmData?.TotalFarms ?? 0) > 0 ? "Mapped" : "Unmapped"
                };
            }).ToList();

            return new PagedListResponse<List<FarmerMembershipDto>>
            {
                Data = result,
                Count = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };
        }
       
        public async Task<List<FarmerModel>> GetFarmersByCooperativeId(
            long? cooperativeId,
            ListQueryParams query)
        {
            query ??= new ListQueryParams();

            // Base farmers query
            var farmersQuery = _context.Farmers
                .AsNoTracking()
                .AsQueryable();

            // Filter by business code
            if (!string.IsNullOrWhiteSpace(query.businessCode))
            {
                farmersQuery = farmersQuery
                    .Where(f => f.BelongToBusinessCode == query.businessCode);
            }

            // Filter by organization (many-to-many)
            if (!string.IsNullOrWhiteSpace(query.organizationCode))
            {
                farmersQuery =
                    from farmer in farmersQuery
                    join fo in _context.FarmerOrganizations.AsNoTracking()
                        on farmer.Id equals fo.FarmerId
                    where fo.OrganizationCode == query.organizationCode
                    select farmer;
            }

            // Filter by cooperative (many-to-many)
            if (cooperativeId.HasValue)
            {
                farmersQuery =
                    from farmer in farmersQuery
                    join fc in _context.FarmerCooperatives.AsNoTracking()
                        on farmer.Id equals fc.FarmerId
                    where fc.CooperativeId == cooperativeId.Value
                    select farmer;
            }

            // Remove duplicates caused by joins
            farmersQuery = farmersQuery.Distinct();

            // Main projection
            var queryResult =
                from farmer in farmersQuery

                join fc in _context.FarmerCooperatives.AsNoTracking()
                    on farmer.Id equals fc.FarmerId into fcGroup
                from farmerCoop in fcGroup.DefaultIfEmpty()

                join coop in _context.MemberShips.AsNoTracking()
                    on farmerCoop.CooperativeId equals coop.Id into coopGroup
                from cooperative in coopGroup.DefaultIfEmpty()

                join image in _context.FarmerImages.AsNoTracking()
                    on farmer.UserId equals image.FarmerUserId into imageGroup
                from img in imageGroup.DefaultIfEmpty()

                join subAccount in _context.SubAccounts.AsNoTracking()
                    on farmer.UserId equals subAccount.UserId into subGroup
                from sub in subGroup.DefaultIfEmpty()

                orderby farmer.OnBoardedOn descending

                select new
                {
                    Farmer = farmer,
                    CooperativeName = cooperative != null ? cooperative.Name : null,
                    FarmerImageKey = img != null ? img.IMageKey : null,
                    SubAccount = sub
                };

            // Pagination
            if (query.PageSize > 0)
            {
                var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;

                var skip = (pageNumber - 1) * query.PageSize;

                queryResult = queryResult
                    .Skip(skip)
                    .Take(query.PageSize);
            }


            var result = await queryResult.ToListAsync();

            if (!result.Any())
                return new List<FarmerModel>();

            // Final mapping
            return result
                .GroupBy(x => x.Farmer.Id) // Prevent duplicates per farmer
                .Select(group =>
                {
                    var item = group.First();

                    var mapped = item.Farmer.Map();
                    mapped.FarmerImage = item.FarmerImageKey;

                    // Multiple cooperatives → comma-separated (or change to List if preferred)
                    mapped.CooperativeName = string.Join(", ",
                        group
                            .Where(x => !string.IsNullOrWhiteSpace(x.CooperativeName))
                            .Select(x => x.CooperativeName)
                            .Distinct());

                    if (item.SubAccount != null)
                    {
                        mapped.NIN = item.SubAccount.NIN;
                        mapped.BVN = item.SubAccount.Bvn;
                        mapped.IsNinVerified = item.SubAccount.IsNinVerified;
                    }

                    return mapped;
                })
                .ToList();
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

                //join cooperative in _context.Cooperatives
                //    on farmer.BelongToCooperativeId equals cooperative.Id into cooperativeGroup
                //from coop in cooperativeGroup.DefaultIfEmpty() 

                join fc in _context.FarmerCooperatives
       on farmer.Id equals fc.FarmerId into fcGroup
                from fc in fcGroup.DefaultIfEmpty()

                join cooperative in _context.MemberShips
                    on fc.CooperativeId equals cooperative.Id into coopGroup
                from coop in coopGroup.DefaultIfEmpty()

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
        public async Task<FarmerImagesModel> GetUserImage(string userId, string businessCode, string? organizationCode)
        {

            var query = _context.FarmerImages
        .Where(f => f.FarmerUserId == userId && f.BelongToBusinessCode == businessCode);

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
        public async Task<FarmerModel> AssignFarmerToAgents(AssignmentArgs args, long farmerId)
        {
            if (args.AgentIds == null || !args.AgentIds.Any())
                return null;

            // Check if the farmer exists
            var farmer = await _context.Farmers
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == farmerId);

            if (farmer == null)
                return null;

            // Get existing assignments for this farmer
            var existingAssignments = await _context.FarmerAgents
                .Where(fa => fa.FarmerId == farmerId && args.AgentIds.Contains(fa.AgentId))
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
                var newAssignments = newAgentIds.Select(agentId => new FarmerAgent
                {
                    FarmerId = farmerId,
                    AgentId = agentId,
                    AssignedOn = DateTimeOffset.UtcNow
                }).ToList();

                await _context.FarmerAgents.AddRangeAsync(newAssignments);
                await _context.SaveChangesAsync();
            }

            // Reload the farmer with updated data
            var updatedFarmer = await _context.Farmers
                .FirstOrDefaultAsync(f => f.Id == farmerId);

            return updatedFarmer.Map();
        }


        public async Task<FarmerModel> AssignFarmerToAgent(string onboardedByUserId, long farmerId)
        {
            var farmer = await _context.Farmers.Where(c => c.Id == farmerId).FirstOrDefaultAsync();
            if (farmer == null) return null;
            farmer.OnboardedByUserId = onboardedByUserId;

            await _context.SaveChangesAsync();

            return farmer.Map();
        }
        public async Task<FarmerProfileDto?> GetFarmerProfileAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            var farmer = await _context.Farmers
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.UserId == userId);

            if (farmer == null)
                return null;

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            var farmerId = farmer.Id;


            var farms = await _context.Farms
                .AsNoTracking()
                .Where(f => f.FarmerId == farmerId)
                .ToListAsync();

            var coordinates = await _context.FarmCoordinates
                .AsNoTracking()
                .Where(c => farms.Select(f => f.Id).Contains(c.FarmId))
                .ToListAsync();

            var documents = await _context.FarmLandDocuments
                .AsNoTracking()
                .Where(d => farms.Select(f => f.Id).Contains(d.FarmId))
                .ToListAsync();

            var farmDtos = farms.Select(f =>
            {
                var latestDoc = documents
                    .Where(d => d.FarmId == f.Id)
                    .OrderByDescending(d => d.UploadedOn)
                    .FirstOrDefault();

                return new FarmerFarmDto
                {
                    FarmId = f.Id,
                    FarmName = f.Name,
                    Location = f.FarmLocation,
                    State = f.FarmState,
                    Lga = f.FarmLga,
                    LandSize = f.FarmSize,
                    MappingStatus = coordinates.Any(c => c.FarmId == f.Id)
                        ? "Mapped"
                        : "Unmapped",
                    DocumentStatus = latestDoc == null
                        ? "Pending"
                        : latestDoc.IsVerified ? "Verified" : "Pending"
                };
            }).ToList();

            var wallet = await _context.UserWallets
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.UserId == userId);

            var transactions = wallet == null
                ? new List<FarmerTransactionDto>()
                : await _context.UserWalletTransactions
                    .AsNoTracking()
                    .Where(t => t.UserWalletId == wallet.Id)
                    .OrderByDescending(t => t.TransactionDate)
                    .Select(t => new FarmerTransactionDto
                    {
                        TransactionId = t.Id,
                        Amount = t.TransactionAmount,
                        BalanceAfter = t.BalanceAfter,
                        TransactionType = EnumExtensions.GetDisplayName(t.WalletTransactionType),
                        Timestamp = t.TransactionDate,
                        Description = t.TransactionDescription
                    })
                    .ToListAsync();

            var activities = await _context.ActivityLogs
                .AsNoTracking()
                .Where(a => a.PerformedByUserId == userId)
                .OrderByDescending(a => a.CreatedOn)
                .Select(a => new FarmerActivityLogDto
                {
                    Timestamp = a.CreatedOn,
                    Description = a.Description,
                    ActivityType = EnumExtensions.GetDisplayName(a.ActivityType),
                    ActivityName = a.ActivityName
                })
                .ToListAsync();

            return new FarmerProfileDto
            {
                PersonalInformation = new FarmerPersonalInfoDto
                {
                    UserId = farmer.UserId,
                    Id = farmer.Id,
                    Name = farmer.Name,
                    Gender = farmer.Gender,
                    DateOfBirth = farmer.DateOfBirth,
                    PhoneNumber = farmer.PhoneNumber,
                    Email = farmer.Email,
                    BVN = user?.BVN,
                    NIN = user?.NIN,
                    DateRegistered = farmer.OnBoardedOn,
                    FarmerLevel = EnumExtensions.GetDisplayName(farmer.Level),
                    IsActive = user?.IsActive ?? false,
                },

                Location = new FarmerProfileLocationDto
                {
                    State = farmer.State,
                    Lga = farmer.Lga,
                    Ward = farmer.Ward,
                    Location = farmer.Location,
                },

                Farms = farmDtos,

                Wallet = new FarmerWalletDto
                {
                    TotalWalletBalance = wallet?.WalletBalance ?? 0,
                    Transactions = transactions
                },

                ActivityLogs = activities
            };
        }
         public async Task<FarmerDetailDto> GetFarmerDetails(string userId)
        {
             var farmer = await _context.Farmers
                .FirstOrDefaultAsync(f => f.UserId == userId);

            if (farmer == null)
                throw new Exception("Farmer not found");

               var cooperatives = await (
                from fc in _context.FarmerCooperatives
                join c in _context.MemberShips
                    on fc.CooperativeId equals c.Id
                where fc.FarmerId == farmer.Id
                select c
            ).ToListAsync();

              var agents = await (
                from fa in _context.FarmerAgents
                join a in _context.Agents
                    on fa.AgentId equals a.Id
                where fa.FarmerId == farmer.Id
                select a
            ).ToListAsync();

             var farmerImage = await _context.FarmerImages
                .FirstOrDefaultAsync(i => i.FarmerUserId == farmer.UserId);

              var subAccount = await _context.SubAccounts
                .FirstOrDefaultAsync(s => s.UserId == farmer.UserId);

              var plots = await _context.FarmPlotDetails
                .Where(p => p.FarmerId == farmer.Id)
                .ToListAsync();

             var repayments = await _context.Repayments
                .Include(r => r.RepaymentBreakdown)
                .Where(r => r.FarmerUserId == farmer.UserId)
                .ToListAsync();

             var visitations = await _context.FarmVisitationActivities
                .Where(v => v.FarmerUserId == userId)
                .OrderByDescending(v => v.CreatedOn)
                .ToListAsync();

            var valueChainSelections = await (
                from selection in _context.FarmerValueChainItems
                where selection.FarmerId == farmer.Id
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

              var mappedFarmer = farmer.Map();
            mappedFarmer.ValueChainItems = valueChainSelections;
            mappedFarmer.NIN = subAccount?.NIN;
            mappedFarmer.BVN = subAccount?.Bvn;
  var result = new FarmerDetailDto
            {
                Farmer = mappedFarmer,
                Cooperatives = cooperatives.Select(c => c.Map()).ToList(),
                Agents = agents.Select(a => a.Map()).ToList(),
                FarmerImage = farmerImage?.Map(),
                PlotDetails = plots.Select(p => p.Map()).ToList(),
                Repayments = repayments.Select(r => r.Map()).ToList(),
                Visitations = visitations.Select(v => v.Map()).ToList()
            };

            return result;
        }
        public async Task<PagedListResponse<List<FarmerDetailDto>>> GetAllFarmerDetails(
          string businessCode,
          string? organizationCode = null,
          IReadOnlyList<long>? cooperativeIds = null,
          IReadOnlyList<long>? agentIds = null,
          string? state = null,
          string? sortBy = "Date",
          bool sortAsc = true)
        {
              var query = _context.Farmers
                .AsNoTracking()
                .Where(f => f.BelongToBusinessCode == businessCode);

             if (!string.IsNullOrWhiteSpace(organizationCode))
            {
                query = query.Where(f =>
                    _context.FarmerOrganizations.Any(fo =>
                        fo.FarmerId == f.Id &&
                        fo.OrganizationCode == organizationCode));
            }

            // Cooperative filter (FarmerCooperatives)
            if (cooperativeIds?.Any() == true)
            {
                query = query.Where(f =>
                    _context.FarmerCooperatives.Any(fc =>
                        fc.FarmerId == f.Id &&
                        cooperativeIds.Contains(fc.CooperativeId)));
            }

            // Agent filter (FarmerAgents)
            if (agentIds?.Any() == true)
            {
                query = query.Where(f =>
                    _context.FarmerAgents.Any(fa =>
                        fa.FarmerId == f.Id &&
                        agentIds.Contains(fa.AgentId)));
            }

            // State filter
            if (!string.IsNullOrWhiteSpace(state))
            {
                query = query.Where(f => f.State == state);
            }

            // ----------------------------------------------------
            // 3. Count BEFORE projection
            // ----------------------------------------------------
            var totalCount = await query.CountAsync();

            // ----------------------------------------------------
            // 4. Sorting
            // ----------------------------------------------------
            query = sortBy switch
            {
                "Date" => sortAsc
                    ? query.OrderBy(x => x.OnBoardedOn)
                    : query.OrderByDescending(x => x.OnBoardedOn),

                _ => query.OrderByDescending(x => x.OnBoardedOn)
            };

            // ----------------------------------------------------
            // 5. Load farmers
            // ----------------------------------------------------
            var farmers = await query.ToListAsync();

            if (!farmers.Any())
            {
                return new PagedListResponse<List<FarmerDetailDto>>
                {
                    Data = new List<FarmerDetailDto>(),
                    Count = 0
                };
            }

            var farmerIds = farmers.Select(f => f.Id).ToList();
            var farmerUserIds = farmers.Select(f => f.UserId).ToList();

            // ----------------------------------------------------
            // 6. Load cooperatives (MANY per farmer)
            // ----------------------------------------------------
            var farmerCoops = await (
                from fc in _context.FarmerCooperatives.AsNoTracking()
                join c in _context.MemberShips.AsNoTracking()
                    on fc.CooperativeId equals c.Id
                where farmerIds.Contains(fc.FarmerId)
                select new
                {
                    fc.FarmerId,
                    Cooperative = c.Map()
                }
            ).ToListAsync();

            var cooperativesByFarmer = farmerCoops
                .GroupBy(x => x.FarmerId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Cooperative).ToList()
                );

            // ----------------------------------------------------
            // 7. Load agents (MANY per farmer)
            // ----------------------------------------------------
            var farmerAgents = await (
                from fa in _context.FarmerAgents.AsNoTracking()
                join a in _context.Agents.AsNoTracking()
                    on fa.AgentId equals a.Id
                where farmerIds.Contains(fa.FarmerId)
                select new
                {
                    fa.FarmerId,
                    Agent = a.Map()
                }
            ).ToListAsync();

            var agentsByFarmer = farmerAgents
                .GroupBy(x => x.FarmerId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Agent).ToList()
                );

            // ----------------------------------------------------
            // 8. Images
            // ----------------------------------------------------
            var images = await _context.FarmerImages
                .AsNoTracking()
                .Where(i => farmerUserIds.Contains(i.FarmerUserId))
                .ToDictionaryAsync(
                    x => x.FarmerUserId,
                    x => x.Map()
                );

            // ----------------------------------------------------
            // 9. SubAccounts
            // ----------------------------------------------------
            var subAccounts = await _context.SubAccounts
                .AsNoTracking()
                .Where(s => farmerUserIds.Contains(s.UserId))
                .ToDictionaryAsync(
                    x => x.UserId,
                    x => x
                );

            // ----------------------------------------------------
            // 10. Plot details
            // ----------------------------------------------------
            var plots = await _context.FarmPlotDetails
                .AsNoTracking()
                .Where(p => farmerIds.Contains(p.FarmerId))
                .Select(p => new { p.FarmerId, Plot = p.Map() })
                .ToListAsync();

            var plotsByFarmer = plots
                .GroupBy(x => x.FarmerId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Plot).ToList()
                );

            // ----------------------------------------------------
            // 11. Visitations
            // ----------------------------------------------------
            var visitations = await _context.FarmVisitationActivities
                .AsNoTracking()
                .Where(v => farmerUserIds.Contains(v.FarmerUserId))
                .Select(v => new { v.FarmerUserId, Visit = v.Map() })
                .ToListAsync();

            var visitsByFarmer = visitations
                .GroupBy(x => x.FarmerUserId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Visit).ToList()
                );

            // ----------------------------------------------------
            // 12. Value chains
            // ----------------------------------------------------
            var valueChains = await _context.FarmerValueChainItems
                .AsNoTracking()
                .Where(v => farmerIds.Contains(v.FarmerId))
                .ToListAsync();

            var valueChainsByFarmer = valueChains
                .GroupBy(v => v.FarmerId)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToList()
                );

            // ----------------------------------------------------
            // 13. Final projection
            // ----------------------------------------------------
            var result = farmers.Select(f =>
            {
                var mappedFarmer = f.Map();

                if (subAccounts.TryGetValue(f.UserId, out var sub))
                {
                    mappedFarmer = mappedFarmer.ApplySubAccount(sub);
                }

                return new FarmerDetailDto
                {
                    Farmer = mappedFarmer,

                    Cooperatives = cooperativesByFarmer.TryGetValue(f.Id, out var coops)
                        ? coops
                        : new List<MemberShipModel>(),

                    Agents = agentsByFarmer.TryGetValue(f.Id, out var agts)
                        ? agts
                        : new List<AgentModel>(),

                    FarmerImage = images.TryGetValue(f.UserId, out var img)
                        ? img
                        : null,

                    PlotDetails = plotsByFarmer.TryGetValue(f.Id, out var plotList)
                        ? plotList
                        : new List<FarmPlotDetailsModel>(),

                    Visitations = visitsByFarmer.TryGetValue(f.UserId, out var visits)
                        ? visits
                        : new List<FarmVisitationActivityModel>(),

                    ValueChainItems = valueChainsByFarmer.TryGetValue(f.Id, out var items)
                        ? items.Select(x => new FarmerValueChainSelectionDto
                        {
                            TypeName = x.ValueChainType,
                            ItemName = x.ItemName
                        }).ToList()
                        : new List<FarmerValueChainSelectionDto>()
                };
            }).ToList();

            // ----------------------------------------------------
            // 14. Return
            // ----------------------------------------------------
            return new PagedListResponse<List<FarmerDetailDto>>
            {
                Data = result,
                Count = totalCount
            };
        }

        // public async Task<PagedListResponse<List<FarmerDetailDto>>> GetAllFarmerDetails(
        //string businessCode,
        //string? organizationCode = null,
        //long? cooperativeId = null,
        //string? agentUserId = null,
        //string? state = null,
        //string? sortBy = "Name", // You can add other fields
        //bool sortAsc = true)
        // {
        //     var query = _context.Farmers
        //         .Where(f => f.BelongToBusinessCode == businessCode);

        //     // Filter by organization via FarmerOrganizations
        //     if (!string.IsNullOrEmpty(organizationCode))
        //     {
        //         query = from f in query
        //                 join fo in _context.FarmerOrganizations
        //                     on f.Id equals fo.FarmerId
        //                 where fo.OrganizationCode == organizationCode
        //                 select f;
        //     }

        //     // Filter by cooperative
        //     if (cooperativeId.HasValue)
        //     {
        //         query = query.Where(f => f.BelongToCooperativeId == cooperativeId.Value);
        //     }

        //     // Filter by state
        //     if (!string.IsNullOrEmpty(state))
        //     {
        //         query = query.Where(f => f.State == state);
        //     }

        //     // Count total before joins
        //     int totalCount = await query.CountAsync();

        //     // Include joins for details
        //     var farmers = await (
        //         from farmer in query
        //             .OrderByDescending(f => f.OnBoardedOn)

        //         join coop in _context.Cooperatives
        //             on farmer.BelongToCooperativeId equals coop.Id into coopGroup
        //         from cooperative in coopGroup.DefaultIfEmpty()

        //         join image in _context.FarmerImages
        //             on farmer.UserId equals image.FarmerUserId into imageGroup
        //         from farmerImage in imageGroup.DefaultIfEmpty()

        //         join agent in _context.Agents
        //             on cooperative.IsAssignedToAgentId equals agent.UserId into agentGroup
        //         from assignedAgent in agentGroup.DefaultIfEmpty()

        //         join subAccount in _context.SubAccounts
        //             on farmer.UserId equals subAccount.UserId into subAccountGroup
        //         from sub in subAccountGroup.DefaultIfEmpty()

        //         select new
        //         {
        //             Farmer = farmer,
        //             Cooperative = cooperative,
        //             FarmerImage = farmerImage,
        //             Agent = assignedAgent,
        //             SubAccount = sub
        //         }
        //     ).ToListAsync();

        //     var farmerIds = farmers.Select(f => f.Farmer.Id).ToList();

        //     // Preload value chain items
        //     var valueChainItems = await _context.FarmerValueChainItems
        //         .Where(v => farmerIds.Contains(v.FarmerId))
        //         .ToListAsync();

        //     var groupedValueChains = valueChainItems
        //         .GroupBy(v => v.FarmerId)
        //         .ToDictionary(g => g.Key, g => g.ToList());

        //     // Final projection
        //     var result = farmers.Select(f => new FarmerDetailDto
        //     {
        //         Farmer = f.Farmer.Map().ApplySubAccount(f.SubAccount),
        //         Cooperative = f.Cooperative?.Map(),
        //         FarmerImage = f.FarmerImage?.Map(),
        //         Agent = f.Agent?.Map(),
        //         PlotDetails = _context.FarmPlotDetails
        //             .Where(p => p.FarmerId == f.Farmer.Id)
        //             .Select(x => x.Map())
        //             .ToList(),

        //         Visitations = _context.FarmVisitationActivities
        //             .Where(v => v.FarmerUserId == f.Farmer.UserId)
        //             .Select(x => x.Map())
        //             .ToList(),

        //         ValueChainItems = groupedValueChains.TryGetValue(f.Farmer.Id, out var items)
        //             ? items.Select(x => new FarmerValueChainSelectionDto
        //             {
        //                 TypeName = x.ValueChainType,
        //                 ItemName = x.ItemName
        //             }).ToList()
        //             : new List<FarmerValueChainSelectionDto>()
        //     }).ToList();

        //     return new PagedListResponse<List<FarmerDetailDto>>
        //     {
        //         Data = result,
        //         Count = totalCount
        //     };
        // }


    }
}

