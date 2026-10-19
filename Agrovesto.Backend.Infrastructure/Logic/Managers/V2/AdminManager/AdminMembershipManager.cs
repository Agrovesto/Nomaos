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
  
    public interface IAdminMembershipManager
    {
        Task<PagedResult<MemberShipListItemDto>> GetMembershipsAsync(MemberShipListQuery p);
    }
    public class AdminMembershipManager : IAdminMembershipManager
    {
        private readonly DataContext _context;
        public AdminMembershipManager(DataContext context)
        {
            _context = context;
        }
        private IQueryable<MemberShip> MembershipsInBusiness(long businessId)
        {
            return _context.MemberShips
                .AsNoTracking()
                .Where(f => _context.UserBusinesses.Any(ub =>
                    ub.UserId == f.UserId &&
                    ub.BusinessId == businessId &&
                    ub.IsActive));
        }
        private IQueryable<MemberShip> ApplyFilters(IQueryable<MemberShip> q, MemberShipListQuery p)
        {
            if (!string.IsNullOrWhiteSpace(p.Search))
            {
                var s = p.Search.Trim();
                var digits = new string(s.Where(char.IsDigit).ToArray());
                long.TryParse(digits, out var idMatch);   // "FMR-1712" -> 1712

                q = q.Where(f =>
                    (f.Name != null && f.Name.Contains(s)) ||
                    (f.PhoneNumber != null && f.PhoneNumber.Contains(s)) ||
                    (f.UserId != null && f.UserId.Contains(s)) ||
                    (idMatch > 0 && f.Id == idMatch));
            }

            if (p.Status.HasValue)
                q = q.Where(f => f.MembershipStatus == p.Status.Value);

            if (!string.IsNullOrWhiteSpace(p.OrganizationCode))
                q = q.Where(f => _context.FarmerOrganizations.Any(fo =>
                    fo.FarmerId == f.Id &&
                    fo.OrganizationCode == p.OrganizationCode &&
                    fo.IsActive));

            if (p.From.HasValue)
                q = q.Where(f => f.OnBoardedOn >= p.From.Value);

            if (p.To.HasValue)
                q = q.Where(f => f.OnBoardedOn < p.To.Value.Date.AddDays(1));

            return q;
        }

        public async Task<PagedResult<MemberShipListItemDto>> GetMembershipsAsync(MemberShipListQuery p)
        {
            var page = Math.Max(p.PageNumber, 1);
            var size = Math.Clamp(p.PageSize, 1, 100);

            var q = ApplyFilters(MembershipsInBusiness(p.BusinessId), p);
            var total = await q.CountAsync();

            var rows = await q
                .OrderByDescending(f => f.OnBoardedOn)
                .ThenByDescending(f => f.Id)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(f => new { f.Id, f.Name, f.PhoneNumber, f.OnBoardedOn, f.MembershipStatus, f.UserId })
                .ToListAsync();

            var ids = rows.Select(r => r.Id).ToList();

            var farmerCounts = await _context.FarmerAgents
                .Where(x => ids.Contains(x.FarmerId))
                .GroupBy(x => x.AgentId)
                .Select(g => new { FarmerId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.FarmerId, x => x.Count);

            var orgs = (await (from fo in _context.OrganizationAgents
                               join o in _context.Organizations on fo.OrganizationId equals o.OrganizationId
                               where ids.Contains(fo.AgentId) && fo.IsActive
                               select new { fo.AgentId, o.CompanyName, o.OrganizationCode }).ToListAsync())
             .ToLookup(x => x.AgentId);


            var items = rows.Select(r => new MemberShipListItemDto
            {
                Id = r.Id,
                AgentId = r.UserId,
                FullName = r.Name,
                PhoneNumber = r.PhoneNumber,
                RegDate = r.OnBoardedOn,
                Status = EnumExtensions.GetDisplayName(r.MembershipStatus),
                Members = farmerCounts.GetValueOrDefault(r.Id),

                Organization = string.Join(", ", orgs[r.Id].Select(o => o.CompanyName).Distinct()),
                OrganizationCode = string.Join(", ", orgs[r.Id].Select(o => o.OrganizationCode).Distinct()),
            }).ToList();

            return new PagedResult<MemberShipListItemDto>(items,  page, size, total);
        }

    }
}
