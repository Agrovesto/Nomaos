using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.DB;
using DocumentFormat.OpenXml.Wordprocessing;
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
            try
            {
                var existingAgent = await _context.Agents
                   .FirstOrDefaultAsync(x => x.UserId == data.UserId);

                if (existingAgent != null)
                {
                    // Agent already exists, return the existing one
                    return (true, existingAgent.Map());
                }

                var entity = data.Map();

                await _context.AddAsync(entity);
                await _context.SaveChangesAsync();

                return (true, entity.Map());
            }
            catch (BaseException ex)
            {
                throw new BaseException(Core.Enums.ExceptionType.OPERATION_FAILED, ex.Message);
            }
            catch (Exception ex)
            {
                throw new NotSuccessfulException($"Could not save agent details: {ex.Message}");
            }
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

        public async Task<AgentModel> GetAgentBySuperVisor(string userId)
        {
            var Agent = await _context.Agents.Where(x => x.SupervisorUserId == userId).FirstOrDefaultAsync();
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


        public async Task<List<AgentModel>> GetAgentsByBusinessCode(string businessCode, ListQueryParams query)
        {

            var agents = _context.Agents
              .Where(x => x.BelongToBusinessCode == businessCode)
              .AsQueryable();
            if (!string.IsNullOrEmpty(query.organizationCode))
            {
                agents = agents.Where(x=> x.OrganizationCode == query.organizationCode);
            }

                     var result = await agents
                .OrderByDescending(s => s.OnBoardedOn)
                .Skip(query.PageNumber)
                .Take(query.PageSize)
                 .Select(s => s.Map()).ToListAsync();


            if (result.Count > 0)
            {
                return result;
            }
            return null;

            //var Agent = await _context.Agents.Where(x => x.BelongToBusinessCode == businessCode).ToListAsync();
            //if (agents.Count > 0)
            //{
            //    return Agent.Select(x => x.Map()).OrderByDescending(x => x.OnBoardedOn).ToList();
            //}
            //return null;
        }


    }
}


