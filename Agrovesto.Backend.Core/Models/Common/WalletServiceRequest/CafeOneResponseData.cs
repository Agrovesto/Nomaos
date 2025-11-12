using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Common.WalletServiceRequest
{
    public class FundTransferResponse
    {
        public string data { get; set; }
    }
    public class FundWithdrawalRequestModel
    {
        public string destinationBankCode { get; set; }
        public string destinationAccountNumber { get; set; }
        public string destinationAccountName { get; set; }
        public string sourceAccountNumber { get; set; }
        public string sourceAccountName { get; set; }
        public string remarks { get; set; }//get from db, default to NGN
        public decimal Amount { get; set; }
        public string currencyId { get; set; }
        public string reference { get; set; }
    }
    public class PostWalletToWalletTransferRequestModel
    {
        public string destinationBankCode { get; set; }
        public string reference { get; set; }
        public string destinationAccountNumber { get; set; }
        public string destinationAccountName { get; set; }
        public string sourceAccountNumber { get; set; }
        public string sourceAccountName { get; set; }
        public string remarks { get; set; }//get from db, default to NGN
        public decimal Amount { get; set; }
    }
    public class WalletToWalletTransferRequestModel
    {
        public string CustomerId { get; set; }
        public string WalletId { get; set; }
        public string OrganizationId { get; set; }
        public string AgrovestoUserId { get; set; }
        public string Remarks { get; set; }
        public string BatchReference { get; set; }
        public string TransactionReference { get; set; }
        public string ProductId { get; set; }  //get from db, either debit or credit id
        public string Direction { get; set; } //get from db, either D or C
        public string Currency { get; set; } //get from db, default to NGN
        public decimal Amount { get; set; }
        public string AccountNumber { get; set; }
    }
    public class CafeOneCustomerWalletResponse
    {
        public string Id {  get; set; }
        public string walletGroupId {  get; set; }
        public string customerId {  get; set; }
        public long availableBalance {  get; set; }
        public long ledgerBalance {  get; set; }
        public string walletRestrictionId {  get; set; }
        public string walletClassificationId {  get; set; }
        public string currencyId {  get; set; }
        public bool isInternal {  get; set; }
        public bool isDefault {  get; set; }
        public string name { get; set; }
        public string overdraft { get; set; }
        public CafeOneVirtualAccount virtualAccount { get; set; }
        public string mobNum { get; set; }
        public string customerTypeId { get; set; }
    }
    public class CafeOneVirtualAccount
    {
        public string accountNumber { get; set; }
        public string bankCode { get; set; }
        public string bankName { get; set; }
    }
    public class CafeoneResponseModel<T>
    {
        public T data { get; set; }
        public string code { get; set; }
        public bool success { get; set; }
        public string message { get; set; }
    }
    public class PayoutCafeoneResponseModel<T>
    {
        public T data { get; set; }
        public int statusCode { get; set; }
        public bool succeeded { get; set; }
        public string message { get; set; }
    }

    public class CafeoneDataModel
    {
        public string Id { get; set; }
        public string OrganizationId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public string Dob { get; set; }
        public string CustomerTypeId { get; set; }
        public string Alias { get; set; }
        public string CountryId { get; set; }
        public string City { get; set; }
        public string Address { get; set; }
        public string MobileNumber { get; set; }
        public string EmailAddress { get; set; }
        public string DateCreated { get; set; }
        public long CreatedAt { get; set; }
        public long UpdatedAt { get; set; }
        public string Tier { get; set; }
        public bool Blacklisted { get; set; }
        public string UserId { get; set; }



        public string BvnNumber { get; set; }
        public string Nin { get; set; }
        public string Photo { get; set; }
        public string TransactionLimit { get; set; }
        public CafeonePhoneModel Phone { get; set; }
        public string DeletedAt { get; set; }
        public string VerifiedAt { get; set; }
        public string UtilityBill { get; set; }
        public string PlaceOfBirth { get; set; }
        public string Country { get; set; }
        public string Address1 { get; set; }
        public string Gender { get; set; }
        public string CustomerId { get; set; }
    }

}
