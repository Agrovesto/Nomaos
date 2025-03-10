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
        public async Task<BusinessEntitiesMetrics> GetBusinessEntitiesMetricsAsync(string businessCode)
        {
            var currentMonth = DateTime.UtcNow.Month;
            var previousMonth = DateTime.UtcNow.AddMonths(-1).Month;
            var currentYear = DateTime.UtcNow.Year;
            var previousYear = DateTime.UtcNow.AddMonths(-1).Year;

            double percentageChange = await CalculateFarmerPercentagePerformance(businessCode, currentMonth, previousMonth, currentYear, previousYear);
            double cooperativePercentageChange = await CalculateCooperativePercentagePerformance(businessCode, currentMonth, previousMonth, currentYear, previousYear);
            double agentPercentageChange = await CalculateAgentPercentagePerformance(businessCode, currentMonth, previousMonth, currentYear, previousYear);

            var metrics = new BusinessEntitiesMetrics

            {
                TotalAgents = await _context.Agents.CountAsync(a => a.BelongToBusinessCode == businessCode),
                TotalCooperative = await _context.Cooperatives.CountAsync(c => c.BelongToBusinessCode == businessCode),
                TotalFarmer = await _context.Farmers.CountAsync(f => f.BelongToBusinessCode == businessCode),
                TotalWarehouse = await _context.Warehouses.CountAsync(w => w.BelongToBusinessCode == businessCode),
                PercentageFarmerPerformance = percentageChange,
                PercentageCooperativePerformance = cooperativePercentageChange,
                PercentageAgentPerformance = agentPercentageChange,
            };

            return metrics;
        }

        private async Task<double> CalculateAgentPercentagePerformance(string businessCode, int currentMonth, int previousMonth, int currentYear, int previousYear)
        {
            var totalAgentsThisMonth = await _context.Agents
                .CountAsync(f => f.BelongToBusinessCode == businessCode && f.OnBoardedOn.Year == currentYear && f.OnBoardedOn.Month == currentMonth);

            var totalAgentsLastMonth = await _context.Agents
                .CountAsync(f => f.BelongToBusinessCode == businessCode && f.OnBoardedOn.Year == previousYear && f.OnBoardedOn.Month == previousMonth);

            double percentageChange = totalAgentsLastMonth == 0
                ? (totalAgentsThisMonth > 0 ? 100 : 0)
                : ((double)(totalAgentsThisMonth - totalAgentsLastMonth) / totalAgentsLastMonth) * 100;
            return percentageChange;
        }

        private async Task<double> CalculateCooperativePercentagePerformance(string businessCode, int currentMonth, int previousMonth, int currentYear, int previousYear)
        {
            var totalCooperativesThisMonth = await _context.Cooperatives
                .CountAsync(f => f.BelongToBusinessCode == businessCode && f.OnBoardedOn.Year == currentYear && f.OnBoardedOn.Month == currentMonth);

            var totalCooperativesLastMonth = await _context.Cooperatives
                .CountAsync(f => f.BelongToBusinessCode == businessCode && f.OnBoardedOn.Year == previousYear && f.OnBoardedOn.Month == previousMonth);

            double percentageChange = totalCooperativesLastMonth == 0
                ? (totalCooperativesThisMonth > 0 ? 100 : 0)
                : ((double)(totalCooperativesThisMonth - totalCooperativesLastMonth) / totalCooperativesLastMonth) * 100;
            return percentageChange;
        }

        private async Task<double> CalculateFarmerPercentagePerformance(string businessCode, int currentMonth, int previousMonth, int currentYear, int previousYear)
        {
            var totalFarmersThisMonth = await _context.Farmers
                .CountAsync(f => f.BelongToBusinessCode == businessCode && f.OnBoardedOn.Year == currentYear && f.OnBoardedOn.Month == currentMonth);

            var totalFarmersLastMonth = await _context.Farmers
                .CountAsync(f => f.BelongToBusinessCode == businessCode && f.OnBoardedOn.Year == previousYear && f.OnBoardedOn.Month == previousMonth);

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
            var entity = data.Map();

            await _context.AddAsync(entity);
            await _context.SaveChangesAsync();

            return (true, entity.Map());
        }

        //public async Task<(bool status, FarmerModel data)> UpdateFarmerProfile(FarmerModel model)
        //{
        //    var exist = await _context.Farmers.Where(c => c.UserId == model.UserId).FirstOrDefaultAsync();
        //    if (exist != null)
        //    {
        //        exist.AccountNumber = model.AccountNumber != null ? model.AccountNumber : exist.AccountNumber;
        //        exist.BankName = model.BankName != null ? model.BankName : exist.BankName;
        //        exist.BankCode = model.BankCode != null ? model.BankCode : exist.BankCode;
        //        exist.Location = model.Location != null ? model.Location : exist.Location;
        //        exist.Name = model.Name != null ? model.Name : exist.Name;

        //        await _context.SaveChangesAsync();


        //        return (true, exist.Map());
        //    }
        //    return (false, model);
        //}

        public async Task<FarmerModel> GetFarmerByUserId(string userId)
        {
            var farmer = await _context.Farmers.Where(x => x.UserId == userId).FirstOrDefaultAsync();
            if (farmer == null)
            {
                return null;
            }
            return farmer.Map();
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
        //public async Task<List<FarmerModel>> GetFarmersByOnboardedUserId(string userId)
        //{
        //    var farmer = await _context.Farmers.Where(x=>x.OnboardedByUserId == userId).ToListAsync();
        //    if (farmer.Count > 0)
        //    {
        //        return farmer.Select(x => x.Map()).OrderByDescending(x => x.OnBoardedOn).ToList();
        //    }
        //    return null;
        //}

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



        public async Task<List<FarmerModel>> GetFarmersByAgent(long agentId, ListQueryParams query)
        {
            var farmers = await (from f in  _context.Farmers
                                join c in _context.Cooperatives
                                on f.BelongToCooperativeId equals c.Id
                                where c.IsAssignedToAgentId == agentId
                                 select f).ToListAsync();

            //var cooperatives = await (from a in _context.Agents
            //                          join c in _context.Cooperatives
            //                          on a.Id equals c.IsAssignedToAgentId
            //                          select c).ToListAsync();

            //var farmers = from f in _context.Farmers
            //              join c in cooperatives
            //              on f.OnboardedByUserId equals c.UserId
            //              select f;

            //var res = await farmers.ToListAsync();
            if (farmers.Count > 0)
            {
                return farmers.Select(f => f.Map()).ToList();
            }


            return null;
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
                return farmers.Select(f => f.Map()).ToList();
            }


            return null;
        }
        public async Task<List<FarmerModel>> GetFarmersByBusinessCode(string businessCode, ListQueryParams query)
        {

            var farmers = await _context.Farmers
                .Where(x => x.BelongToBusinessCode == businessCode)
                .ToListAsync();

            if (farmers == null || !farmers.Any())
            {
                throw new Exception("No record found");
            }

            // Fetch cooperative names for all distinct cooperativeIds in farmers
            var cooperativeId = farmers.Select(b => b.BelongToCooperativeId).Distinct().ToList();
            var cooperatives = await _context.Cooperatives
                .Where(w => cooperativeId.Contains(w.Id))
                .ToDictionaryAsync(w => w.Id, w => w.Name);

            return farmers.Select(farmer =>
            {
                var mappedFarmer = farmer.Map();
                if (cooperatives.TryGetValue(farmer.BelongToCooperativeId, out var cooperativeName))
                {
                    mappedFarmer.CooperativeName = cooperativeName;
                }
                return mappedFarmer;
            }).ToList();
        }

        //public async Task<List<FarmerModel>> GetFarmersByBusinessCode(string businessCode, ListQueryParams query)
        //{

        //    var farmers = await _context.Farmers
        //        .AsNoTracking()
        //        .Where(s
        //        =>
        //        //(!string.IsNullOrEmpty(query.Location) && (s.Location.Contains(query.Location) || s.State.Contains(query.Location)
        //        // || s.Lga.Contains(query.Location))) &&
        //        s.BelongToBusinessCode == businessCode)
        //        .OrderByDescending(s => s.OnBoardedOn)
        //        .Skip(query.PageNumber)
        //        .Take(query.PageSize)
        //         .Select(s => s.Map()).ToListAsync();


        //    if (farmers.Count > 0)
        //    {
        //        return farmers;
        //    }
        //    return null;
        //}
      
        public async Task<(bool status, FarmerImagesModel data)> SaveImage(FarmerImagesModel data)
        {
            var entity = data.Map();

            await _context.AddAsync(entity);
            await _context.SaveChangesAsync();

            return (true, entity.Map());
        }

        public async Task<FarmerImagesModel> GetFarmerImage(string userId, string businessCode)
        {
            var farmer = await _context.FarmerImages.Where(x => x.FarmerUserId == userId && x.BelongToBusinessCode==businessCode).FirstOrDefaultAsync();
            if (farmer == null)
            {
                return null;
            }
            return farmer.Map();
        }
        public async Task<List<FarmerImagesModel>> GetImagesByBusinessCode(string businessCode)
        {
            var query= await _context.FarmerImages
                                 .Where(img => img.BelongToBusinessCode == businessCode)
                                 .ToListAsync();

            if (query == null)
            {
                return null;
            }
            return query.Select(x=>x.Map()).ToList();
        }

    }
}

