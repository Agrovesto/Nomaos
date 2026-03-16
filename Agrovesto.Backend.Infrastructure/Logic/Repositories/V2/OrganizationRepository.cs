using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories.V2
{
    public interface IOrganizationRepository
    {
        Task<OrganizationLocationModel> CreateLocationAsync(OrganizationLocationModel model);
        Task<OrganizationRoleModel> CreateRoleAsync(OrganizationRoleModel model);
        Task<List<OrganizationLocationModel>> GetLocationAsync(ListQueryParams query);
        Task<OrganizationLocationModel> GetLocationByIAsync(long Id);
        Task<OrganizationAgentResponse> GetOrganizationAgentDetail(ListQueryParams args);
        Task<PagedListResponse<List<OrganizationAgentResponse>>> GetOrganizationAgents(ListQueryParams args);
        Task<OrganizationRoleModel> GetRoleByIAsync(long Id);
        Task<List<OrganizationRoleModel>> GetRolesAsync(ListQueryParams query);
    }
    public class OrganizationRepository : IOrganizationRepository
    {
        private readonly DataContext _context;
        public OrganizationRepository(DataContext context)
        {
            _context = context;
        }

        public async Task<OrganizationAgentResponse> GetOrganizationAgentDetail(ListQueryParams args)
        {
            var orgAgent = await _context.OrganizationAgents
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AgentId ==args.Id  && x.IsActive);

            if (orgAgent == null)
                return null;

            var agent = await _context.Agents
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == orgAgent.AgentId);

            var location = await _context.OrganizationLocations
                .AsNoTracking()
                .Where(x => x.Id == orgAgent.LocationId)
                .Select(x => new OrganizationLocationDto
                {
                    LocationId = x.Id,
                    LocationName = x.LocationName,
                    LocationType = x.LocationType.Type,
                    State = x.State.State,
                    Lga = x.LGA.Name
                })
                .FirstOrDefaultAsync();

            var roles = await _context.OrganizationAgentRoles
                .AsNoTracking()
                .Where(x => x.OrganizationAgentId == orgAgent.Id)
                .Select(x => new OrganiationRoleDto
                {
                    RoleId = x.RoleId,
                    RoleName = x.OrganizationRole.RoleName
                })
                .ToListAsync();

            return new OrganizationAgentResponse
            {
                AgentId = agent.Id,
                UserId = agent.UserId,
                Name = agent.Name,
                Email = agent.Email,
                PhoneNumber = agent.PhoneNumber,
                State = agent.State,
                Lga = agent.Lga,
                Ward = agent.Ward,
                OnBoardedOn = agent.OnBoardedOn,
                Locations = location == null ? new List<OrganizationLocationDto>() : new List<OrganizationLocationDto> { location },
                Roles = roles
            };
        }
        public async Task<PagedListResponse<List<OrganizationAgentResponse>>> GetOrganizationAgents(ListQueryParams args)
        {
            var organizationAgentsQuery = _context.OrganizationAgents
                .AsNoTracking()
                .Where(x => x.OrganizationId == args.Id && x.IsActive);

            var totalCount = await organizationAgentsQuery.CountAsync();

            var pagedAgents = await organizationAgentsQuery
                .OrderByDescending(x => x.AssignedOn)
                .Skip((args.PageNumber - 1) * args.PageSize)
                .Take(args.PageSize)
                .ToListAsync();

            var agentIds = pagedAgents.Select(x => x.AgentId).ToList();
            var orgAgentIds = pagedAgents.Select(x => x.Id).ToList();
            var locationIds = pagedAgents.Select(x => x.LocationId).Distinct().ToList();

            var agents = await _context.Agents
                .AsNoTracking()
                .Where(x => agentIds.Contains(x.Id))
                .ToListAsync();

            var locations = await _context.OrganizationLocations
                .AsNoTracking()
                .Where(x => locationIds.Contains(x.Id))
                .Select(x => new OrganizationLocationDto
                {
                    LocationId = x.Id,
                    LocationName = x.LocationName,
                    LocationType = x.LocationType.Type,
                    State = x.State.State,
                    Lga = x.LGA.Name
                })
                .ToListAsync();

            var roles = await _context.OrganizationAgentRoles
                .AsNoTracking()
                .Where(x => orgAgentIds.Contains(x.OrganizationAgentId))
                .Select(x => new
                {
                    x.OrganizationAgentId,
                    Role = new OrganiationRoleDto
                    {
                        RoleId = x.RoleId,
                        RoleName = x.OrganizationRole.RoleName
                    }
                })
                .ToListAsync();

            var result = pagedAgents.Select(oa =>
            {
                var agent = agents.FirstOrDefault(x => x.Id == oa.AgentId);

                return new OrganizationAgentResponse
                {
                    AgentId = agent?.Id ?? 0,
                    UserId = agent?.UserId,
                    Name = agent?.Name,
                    Email = agent?.Email,
                    PhoneNumber = agent?.PhoneNumber,
                    State = agent?.State,
                    Lga = agent?.Lga,
                    Ward = agent?.Ward,
                    OnBoardedOn = agent?.OnBoardedOn ?? DateTimeOffset.MinValue,

                    Locations = locations
                        .Where(l => l.LocationId == oa.LocationId)
                        .ToList(),

                    Roles = roles
                        .Where(r => r.OrganizationAgentId == oa.Id)
                        .Select(r => r.Role)
                        .ToList()
                };
            }).ToList();

            return new PagedListResponse<List<OrganizationAgentResponse>>
            {
                PageNumber = args.PageNumber,
                PageSize = args.PageSize,
                Count = totalCount,
                Data = result
            };
        }
        public async Task<OrganizationLocationModel> CreateLocationAsync(OrganizationLocationModel model)
        {
            var orgExist = await _context.OrganizationLocations
              .FirstOrDefaultAsync(x => x.LocationName == model.LocationName);

            if (orgExist != null)
            {
                return orgExist.ViewMap();
            }
            OrganizationLocation org = model.Map();
            org.IsActive = true;
            _context.Set<OrganizationLocation>().Add(org);
            await _context.SaveChangesAsync();
            var response = org.ViewMap();
            return response;
        }
        public async Task<List<OrganizationLocationModel>> GetLocationAsync(ListQueryParams query)
        {
            var org = await _context.Set<OrganizationLocation>()
                .Where(x => x.BusinessCode == query.businessCode && x.OrganizationId == query.Id).ToListAsync();

            return org.Select(x => x.ViewMap()).ToList();
        }
        public async Task<OrganizationLocationModel> GetLocationByIAsync(long Id)
        {
            var org = await _context.Set<OrganizationLocation>()
                .FirstOrDefaultAsync(x => x.Id == Id);
            if (org == null)
            {
                return null;
            }
            return org.ViewMap();
        }

        public async Task<OrganizationRoleModel> CreateRoleAsync(OrganizationRoleModel model)
        {
            var orgExist = await _context.OrganizationRoles
              .FirstOrDefaultAsync(x => x.RoleName == model.RoleName && x.OrganizationId== model.OrganizationId);

            if (orgExist != null)
            {
                return orgExist.ViewMap();
            }
            OrganizationRole org = model.Map();
            org.CreatedOn = DateTimeOffset.Now;
            _context.Set<OrganizationRole>().Add(org);
            await _context.SaveChangesAsync();
            var response = org.ViewMap();
            return response;
        }
        public async Task<List<OrganizationRoleModel>> GetRolesAsync(ListQueryParams query)
        {
            var org = await _context.Set<OrganizationRole>()
                .Where(x => x.BusinessCode == query.businessCode && x.OrganizationId == query.Id).ToListAsync();

            return org.Select(x => x.ViewMap()).ToList();
        }
        public async Task<OrganizationRoleModel> GetRoleByIAsync(long Id)
        {
            var org = await _context.Set<OrganizationRole>()
                .FirstOrDefaultAsync(x => x.Id == Id);
            if (org == null)
            {
                return null;
            }
            return org.ViewMap();
        }
    }
}
