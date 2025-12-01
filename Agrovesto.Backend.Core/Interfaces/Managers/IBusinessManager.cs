using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Common.WalletServiceRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Managers
{
    public interface IBusinessManager
    {
        Task<PagedListResponse<List<CooperativeModel>>> GetCooperativesByAgent(string userId,
       ListQueryParams query);
        Task<CooperativeModel> AssignCooperativeToAgent(string agentId, long cooperativeId);
        Task<BusinessModel> CreateBusiness(BusinessModel business, BusinessRegistrationArgs args);
        Task<SubAccountModel> CreateBusinessUser(UserModel model, BusinessRegistrationArgs args);
        Task<BusinessWorkerModel> CreateBusinessWorkerAsync(BusinessWorkerModel bWorker, BusinessRegistrationArgs args);
        Task<PagedListResponse<List<AgentModel>>> GetAgentsByBusinessCode(string userId, ListQueryParams query);
        Task<BusinessLoadModel> GetBusinessBySinageSaltAsync(string businessCode, string signagesalt);
        Task<PagedListResponse<List<BusinessModel>>> GetBusinessesAsync(ListQueryParams query);
        Task<PagedListResponse<List<CooperativeModel>>> GetCooperativesByBusinessCode(string userId, ListQueryParams query);
        Task<PagedListResponse<List<FarmerModel>>> GetFarmersByBusinessCode(string userId, ListQueryParams query);
        Task<PagedListResponse<List<FarmerModel>>> GetFarmersByCooperativeUserId(string userId, ListQueryParams query);
        Task<PagedListResponse<List<BusinessWorkerModel>>> GetStaffbyBusiness(string userId, ListQueryParams query);
        Task<PagedListResponse<List<BusinessWorkerModel>>> GetStaffByBusinessCode(string businessCode, string organizationCode);
        Task<SubAccountModel> UpdateUserAccountProfile(SubAccountModel subaccount, BusinessRegistrationArgs args, string userId);
        Task<AgentModel> ViewAgentAssignedToCooperative(long cooperativeId);
        Task<PagedListResponse<List<FarmerModel>>> GetFarmersByAgent(string userId, ListQueryParams query);
        Task<PagedListResponse<List<FarmerModel>>> GetFarmersByCooperativeId(string userId, ListQueryParams query);
        Task<Response<BusinessEntitiesMetrics>> GetBusinessEntitiesMetricsAsync(ListQueryParams query);
        Task<ImageUploadResponse> SaveFarmerImage(ImageModel model, BusinessRegistrationArgs args);
        Task<FarmerModel> AssignFarmerToCooperative(long farmerId, long cooperativeId);
        Task<Response<BusinessEntitiesMetrics>> GetBusinessEntitiesMetricsAsyncByAgent(ListQueryParams query);
        Task<UserModel> MakeUserAgent(BusinessRegistrationArgs args);
        Task<AgentModel> AssignSupervisorToAgent(string agentId, string supervisorId);
        Task<PagedListResponse<List<AgentModel>>> AgentsBySupervisor(string userId, ListQueryParams query);
        Task<Response<UserAccountDetailsModel>> GetUserBankAccountDetails(ListQueryParams query);
        Task<Response<UserWalletBalanceModel>> GetUserBankAccountBalance(ListQueryParams query);
        Task<Response<FarmerModel>> GetFarmerDetailWithImage(ListQueryParams query, string imageKey);
        Task<Response<CafeOneCustomerWalletResponse>> RetrieveCustomerWallet(ListQueryParams query);
        Task<OrganizationModel> CreateOrganization(OrganizationModel org, BusinessRegistrationArgs args);
        Task<List<OrganizationModel>> GetBusinessOrganizationAsync(string businessCode);
        Task<FarmerDetailDto> GetFarmerDetails( ListQueryParams query);
        Task<FileDownloadModel> GetAllFarmerDetails(ListQueryParams query);
        Task<CooperativeModel> GetCooperativeDetailByUserId(string userId);
        Task<Response<CooperativeModel>> GetCooperativeDetailById(long Id);
        Task<BulkUploadFarmersResponse> BulkUploadFarmersAsync(FarmerUploadModel request, BusinessRegistrationArgs args);
    }
}
