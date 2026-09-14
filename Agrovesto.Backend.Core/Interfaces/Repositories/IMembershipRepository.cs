using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Payment;
using Agrovesto.Backend.Core.V2.Onboarding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Repositories
{
    public interface IMembershipRepository
    {
        Task<List<AgentModel>> AgentsBySupervisor(string supervisorId, ListQueryParams query);
       // Task<MemberShipModel> AssignCooperativeToAgent(string agentId, long cooperativeId);
        Task<List<FarmerModel>> AssignFarmerToAgent(string agentUserId, long cooperativeId);
        Task<AgentModel> AssignSupervisorToAgent(string agentId, string supervisorId);
        Task<(bool status, MemberShipModel data)> CreateCooperative(MemberShipModel data);
        Task<(bool status, MemberShipModel data)> CreateUpdateMembership(MemberShipModel data);
        Task<(bool status, MemberShipTypeModel data)> CreateUpdateMemberShipType(MemberShipTypeModel data, BusinessRegistrationArgs args);
       Task<PagedListResponse<List<MemberShipResponseModel>>> GetCooperativeByAgent(string AgentId, ListQueryParams query);
        Task<MemberShipModel> GetCooperativeDetailById(long Id);
        Task<MemberShipModel> GetMembershipDetailByUserId(string userId);
        Task<List<MemberShipModel>> GetCooperatives();
        Task<List<MemberShipModel>> GetCooperativesByBusinessCode(string businessCode,ListQueryParams query);
        Task<MembershipDashboardDto> GetMembershipDetailAsync(string membershipuserId, string organizationCode = null);
        Task<MemberShipTypeModel?> GetMemberShipTypeById(long id);
        Task<List<MemberShipTypeModel>> GetMembershipTypes(BusinessRegistrationArgs args);
        Task<AgentModel> ViewAgentAssignedToCooperative(long cooperativeId);
        Task<MemberShipModel> AssignMembershipToAgents(AssignmentArgs args, long membershipId);
        Task<PagedListResponse<List<AgentMembershipDto>>> GetAgentsByMembership(long membershipId, ListQueryParams query);
        Task<AgentOverviewDto> GetAgentOverview(ListQueryParams args);
        Task<List<MemberShipModel>> AssignMemberShipToAgent(string agentUserId, List<long> membershipIds);
        Task<MembershipResponse> AssignLeaderAsync(AssignLeaderDto dto, string updatedById);
        Task<MembershipResponse> CreateMembershipAsync(CreateMembershipDto dto, OnBoardingArgs args);
        Task<bool> DeactivateUser(AssignmentArgs args, TaskDescription model);
        Task<PagedListResponse<List<MembershipAgentDto>>> GetMembershipAgentsAsync(string membershipUserId, ListQueryParams query);
        Task<PagedListResponse<List<MembershipFarmerDto>>> GetMembershipFarmersAsync(string membershipUserId, ListQueryParams query);
        Task<PagedListResponse<List<MembershipFinancingBreakdownDto>>> GetMembershipFinancingBreakdownAsync(string membershipUserId, ListQueryParams query);
    }
}
