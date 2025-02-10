using Agrovesto.Backend.Core.Interfaces.Repositories;
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
        public async Task<List<FarmerModel>> GetFarmersByOnboardedUserId(string userId)
        {
            var farmer = await _context.Farmers.Where(x=>x.OnboardedByUserId == userId).ToListAsync();
            if (farmer.Count > 0)
            {
                return farmer.Select(x => x.Map()).OrderByDescending(x => x.OnBoardedOn).ToList();
            }
            return null;
        }
        public async Task<List<FarmerModel>> GetFarmersByBusinessCode(string businessCode)
        {
            var farmer = await _context.Farmers.Where(x => x.BelongToBusinessCode == businessCode).ToListAsync();
            if (farmer.Count > 0)
            {
                return farmer.Select(x => x.Map()).OrderByDescending(x => x.OnBoardedOn).ToList();
            }
            return null;
        }
    }
}

