using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices.WalletServices;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Common
{
    public class UserOrganizationWallet
    {
        [Key]
        public long Id { get; set; }

        public string UserId { get; set; }

        public string OrganizationId { get; set; }

        public long UserWalletId { get; set; }
        public UserWallet UserWallet { get; set; }

        public bool IsDefaultForOrganization { get; set; }
    }
    [Index(nameof(UserId), nameof(WalletProviderId), IsUnique = true)]
    public class UserWallet
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string UserId { get; set; }

        public long WalletProviderId { get; set; }
        public WalletProvider WalletProvider { get; set; }

        public decimal WalletBalance { get; set; }

        [MaxLength(3)]
        public string Currency { get; set; } = "NGN";

       
        public bool IsActive { get; set; } = true;

        public DateTimeOffset AddedOn { get; set; } = DateTimeOffset.UtcNow;

        [Required]
        [MaxLength(50)]
        public string WalletNumber { get; set; } // account number, phone number etc.

        [Timestamp]
        public byte[] RowVersion { get; set; }

        public bool IsDeleted { get; set; } = false;

        [MaxLength(50)]
        public string? BelongToBusinessCode { get; set; }

        // Navigation
        public ICollection<UserWalletTransactions> Transactions { get; set; }
            = new HashSet<UserWalletTransactions>();

        public ICollection<UserWalletContext> WalletContexts { get; set; }
            = new HashSet<UserWalletContext>();
    }

    public class WalletSecurity
    {
        [Key]
        public long Id { get; set; }

        public long UserWalletId { get; set; }

        public UserWallet UserWallet { get; set; }

        [MaxLength(200)]
        public string TransactionPinHash { get; set; }
        public string Txn { get; set; } 

        public int PinFailedAttempts { get; set; } = 0;

        public DateTimeOffset? PinLockedUntil { get; set; }

        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? LastPinChangeDate { get; set; }
    }
    public enum WalletContextType
    {
        Personal = 1,
        Organization = 2,
        Cooperative = 3
    }
    [Index(nameof(UserId), nameof(ContextType), nameof(ContextReferenceId), IsUnique = true)]
    public class UserWalletContext
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string UserId { get; set; }

        public long UserWalletId { get; set; }
        public UserWallet UserWallet { get; set; }

        public WalletContextType ContextType { get; set; }

        [MaxLength(100)]
        public string? ContextReferenceId { get; set; }
        // null = Personal
        // OrganizationId = Organization
        // CooperativeId = Cooperative

        public bool IsDefault { get; set; } = true;

        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
    }
    [Index(nameof(TransactionReference), IsUnique = true)]
    [Index(nameof(TransactionReference), IsUnique = true)]
    public class UserWalletTransactions
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long UserWalletId { get; set; }
        public UserWallet UserWallet { get; set; }

        public WalletTransactionType WalletTransactionType { get; set; }

        public decimal TransactionAmount { get; set; }
        public decimal CommissionFee { get; set; }
        public decimal TransactionFee { get; set; }

        [MaxLength(3)]
        public string Currency { get; set; } = "NGN";

        [MaxLength(500)]
        public string? TransactionDescription { get; set; }

        [MaxLength(100)]
        public string? ChargedTo { get; set; }

        [MaxLength(100)]
        public string? ChargedToId { get; set; }

        public DateTimeOffset TransactionDate { get; set; } = DateTimeOffset.UtcNow;

        public WalletTransactionStatus WalletTransactionStatus { get; set; }

        [Required]
        [MaxLength(50)]
        public string TransactionReference { get; set; }

        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }

        [MaxLength(100)]
        public string? ProviderReference { get; set; }

        [MaxLength(50)]
        public string? ProviderStatus { get; set; }
    }
    public enum WalletTransactionStatus
    {
        Initiated = 0,

        DebitPending = 1,
        DebitSuccessful=2,
        DebitFailed=3,

        CreditPending=4,
        CreditSuccessful=5,
        CreditFailed=6,


        Completed = 7,

        ReconciliationRequired =8,
        ReversalPending=9,
        Reversed =10,

        Failed =11,

    }

    public enum WalletTransactionType
    {
        Credit = 1,
        Debit = 2
    }
}

public class ProviderWalletTransaction
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

    public long Id { get; set; }

    public string Reference { get; set; }

    public WalletProviderType Provider { get; set; }

    public decimal Amount { get; set; }

    public WalletTransactionStatus Status { get; set; }

    public string SubscriberWalletNumber { get; set; }

    public string BusinessWalletNumber { get; set; }

    public string RecipientWalletNumber { get; set; }

    public string DebitReference { get; set; }

    public string CreditReference { get; set; }

    public string ProviderTransactionId { get; set; }

    public string FailureReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}
public class ProviderWalletTransactionLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

    public long Id { get; set; }

    public long ProviderWalletTransactionId { get; set; }
    public ProviderWalletTransaction ProviderWalletTransaction { get; set; }

    public string Step { get; set; }

    public string RequestPayload { get; set; }

    public string ResponsePayload { get; set; }

    public bool IsSuccess { get; set; }

    public DateTime CreatedAt { get; set; }
}
