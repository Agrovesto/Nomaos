using Agrovesto.Backend.Core.Models.Admin;
using Agrovesto.Backend.Core.Models.Businesses.Input;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Utilities;
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
            var rows = await _context.InputPackageSubscriptions
                .AsNoTracking()
                .Where(p => p.SubscriberUserId == farmerUserId)
                .Select(p => new
                {
                    p.Status,
                    p.TotalFinancingValue,
                    // nullable cast so a subscription with no payments returns 0 instead of failing
                    Paid = p.Payments
                        // .Where(pay => pay.Status == TransactionStatus.Successful)  // see note below
                        .Sum(pay => (decimal?)pay.AmountPaid) ?? 0m
                })
                .ToListAsync();

            var active = rows.Where(r => r.Status == SubscriptionStatus.Active).ToList();

            return new InputFinanceSummaryDto
            {
                TotalPackages = rows.Count,
                ActivePackages = active.Count,
    //            TotalFinancingValue = rows
    //.Where(r => (r.Status >= SubscriptionStatus.Disbursed && r.Status < SubscriptionStatus.PendingReleaseApproval))
    //.Sum(r => r.TotalFinancingValue),
                TotalDisbursed = rows.Count(r => r.Status == SubscriptionStatus.Disbursed),
                PendingDisbursement = rows.Count(r => r.Status == SubscriptionStatus.PendingDisbursement),
                // Only active packages carry an unpaid balance; floor each at 0 so an
                // overpayment on one package doesn't reduce the balance of another
                TotalFinancingValue = rows.Where(r => IsFinanced(r.Status)).Sum(r => r.TotalFinancingValue),
                OutstandingBalance = rows.Where(r => IsOwing(r.Status))
                          .Sum(r => Math.Max(0m, r.TotalFinancingValue - r.Paid)),
            };
        }
        public async Task<(List<InputFinancePackageDto>, int)> GetPackagesAsync(
     string farmerUserId, int page, int pageSize)
        {
            var q = _context.InputPackageSubscriptions.AsNoTracking()
                .Where(s => s.SubscriberUserId == farmerUserId);

            var total = await q.CountAsync();

            var rows = await q.OrderByDescending(s => s.Id)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(s => new
                {
                    s.InputPackageId,
                    s.InputPackage.Name,
                    s.InputPackage.InputSeason,
                    s.InputPackage.PackageStructure,
                    s.Status,
                    s.ApprovedOn,
                    s.DisbursedOn,
                    s.TotalFinancingValue,      // the farmer's own financing value
                    Paid = s.Payments
                        // .Where(pay => pay.Status == TransactionStatus.Successful)  // use your real "paid" value
                        .Sum(pay => (decimal?)pay.AmountPaid) ?? 0m
                })
                .ToListAsync();

            var items = rows.Select(r =>
            {
                var financed = IsFinanced(r.Status) ? r.TotalFinancingValue : 0m;
                var disbursed = r.DisbursedOn != null ? r.TotalFinancingValue : 0m;
                var outstanding = IsOwing(r.Status) ? Math.Max(0m, r.TotalFinancingValue - r.Paid) : 0m;

                return new InputFinancePackageDto
                {
                    PackageId = r.InputPackageId,
                    SubscriptionType = EnumExtensions.GetDisplayName(r.PackageStructure),
                    PackageName = r.Name,
                    Season = r.InputSeason,
                    Status = r.Status.ToString(),
                    ApprovedDate = r.ApprovedOn,
                    TotalFinancingValue = financed,
                    TotalDisbursedValue = disbursed,
                    TotalOutstandingValue = outstanding
                };
            }).ToList();

            return (items, total);
        }

        // Financing that has actually gone out to the farmer
        private static readonly SubscriptionStatus[] FinancedStatuses =
        {
    SubscriptionStatus.Disbursed,
    SubscriptionStatus.PartiallyDisbursed,
    SubscriptionStatus.Active,
    SubscriptionStatus.PendingRepayment,
    SubscriptionStatus.Completed,
    SubscriptionStatus.Defaulted
};

        // Financed and still owing money
        private static readonly SubscriptionStatus[] OwingStatuses =
        {
   
    //SubscriptionStatus.Active,
    SubscriptionStatus.PendingRepayment,
    SubscriptionStatus.Defaulted
};

 //       private static bool IsFinanced(SubscriptionStatus s) =>
 //s >= SubscriptionStatus.Disbursed && s < SubscriptionStatus.PendingReleaseApproval;
        private static bool IsFinanced(SubscriptionStatus s) => FinancedStatuses.Contains(s);
        private static bool IsOwing(SubscriptionStatus s) => OwingStatuses.Contains(s);
    }
    }
