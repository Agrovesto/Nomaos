using Agrovesto.Backend.Core.Models.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Managers
{
    public interface IFarmPlotDetailsManager
    {
        Task<FarmActivityTypeModel> CreateActivityType(FarmActivityTypeModel model, BusinessRegistrationArgs args);
        Task<FarmActivityTypeModel> GetActivityTypeById(BusinessRegistrationArgs args, long farmerId);
        Task<List<FarmActivityTypeModel>> GetActivityTypes(BusinessRegistrationArgs args);
        Task<List<FarmFlagAuditTrailModel>> GetAuditTrail(BusinessRegistrationArgs args, long plotId);
        Task<List<FarmPlotDetailsModel>> GetFarmDetailByFarmer(long farmerId);
        Task<List<FarmPlotDetailsModel>> GetFarmDetailsByBusiness(ListQueryParams args);
        Task<List<FarmPlotDetailsModel>> GetFarmDetailsByCooperative(ListQueryParams args);
        Task<List<FarmVisitationActivityModel>> GetVisitationLogByFarmer(BusinessRegistrationArgs args, string farmerId);
        Task<FarmVisitationActivityModel> GetVisitationLogById(BusinessRegistrationArgs args, long visitationId);
        Task<List<FarmVisitationActivityModel>> GetVisitationLogsByActivityType(BusinessRegistrationArgs args, long activitytypeId);
        Task<List<FarmVisitationActivityModel>> GetVisitationLogsByBusiness(BusinessRegistrationArgs args);
        Task<List<FarmVisitationActivityModel>> GetVisitationLogsByCooperative(BusinessRegistrationArgs args);
        Task<List<FarmVisitationActivityModel>> GetVisitationLogsByOfficerInCharge(BusinessRegistrationArgs args);
        Task<FarmVisitationActivityModel> LogFarmVisit(FarmVisitationActivityModel model, BusinessRegistrationArgs args);
        Task<bool> UnflagFarmAsync(ListQueryParams args, long farmId);
        Task<FarmPlotDetailsModel> UpdateFarmCoordinateDetails(FarmPlotDetailsModel model, BusinessRegistrationArgs args);
        Task<FarmPlotDetailsModel> UpdateFarmPlotDetails(FarmPlotDetailsModel model, BusinessRegistrationArgs args);
        Task<(bool isValid, string message)> ValidateAndSaveFarm(FarmPlotDetailsModel newFarm, BusinessRegistrationArgs args);
    }
}
