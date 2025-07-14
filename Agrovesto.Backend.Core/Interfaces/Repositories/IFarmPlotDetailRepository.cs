using Agrovesto.Backend.Core.Models.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Repositories
{
    public interface IFarmPlotDetailRepository
    {
        Task<(bool status, FarmActivityTypeModel data)> CreateVsitationLogType(FarmActivityTypeModel data);
        Task<List<FarmFlagAuditTrailModel>> GetAuditTrail(long plotId, string? businessCode = null, string? organizationCode = null);
        Task<List<FarmPlotDetailsModel>> GetFarmDetailByFarmer(long farmerId);
        Task<FarmPlotDetailsModel> GetFarmDetailByIdAsync(long farmId);
        Task<List<FarmPlotDetailsModel>> GetFarmDetails(string? businessCode = null, string? organizationCode = null);
        Task<List<FarmPlotDetailsModel>> GetFarmDetailsByBusiness(string? businessCode = null, string? organizationCode = null);
        Task<List<FarmPlotDetailsModel>> GetFarmDetailsByCooperative(long cooperativeId, string? businessCode = null, string? organizationCode = null);
        Task<List<FarmPlotDetailsModel>> GetFlaggedFarmsAsync(string? businessCode = null, string? organizationCode = null);
        Task<List<FarmVisitationActivityModel>> GetVisitationLogByFarmer(string businessCode, string farmerId);
        Task<FarmVisitationActivityModel> GetVisitationLogById(string businessCode, long Id);
        Task<List<FarmVisitationActivityModel>> GetVisitationLogsByActivityType(string businessCode, long activityTypeId);
        Task<List<FarmVisitationActivityModel>> GetVisitationLogsByBusiness(string businessCode, string organizationCode);
        Task<List<FarmVisitationActivityModel>> GetVisitationLogsByCooperative(string businessCode, long cooperativeId);
        Task<List<FarmVisitationActivityModel>> GetVisitationLogsByOfficerInCharge(string businessCode, string officerInCharge);
        Task<FarmActivityTypeModel> GetVisitationLogtypebyId(string businessCode, long Id);
        Task<List<FarmActivityTypeModel>> GetVisitationLogtypesByBusiness(string businessCode, string organizationCode);
        Task UpdateFarmAsync(FarmPlotDetailsModel farm);
        Task<(bool status, FarmPlotDetailsModel data)> UpdateFarmerFarmPlot(FarmPlotDetailsModel data);
        Task<(bool status, FarmVisitationActivityModel data)> VisitationLog(FarmVisitationActivityModel data);
    }
}
