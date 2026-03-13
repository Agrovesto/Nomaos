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
        public async Task<PagedListResponse<List<OrganizationAgentResponse>>> GetOrganizationAgents(ListQueryParams args)
        {
            var baseQuery =
       from oa in _context.OrganizationAgents.AsNoTracking()
       join a in _context.Agents.AsNoTracking()
           on oa.AgentId equals a.Id
       join loc in _context.OrganizationLocations.AsNoTracking()
           on oa.LocationId equals loc.Id into locGroup
       from loc in locGroup.DefaultIfEmpty()
       where oa.OrganizationId == args.Id && oa.IsActive
       select new { oa, a, loc };

            var totalCount = await baseQuery.CountAsync();

            var agents = await baseQuery
                .OrderByDescending(x => x.oa.AssignedOn)
                .Skip((args.PageNumber - 1) * args.PageSize)
                .Take(args.PageSize)
                .Select(x => new OrganizationAgentResponse
                {
                    AgentId = x.a.Id,
                    UserId = x.a.UserId,
                    Name = x.a.Name,
                    Email = x.a.Email,
                    PhoneNumber = x.a.PhoneNumber,
                    State = x.a.State,
                    Lga = x.a.Lga,
                    Ward = x.a.Ward,
                    OnBoardedOn = x.a.OnBoardedOn,

                    Location = x.loc == null ? null : new OrganizationLocationDto
                    {
                        LocationId = x.loc.Id,
                        LocationName = x.loc.LocationName,
                        LocationType = x.loc.LocationType.Type,
                        State = x.loc.State.State,
                        Lga = x.loc.LGA.Name
                    },

                    Roles = _context.OrganizationAgentRoles
                        .Where(r => r.OrganizationAgentId == x.oa.Id)
                        .Select(r => new OrganiationRoleDto
                        {
                            RoleId = r.RoleId,
                            RoleName = r.OrganizationRole.RoleName
                        })
                        .ToList()
                })
                .ToListAsync();

            return new PagedListResponse<List<OrganizationAgentResponse>>
            {
                PageNumber = args.PageNumber,
                PageSize = args.PageSize,
                Count = totalCount,
                Data = agents
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
