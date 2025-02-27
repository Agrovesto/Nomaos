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
        Task<List<BundleSubscriptionInterestModel>> GetSubscriptionInterestByBusiness(string businessCode);
        Task<List<BundleSubscriptionInterestModel>> GetSubscriptionInterestByCooperative(string businessCode, long CooperativeId);
        Task<List<BundleSubscriptionInterestModel>> GetUnpaidSubscriptionInterestByFarmer(BundleSubscriptionInterestModel data);
        Task<List<BundleSubscriptionInterestModel>> GetSubscriptionInterestByLocation(string businessCode, string Location);
        Task<BundleSubscriptionInterestModel> UpdateSubscriptionPayment(BundleSubscriptionInterestModel model);
        Task<List<BundleSubscriptionSummary>> FarmerBundleSubscriptionInterests(string businessCode);
        Task<SubscriptionMetrics> FarmerBundleSubscriptions(string businessCode);
    }
}
