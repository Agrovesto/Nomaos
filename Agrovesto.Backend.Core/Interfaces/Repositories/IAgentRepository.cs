using Agrovesto.Backend.Core.Models.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Repositories
{
    public interface IAgentRepository
    {
        Task<(bool status, AgentModel data)> CreateAgent(AgentModel data);
        Task<(bool status, AgentModel data)> CreateUpdateAgent(AgentModel data);
        Task<AgentModel> GetAgentBySuperVisor(string userId);
        Task<AgentModel> GetAgentDetailById(long Id);
        Task<AgentModel> GetAgentDetailByUserId(string userId);
        Task<List<AgentModel>> GetAgents();
        Task<List<AgentModel>> GetAgentsByBusinessCode(string businessCode, ListQueryParams query);
    }
}
