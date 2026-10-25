using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Enums
{
    public enum AdminUserStatus { Active = 1, Suspended = 2, Deactivated = 3, Pending = 4 }
    public enum FarmerStatus {
        [Display(Name = "Active")]
        Active = 1,
        [Display(Name = "Suspended")]
        Suspended = 2,
        [Display(Name = "Deactivated")]
        Deactivated = 3,
        [Display(Name = "Pending")]
        Pending = 4 }
    public enum AgentStatus {

        [Display(Name = "Active")]
        Active = 1,

        [Display(Name = "Suspended")]
        Suspended = 2,

        [Display(Name = "Deactivated")]
        Deactivated = 3,

        [Display(Name = "Pending")]
        Pending = 4 }
    public enum OrganizationStatus {
        [Display(Name = "Active")]
        Active = 1,
        [Display(Name = "Suspended")]
        Suspended = 2,
        [Display(Name = "Deactivated")]
        Deactivated = 3,
        [Display(Name = "Pending")]
        Pending = 4 }

    public enum MembershipStatus {
        [Display(Name = "Active")]
        Active = 1,
        [Display(Name = "Suspended")]
        Suspended = 2,
        [Display(Name = "Deactivated")]
        Deactivated = 3,
        [Display(Name = "Pending")]
        Pending = 4 }
    public enum MappingStatus
    {
        UnMapped, //no coordinates added
        Mapped, //coordinates added but not verified
        Verified // verified
    }
    public enum MembershipParticipantType
    {
        Farmer=1,
        Agent=2
    }

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
        FORGOT_CREDENTIAL,      // per account (email/phone)
        FORGOT_CREDENTIAL_IP,
        RESEND_CREDENTIAL_COOLDOWN, // e.g. 1 request / 60 seconds per account
        RESEND_CREDENTIAL,          // e.g. 3 requests / 10 minutes per account
        RESEND_CREDENTIAL_IP
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
