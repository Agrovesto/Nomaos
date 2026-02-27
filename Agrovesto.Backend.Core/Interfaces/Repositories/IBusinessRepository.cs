using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.V2.Onboarding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Repositories
{
    public interface IBusinessRepository
    {
        Task<int> BusinessCount();
        Task<BusinessModel> CreateBusiness(BusinessModel businessModel, string userId);
        Task<BusinessWorkerModel> CreateBusinessWorker(BusinessWorkerModel bWorker);
        Task<OrganizationModel> CreateOrganization(OrganizationModel orgModel);
        Task<OrganizationResponseModel> CreateOrganizationV2(OrganizationOnboardingModel orgModel, OrganizationOnboardingArgs args);
        Task<BusinessModel> GetBusinessAsync(string businessName);
        Task<BusinessModel> GetBusinessByCodeAsync(string businessCode);
        Task<BusinessModel> GetBusinessByIdAsync(long businessId);
        Task<BusinessModel> GetBusinessBySinageSaltAsync(string signagesalt);
        Task<BusinessModel> GetBusinessByUserIdAsync(string userId);
        Task<List<BusinessModel>> GetBusinessesAsync();
        Task<List<OrganizationModel>> GetBusinessOrganizationAsync(string businessCode);
        Task<OrganizationModel> GetOrganizationAsync(string organizationCode);
        Task<OrganizationModel> GetOrganizationByCodeAsync(string organizationCode);
        Task<List<BusinessWorkerModel>> GetStaffByBusinessCode(long businessId, string organizationCode);
        Task<int> OrganizationCount();
    }
}
