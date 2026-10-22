using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Models.Admin;
using Agrovesto.Backend.Core.Models.Businesses.Input;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Utilities;
using CsvHelper;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgentDto = Agrovesto.Backend.Core.Models.Admin.AgentDto;
using InputPackageBreakdownDto = Agrovesto.Backend.Core.Models.Admin.InputPackageBreakdownDto;
using RepaymentBreakdownDto = Agrovesto.Backend.Core.Models.Admin.RepaymentBreakdownDto;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2.AdminManager
{
    public interface IInputFinanceReader
    {
        Task<List<InputPackageBreakdownDto>> GetPackageContent(long packageId, long? subscriptionId = null);
        Task<InputFinancePackageOverviewDto> GetPackageOverviewAsync(string farmerUserId, long packageId);
        Task<Core.Models.Admin.PagedResult<RepaymentBreakdownDto>> GetPackageRepaymentBreakdownAsync(long packageId, long subscriptionId,int pageNumber = 1, int pageSize = 20);
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

            var response = new InputFinanceSummaryDto
            {
                TotalPackages = rows.Count,
                ActivePackages = active.Count,
    //            TotalFinancingValue = rows
    //.Where(r => (r.Status >= SubscriptionStatus.Disbursed && r.Status < SubscriptionStatus.PendingReleaseApproval))
    //.Sum(r => r.TotalFinancingValue),
                TotalDisbursed = rows.Where(r=>r.Status == SubscriptionStatus.Disbursed).Sum(r=> r.TotalFinancingValue),//rows.Count(r => r.Status == SubscriptionStatus.Disbursed),
                PendingDisbursement = rows.Count(r => r.Status == SubscriptionStatus.PendingDisbursement),
                // Only active packages carry an unpaid balance; floor each at 0 so an
                // overpayment on one package doesn't reduce the balance of another
                TotalFinancingValue = rows.Where(r => IsFinanced(r.Status)).Sum(r => r.TotalFinancingValue),
                OutstandingBalance = rows.Where(r => IsOwing(r.Status))
                          .Sum(r => Math.Max(0m, r.TotalFinancingValue - r.Paid)),
            };

            return response;
        }
        public async Task<(List<InputFinancePackageDto>, int)> GetPackagesAsync(
     string farmerUserId, int page, int pageSize)
        {
            var q = _context.InputPackageSubscriptions
                .Include(s=>s.InputPackage).AsNoTracking()
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
          public async Task<List<InputPackageBreakdownDto>> GetPackageContent(long packageId, long? subscriptionId = null)
        {
            var package = await _context.InputPackages
                .AsNoTracking()
                .Include(x => x.InputFinancingCategory)
                .Include(x => x.InputPackageItems)
                    .ThenInclude(x => x.OrganizationProduct)
                .Include(x => x.AdditionalCosts)
                .Include(x => x.CommitmentPlan)
                .Include(x => x.RepaymentPlan)
                .AsSplitQuery()
                .FirstOrDefaultAsync(x => x.Id == packageId);

            if (package == null)
                return null;

            // What this subscriber actually received, summed per product.
            // Summing covers the case where a partial disbursement is later topped up
            // and produces more than one line item for the same product.
            var disbursedByProduct = new Dictionary<long, decimal>();

            if (subscriptionId.HasValue)
            {
                disbursedByProduct = await _context.DisbursementLineItems
                    .AsNoTracking()
                    .Where(x => x.SubscriptionId == subscriptionId.Value)
                    .GroupBy(x => x.OrganizationProductId)
                    .Select(g => new
                    {
                        OrganizationProductId = g.Key,
                        Disbursed = g.Sum(x => x.DisbursedQuantity)
                    })
                    .ToDictionaryAsync(x => x.OrganizationProductId, x => x.Disbursed);
            }

            var contents = package.InputPackageItems.Select(i =>
            {
                disbursedByProduct.TryGetValue(i.OrganizationProductId, out var fulfilled);

                // never report more than was allocated
                fulfilled = Math.Min(fulfilled, i.Quantity);

                var status = fulfilled <= 0
                    ? InputItemFulfilmentStatus.NotDisbursed
                    : fulfilled >= i.Quantity
                        ? InputItemFulfilmentStatus.Fulfilled
                        : InputItemFulfilmentStatus.PartiallyDisbursed;

                return new InputPackageBreakdownDto
                {
                   // OrganizationProductId = i.OrganizationProductId,
                    ProductId = i.Id,
                    ProductName = i.OrganizationProduct?.ProductName,
                    Unit = i.OrganizationProduct?.UnitType,              // adjust to your unit-of-measure property
                    QuantityAllocated = i.Quantity,                           // quantity allocated
                    QuantityFulfilled = fulfilled,                   // actually disbursed
                    QuantityPending = i.Quantity - fulfilled,
                    UnitCost = i.UnitCost,
                    TotalCost = i.Quantity * i.UnitCost,
                    FulfilledValue = fulfilled * i.UnitCost,
                    Status = EnumExtensions.GetDisplayName(status)
                };
            }).ToList();

            return contents;
        }

        public async Task<Core.Models.Admin.PagedResult<RepaymentBreakdownDto>> GetPackageRepaymentBreakdownAsync(
            long packageId,
            long subscriptionId,
            int pageNumber = 1,
            int pageSize = 20)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;

            var query = _context.InputPackageSubscriptionPayments
                .AsNoTracking()
                .Where(x => x.PaymentType == PaymentType.Repayment
                         && x.Subscription.InputPackageId == packageId && x.SubscriptionId == subscriptionId);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.PaidOn)
                .ThenByDescending(x => x.Id)          // stable paging when PaidOn ties
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new RepaymentBreakdownDto
                {
                    Id = x.Id,
                    SubscriptionId = x.SubscriptionId,

                    // next installment still owed (skips deleted / superseded schedules)
                    DueDate = x.Subscription.RepaymentSchedules
                        .Where(rs => !rs.IsDeleted
                                  && rs.SupersededByScheduleId == null
                                  && !rs.IsPaid)
                        .OrderBy(rs => rs.InstallmentNumber)
                        .Select(rs => (DateTimeOffset?)rs.DueDate)
                        .FirstOrDefault(),

                    LoanAmount = x.Subscription.TotalFinancingValue,

                    BalanceRemaining = x.Subscription.TotalFinancingValue
                        - (x.Subscription.Payments
                            .Where(p => p.PaymentType == PaymentType.Repayment)
                            .Sum(p => (decimal?)p.AmountPaid) ?? 0m),

                    AmountPaid = x.AmountPaid,
                    Date = x.PaidOn
                })
                .ToListAsync();

            return new Core.Models.Admin.PagedResult<RepaymentBreakdownDto>(items, pageNumber, pageSize, totalCount);
        }


        public async Task<InputFinancePackageOverviewDto> GetPackageOverviewAsync(string farmerUserId, long packageId)
        {
            var item = await _context.InputPackageSubscriptions
                .Include(s => s.InputPackage).ThenInclude(p => p.Organization)
                .Include(s => s.SubscriberUser)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SubscriberUserId == farmerUserId && s.InputPackageId == packageId);

            if (item == null) return null; // or throw / return a not-found result

            var organizationId = item.InputPackage.OrganizationId; // or item.InputPackage.Organization.Id

            // farmer's integer id from the Farmers table
            var farmerId = await _context.Farmers
                .AsNoTracking()
                .Where(f => f.UserId == farmerUserId)
                .Select(f => f.Id)
                .FirstOrDefaultAsync();

            // membership bodies: farmer's active memberships that the organization also has
            var membershipBodies = await (
                from fc in _context.FarmerCooperatives
                join om in _context.OrganizationMemberships
                    on fc.CooperativeId equals om.MembershipId        // FarmerCooperative.CooperativeId == MemberShip.Id
                join m in _context.MemberShips
                    on fc.CooperativeId equals m.Id
                where fc.FarmerId == farmerId
                      && fc.IsActive
                      && om.OrganizationId == organizationId
                select new MembershipDto
                {
                    Id = m.Id,
                    Name = m.Name
                })
                .AsNoTracking()
                .Distinct()
                .ToListAsync();

            // agents: farmer's agents that are also assigned to the organization
            var agents = await (
                from fa in _context.FarmerAgents
                join oa in _context.OrganizationAgents
                    on fa.AgentId equals oa.AgentId
                join a in _context.Agents
                    on fa.AgentId equals a.Id
                where fa.FarmerId == farmerId
                      && oa.OrganizationId == organizationId
                select new AgentDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    PhoneNumber = a.PhoneNumber
                })
                .AsNoTracking()
                .Distinct()
                .ToListAsync();

            var result = new InputFinancePackageOverviewDto
            {
                PackageName = item.InputPackage.Name,
                PackageId = item.InputPackage.Id,
                SubscriptionStatus = EnumExtensions.GetDisplayName(item.Status),
                DateSubscribed = item.SubscribedOn,
                PackageType = EnumExtensions.GetDisplayName(item.InputPackage.PackageStructure),
                InputProgramme = item.InputPackage.InputTarget,
                FinancingPeriod = item.InputPackage.InputSeason, //this can be calculated from INputpackage repayment period,
                SubscriberName = $"{item.SubscriberUser.FirstName} {item.SubscriberUser.MiddleName} {item.SubscriberUser.LastName}",
                SubscriberId = item.SubscriberUserId,
                PhoneNumber = item.SubscriberUser.PhoneNumber,
                Organization = item.InputPackage.Organization.CompanyName,
                OrganizationCode = item.InputPackage.Organization.OrganizationCode,
                MembershipBodies = membershipBodies,
                Agents = agents,
                SubscriptionId = item.Id
            };

            return result;
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
