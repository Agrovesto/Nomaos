using Agrovesto.Backend.Core.CoreUtils;
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

        public async Task<List<FarmPlotDetailsModel>> GetFlaggedFarmsAsync(

           string? businessCode = null,
           string? organizationCode = null)
        {
            var query = _context.FarmPlotDetails
                .Where(x=>x.IsFarmFlaggedBySystem == true)
                .AsQueryable();


            if (!string.IsNullOrWhiteSpace(businessCode))
            {

                query = query.Where(x => x.BelongToBusinessCode == businessCode);
            }
            if (!string.IsNullOrWhiteSpace(organizationCode))
            {
                query = query.Where(x => x.OrganizationCode == organizationCode);
            }
            if (query.Count() == 0) { return null; }

            var totalCount = await query.CountAsync();


            var pagedResult = await query
                .OrderByDescending(x => x.CreatedOn)
               // .Skip((pageNumber - 1) * pageSize)
               //.Take(pageSize)
               .ToListAsync();
            return pagedResult.Select(x => x.Map()).ToList();
        }

        public async Task<List<FarmPlotDetailsModel>> GetFarmDetails(
         
            string? businessCode=null, 
            string? organizationCode=null)
        {
            var query =  _context.FarmPlotDetails
                .AsQueryable();


            if (!string.IsNullOrWhiteSpace(businessCode))
            {
             
                query = query.Where(x =>x.BelongToBusinessCode == businessCode);
            }
            if (!string.IsNullOrWhiteSpace(organizationCode))
            {
                query = query.Where(x => x.OrganizationCode == organizationCode);
            }
            if (query.Count() == 0) { return null; }

            var totalCount = await query.CountAsync();


            var pagedResult = await query
                .OrderByDescending(x => x.CreatedOn)
               // .Skip((pageNumber - 1) * pageSize)
                //.Take(pageSize)
               .ToListAsync();
            return pagedResult.Select(x => x.Map()).ToList();
               
         
        }



        public async Task<List<FarmPlotDetailsModel>> GetFarmDetailsByCooperative(
            long cooperativeId,
            string? businessCode = null,
            string? organizationCode = null)
        {
            var query = from farm in _context.FarmPlotDetails
                        join farmer in _context.Farmers
                            on farm.FarmerId equals farmer.Id
                        where farmer.BelongToCooperativeId == cooperativeId
                        select new { farm, farmer };

            if (!string.IsNullOrWhiteSpace(businessCode))
            {
                query = query.Where(x => x.farm.BelongToBusinessCode == businessCode);
            }

            if (!string.IsNullOrWhiteSpace(organizationCode))
            {
                query = query.Where(x => x.farm.OrganizationCode == organizationCode);
            }

            var result = await query
                .OrderByDescending(x => x.farm.CreatedOn)
                .ToListAsync();

            return result.Select(x =>
            {
                var mapped = x.farm.Map();
                mapped.CooperativeId = x.farmer.BelongToCooperativeId;
                return mapped;
            }).ToList();
        }

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
            var activities = await _context.FarmVisitationActivities
         .Where(x => x.BelongToBusinessCode == businessCode)
         .Select(x => new FarmVisitationActivityModel
         {
          
             Id = x.Id,
             Description = x.Description,
             BelongToBusinessCode = businessCode,
             CooperativeId = x.CooperativeId,
             CreatedBy = x.CreatedBy,
             CreatedOn = x.CreatedOn,
             FarmActivityId = x.FarmActivityId,
             FarmerName = x.FarmerName,
             FarmerUserId = x.FarmerUserId,
             OfficerInChargeId = x.OfficerInChargeId,
             OfficerName    = x.FarmerName,
             Recommendation=x.Recommendation,
        SupportingImageUrl=x.SupportingImageUrl,
        FarmerLabourCost=x.FarmerLabourCost,
        FarmerTransportCost=x.FarmerTransportCost,
        InputCost=x.InputCost,
        InputQuantity=x.InputQuantity,
        InputType=x.InputType,
        OrganizationCode=x.OrganizationCode,
        OtherCost=x.OtherCost,
        TotalCost=x.TotalCost,
             ActivityType = _context.FarmActivityTypes
                 .Where(t => t.Id == x.FarmActivityId)
                 .Select(t => t.Name)
                 .FirstOrDefault(), 
         })
         .ToListAsync();

            return activities.Any() ? activities : null;
        }

        public async Task<List<FarmVisitationActivityModel>> GetVisitationLogByFarmer(string businessCode, string farmerId)
        {
            var activities = await _context.FarmVisitationActivities
                .Where(x => x.BelongToBusinessCode == businessCode && x.FarmerUserId == farmerId)
                .Select(x => new FarmVisitationActivityModel
                {

                    Id = x.Id,
                    Description = x.Description,
                    BelongToBusinessCode = businessCode,
                    CooperativeId = x.CooperativeId,
                    CreatedBy = x.CreatedBy,
                    CreatedOn = x.CreatedOn,
                    FarmActivityId = x.FarmActivityId,
                    FarmerName = x.FarmerName,
                    FarmerUserId = x.FarmerUserId,
                    OfficerInChargeId = x.OfficerInChargeId,
                    OfficerName = x.FarmerName,
                    Recommendation = x.Recommendation,
                    SupportingImageUrl = x.SupportingImageUrl,
                    FarmerLabourCost = x.FarmerLabourCost,
                    FarmerTransportCost = x.FarmerTransportCost,
                    InputCost = x.InputCost,
                    InputQuantity = x.InputQuantity,
                    InputType = x.InputType,
                    OrganizationCode = x.OrganizationCode,
                    OtherCost = x.OtherCost,
                    TotalCost = x.TotalCost,
                    ActivityType = _context.FarmActivityTypes
                 .Where(t => t.Id == x.FarmActivityId)
                 .Select(t => t.Name)
                 .FirstOrDefault(),
                })
         .ToListAsync();

            return activities.Any() ? activities : null;
        }
        public async Task<List<FarmVisitationActivityModel>> GetVisitationLogsByOfficerInCharge(string businessCode, string officerInCharge)
        {
            var type = await _context.FarmVisitationActivities
                .Where(x => x.BelongToBusinessCode == businessCode && x.OfficerInChargeId == officerInCharge)
                 .Select(x => new FarmVisitationActivityModel
                 {

                     Id = x.Id,
                     Description = x.Description,
                     BelongToBusinessCode = businessCode,
                     CooperativeId = x.CooperativeId,
                     CreatedBy = x.CreatedBy,
                     CreatedOn = x.CreatedOn,
                     FarmActivityId = x.FarmActivityId,
                     FarmerName = x.FarmerName,
                     FarmerUserId = x.FarmerUserId,
                     OfficerInChargeId = x.OfficerInChargeId,
                     OfficerName = x.FarmerName,
                     Recommendation = x.Recommendation,
                     SupportingImageUrl = x.SupportingImageUrl,
                     FarmerLabourCost = x.FarmerLabourCost,
                     FarmerTransportCost = x.FarmerTransportCost,
                     InputCost = x.InputCost,
                     InputQuantity = x.InputQuantity,
                     InputType = x.InputType,
                     OrganizationCode = x.OrganizationCode,
                     OtherCost = x.OtherCost,
                     TotalCost = x.TotalCost,
                     ActivityType = _context.FarmActivityTypes
                 .Where(t => t.Id == x.FarmActivityId)
                 .Select(t => t.Name)
                 .FirstOrDefault(),
                 }).OrderByDescending(x=>x.CreatedOn)
         .ToListAsync();

            return type.Any() ? type : null;
        }
        public async Task<List<FarmVisitationActivityModel>> GetVisitationLogsByCooperative(string businessCode, long cooperativeId)
        {
            var type = await _context.FarmVisitationActivities
                .Where(x => x.BelongToBusinessCode == businessCode && x.CooperativeId == cooperativeId)
                .Select(x => new FarmVisitationActivityModel
                {

                    Id = x.Id,
                    Description = x.Description,
                    BelongToBusinessCode = businessCode,
                    CooperativeId = x.CooperativeId,
                    CreatedBy = x.CreatedBy,
                    CreatedOn = x.CreatedOn,
                    FarmActivityId = x.FarmActivityId,
                    FarmerName = x.FarmerName,
                    FarmerUserId = x.FarmerUserId,
                    OfficerInChargeId = x.OfficerInChargeId,
                    OfficerName = x.FarmerName,
                    Recommendation = x.Recommendation,
                    SupportingImageUrl = x.SupportingImageUrl,
                    FarmerLabourCost = x.FarmerLabourCost,
                    FarmerTransportCost = x.FarmerTransportCost,
                    InputCost = x.InputCost,
                    InputQuantity = x.InputQuantity,
                    InputType = x.InputType,
                    OrganizationCode = x.OrganizationCode,
                    OtherCost = x.OtherCost,
                    TotalCost = x.TotalCost,
                    ActivityType = _context.FarmActivityTypes
                 .Where(t => t.Id == x.FarmActivityId)
                 .Select(t => t.Name)
                 .FirstOrDefault(),
                })
         .ToListAsync();

            return type.Any() ? type : null;
        }
        public async Task<FarmVisitationActivityModel> GetVisitationLogById(string businessCode, long Id)
        {
            var type = await _context.FarmVisitationActivities
                .Where(x => x.BelongToBusinessCode == businessCode && x.Id == Id)
                 .Select(x => new FarmVisitationActivityModel
                 {

                     Id = x.Id,
                     Description = x.Description,
                     BelongToBusinessCode = businessCode,
                     CooperativeId = x.CooperativeId,
                     CreatedBy = x.CreatedBy,
                     CreatedOn = x.CreatedOn,
                     FarmActivityId = x.FarmActivityId,
                     FarmerName = x.FarmerName,
                     FarmerUserId = x.FarmerUserId,
                     OfficerInChargeId = x.OfficerInChargeId,
                     OfficerName = x.FarmerName,
                     Recommendation = x.Recommendation,
                     SupportingImageUrl = x.SupportingImageUrl,
                     FarmerLabourCost = x.FarmerLabourCost,
                     FarmerTransportCost = x.FarmerTransportCost,
                     InputCost = x.InputCost,
                     InputQuantity = x.InputQuantity,
                     InputType = x.InputType,
                     OrganizationCode = x.OrganizationCode,
                     OtherCost = x.OtherCost,
                     TotalCost = x.TotalCost,
                     ActivityType = _context.FarmActivityTypes
                 .Where(t => t.Id == x.FarmActivityId)
                 .Select(t => t.Name)
                 .FirstOrDefault(),
                 })
         .FirstOrDefaultAsync();

            return type !=null ? type : null;
        }

        public async Task<List<FarmVisitationActivityModel>> GetVisitationLogsByActivityType(string businessCode, long activityTypeId)
        {
            var type = await _context.FarmVisitationActivities
                .Where(x => x.BelongToBusinessCode == businessCode && x.FarmActivityId == activityTypeId)
                 .Select(x => new FarmVisitationActivityModel
                 {

                     Id = x.Id,
                     Description = x.Description,
                     BelongToBusinessCode = businessCode,
                     CooperativeId = x.CooperativeId,
                     CreatedBy = x.CreatedBy,
                     CreatedOn = x.CreatedOn,
                     FarmActivityId = x.FarmActivityId,
                     FarmerName = x.FarmerName,
                     FarmerUserId = x.FarmerUserId,
                     OfficerInChargeId = x.OfficerInChargeId,
                     OfficerName = x.FarmerName,
                     Recommendation = x.Recommendation,
                     SupportingImageUrl = x.SupportingImageUrl,
                     FarmerLabourCost = x.FarmerLabourCost,
                     FarmerTransportCost = x.FarmerTransportCost,
                     InputCost = x.InputCost,
                     InputQuantity = x.InputQuantity,
                     InputType = x.InputType,
                     OrganizationCode = x.OrganizationCode,
                     OtherCost = x.OtherCost,
                     TotalCost = x.TotalCost,
                     ActivityType = _context.FarmActivityTypes
                 .Where(t => t.Id == x.FarmActivityId)
                 .Select(t => t.Name)
                 .FirstOrDefault(),
                 })
         .ToListAsync();

            return type.Any() ? type : null;
        }
        #endregion visitationlog
    }
}
