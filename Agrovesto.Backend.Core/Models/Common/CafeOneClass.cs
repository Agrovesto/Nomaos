using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Common
{

    public class CafeOneBankData
    {
        public int id { get; set; }
        public string cbn { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public bool is_active { get; set; }
        public bool supports_readycash_ussd_funding { get; set; }
    }
    public class CafeOneBankDataResponse
    {
        public List<CafeOneBankData> data { get; set; }
    }

    public class AccountResolutionResponse<T>
    {
        public T data { get; set; }
    }
    public class AccountResolutionResolutionResponseData
    {
        public string account_name { get; set; }
    }
    public class AccountResolutionDetail
    {
        public string account_name { get; set; }
    }
    public class CafeOneAuthResponse
    {
        public string AccessToken { get; set; }
        public string Email { get; set; }
        public bool TwoFaEnabled { get; set; }
        public string UserId { get; set; }
        public string Type { get; set; }
    }
    public class CafeOneParams
    {
        public string Url { get; set; }
        public string SecretKey { get; set; }
        public string PublicKey { get; set; }
        public string OrganizationId { get; set; }
        public string OwnerId { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string accountNumber { get; set; }
    }
    public class ExternalBankTransferModel

    {
        public decimal amount { get; set; }
        public string fromWalletAccountNumber { get; set; }
        public string toExternalAccountNumber { get; set; }
        public string accountName { get; set; }
        public string bankName { get; set; }
        public string bankCode { get; set; }
        public string memo { get; set; }
        public string ownerId { get; set; }
        public string organizationId { get; set; }
        public string yourTnxRef { get; set; }
    }
    public class CafeOnWalletResponse<T>
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public int StatusCode { get; set; }
        public T Data { get; set; }
    }

    public class CafeOneExternalTransferResponse
    {
        public string id { get; set; }
        public decimal amount { get; set; }
        public string customerBillerId { get; set; }
        public string userId { get; set; }
        public string transactionReference { get; set; }
        public string transactionStatus { get; set; }
        public string transactionType { get; set; }
        public string user { get; set; }
        public string product { get; set; }
        public DateTimeOffset timeCreated { get; set; }
        public string internalTransactionId { get; set; }
        public mydata data { get; set; }
    }
    public class mydata
    {
        public string your_tnx_ref { get; set; }
    }

    public class WalletResponseData
    {
        public string AccountNumber { get; set; }
        public string LedgerAccountNumber { get; set; }
        public string WalletId { get; set; }
        public string InternalAccountId { get; set; }
    }


    public class CafeOneWalletProvider
    {
        public string AccountNumber { get; set; }
        public string Provider { get; set; }
        public string ProviderAccountName { get; set; }
        public string ProviderBankCode { get; set; }
        public string ProviderBankName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class Customer
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Tier { get; set; }
        public string OrganizationId { get; set; }
    }
    public class AccountNumbers
    {
        public List<CafeOneWalletProvider> Providers { get; set; }

    }
    public class CafeOneData
    {
        public string InternalAccountId { get; set; }
        public string AccountName { get; set; }
        public string Currency { get; set; }
        public string LedgerAccountNumber { get; set; }
        public string Balance { get; set; }
        public string WalletId { get; set; }
        public string OwnerId { get; set; }
        public List<CafeOneWalletProvider> WalletProviders { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }
        public Customer Customer { get; set; }
        public bool PndEnabled { get; set; }
    }

    public class CafeoneCustomerRequest
    {
        public string phoneNumber { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string email { get; set; }
        public string dob { get; set; }
        public string bvnNumber { get; set; }
        public string nin { get; set; }
    }

    public class CafeoneResponseModel
    {
        public CafeoneDataModel Data { get; set; }
        public int StatusCode { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
    }

    public class CafeoneDataModel
    {
        public long CreatedAt { get; set; }
        public long UpdatedAt { get; set; }
        public string Tier { get; set; }
        public bool Blacklisted { get; set; }
        public string Id { get; set; }
        public string OrganizationId { get; set; }
        public string UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Dob { get; set; }
        public string BvnNumber { get; set; }
        public string Nin { get; set; }
        public string Photo { get; set; }
        public string PhoneNumber { get; set; }
        public string TransactionLimit { get; set; }
        public CafeonePhoneModel Phone { get; set; }
        public string DeletedAt { get; set; }
        public string VerifiedAt { get; set; }
        public string UtilityBill { get; set; }
        public string PlaceOfBirth { get; set; }
        public string Country { get; set; }
        public string Address1 { get; set; }
        public string Gender { get; set; }
    }

    public class CafeonePhoneModel
    {
        public string CreatedAt { get; set; }
        public string UpdatedAt { get; set; }
        public string Id { get; set; }
        public string PhoneCountryCode { get; set; }
        public string PhoneNumber { get; set; }
        public string CustomerId { get; set; }
        public string DeletedAt { get; set; }
    }

}
