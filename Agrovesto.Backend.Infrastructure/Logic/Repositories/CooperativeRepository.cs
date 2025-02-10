using Agrovesto.Backend.Core.Interfaces.Repositories;
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
    public class CooperativeRepository : ICooperativeRepository
    {
        private readonly DataContext _context;

        public CooperativeRepository(DataContext context)
        {
            _context = context;
        }
        public async Task<(bool status, CooperativeModel data)> CreateCooperative(CooperativeModel data)
        {
            var entity =data.Map();

            await _context.AddAsync(entity);
            await _context.SaveChangesAsync();

            return (true, entity.Map());
        }

        public async Task<CooperativeModel> GetCooperativeDetailByUserId(string userId)
        {
            var cooperative = await _context.Cooperatives.Where(x => x.UserId == userId).FirstOrDefaultAsync();
            if (cooperative == null)
            {
                return null;
            }
            return cooperative.Map();
        }

        public async Task<CooperativeModel> GetCooperativeDetailById(long Id)
        {
            var cooperative = await _context.Cooperatives.Where(x => x.Id == Id).FirstOrDefaultAsync();
            if (cooperative == null)
            {
                return null;
            }
            return cooperative.Map();
        }
        public async Task<List<CooperativeModel>> GetCooperatives() //restricted to agrovesto
        {

            var cooperative = await _context.Cooperatives.ToListAsync();
            if (cooperative.Count > 0)
            {
                return cooperative.Select(x => x.Map()).OrderByDescending(x => x.OnBoardedOn).ToList();
            }
            return null;
        }


        public async Task<List<CooperativeModel>> GetCooperativesByBusinessCode(string businessCode)
        {

            var cooperative = await _context.Cooperatives.Where(x=>x.BelongToBusinessCode == businessCode).ToListAsync();
            if (cooperative.Count > 0)
            {
                return cooperative.Select(x => x.Map()).OrderByDescending(x => x.OnBoardedOn).ToList();
            }
            return null;
        }

       
    }
}

