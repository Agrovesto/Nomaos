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

        #region visitationlog
        public async Task<(bool status, FarmActivityTypeModel data)> CreateVsitationLogType(FarmActivityTypeModel data)
        {
            var entity = data.Map();

            await _context.AddAsync(entity);
            await _context.SaveChangesAsync();
            return (true, entity.Map());
        }

        public async Task<List<FarmActivityTypeModel>> GetVisitationLogtypesByBusiness(string businessCode)
        {
            var type = await _context.FarmActivityTypes.Where(x => x.BelongToBusinessCode == businessCode).ToListAsync();
            if (type.Count == 0) { return null; }

            return type.Select(x => x.Map()).ToList();
        }

        public async Task<FarmActivityTypeModel> GetVisitationLogtypebyId(string businessCode, long Id)
        {
            var type = await _context.FarmActivityTypes.Where(x => x.BelongToBusinessCode == businessCode && x.Id==Id).FirstOrDefaultAsync();
            if (type ==null) { return null; }

            return type.Map();
        }
        //TODO: update activitylog type
        //TODO: delete activitylog type

        public async Task<(bool status, FarmVisitationActivityModel data)> VisitationLog(FarmVisitationActivityModel data)
        {
            var entity = data.Map();

            await _context.AddAsync(entity);
            await _context.SaveChangesAsync();
            return (true, entity.Map());
        }

        public async Task<List<FarmVisitationActivityModel>> GetVisitationLogsByBusiness(string businessCode)
        {
            var type = await _context.FarmVisitationActivities.Where(x => x.BelongToBusinessCode == businessCode).ToListAsync();
            if (type.Count == 0) { return null; }

            return type.Select(x => x.Map()).ToList();
        }

        public async Task<List<FarmVisitationActivityModel>> GetVisitationLogByFarmer(string businessCode, string farmerId)
        {
            var type = await _context.FarmVisitationActivities.Where(x => x.BelongToBusinessCode == businessCode && x.FarmerUserId == farmerId).ToListAsync();
            if (type.Count == 0) { return null; }


            return type.Select(x => x.Map()).ToList();
        }
        public async Task<List<FarmVisitationActivityModel>> GetVisitationLogsByOfficerInCharge(string businessCode, string officerInCharge)
        {
            var type = await _context.FarmVisitationActivities.Where(x => x.BelongToBusinessCode == businessCode && x.OfficerInChargeId == officerInCharge).ToListAsync();
            if (type.Count == 0) { return null; }


            return type.Select(x => x.Map()).ToList();
        }
        public async Task<List<FarmVisitationActivityModel>> GetVisitationLogsByCooperative(string businessCode, long cooperativeId)
        {
            var type = await _context.FarmVisitationActivities.Where(x => x.BelongToBusinessCode == businessCode && x.CooperativeId == cooperativeId).ToListAsync();
            if (type.Count == 0) { return null; }


            return type.Select(x => x.Map()).ToList();
        }
        public async Task<FarmVisitationActivityModel> GetVisitationLogById(string businessCode, long Id)
        {
            var type = await _context.FarmVisitationActivities.Where(x => x.BelongToBusinessCode == businessCode && x.Id == Id).FirstOrDefaultAsync();
            if (type == null) { return null; }

            return type.Map();
        }

        public async Task<List<FarmVisitationActivityModel>> GetVisitationLogsByActivityType(string businessCode, long activityTypeId)
        {
            var type = await _context.FarmVisitationActivities.Where(x => x.BelongToBusinessCode == businessCode && x.FarmActivityId == activityTypeId).ToListAsync();
            if (type.Count == 0) { return null; }


            return type.Select(x => x.Map()).ToList();
        }
        #endregion visitationlog
    }
}
