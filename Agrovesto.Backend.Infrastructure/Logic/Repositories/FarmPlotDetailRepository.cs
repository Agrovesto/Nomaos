using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.DB;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories
{
    public class FarmPlotDetailRepository : IFarmPlotDetailRepository
    {
        private readonly DataContext _context;
        public FarmPlotDetailRepository(DataContext context)
        {
            _context = context;
        }
        public async Task<(bool status, FarmPlotDetailsModel data)> UpdateFarmerFarmPlot(FarmPlotDetailsModel data)
        {
            var entity = data.Map(); 
           
                await _context.AddAsync(entity);
                await _context.SaveChangesAsync();
            return (true, entity.Map());
        }
    
        public async Task<List<FarmPlotDetailsModel>> GetFarmDetailByFarmer(long farmerId)
        {
            var farmer = await _context.FarmPlotDetails.Where(x => x.FarmerId == farmerId).ToListAsync();
            if (farmer == null)
            {
                return null;
            }
            return farmer.Select(x=>x.Map()).OrderByDescending(x=>x.CreatedOn).ToList();
        }
        
        //public async Task<List<FarmerModel>> GetAllFarmPlotDetailsByCooperative(string userId, ListQueryParams query)
        //{
        //    //get the farm details based on cooperatives..
        //    //group farm details for farmers based on all cooperatives
        //}

        
    }
}
