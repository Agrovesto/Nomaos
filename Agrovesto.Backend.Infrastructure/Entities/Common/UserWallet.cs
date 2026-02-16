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
    [Index(nameof(UserId), nameof(WalletProviderId), IsUnique = true)]
    public class UserWallet
    {
     
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string UserId { get; set; }
        public long WalletProviderId { get; set; }
        public WalletProvider WalletProvider { get; set; }
        public decimal WalletBalance { get; set; }
        public string Currency { get; set; } = "NGN";
        public bool IsActive { get; set; } = true;
public bool IsDefault { get; set; } = false;
        public DateTimeOffset AddedOn { get; set; } = DateTimeOffset.Now;
        [Required]
        [MaxLength(50)]
        public string WalletNumber { get; set; } //account number, phonenumber etc

        [Timestamp]
        public byte[] RowVersion { get; set; }
        public ICollection<UserWalletTransactions> UserWalletTransactions { get; set; } = new HashSet<UserWalletTransactions>();
        public bool IsDeleted { get; set; }

    }
    public class UserWalletTransactions
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public long UserWalletId { get; set; }
        public UserWallet UserWallet { get; set; }
        public WalletTransactionType WalletTransactionType { get; set; } //debit or credit, for debit, remove from wallet balance, for credit, add to wallet balance
        public decimal TransactionAmount { get; set; }
        public decimal CommissionFee { get; set; }
        public decimal TransactionFee { get; set; }
        public string Currency { get; set; } = "NGN";

        public string TransactionDescription    { get; set; }
        public string ChargedTo { get; set; } //i.e, where the money is going to, e.g, organization , another seller. etc
        public string ChargedToId { get; set; } //e.g, organizationId, service providerId, etc
        public DateTimeOffset TransactionDate { get; set; } = DateTimeOffset.UtcNow;
        public WalletTransactionStatus WalletTransactionStatus {  get; set; }
        [Required]
        [MaxLength(50)]
        public string TransactionReference { get; set; } // unique transaction reference
        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }
        public string? ProviderReference { get; set; }
        public string ProviderStatus { get; set; }
    }
    public enum WalletTransactionStatus
    {
        Pending = 1,
        Successful = 2,
        Failed = 3,
        Reversed = 4
    }
    public enum WalletTransactionType
    {
        Credit = 1,
        Debit = 2
    }

}
