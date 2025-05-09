using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Models.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Managers
{
    public interface IInputFinancingManagement
    {
        Task<(bool status, BundleSubscriptionInterestResponseModel data)> CreateInputFinancingInterest(BundleSubscriptionInterestModel model, BusinessRegistrationArgs args);

        Task<(bool status, BundleSubscriptionInterestModel data)> ConfirmEquityCommitmentPayment(BundleSubscriptionInterestModel model, BusinessRegistrationArgs args);
        Task<List<BundleSubscriptionInterestModel>> GetInterestRequestByLocation(BusinessRegistrationArgs args);
        Task<List<BundleSubscriptionInterestModel>> GetInterestRequestByCooperative(BusinessRegistrationArgs args);
        Task<PagedListResponse<List<BundleSubscriptionInterestModel>>> GetInterestRequestByBusiness(BusinessRegistrationArgs args);
        Task<Response<List<BundleSubscriptionSummary>>> GetBundleSubscriptionAnalysisByBusiness(ListQueryParams query);
        Task<Response<List<SubscriptionData>>> FarmerBundleSubscriptions(ListQueryParams query);
        Task<BundleDisbursementModel> DisburseInput(BundleDisbursementModel model, BusinessRegistrationArgs args);
        Task<List<BundleDisbursementModel>> GetAllInputDisbursement(BusinessRegistrationArgs args);
        Task<PagedListResponse<List<BundleSubscriptionInterestModel>>> GetInterestRequestByAgent(BusinessRegistrationArgs args);
        Task<List<BundleSubscriptionInterestModel>> GetPaidSubscriptionInterestByBusinessAgent(BusinessRegistrationArgs args);
        Task<List<BundleSubscriptionInterestModel>> GetPaidSubscriptionInterestByBusiness(BusinessRegistrationArgs args);
        Task<BundleInterestReceiptModel> RetrieveFarmerInputDisbursementReceipt(BundleSubscriptionInterestModel model, BusinessRegistrationArgs args);
        Task<BundleSubscriptionInterestModel> ApproveEquityForDisbursement(BundleSubscriptionInterestModel model, BusinessRegistrationArgs args);
    }
}
