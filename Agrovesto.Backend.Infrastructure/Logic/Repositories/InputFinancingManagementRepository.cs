using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories
{
    public class InputFinancingManagementRepository : IInputFinancingManagementRepository
    {
        private readonly DataContext _context;
        public InputFinancingManagementRepository(DataContext context)
        {
            _context = context;
        }
        //indicate interest

        public async Task<(bool status, BundleSubscriptionInterestModel data)> CreateInputFinancingInterestThroughBundle(BundleSubscriptionInterestModel data)
        {
            var entity = data.Map();
            await _context.AddAsync(entity);
            await _context.SaveChangesAsync();

            return (true, entity.Map());
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
