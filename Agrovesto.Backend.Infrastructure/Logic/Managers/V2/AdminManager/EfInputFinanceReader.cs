using Agrovesto.Backend.Core.Models.Admin;
using Agrovesto.Backend.Infrastructure.DB;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2.AdminManager
{
    public interface IInputFinanceReader
    {
        Task<(List<InputFinancePackageDto>, int)> GetPackagesAsync(string farmerUserId, int page, int pageSize);
        Task<InputFinanceSummaryDto> GetSummaryAsync(string farmerUserId);
    }
    public class EfInputFinanceReader :IInputFinanceReader
    {
        private readonly DataContext _context;

        public EfInputFinanceReader(DataContext context)
        {
            _context = context;
        }
        public async Task<InputFinanceSummaryDto> GetSummaryAsync(string farmerUserId)
        {
            var q = _context.InputPackageSubscriptions
                .Include(c=>c.InputPackage)
                .Include(c=>c.Payments)
                .AsNoTracking()
                .Where(p => p.SubscriberUserId == farmerUserId);

            var s = await q.GroupBy(_ => 1).Select(g => new
            {
                Total = g.Count(),
                Active = g.Count(p => p.Status == Core.Models.Businesses.Input.SubscriptionStatus.Active),
                Value = g.Sum(p => p.TotalFinancingValue),
                // Only active packages carry an unpaid balance
                Outstanding = g.Where(p => p.Status == Core.Models.Businesses.Input.SubscriptionStatus.Active)
                               .Sum(p => p.TotalFinancingValue - p.Payments.Sum(pay => pay.AmountPaid))
            }).FirstOrDefaultAsync();

            return new InputFinanceSummaryDto
            {
                TotalPackages = s?.Total ?? 0,
                ActivePackages = s?.Active ?? 0,
                TotalValue = s?.Value ?? 0,
                OutstandingBalance = Math.Max(0, s?.Outstanding ?? 0)
            };
        }

        public async Task<(List<InputFinancePackageDto>, int)> GetPackagesAsync(string farmerUserId, int page, int pageSize)
        {
            var q = _context.InputPackageSubscriptions
                .Include(c => c.InputPackage)
                .Include(c => c.Payments).AsNoTracking()
                .Where(p => p.SubscriberUserId == farmerUserId);

            var total = await q.CountAsync();

            var rows = await q.OrderByDescending(p => p.Id)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(p => new { p.Id, p.InputPackage.Name, p.InputPackage.InputSeason, p.InputPackage.TotalFinancingValue, p.Status, p.ApprovedOn })
                .ToListAsync();

            var items = rows.Select((p, i) => new InputFinancePackageDto
            {
                SerialNo = (page - 1) * pageSize + i + 1,
                Id = p.Id,
                PackageId = $"PKG{p.Id:D6}",
                PackageName = p.Name,
                Season = p.InputSeason,
                Value = p.TotalFinancingValue,
                Status = p.Status.ToString(),
                ApprovedDate = p.ApprovedOn
            }).ToList();

            return (items, total);
        }
    }
}
