using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.DB;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories
{
  

    public class AgentRepository : IAgentRepository
    {
        private readonly DataContext _context;

        public AgentRepository(DataContext context)
        {
            _context = context;
        }
        public async Task<(bool status, AgentModel data)> CreateAgent(AgentModel data)
        {
            var entity = data.Map();

            await _context.AddAsync(entity);
            await _context.SaveChangesAsync();

            return (true, entity.Map());
        }

        public async Task<AgentModel> GetAgentDetailByUserId(string userId)
        {
            var Agent = await _context.Agents.Where(x => x.UserId == userId).FirstOrDefaultAsync();
            if (Agent == null)
            {
                return null;
            }
            return Agent.Map();
        }

        public async Task<AgentModel> GetAgentDetailById(long Id)
        {
            var Agent = await _context.Agents.Where(x => x.Id == Id).FirstOrDefaultAsync();
            if (Agent == null)
            {
                return null;
            }
            return Agent.Map();
        }
        public async Task<List<AgentModel>> GetAgents() //restricted to agrovesto
        {

            var Agent = await _context.Agents.ToListAsync();
            if (Agent.Count > 0)
            {
                return Agent.Select(x => x.Map()).OrderByDescending(x => x.OnBoardedOn).ToList();
            }
            return null;
        }


        public async Task<List<AgentModel>> GetAgentsByBusinessCode(string businessCode)
        {

            var Agent = await _context.Agents.Where(x => x.BelongToBusinessCode == businessCode).ToListAsync();
            if (Agent.Count > 0)
            {
                return Agent.Select(x => x.Map()).OrderByDescending(x => x.OnBoardedOn).ToList();
            }
            return null;
        }


    }
}


