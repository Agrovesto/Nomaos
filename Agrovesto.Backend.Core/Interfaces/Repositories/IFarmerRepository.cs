using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.V2.Onboarding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Repositories
{
    public interface IFarmerRepository
    {
        Task<(bool status, FarmerModel data)> CreateFarmer(FarmerModel data);
        Task<BusinessEntitiesMetrics> GetBusinessEntitiesMetricsAsync(string businessCode, string? organizationCode);
        Task<FarmerModel> GetFarmerById(long Id);
        Task<FarmerModel> GetFarmerByUserId(string userId);
        Task<List<FarmerModel>> GetFarmers();
        Task<List<FarmerModel>> GetFarmersByAgent(string agentId, ListQueryParams query);
        Task<List<FarmerModel>> GetFarmersByBusinessCode(string businessCode, ListQueryParams query);
        Task<List<FarmerModel>> GetFarmersByCooperativeId(long? cooperativeId, ListQueryParams query);
        Task<List<FarmerModel>> GetFarmersByOnboardedUserId(string userId, ListQueryParams query);
        Task<(bool status, FarmerImagesModel data)> SaveImage(FarmerImagesModel data);

        Task<FarmerImagesModel> GetFarmerImage(string userId, string businessCode, string organizationCode);
        Task<List<FarmerImagesModel>> GetImagesByBusinessCode(string businessCode, string? organizationCode);
        Task<FarmerModel> AssignFarmerToCooperative(long farmerId,
    long cooperativeId,
    string? assignedByUserId = null);
        Task<BusinessEntitiesMetrics> GetBusinessEntitiesMetricsAsyncByAgent(string agentId, ListQueryParams query);
        Task<FarmerModel> AssignFarmerToAgent(string agentuserId, long farmerId);
        Task<List<FarmerModel>> FilterFarmerForDownload(string? businessCode,
   string? organizationCode, string? cooperativeName, string? state, DateTime? startDate, DateTime? endDate);
        Task<FarmerDetailDto> GetFarmerDetails(string userId);
        Task<PagedListResponse<List<FarmerDetailDto>>> GetAllFarmerDetails(string businessCode,
          string? organizationCode = null,
          IReadOnlyList<long>? cooperativeIds = null,
          IReadOnlyList<long>? agentIds = null,
          string? state = null,
          string? sortBy = "Date",
          bool sortAsc = true);
        Task<bool> DoesPhoneAlreadyExistWithinOrganization(string phonenumber, string organizationCode);
        Task<List<string>> GetAllPhonesForOrganizationAsync(string organizationCode);
        Task<FarmerImagesModel> GetUserImage(string userId, string businessCode, string? organizationCode);
        Task<FarmerModel> AssignFarmerToAgents(AssignmentArgs args, long farmerId);
        Task<FarmerModel> AssignFarmerToCooperatives(AssignmentArgs args, long farmerId);
    }
}
