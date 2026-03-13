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
        Task<List<OrganizationLocationModel>> GetLocationAsync(ListQueryParams query);
        Task<OrganizationLocationModel> GetLocationByIAsync(long Id);
    }
    public class OrganizationRepository : IOrganizationRepository
    {
        private readonly DataContext _context;
        public OrganizationRepository(DataContext context)
        {
            _context = context;
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


    }
}
