using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Common
{
    public class UserWalletBalanceModel
    {
        public string WalletId { get; set; }
        public string CustomerId { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public long AvailableBalance { get; set; }
        public long LedgerBalance { get; set; }
    }

    public class UserAccountDetailsModel
    {
        public string WalletId { get; set; }
        public string CustomerId { get; set; }
        public string AccountNumber { get; set; }
        public string BankCode { get; set; }
        public string BankName { get; set; }
        public string AccountName { get; set; }
    }
    public class SterlingBankWalletModel
    {
        public long Id { get; set; }
        public string UserId { get; set; }
        public string AgrovestoUserId { get; set; }
        public string CustomerId { get; set; }
        public string WalletId { get; set; }
        public string AccountNumber { get; set; }
        public string BankCode { get; set; }
        public string BankName { get; set; }
        public string walletClassificationId { get; set; }
        public string currencyId { get; set; }
        public string AccountName { get; set; }
        public string customerTypeId { get; set; }

        public string LedgerAccountNumber { get; set; }
        public string InternalAccountId { get; set; }
        
        public string OrganizationId { get; set; } //if user is agrovesto, then organizationId is gotten from walletprovider parameter, else it is the ownerId of the business

        public string OwnerId { get; set; }
        public bool IsABusinessAccount { get; set; } //true for businesses, false for ordinary users
        public DateTimeOffset CreatedOn { get; set; }

    }
}
