using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Google;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories
{
 
    public interface IFarmRepository
    {
        Task<Farm> AddFarmAsync(Farm farm);
        Task<FarmAllocation> AddAsync(FarmAllocation allocation);
        Task<List<FarmAllocation>> GetByFarmAsync(long farmId);
        Task<Farm?> GetByIdAsync(long id);
        Task<List<Farm>> GetAllWithCoordinatesAsync();
        Task AddAuditTrailAsync(FarmMappingAuditTrail trail);
        Task<List<Farm>> CooperativeFarmMaps(long CooperativeId);
        Task<List<Farm>> GetAllWithCoordinatesAsync(BusinessRegistrationArgs args);
    }
    public class FarmRepository : IFarmRepository
    {
        private readonly DataContext _context;
        public FarmRepository(DataContext context) => _context = context;

        public async Task<Farm> AddFarmAsync(Farm farm)
        {
            _context.Farms.Add(farm);
            await _context.SaveChangesAsync();
            return farm;
        }

        public async Task<Farm?> GetByIdAsync(long id)
        {
            return await _context.Farms
                .Include(f => f.Coordinates)
                .Include(f => f.AuditTrails)
                .FirstOrDefaultAsync(f => f.Id == id);
        }
        public async Task<List<Farm>> CooperativeFarmMaps(long CooperativeId)
        {
            return await _context.Farms
               // .Where(c => c.MembershipId == CooperativeId)
                .Include(f => f.Coordinates)
                .Include(f => f.AuditTrails)
                .ToListAsync();
        }
        public async Task<List<Farm>> GetAllWithCoordinatesAsync()
        {
            return await _context.Farms
                .Include(f => f.Coordinates)
                .Include(f => f.AuditTrails)
                .Include(f=>f.FarmLandDocuments)
                .Include(f=>f.Allocations)
                .ToListAsync();
        }
        public async Task<List<Farm>> GetAllWithCoordinatesAsync(BusinessRegistrationArgs args)
        {
            var query=  _context.Farms
                .Include(f => f.Coordinates)
                .Include(f => f.AuditTrails)
                .Include(f=> f.FarmLandDocuments)
                .Include(f=> f.Allocations)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(args.organizationCode))
            {
                query = query.Where(x => x.OrganizationCode == args.organizationCode);
            }
            //if (args.CooperativeId >0)
            //{
            //    query = query.Where(x => x.MembershipId == args.CooperativeId);
            //}
            return await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

        }
        public async Task AddAuditTrailAsync(FarmMappingAuditTrail trail)
        {
            _context.FarmMappingAuditTrails.Add(trail);
            await _context.SaveChangesAsync();
        }
        public async Task<FarmAllocation> AddAsync(FarmAllocation allocation)
        {
            _context.FarmAllocations.Add(allocation);
            await _context.SaveChangesAsync();
            return allocation;
        }

        public async Task<List<FarmAllocation>> GetByFarmAsync(long farmId) =>
            await _context.FarmAllocations.Where(a => a.FarmId == farmId).ToListAsync();

    }
}
