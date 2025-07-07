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
        Task<List<FarmPlotDetailsModel>> GetFarmDetailByFarmer(long farmerId);
        Task<List<FarmVisitationActivityModel>> GetVisitationLogByFarmer(string businessCode, string farmerId);
        Task<FarmVisitationActivityModel> GetVisitationLogById(string businessCode, long Id);
        Task<List<FarmVisitationActivityModel>> GetVisitationLogsByActivityType(string businessCode, long activityTypeId);
        Task<List<FarmVisitationActivityModel>> GetVisitationLogsByBusiness(string businessCode);
        Task<List<FarmVisitationActivityModel>> GetVisitationLogsByCooperative(string businessCode, long cooperativeId);
        Task<List<FarmVisitationActivityModel>> GetVisitationLogsByOfficerInCharge(string businessCode, string officerInCharge);
        Task<FarmActivityTypeModel> GetVisitationLogtypebyId(string businessCode, long Id);
        Task<List<FarmActivityTypeModel>> GetVisitationLogtypesByBusiness(string businessCode);
        Task<(bool status, FarmPlotDetailsModel data)> UpdateFarmerFarmPlot(FarmPlotDetailsModel data);
        Task<(bool status, FarmVisitationActivityModel data)> VisitationLog(FarmVisitationActivityModel data);
    }
}
