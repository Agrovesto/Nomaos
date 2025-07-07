using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Models.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Repositories
{
    public interface IInputFinancingManagementRepository
    {
        Task<(bool status, BundleSubscriptionInterestModel data)> CreateInputFinancingInterestThroughBundle(BundleSubscriptionInterestModel data);
        Task<PagedListResponse<List<BundleSubscriptionInterestModel>>> GetSubscriptionInterestByBusiness(string businessCode, int pageNumber = 1,
      int pageSize = 10,
         DateTimeOffset? startDate = null,
         DateTimeOffset? endDate = null,
         string? location = null);
        Task<List<BundleSubscriptionInterestModel>> GetSubscriptionInterestByCooperative(string businessCode, long CooperativeId);
        Task<List<BundleSubscriptionInterestModel>> GetUnpaidSubscriptionInterestByFarmer(BundleSubscriptionInterestModel data);
        Task<List<BundleSubscriptionInterestModel>> GetSubscriptionInterestByLocation(string businessCode, string Location);
        Task<BundleSubscriptionInterestModel> UpdateSubscriptionPayment(BundleSubscriptionInterestModel model);
        Task<List<BundleSubscriptionSummary>> FarmerBundleSubscriptionInterests(string businessCode);
        //   Task<SubscriptionMetrics> FarmerBundleSubscriptions(string businessCode);
        Task<List<SubscriptionData>> FarmerBundleSubscriptions(string businessCode);
        Task<BundleSubscriptionInterestModel> GetPaidSubscriptionByFarmer(string businessCode, string farmerUserId, string reference);
        Task<BundleDisbursementModel> GetDisbursedSubscription(string businessCode, string farmerUserId, string reference);
        Task<BundleDisbursementModel> UpdateDisbursement(BundleDisbursementModel model);
        Task<List<BundleDisbursementModel>> GetDisbursedSubscriptions(string businessCode);
        Task<PagedListResponse<List<BundleSubscriptionInterestModel>>> GetInterestRequestByAgent(string businessCode, string userId, int pageNumber = 1,
         int pageSize = 10);
        Task<List<BundleSubscriptionInterestModel>> GetPaidSubscriptionInterestByBusinessAgent(string businessCode, string userId);
        Task<List<BundleSubscriptionInterestModel>> GetPaidSubscriptionInterestByBusiness(string businessCode);
        Task<BundleSubscriptionInterestModel> UpdateEquitySubcriiptonStatus(BundleSubscriptionInterestModel model);
        Task<List<BundleSubscriptionInterestModel>> GetSubscriptionInterestByBusinessAgent(string businessCode, string userId);
        Task<List<BundleSubscriptionInterestModel>> GetExistingSubscription(BundleSubscriptionInterestModel data);
        Task<BundleSubscriptionInterestModel> ApproveEquityForDisbursement(BundleSubscriptionInterestModel model);
        Task<BundleInterestReceiptModel> RetrieveFarmerInputDisbursementReceipt(string businessCode, string farmerUserId, string reference);
        Task<PagedListResponse<List<BundleDisbursementModel>>> GetDisbursementByAgent(string businessCode, string warehouseManger, int pageNumber = 1, int pageSize = 10, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, string? location = null, string? disbursedBy = null);
        Task<PagedListResponse<List<BundleDisbursementModel>>> GetDisbursementByBusiness(string businessCode, int pageNumber = 1, int pageSize = 10, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, string? location = null, string? disbursedBy = null);
        Task<List<BundleDisbursementModel>> FilterDisbursementForDownload(string? businessCode, string? disbursedBy, string? state, DateTime? startDate, DateTime? endDate);
        Task<List<BundleSubscriptionInterestModel>> FilterSubscriptionForDownload(string? businessCode, string? cooperativeName, string? state, DateTime? startDate, DateTime? endDate);
        Task<List<BundleSubscriptionInterestModel>> GetFarmerSubscriptionInterestDetails(BundleSubscriptionInterestModel data);
        Task<List<BundleSubscriptionInterestModel>> isIdentifierPresent(BundleSubscriptionInterestModel data);
        Task<BundleSubscriptionInterestModel> SettlePendingFarmerBundleSubscriptionEquityToBusiness(BundleSubscriptionInterestModel model);
    }
}
