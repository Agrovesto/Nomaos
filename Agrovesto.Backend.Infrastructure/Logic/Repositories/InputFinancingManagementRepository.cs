using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Interfaces.Repositories;
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
using static Amazon.S3.Util.S3EventNotification;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories
{
    public class InputFinancingManagementRepository : IInputFinancingManagementRepository
    {
        private readonly DataContext _context;
        public InputFinancingManagementRepository(DataContext context)
        {
            _context = context;
        }
        public async Task<List<SubscriptionData>> FarmerBundleSubscriptions(string businessCode)
        {
            var query = await _context.BundleSubscriptionInterests
                .Where(w => w.BelongToBusinessCode == businessCode)
                .GroupBy(b => 1) // Group everything together to get overall counts
                .Select(g => new
                {
                    ActiveSubscribers = g.Count(b => b.PaymentStatus == Enum.GetName(typeof(EquityPaymentStatus), EquityPaymentStatus.Paid)),
                    PendingSubscribers = g.Count(b =>
                        b.PaymentStatus == Enum.GetName(typeof(EquityPaymentStatus), EquityPaymentStatus.Pending) ||
                        b.PaymentStatus == Enum.GetName(typeof(EquityPaymentStatus), EquityPaymentStatus.Initial)),
                    OverdueSubscribers = g.Count(b => b.PaymentStatus == Enum.GetName(typeof(EquityPaymentStatus), EquityPaymentStatus.Partial_Payment))
                })
                .FirstOrDefaultAsync(); // We expect a single object as output

            if (query == null)
            {
                return new List<SubscriptionData>();
            }

            return new List<SubscriptionData>
    {
        new SubscriptionData { Name = "Active Subscribers", Value = query.ActiveSubscribers },
        new SubscriptionData { Name = "Pending Subscribers", Value = query.PendingSubscribers },
        new SubscriptionData { Name = "Overdue Subscribers", Value = query.OverdueSubscribers }
    };
        }

       

        //public async Task<SubscriptionMetrics> FarmerBundleSubscriptions(string businessCode)
        //{
        //    var query = await _context.BundleSubscriptionInterests
        //        .Where(w => w.BelongToBusinessCode == businessCode)
        //.GroupBy(b => 1) // Group everything together to get overall counts
        //.Select(g => new SubscriptionMetrics
        //{
        //    TotalActiveSubscribers = g.Count(b => b.PaymentStatus == Enum.GetName(typeof(EquityPaymentStatus), EquityPaymentStatus.Paid)),
        //    PendingSubscribers = g.Count(b => b.PaymentStatus == Enum.GetName(typeof(EquityPaymentStatus), EquityPaymentStatus.Pending) ||
        //    b.PaymentStatus == Enum.GetName(typeof(EquityPaymentStatus), EquityPaymentStatus.Initial)),
        //    PartialPaymentSubscribers = g.Count(b => b.PaymentStatus == Enum.GetName(typeof(EquityPaymentStatus), EquityPaymentStatus.Partial_Payment))
        //})
        //.FirstOrDefaultAsync(); // We expect a single object as output
        //    if (query == null) { return null; }
        //    return query;
        //}
        public async Task<List<BundleSubscriptionSummary>> FarmerBundleSubscriptionInterests(string businessCode)
        {
            var query = await _context.BundleSubscriptionInterests
                .Where(w => w.BelongToBusinessCode == businessCode)
                .GroupBy(b => b.InterestedBundleName) // Assuming BundleName represents the bundle type
                .Select(g => new BundleSubscriptionSummary
                {
                    Name = g.Key,
                    Value = g.Count() // Count number of farmers subscribed to each bundle
                })
                .ToListAsync();
            if (query.Count == 0) { return null; }
            return query;
        }
        public async Task<(bool status, BundleSubscriptionInterestModel data)> CreateInputFinancingInterestThroughBundle(BundleSubscriptionInterestModel data)
        {
            var entity = data.Map();
            await _context.AddAsync(entity);
            await _context.SaveChangesAsync();

            return (true, entity.Map());
        }

        public async Task<List<BundleSubscriptionInterestModel>> GetUnpaidSubscriptionInterestByFarmer(BundleSubscriptionInterestModel data)
        {
            var interest = await _context.BundleSubscriptionInterests.Where(x => x.FarmerUserId == data.FarmerUserId && x.PaymentStatus != EquityPaymentStatus.Paid.ToString()).ToListAsync();

            if (!interest.Any()) { return null; }

            return interest.Select(x => x.Map()).ToList();
        }
        public async Task<List<BundleSubscriptionInterestModel>> GetSubscriptionInterestByLocation(string businessCode, string Location)
        {
            var interest = await _context.BundleSubscriptionInterests.Where(x => x.BelongToBusinessCode == businessCode && x.Location == Location).ToListAsync();

            if (!interest.Any()) { return null; }

            return interest.Select(x => x.Map()).ToList();
        }
        public async Task<List<BundleSubscriptionInterestModel>> GetSubscriptionInterestByCooperative(string businessCode, long CooperativeId)
        {
            var interest = await _context.BundleSubscriptionInterests.Where(x => x.BelongToBusinessCode == businessCode && x.CooperativeId == CooperativeId).ToListAsync();

            if (!interest.Any()) { return null; }

            return interest.Select(x => x.Map()).ToList();
        }
          public async Task<PagedListResponse<List<BundleSubscriptionInterestModel>>> GetInterestRequestByAgent(string businessCode, string userId, int pageNumber = 1,
      int pageSize = 10)
        {
            var interest = await _context.BundleSubscriptionInterests.Where(x => x.BelongToBusinessCode == businessCode 
            && x.OfficerInChargeUserId == userId).ToListAsync();

            if (interest.Count() == 0) { return null; }
            var totalCount =  interest.Count();

            var data= interest.OrderByDescending(x => x.CreatedOn)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => x.Map()).ToList();
            return new PagedListResponse<List<BundleSubscriptionInterestModel>>
            {
                Data = data,//pagedResult.Select(x => x.Map()).ToList(),
                Count = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<BundleSubscriptionInterestModel> GetPaidSubscriptionByFarmer(string businessCode, string farmerUserId, string reference)
        {
            var interest = await _context.BundleSubscriptionInterests.Where(x => x.BelongToBusinessCode == businessCode && x.FarmerUserId == farmerUserId && x.Reference== reference).FirstOrDefaultAsync();

            if (interest == null) { return null; }

            return interest.Map();
        }
        public async Task<PagedListResponse<List<BundleSubscriptionInterestModel>>> GetSubscriptionInterestByBusiness(string businessCode, int pageNumber = 1,
      int pageSize = 10, 
         DateTimeOffset? startDate = null,
         DateTimeOffset? endDate = null,
         string? location = null)
        {
            var query =  _context.BundleSubscriptionInterests.Where(x => x.BelongToBusinessCode == businessCode
            ).AsQueryable();

         

            if (startDate.HasValue)
            {
                query = query.Where(x => x.CreatedOn >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(x => x.CreatedOn <= endDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(x => x.Location == location);
            }
            if (query.Count() == 0) { return null; }
            var totalCount = await query.CountAsync();
            var data = await query.OrderByDescending(x => x.CreatedOn)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => x.Map()).ToListAsync();
            return new PagedListResponse<List<BundleSubscriptionInterestModel>>
            {
                Data = data,//pagedResult.Select(x => x.Map()).ToList(),
                Count = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            }
        public async Task<BundleSubscriptionInterestModel> UpdateSubscriptionPayment(BundleSubscriptionInterestModel model)
        {
            var interest = await _context.BundleSubscriptionInterests.Where(x => x.FarmerUserId == model.FarmerUserId && x.InterestedBundleId == model.InterestedBundleId).FirstOrDefaultAsync();

            if (interest != null)
            {
                interest.PaymentStatus = Enum.GetName(typeof(EquityPaymentStatus), model.Status);
                interest.PaymentDate = model.PaymentDate;
                interest.IsCommissionPaidOut = model.IsCommissionPaidOut;
                interest.TotalQuantityPaidFor = model.TotalQuantityPaidFor;
                interest.TotalEquityPaid = model.TotalEquityPaid;
                interest.Reference = model.Reference;


                await _context.SaveChangesAsync();
            }
            return null;

        }

        public async Task<BundleDisbursementModel> UpdateDisbursement(BundleDisbursementModel model)
        {
            var disbursement = model.Map();
            await _context.AddAsync(disbursement);
            await _context.SaveChangesAsync();

            return  disbursement.Map();
        }
        public async Task<List<BundleDisbursementModel>> GetDisbursedSubscriptions(string businessCode)
        {
            var interest = await _context.BundleSubscriptionDisbursements.Where(x => x.BelongToBusinessCode == businessCode).ToListAsync();

            if (interest.Count == 0) { return null; }

            return interest.Select(x=>x.Map()).ToList();
        }
        public async Task<BundleDisbursementModel> GetDisbursedSubscription(string businessCode, string farmerUserId, string reference)
        {
            var interest = await _context.BundleSubscriptionDisbursements.Where(x => x.BelongToBusinessCode == businessCode && x.FarmerUserId == farmerUserId && x.Reference == reference).FirstOrDefaultAsync();

            if (interest == null) { return null; }

            return interest.Map();
        }

        public async Task<List<BundleSubscriptionInterestModel>> GetPaidSubscriptionInterestByBusiness(string businessCode)
        {
            var interest = await _context.BundleSubscriptionInterests.Where(x => x.BelongToBusinessCode == businessCode && x.PaymentStatus == "Paid").ToListAsync();

            if (!interest.Any()) { return null; }

            return interest.Select(x => x.Map()).ToList();
        }
        public async Task<List<BundleSubscriptionInterestModel>> GetSubscriptionInterestByBusinessAgent(string businessCode, string userId)
        {
            var interest = await _context.BundleSubscriptionInterests.Where(x => x.BelongToBusinessCode == businessCode && x.OfficerInChargeUserId == userId && x.PaymentStatus != "Paid")
                .OrderByDescending(x => x.CreatedOn).ToListAsync();

            if (!interest.Any()) { return null; }

            return interest.Select(x => x.Map()).ToList();
        }

        public async Task<List<BundleSubscriptionInterestModel>> GetPaidSubscriptionInterestByBusinessAgent(string businessCode, string userId)
        {
            var interest = await _context.BundleSubscriptionInterests.Where(x => x.BelongToBusinessCode == businessCode && x.OfficerInChargeUserId == userId && x.PaymentStatus == "Paid")
                .OrderByDescending(x=> x.CreatedOn).ToListAsync();

            if (!interest.Any()) { return null; }

            return interest.Select(x => x.Map()).ToList();
        }
        //update input interest payment
        //fetch account details with balance from cafeone

        //public async Task<bool> UpdateInterestPayment(BundleSubscriptionInterestModel data, CafeOnWalletResponse<WalletTransferResult> deduction)
        //{
        //    var interest = await _context.Set<BundleSubscriptionInterest>().FindAsync(data.Id);
        //    if (interest == null) return false;

        //    interest.TotalEquityPaid = data.TotalEquityPaid;
        //    interest.TotalQuantityPaidFor = ((long)data.TotalEquityPaid / interest.QuantityOfInterest);
        //    interest.EquityPaymentBalanceDeficit = interest.ExpectedTotalEquityPrice - interest.TotalEquityPaid;
        //    if (interest.EquityPaymentBalanceDeficit == 0 && deduction.Data.success) 
        //      {
        //        interest.PaymentStatus = EquityPaymentStatus.Paid.ToString();

        //    }
        //    else if(interest.EquityPaymentBalanceDeficit > 0 )
        //    {
        //        interest.PaymentStatus = EquityPaymentStatus.Partial_Payment.ToString();
        //    }
        //    else
        //    {
        //        interest.PaymentStatus = EquityPaymentStatus.Pending.ToString();
        //    }
        //    interest.CommissionPrice = data.CommissionPrice;
        //    interest.


        //    _context.Set<WarehouseInputInventory>().Update(interest);
        //    await _context.SaveChangesAsync();
        //    return true;
        //}
        public async Task<BundleSubscriptionInterestModel> UpdateEquitySubcriiptonStatus(BundleSubscriptionInterestModel model)
        {
            var interest = await _context.BundleSubscriptionInterests.Where(x => x.FarmerUserId == model.FarmerUserId && x.InterestedBundleId == model.InterestedBundleId).FirstOrDefaultAsync();

            if (interest != null)
            {
                interest.PaymentStatus = Enum.GetName(typeof(EquityPaymentStatus), model.Status);               

                await _context.SaveChangesAsync();
            }
            return null;

        }

    }
}
