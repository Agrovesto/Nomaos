using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories
{


    public class AgentRepository : IAgentRepository
    {
        private readonly DataContext _context;

        public AgentRepository(DataContext context)
        {
            _context = context;
        }

        public async Task<bool> UpdateOrganizationAgentLocation(OrganizationAgentArgs data)
        {
            var agent = await _context.Agents
                .FirstOrDefaultAsync(x => x.UserId == data.UserId);

            if (agent == null)
                throw new Exception("Agent not found");

            if (!data.OrganizationId.HasValue)
                throw new Exception("OrganizationId is required");

            var existingAssignment = await _context.OrganizationAgents
                .FirstOrDefaultAsync(x =>
                    x.AgentId == agent.Id &&
                    x.OrganizationId == data.OrganizationId &&
                    x.IsActive);

            if (existingAssignment == null)
            {
                await _context.OrganizationAgents.AddAsync(new OrganizationAgent
                {
                    AgentId = agent.Id,
                    OrganizationId = data.OrganizationId.Value,
                    AssignedByUserId = data.OnboardedById,
                    AssignedOn = DateTimeOffset.Now,
                    LocationId = data.LocationId,
                    IsActive = true
                });
            }
            else
            {
                existingAssignment.LocationId = data.LocationId;
                existingAssignment.AssignedOn = DateTimeOffset.Now;
                existingAssignment.AssignedByUserId = data.OnboardedById;

                _context.OrganizationAgents.Update(existingAssignment);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> UpdateOrganizationAgentRole(OrganizationAgentArgs data)
        {
            if (data.RoleId <=0 )
                throw new Exception("RoleId is required");

            var agent = await _context.Agents
                .FirstOrDefaultAsync(x => x.UserId == data.UserId);

            if (agent == null)
                throw new Exception("Agent not found");

            var organizationAgent = await _context.OrganizationAgents
                .FirstOrDefaultAsync(x =>
                    x.AgentId == agent.Id &&
                    x.OrganizationId == data.OrganizationId &&
                    x.IsActive);

            if (organizationAgent == null)
                throw new Exception("Agent is not assigned to this organization");
bool exists = await _context.OrganizationAgentRoles.AnyAsync(x =>
                x.OrganizationAgentId == organizationAgent.Id &&
                x.RoleId == data.RoleId);

            if (!exists)
            {
                await _context.OrganizationAgentRoles.AddAsync(new OrganizationAgentRole
                {
                    OrganizationAgentId = organizationAgent.Id,
                    RoleId = data.RoleId,
                    AssignedOn = DateTimeOffset.Now
                });

                await _context.SaveChangesAsync();
            }

            return true;
        }
        public async Task<(bool status, AgentModel data)> CreateUpdateAgent(AgentModel data)
        {
            try
            {
                var existingAgent = await _context.Agents
                    .FirstOrDefaultAsync(x => x.UserId == data.UserId);

                if (existingAgent != null)
                {
                    // PATCH: update only fields that have values
                    if (!string.IsNullOrWhiteSpace(data.State))
                        existingAgent.State = data.State;

                    if (!string.IsNullOrWhiteSpace(data.Lga))
                        existingAgent.Lga = data.Lga;

                    if (!string.IsNullOrWhiteSpace(data.PhoneNumber))
                        existingAgent.PhoneNumber = data.PhoneNumber;

                    if (!string.IsNullOrWhiteSpace(data.Email))
                        existingAgent.Email = data.Email;

                    if (!string.IsNullOrWhiteSpace(data.Location))
                        existingAgent.Location = data.Location;

                    if (!string.IsNullOrWhiteSpace(data.Name))
                        existingAgent.Name = data.Name;
                    if (!string.IsNullOrWhiteSpace(data.Ward))
                        existingAgent.Ward = data.Ward;

                    if (data.OrganizationId.HasValue)
                    {
                        bool exists = await _context.OrganizationAgents.AnyAsync(x =>
                            x.AgentId == existingAgent.Id &&
                            x.OrganizationId == data.OrganizationId.Value);

                        if (!exists)
                        {
                            await _context.OrganizationAgents.AddAsync(new OrganizationAgent
                            {
                                AgentId = existingAgent.Id,
                                OrganizationId = data.OrganizationId.Value,
                                AssignedByUserId = data.OnboardedById,
                                AssignedOn= DateTimeOffset.Now
                            });
                        }
                    }
                
                    _context.Agents.Update(existingAgent);
                    await _context.SaveChangesAsync();

                    return (true, existingAgent.Map());
                }

                var entity = data.Map();
                 await _context.Agents.AddAsync(entity);
                await _context.SaveChangesAsync();
                if (data.OrganizationId.HasValue)
                {
                    bool alreadyLinked = await _context.OrganizationAgents
                        .AnyAsync(fo => fo.AgentId == entity.Id && fo.OrganizationId == data.OrganizationId.Value);

                    if (!alreadyLinked)
                    {
                        await _context.OrganizationAgents.AddAsync(new OrganizationAgent
                        {
                            AgentId = entity.Id,
                            OrganizationId = data.OrganizationId.Value,
                            AssignedByUserId = data.OnboardedById,
                            AssignedOn = DateTimeOffset.Now
                        });
                        await _context.SaveChangesAsync();
                    }
                }
                return (true, entity.Map());
            }
            catch (BaseException ex)
            {
                throw new BaseException(
                    Core.Enums.ExceptionType.OPERATION_FAILED,
                    ex.Message);
            }
            catch (Exception ex)
            {
                throw new NotSuccessfulException(
                    $"Could not save agent details: {ex.Message}");
            }
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
            //if (!string.IsNullOrEmpty(query.organizationCode))
            //{
            //    agents = agents.Where(x=> x.OrganizationCode == query.organizationCode);
            //}

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

        }


    }
}


