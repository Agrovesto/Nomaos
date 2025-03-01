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
        public async Task<SubscriptionMetrics> FarmerBundleSubscriptions(string businessCode)
        {
            var query = await _context.BundleSubscriptionInterests
                .Where(w => w.BelongToBusinessCode == businessCode)
        .GroupBy(b => 1) // Group everything together to get overall counts
        .Select(g => new SubscriptionMetrics
        {
            TotalActiveSubscribers = g.Count(b => b.PaymentStatus == Enum.GetName(typeof(EquityPaymentStatus), EquityPaymentStatus.Paid)),
            PendingSubscribers = g.Count(b => b.PaymentStatus == Enum.GetName(typeof(EquityPaymentStatus), EquityPaymentStatus.Pending) || 
            b.PaymentStatus == Enum.GetName(typeof(EquityPaymentStatus), EquityPaymentStatus.Initial)),
            PartialPaymentSubscribers = g.Count(b => b.PaymentStatus == Enum.GetName(typeof(EquityPaymentStatus), EquityPaymentStatus.Partial_Payment))
        })
        .FirstOrDefaultAsync(); // We expect a single object as output
            if(query == null) { return null; }
            return query;
        }
        public async Task<List<BundleSubscriptionSummary>> FarmerBundleSubscriptionInterests(string businessCode)
        {
            var query = await _context.BundleSubscriptionInterests
                .Where(w=>w.BelongToBusinessCode == businessCode)
                .GroupBy(b => b.InterestedBundleName) // Assuming BundleName represents the bundle type
                .Select(g => new BundleSubscriptionSummary
                {
                    BundleName = g.Key,
                    TotalSubscribers = g.Count() // Count number of farmers subscribed to each bundle
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

        public async Task<List<BundleSubscriptionInterestModel>> GetSubscriptionInterestByBusiness(string businessCode)
        {
            var interest = await _context.BundleSubscriptionInterests.Where(x => x.BelongToBusinessCode == businessCode).ToListAsync();

            if (!interest.Any()) { return null; }

            return interest.Select(x => x.Map()).ToList();
        }

        public async Task<BundleSubscriptionInterestModel> UpdateSubscriptionPayment(BundleSubscriptionInterestModel model)
        {
            var interest = await _context.BundleSubscriptionInterests.Where(x => x.FarmerUserId == model.FarmerUserId && x.InterestedBundleId == model.InterestedBundleId).FirstOrDefaultAsync();

            if (interest != null)
            {
                interest.PaymentStatus = Enum.GetName(typeof(EquityPaymentStatus), model.PaymentStatus);
                interest.PaymentDate = model.PaymentDate;
                interest.IsCommissionPaidOut = model.IsCommissionPaidOut;
                interest.TotalQuantityPaidFor = model.TotalQuantityPaidFor;
                interest.TotalEquityPaid = model.TotalEquityPaid;
                interest.Reference = model.Reference;


                await _context.SaveChangesAsync();
            }
            return null;

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

    }
}
