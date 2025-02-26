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

        Task<(bool status, string message)> ConfirmEquityCommitmentPayment(BundleSubscriptionInterestModel model, BusinessRegistrationArgs args);
        Task<List<BundleSubscriptionInterestModel>> GetInterestRequestByLocation(BusinessRegistrationArgs args);
        Task<List<BundleSubscriptionInterestModel>> GetInterestRequestByCooperative(BusinessRegistrationArgs args);
        Task<List<BundleSubscriptionInterestModel>> GetInterestRequestByBusiness(BusinessRegistrationArgs args);
    }
}
