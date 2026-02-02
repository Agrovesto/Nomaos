using Agrovesto.Backend.Core.Models.Businesses;
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
        Task<MemberShipModel> AssignCooperativeToAgent(string agentId, long cooperativeId);
        Task<List<FarmerModel>> AssignFarmerToAgent(string agentUserId, long cooperativeId);
        Task<AgentModel> AssignSupervisorToAgent(string agentId, string supervisorId);
        Task<(bool status, MemberShipModel data)> CreateCooperative(MemberShipModel data);
        Task<(bool status, MemberShipModel data)> CreateUpdateMembership(MemberShipModel data);
        Task<(bool status, MemberShipTypeModel data)> CreateUpdateMemberShipType(MemberShipTypeModel data, BusinessRegistrationArgs args);
        Task<List<MemberShipModel>> GetCooperativeByAgent(string AgentId, ListQueryParams query);
        Task<MemberShipModel> GetCooperativeDetailById(long Id);
        Task<MemberShipModel> GetCooperativeDetailByUserId(string userId);
        Task<List<MemberShipModel>> GetCooperatives();
        Task<List<MemberShipModel>> GetCooperativesByBusinessCode(string businessCode,ListQueryParams query);
        Task<MemberShipTypeModel?> GetMemberShipTypeById(long id);
        Task<List<MemberShipTypeModel>> GetMembershipTypes(BusinessRegistrationArgs args);
        Task<AgentModel> ViewAgentAssignedToCooperative(long cooperativeId);
    }
}
