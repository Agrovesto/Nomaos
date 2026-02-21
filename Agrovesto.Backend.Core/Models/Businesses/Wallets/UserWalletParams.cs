using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses.Wallets
{
    public class UserWalletParams
    {
        public string businessCode { get; set; }
        public string UserId { get; set; }
        public string Env { get; set; }
    }
  public class UserWalletModel
    {
        public long Id { get; set; }

        // User
        public string UserId { get; set; }

        // Wallet Provider
        public long WalletProviderId { get; set; }

        // Account details
        public string WalletNumber { get; set; }

        public decimal WalletBalance { get; set; } = 0;

        public string Currency { get; set; } = "NGN";

        public bool IsActive { get; set; } = true;

        // Business (if wallet belongs to a business context)
        public string? BelongToBusinessCode { get; set; }

        // ===== CONTEXT SETTINGS =====

        /// <summary>
        /// Personal / Organization / Cooperative
        /// </summary>
        public string ContextType { get; set; } = "Personal";//WalletContextType.Personal;

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

        public string ProviderName { get; set; }
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

        public List<UserWalletContextModel> Contexts { get; set; } = new();
    }
    public class UserWalletContextModel
    {
        public string ContextType { get; set; }
        public string? ContextReferenceId { get; set; }
        public bool IsDefault { get; set; }
    }
}
