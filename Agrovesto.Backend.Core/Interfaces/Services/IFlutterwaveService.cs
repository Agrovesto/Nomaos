using Agrovesto.Backend.Core.Models.Payment.Flutterwave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Services
{
    public interface IFlutterwaveService
    {
        Task<PaymentTransferResponseModel> FetchTransfer(long id, string Env);
        Task<BankResponseModel> GetBanks(string Env, string countryCode = "NG");
        Task<PaymentResponseModel> InitializePaymet(PaymentRequestModel model, string Env);
        Task<PaymentTransferResponseModel> ResolveAccount(string accountNumber, string bankCode, string Env);
        Task<PaymentTransferResponseModel> Transfer(PaymentTransferRequest model, string Env);
        Task<PaymentVerificationResponseModel> VerifyTransaction(int transactionId, string Env);
    }
}
