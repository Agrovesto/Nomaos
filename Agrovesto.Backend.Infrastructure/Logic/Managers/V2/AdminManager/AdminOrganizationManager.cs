using Agrovesto.Backend.Core.Models.Admin;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2.AdminManager
{
   
    public interface IAdminOrganizationManager
    {
        Task<PagedResult<OrganizationListItemDto>> GetOrganizationsAsync(OrganizationListQuery p);
    }
    public class AdminOrganizationManager : IAdminOrganizationManager
    {
        private readonly DataContext _context;
        public AdminOrganizationManager(DataContext context)
        {
            _context = context;
        }
        private IQueryable<Organization> OrganizationsInBusiness(long businessId)
        {
            return _context.Organizations
                .AsNoTracking()
                .Where(f => _context.UserBusinesses.Any(ub =>
                    ub.UserId == f.UserId &&
                    ub.BusinessId == businessId &&
                    ub.IsActive));
        }
        private IQueryable<Organization> ApplyFilters(IQueryable<Organization> q, OrganizationListQuery p)
        {
            if (!string.IsNullOrWhiteSpace(p.Search))
            {
                var s = p.Search.Trim();
                var digits = new string(s.Where(char.IsDigit).ToArray());
                long.TryParse(digits, out var idMatch);   // "FMR-1712" -> 1712

                q = q.Where(f =>
                    (f.CompanyName != null && f.CompanyName.Contains(s)) ||
                    (f.PhoneNumber != null && f.PhoneNumber.Contains(s)) ||
                    (f.UserId != null && f.UserId.Contains(s)) ||
                    (idMatch > 0 && f.OrganizationId == idMatch));
            }

            if (p.Status.HasValue)
                q = q.Where(f => f.OrganizationStatus == p.Status.Value);

         

            if (p.From.HasValue)
                q = q.Where(f => f.OnboardedOn >= p.From.Value);

            if (p.To.HasValue)
                q = q.Where(f => f.OnboardedOn < p.To.Value.Date.AddDays(1));

            return q;
        }

        public async Task<PagedResult<OrganizationListItemDto>> GetOrganizationsAsync(OrganizationListQuery p)
        {
            var page = Math.Max(p.PageNumber, 1);
            var size = Math.Clamp(p.PageSize, 1, 100);

            var q = ApplyFilters(OrganizationsInBusiness(p.BusinessId), p);
            var total = await q.CountAsync();

            var rows = await q
                .OrderByDescending(f => f.OnboardedOn)
                .ThenByDescending(f => f.OrganizationId)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(f => new { f.OrganizationId, f.CompanyName, f.PhoneNumber, f.OnboardedOn, f.OrganizationStatus, f.UserId, f.OrganizationCode })
                .ToListAsync();

            var ids = rows.Select(r => r.OrganizationId).ToList();

            //var farmerCounts = await _context.FarmerAgents
            //    .Where(x => ids.Contains(x.FarmerId))
            //    .GroupBy(x => x.AgentId)
            //    .Select(g => new { FarmerId = g.Key, Count = g.Count() })
            //    .ToDictionaryAsync(x => x.FarmerId, x => x.Count);

            //var orgs = (await (from fo in _context.OrganizationAgents
            //                   join o in _context.Organizations on fo.OrganizationId equals o.OrganizationId
            //                   where ids.Contains(fo.AgentId) && fo.IsActive
            //                   select new { fo.AgentId, o.CompanyName, o.OrganizationCode }).ToListAsync())
            // .ToLookup(x => x.AgentId);


            var items = rows.Select(r => new OrganizationListItemDto
            {
                Id = r.OrganizationId,
                OrganizationCode = r.OrganizationCode,
                OrganizationName=r.CompanyName,
                PhoneNumber = r.PhoneNumber,
                RegDate = r.OnboardedOn,
                Status = EnumExtensions.GetDisplayName(r.OrganizationStatus),
            }).ToList();

            return new PagedResult<OrganizationListItemDto>(items, total, page, size);
        }

    }

}
