using Agrovesto.Backend.Core.CoreUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses.Wallets
{
    public class ProviderWebhookRequest
    {       
        public string TransactionReference { get; set; }
        public string? ProviderReference { get; set; }
        public string Status { get; set; }
        public decimal? Amount { get; set; }
        public string? Currency { get; set; }
        public string Provider { get; set; }
        public string? RawPayload { get; set; }
        public string BusinessCode { get; set; }
        public string? Signature { get; set; }
        public DateTimeOffset? EventTime { get; set; }
    }
    public class ValidateWalletPinRequest
    {
        public long WalletId { get; set; }

        public string Pin { get; set; }
    }
    public class ValidateWalletPinResult
    {
        public bool IsValid { get; set; }
        public string? Token { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
    }
    public class VerifyPinResetOtpRequest
    {
        [Required]
        public string Code { get; set; }
    }
    public class WalletPinResetOtpResult
    {
        /// <summary>Partially masked, so the caller can show where the code went without leaking the address.</summary>
        public string Destination { get; set; }

        public DateTimeOffset ExpiresAt { get; set; }
    }
    public class UpdateWalletPinRequest
    {
        public long WalletId { get; set; }

        public string NewPin { get; set; }

        public string ConfirmNewPin { get; set; }

        public string Token { get; set; }
    }
    public class WithdrawWalletRequest
    {
        public long UserWalletId { get; set; }

        public decimal Amount { get; set; }

        public string WalletProviderCode { get; set; }

        public string? Description { get; set; }

        public string? RecipientAccount { get; set; }

        public string? RecipientName { get; set; }

        public string? TransactionPin { get; set; }
    }
    public class ProviderResponse
    {
        public bool IsSuccess { get; set; }

        public string Message { get; set; }

        public string? ProviderReference { get; set; }

        public object? Data { get; set; } // e.g. payment link, account number, ussd code  etc.
    }
    public class TopUpWalletRequest
    {
        public long UserWalletId { get; set; }

        public decimal Amount { get; set; }

        public string WalletProviderCode { get; set; }

        public string? Description { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }
    public class WalletTopUpProviderRequest
    {
        public string CustomerMac { get; set; }
        public decimal Amount { get; set; }

        public string Currency { get; set; }
        public string WalletName { get; set; }

        public string WalletNumber { get; set; }

        public string TransactionReference { get; set; }

        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Message { get; set; } = null;
    }
    public class UserWalletParams
    {
        public string businessCode { get; set; }
        public string? organizationCode { get; set; }
        public string? UserId { get; set; }
        public string Env { get; set; }
        public long Id { get; set; }
        public int pageNumber { get; set; } = 1;
        public int pageSize { get; set; } = 10;
    }
  public class UserWalletModel
    {
        public long WalletId { get; set; }

        // User
        public string? UserId { get; set; }
        public string? MaxCheckSum { get; set; }

        // Wallet Provider
        public long WalletProviderId { get; set; }

        // Account details
        public string? WalletNumber { get; set; }

        public decimal WalletBalance { get; set; } = 0;

        public string? Currency { get; set; } = "NGN";

        public bool IsActive { get; set; } = true;

        // Business (if wallet belongs to a business context)
        public string? BelongToBusinessCode { get; set; }
        public string? TransactionPin { get; set; }

       

        /// <summary>
        /// Personal / Organization / Cooperative
        /// </summary>
        public string? ContextType { get; set; } = "Personal";//WalletContextType.Personal;

        /// <summary>
        /// null for Personal  
        /// OrganizationId for Organization  
        /// CooperativeId for Cooperative
        /// </summary>
        public string? ContextReferenceId { get; set; }

        /// <summary>
        /// Should this wallet be the default for the given context?
        /// </summary>
        public bool SetAsDefaultForContext { get; set; } = true;

        public string? ProviderName { get; set; }
    }
    public class UserWalletDetailResponseModel
    {
        public long Id { get; set; }
        public string UserId { get; set; }  
        public string ProviderName { get; set; }
        public decimal WalletLedgerBalance { get; set; }
        public bool IsActive { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal InputObligations { get; set; }
        public string WalletNumber { get; set; }
        public decimal WalletBalance { get; set; } = 0;

        public string Currency { get; set; }
        public string? BelongToBusinessCode { get; set; }
        public long WalletProviderId { get; set; }
        public List<UserWalletContextModel> Contexts { get; set; } = new();
    }
    public class UserWalletResponseModel
    {
        public long Id { get; set; }
        public string UserId { get; set; }
        public long WalletProviderId { get; set; }
        public string ProviderName { get; set; }
        public string WalletNumber { get; set; }
        public decimal WalletBalance { get; set; }
        public string Currency { get; set; }
        public bool IsActive { get; set; }
        public string? BelongToBusinessCode { get; set; }
        public decimal ReservedForInputObligations { get; set; }
        public DateTimeOffset DateAdded { get; set; }

        public List<UserWalletContextModel> Contexts { get; set; } = new();
    }
    public class UserWalletContextModel
    {
        public string ContextType { get; set; }
        public string? ContextReferenceId { get; set; }
        public bool IsDefault { get; set; }
    }

    public class CreateWalletResult
    {
        public long WalletId { get; set; }
        public string UserId { get; set; }
        public string? AccountName { get; set; }
        public string? AccountNumber { get; set; }
        public long WalletProviderId { get; set; }
        public string? ProviderName { get; set; }
        public string? ProviderCode { get; set; }
        public decimal WalletBalance { get; set; }
        public string? Currency { get; set; }
        public string? ContextType { get; set; }
        public string? ContextReferenceId { get; set; }
        public bool IsDefault { get; set; }
    }

}
