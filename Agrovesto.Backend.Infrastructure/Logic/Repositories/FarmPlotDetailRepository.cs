using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Agrovesto.Backend.Infrastructure.Logic.Repositories.FarmPlotDetailRepository;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories
{
    public class FarmPlotDetailRepository : IFarmPlotDetailRepository
    {
        private readonly DataContext _context;
        public FarmPlotDetailRepository(DataContext context)
        {
            _context = context;
        }


        public async Task UpdateFarmAsync(FarmPlotDetailsModel farm)
        {
            var data = farm.Map();
            data.Id=farm.Id;
            var existingFarm = await _context.FarmPlotDetails
                .Include(f => f.AuditTrails)
                .FirstOrDefaultAsync(f => f.Id == data.Id);

            if (existingFarm == null)
            {
                throw new InvalidOperationException($"Farm with ID {data.Id} not found.");
            }

            _context.Entry(existingFarm).CurrentValues.SetValues(data);

            foreach (var audit in data.AuditTrails)
            {
                if (audit.Id == 0) 
                {
                    existingFarm.AuditTrails.Add(audit);
                }
            }

            await _context.SaveChangesAsync();
        }


        //public async Task<(bool status, FarmPlotDetailsModel data)> UnflagFarmAsync(FarmPlotDetailsModel data)
        //{
        //    var farm = await GetFarmDetailByIdAsync(data.Id);


        //    await _context.SaveChangesAsync();
        //    return (true, entity.Map());
        //}
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
            return farmer.Select(x => x.Map()).OrderByDescending(x => x.CreatedOn).ToList();
        }

        public async Task<List<FarmPlotDetailsModel>> GetFlaggedFarmsAsync(

           string? businessCode = null,
           string? organizationCode = null)
        {
            var query = _context.FarmPlotDetails
                .Where(x => x.IsFarmFlaggedBySystem == true)
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

            string? businessCode = null,
            string? organizationCode = null)
        {
            var query = _context.FarmPlotDetails
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



        public async Task<List<FarmPlotDetailsModel>> GetFarmDetailsByCooperative(
     long cooperativeId,
     string? businessCode = null,
     string? organizationCode = null)
        {
            // Join FarmPlotDetails -> Farmers -> FarmerCooperatives
            var query =
                from farm in _context.FarmPlotDetails.AsNoTracking()
                join farmer in _context.Farmers.AsNoTracking()
                    on farm.FarmerId equals farmer.Id
                join fc in _context.FarmerCooperatives.AsNoTracking()
                    on farmer.Id equals fc.FarmerId
                where fc.CooperativeId == cooperativeId
                select new { farm, farmer, fc };

            // Apply optional filters
            if (!string.IsNullOrWhiteSpace(businessCode))
                query = query.Where(x => x.farm.BelongToBusinessCode == businessCode);

            if (!string.IsNullOrWhiteSpace(organizationCode))
                query = query.Where(x => x.farm.OrganizationCode == organizationCode);

            var result = await query
                .OrderByDescending(x => x.farm.CreatedOn)
                .ToListAsync();

            return result.Select(x =>
            {
                var mapped = x.farm.Map();
                mapped.CooperativeId = x.fc.CooperativeId; // map cooperative from FarmerCooperative
                return mapped;
            }).ToList();
        }


        public async Task<List<FarmPlotDetailsModel>> GetFarmDetailsByBusiness(
     string? businessCode = null,
     string? organizationCode = null)
        {
            // Join FarmPlotDetails -> Farmers -> FarmerCooperatives -> Cooperatives
            var query =
                from farm in _context.FarmPlotDetails.AsNoTracking()
                join farmer in _context.Farmers.AsNoTracking()
                    on farm.FarmerId equals farmer.Id
                join fc in _context.FarmerCooperatives.AsNoTracking()
                    on farmer.Id equals fc.FarmerId into fcGroup
                from farmerCoop in fcGroup.DefaultIfEmpty()
                join cooperative in _context.Cooperatives.AsNoTracking()
                    on farmerCoop.CooperativeId equals cooperative.Id into coopGroup
                from coop in coopGroup.DefaultIfEmpty()
                select new { farm, farmer, CooperativeId = farmerCoop.CooperativeId, CooperativeName = coop != null ? coop.Name : null };

            // Apply optional filters
            if (!string.IsNullOrWhiteSpace(businessCode))
                query = query.Where(x => x.farm.BelongToBusinessCode == businessCode);

            if (!string.IsNullOrWhiteSpace(organizationCode))
                query = query.Where(x => x.farm.OrganizationCode == organizationCode);

            var result = await query
                .OrderByDescending(x => x.farm.CreatedOn)
                .ToListAsync();

            return result.Select(x =>
            {
                var mapped = x.farm.Map();
                mapped.CooperativeId = x.CooperativeId;
                mapped.CooperativeName = x.CooperativeName;
                return mapped;
            }).ToList();
        }


        public async Task<FarmPlotDetailsModel> GetFarmDetailByIdAsync(long farmId)
        {
            var farm = await _context.FarmPlotDetails.Where(x => x.Id == farmId).FirstOrDefaultAsync();
            if (farm == null)
            {
                return null;
            }
            return farm.Map();
        }

        public async Task<List<FarmFlagAuditTrailModel>> GetAuditTrail(
            long plotId,
       string? businessCode = null,
       string? organizationCode = null)
        {
            var query = _context.FarmFlagAuditTrail
                .Where(x => x.FarmPlotDetailsId == plotId)
                .AsQueryable();


          
            if (query.Count() == 0) { return null; }

            var totalCount = await query.CountAsync();


            var pagedResult = await query
                .OrderByDescending(x => x.PerformedOn)
               // .Skip((pageNumber - 1) * pageSize)
               //.Take(pageSize)
               .ToListAsync();
            return pagedResult.Select(x => x.Map()).ToList();
        }

        #region visitationlog
        public async Task<(bool status, FarmActivityTypeModel data)> CreateVsitationLogType(FarmActivityTypeModel data)
        {
            var entity = data.Map();

            await _context.AddAsync(entity);
            await _context.SaveChangesAsync();
            return (true, entity.Map());
        }

        public async Task<List<FarmActivityTypeModel>> GetVisitationLogtypesByBusiness(string businessCode, string organizationCode)
        {
           
            var type = _context.FarmActivityTypes
                .Where(x => x.BelongToBusinessCode == businessCode)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(organizationCode))
            {
                type = type.Where(x => x.OrganizationCode == organizationCode);
            }

            if (type.Count() == 0) { return null; }


            var result = await type
                .ToListAsync();


            return result.Select(x => x.Map()).ToList();
        }

        public async Task<FarmActivityTypeModel> GetVisitationLogtypebyId(string businessCode, long Id)
        {
            var type = await _context.FarmActivityTypes.Where(x => x.BelongToBusinessCode == businessCode && x.Id == Id).FirstOrDefaultAsync();
            if (type == null) { return null; }

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

        public async Task<List<FarmVisitationActivityModel>> GetVisitationLogsByBusiness(string businessCode, string organizationCode)
        {

            var type = _context.FarmVisitationActivities
                .Where(x => x.BelongToBusinessCode == businessCode)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(organizationCode))
            {
                type = type.Where(x => x.OrganizationCode == organizationCode);
            }

            if (type.Count() == 0) { return null; }


            var result = await type
                .ToListAsync();


            return result.Select(x => x.Map()).ToList();
      
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
        public async Task<List<FarmVisitationActivityModel>> GetVisitationLogsByOfficerInCharge(string businessCode, string officerInCharge, ListQueryParams args)
        {
            var query = _context.FarmVisitationActivities
                .Where(x => x.BelongToBusinessCode == businessCode && x.OfficerInChargeId == officerInCharge);

            // Apply optional organization code filter
            if (!string.IsNullOrWhiteSpace(args.organizationCode))
            {
                query = query.Where(x => x.OrganizationCode == args.organizationCode);
            }

            var type = await query
                .OrderByDescending(x => x.CreatedOn)
                .Select(x => new FarmVisitationActivityModel
                {
                    Id = x.Id,
                    Description = x.Description,
                    BelongToBusinessCode = x.BelongToBusinessCode,
                    CooperativeId = x.CooperativeId,
                    CreatedBy = x.CreatedBy,
                    CreatedOn = x.CreatedOn,
                    FarmActivityId = x.FarmActivityId,
                    FarmerName = x.FarmerName,
                    FarmerUserId = x.FarmerUserId,
                    OfficerInChargeId = x.OfficerInChargeId,
                    OfficerName = x.FarmerName, // Is this supposed to be FarmerName? Consider checking.
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
                        .FirstOrDefault()
                })
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

            return type != null ? type : null;
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
