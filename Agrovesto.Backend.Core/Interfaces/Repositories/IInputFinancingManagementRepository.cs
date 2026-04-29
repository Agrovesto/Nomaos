using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Input;
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
         string? location = null, string? organizationCode = null);
        Task<List<BundleSubscriptionInterestModel>> GetSubscriptionInterestByCooperative(string businessCode, long CooperativeId);
        Task<List<BundleSubscriptionInterestModel>> GetUnpaidSubscriptionInterestByFarmer(BundleSubscriptionInterestModel data);
        Task<List<BundleSubscriptionInterestModel>> GetSubscriptionInterestByLocation(string businessCode, string Location);
        Task<BundleSubscriptionInterestModel> UpdateSubscriptionPayment(BundleSubscriptionInterestModel model);
        Task<List<BundleSubscriptionSummary>> FarmerBundleSubscriptionInterests(ListQueryParams args);
        //   Task<SubscriptionMetrics> FarmerBundleSubscriptions(string businessCode);
        Task<List<SubscriptionData>> FarmerBundleSubscriptions(ListQueryParams args);
        Task<BundleSubscriptionInterestModel> GetPaidSubscriptionByFarmer(string businessCode, string farmerUserId, string reference);
        Task<BundleDisbursementModel> GetDisbursedSubscription(string businessCode, string farmerUserId, string reference);
        Task<BundleDisbursementModel> UpdateDisbursement(BundleDisbursementModel model);
        Task<List<BundleDisbursementModel>> GetDisbursedSubscriptions(string businessCode);
        Task<PagedListResponse<List<BundleSubscriptionInterestModel>>> GetInterestRequestByAgent(string businessCode, string userId, int pageNumber = 1,
         int pageSize = 10, string organizationCode = null);
        Task<List<BundleSubscriptionInterestModel>> GetPaidSubscriptionInterestByBusinessAgent(string businessCode, string userId);
        Task<List<BundleSubscriptionInterestModel>> GetPaidSubscriptionInterestByBusiness(string businessCode);
        Task<BundleSubscriptionInterestModel> UpdateEquitySubcriiptonStatus(BundleSubscriptionInterestModel model);
        Task<List<BundleSubscriptionInterestModel>> GetSubscriptionInterestByBusinessAgent(string businessCode, string userId);
        Task<List<BundleSubscriptionInterestModel>> GetExistingSubscription(BundleSubscriptionInterestModel data);
        Task<BundleSubscriptionInterestModel> ApproveEquityForDisbursement(BundleSubscriptionInterestModel model);
        Task<BundleInterestReceiptModel> RetrieveFarmerInputDisbursementReceipt(string businessCode, string farmerUserId, string reference);
        Task<PagedListResponse<List<BundleDisbursementModel>>> GetDisbursementByAgent(string businessCode, string warehouseManger, int pageNumber = 1, int pageSize = 10, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, string? location = null, string? disbursedBy = null);
        Task<PagedListResponse<List<BundleDisbursementModel>>> GetDisbursementByBusiness(string businessCode, int pageNumber = 1, int pageSize = 10, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, string? location = null, string? disbursedBy = null, string? organizationCode =null);
        Task<List<BundleDisbursementModel>> FilterDisbursementForDownload(string? businessCode, string? disbursedBy, string? state, DateTime? startDate, DateTime? endDate);
        Task<List<BundleSubscriptionInterestModel>> FilterSubscriptionForDownload(string? businessCode, string? cooperativeName, string? state, DateTime? startDate, DateTime? endDate);
        Task<List<BundleSubscriptionInterestModel>> GetFarmerSubscriptionInterestDetails(BundleSubscriptionInterestModel data);
        Task<List<BundleSubscriptionInterestModel>> isIdentifierPresent(BundleSubscriptionInterestModel data);
        Task<BundleSubscriptionInterestModel> SettlePendingFarmerBundleSubscriptionEquityToBusiness(BundleSubscriptionInterestModel model);
        Task<BundleSubscriptionInterestModel> GetSingleSubscription(string farmerUserId, long bundleId);
        Task SaveBundleSubscriptionRepayment(RepaymentModel model);
        Task<LoanDetailDto> GetLoanDetailAsync(long inputAllocationId);
        Task<PagedListResponse<List<FinancingBreakdownDto>>> GetFinancingBreakdownAsync(ListQueryParams args);
        Task<InputLoanSummaryDto> GetInputLoanSummaryAsync(ListQueryParams args);
        Task<FinancingRepaymentSummaryDto> GetFinancingRepaymentSummaryAsync(long inputAllocationId);
        Task<PagedListResponse<List<RepaymentBreakdownDto>>> GetRepaymentBreakdownAsync(long inputAllocationId, ListQueryParams args);
        Task<PagedListResponse<List<FarmerInputObligationDto>>> GetFarmerInputObligations(ListQueryParams args);
    }
}
