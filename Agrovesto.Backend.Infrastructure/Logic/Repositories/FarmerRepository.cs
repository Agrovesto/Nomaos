using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.DB;
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
            var metrics = new BusinessEntitiesMetrics
            {
                TotalAgents = await _context.Agents.CountAsync(a => a.BelongToBusinessCode == businessCode),
                TotalCooperative = await _context.Cooperatives.CountAsync(c => c.BelongToBusinessCode == businessCode),
                TotalFarmer = await _context.Farmers.CountAsync(f => f.BelongToBusinessCode == businessCode),
                TotalWarehouse = await _context.Warehouses.CountAsync(w => w.BelongToBusinessCode == businessCode)
            };

            return metrics;
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



        public async Task<List<FarmerModel>> GetFarmersByAgent(string userId, ListQueryParams query)
        {

            var cooperatives = await (from a in _context.Agents
                                      join c in _context.Cooperatives
                                      on a.Id equals c.IsAssignedToAgentId
                                      select c).ToListAsync();

            var farmers = from f in _context.Farmers
                          join c in cooperatives
                          on f.OnboardedByUserId equals c.UserId
                          select f;

            var res = await farmers.ToListAsync();
            if (res.Count > 0)
            {
                return res.Select(f => f.Map()).ToList();
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
                .AsNoTracking()
                .Where(s
                =>
                //(!string.IsNullOrEmpty(query.Location) && (s.Location.Contains(query.Location) || s.State.Contains(query.Location)
                // || s.Lga.Contains(query.Location))) &&
                s.BelongToBusinessCode == businessCode)
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

    }
}

