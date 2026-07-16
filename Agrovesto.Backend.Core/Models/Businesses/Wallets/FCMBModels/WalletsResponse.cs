using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses.Wallets.FCMBModels
{

    public class WalletsResponse<T>
    {
        public string message { get; set; }
        public string status { get; set; }
        public T data { get; set; }
    }

    public class CreateWalletResponse
    {

        [JsonPropertyName("noOfSuccessfulVirtualAccountCreation")]
        public long NoOfSuccessfulVirtualAccountCreation { get; set; }

        [JsonPropertyName("noOfFailedVirtualAccountCreation")]
        public long NoOfFailedVirtualAccountCreation { get; set; }

        [JsonPropertyName("successfulVirtualAccounts")]
        public WalletAccounts[] SuccessfulVirtualAccounts { get; set; }

    }
    public class WalletAccounts
    {
        [JsonPropertyName("virtualAccountNumber")]
        public string VirtualAccountNumber { get; set; }
        [JsonPropertyName("virtualAccountName")]
        public string VirtualAccountName { get; set; }
        [JsonPropertyName("payload")]
        public WalletAccountsPayload Payload { get; set; }
    }

    public class WalletAccountsPayload
    {
        [JsonPropertyName("clientId")]
        public string ClientId { get; set; }
        [JsonPropertyName("bvn")]
        public string Bvn { get; set; }
        [JsonPropertyName("firstName")]
        public string FirstName { get; set; }
        [JsonPropertyName("lastName")]
        public string LastName { get; set; }
        [JsonPropertyName("nin")]
        public string Nin { get; set; }
        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; }
        [JsonPropertyName("emailAddress")]
        public string EmailAddress { get; set; }
    }

    public class WalletsRequest
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string AccountNumber { get; set; }
        public string? NIN { get; set; }
        public string? BVN { get; set; }
        public string? CustomerId { get; set; }
        public string? CurrencyId { get; set; }
        public string? DOB { get; set; }
        public string? MiddleName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Alias { get; set; }
    }


    public class BanksResponseData
    {

        [JsonPropertyName("bankCode")]
        public string BankCode { get; set; }

        [JsonPropertyName("bankName")]
        public string BankName { get; set; }

        [JsonPropertyName("bankShortName")]
        public string BankShortName { get; set; }

        [JsonPropertyName("domeBankCode")]
        public string DomBankCode { get; set; }
    }
    public class TokenResponse
    {
        [JsonPropertyName("access_token")]

        public string access_token { get; set; }

        [JsonPropertyName("expires_in")]
        public string expires_in { get; set; }
        [JsonPropertyName("refresh_expires_in")]
        public string resfresh_expires_in { get; set; }
        [JsonPropertyName("token_type")]
        public string token_type { get; set; }
        [JsonPropertyName("not-before-policy")]
        public string not_before_policy { get; set; }
        [JsonPropertyName("scope")]
        public string Scope { get; set; }

    }

    public class FCMBParams
    {
        public string GrantType { get; set; }

        public string ClientId { get; set; }


        public string ClientSecret { get; set; }


        public string Scope { get; set; }
        public string BaseUrl { get; set; }
    }


}
