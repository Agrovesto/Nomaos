using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Enums
{
    public enum KYCVerificationType
    {
        NIN,
        BVN
    }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Channel
    {
        SMS = 1,
        Email = 2
    }
    public enum RateLimitType
    {
        OTP,
        LOGIN,
        RESET_PASSWORD,
    }
    public enum InventoryTransactionType
    {
        [Display(Name = "Opening Balance")]
        OpeningBalance,

        [Display(Name = "Stock In")]
        StockIn,

        [Display(Name = "Stock Out")]
        StockOut,

        [Display(Name = "Transfer In")]
        TransferIn,

        [Display(Name = "Transfer Out")]
        TransferOut,

        [Display(Name = "Adjustment Increase")]
        AdjustmentIncrease,

        [Display(Name = "Adjustment Decrease")]
        AdjustmentDecrease,

        [Display(Name = "Damaged Stock")]
        Damaged,

        [Display(Name = "Expired Stock")]
        Expired,

        [Display(Name = "Returned Stock")]
        Returned,

        [Display(Name = "Sold Stock")]
        Sold,

        [Display(Name = "Consumed Stock")]
        Consumed,

        [Display(Name = "Inventory Edit")]
        Edit,

        [Display(Name = "Inventory Deleted")]
        Delete,

        [Display(Name = "Input Disbursement")]
        Disbursement,


        [Display(Name = "Additional Cost Disbursement")]
        AdditionalCostDisbursement,

        [Display(Name = "Repayment Receipt")]
        RepaymentReceipt
    }
}
