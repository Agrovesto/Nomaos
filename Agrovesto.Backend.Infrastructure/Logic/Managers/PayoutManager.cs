using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common.WalletServiceRequest;
using Agrovesto.Backend.Infrastructure.DB;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers
{
    public class PayoutManager : IPayoutManager
    {
        private readonly IBusinessRepository _businessRepository;
        private readonly IAuthenticationRepository _authenticationRepository;
        private readonly DataContext _context;
        private readonly ILogger<PayoutManager> _log;
        private readonly ICafeOneWalletService _cafeOneWalletService;
        private readonly IWalletManager _walletManager;
        public PayoutManager( IBusinessRepository businessRepository,
            IAuthenticationRepository authenticationRepository,
              DataContext context, ILogger<PayoutManager> log,
              ICafeOneWalletService cafeOneWalletService, IWalletManager walletManager)
        {
            _businessRepository = businessRepository;
            _authenticationRepository = authenticationRepository;
            _context = context;
            _log = log;
            _cafeOneWalletService = cafeOneWalletService;
            _walletManager = walletManager;
        }
        public async Task<(bool status, PayoutModel data)> WithdrawFund(PayoutModel model, BusinessRegistrationArgs args)
        {
            //verify userpin
            // get sourceaccount balance
            //compare account balance with amount to withdraw
            //save transaction with pending status
            // do fund transfer
            //update transaction with success
            //return

            var farmerWallet = new CafeOneCustomerWalletResponse();
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                 ?? throw new Exception("Business does not exist");
           
            args.UserId ??= business.UserId;

            ///REFACTOR, LET IT RETURN THE SUBSCIRPTION THE FARMER MADE AND SHOULD INCLUDE THE SUBSCRIPTION DETAIL
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    model.UserId = args.UserId;
                    var user = await _authenticationRepository.GetUserById(model.UserId);
                    if (user.model == null) throw new Exception("User record not found");

                    var userSterlinWallet = await _walletManager.GetUserSterlingAccount(user.model.Id);
                   
                    if (userSterlinWallet != null)
                    {
                        farmerWallet = await _walletManager.RetrieveCustomerWallet(model.UserId, args.Env);
                    }

                    //if (model.Amount > farmerWallet.availableBalance)
                    //{
                    //    throw new BaseException(Core.Enums.ExceptionType.OPERATION_FAILED, "Insufficient Funds");
                    //}
                    var datePart = DateTime.UtcNow.ToString("MM-dd"); // e.g., "05-24"
                    var randomDigits = new Random().Next(100, 1000);

                    var fundWithdrawal = await _cafeOneWalletService.TransferFromUserAccountToBusinessAccount(new WalletToWalletTransferRequestModel
                    {
                        Amount = model.Amount,
                        TransactionReference = $"{business.BusinessCode}-TRF-{datePart}-{randomDigits}",
                        AccountNumber = model.DestinationAccountNumber,
                        BatchReference = $"{business.BusinessCode}-WTH-{datePart}-{randomDigits}",
                        Remarks = !string.IsNullOrEmpty(model.Narration) ? model.Narration : "Fund withdrawal",
                        WalletId= userSterlinWallet.WalletId,
                        CustomerId=userSterlinWallet.CustomerId
                    }, args.Env); 

                    var createPayout = model.Map();

                    if (fundWithdrawal != null)
                    {
                        if (fundWithdrawal.success)
                        {
                            createPayout.IsSuccessfulTransaction = true;
                            //createPayout.Reference = fundWithdrawal.data.data;
                        }
                    }
                    await _context.AddAsync(createPayout);


                    _log.LogInformation("Response from Wallet WIthdrawal", JsonConvert.SerializeObject(fundWithdrawal));
                    //log reeponse
                   
                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();
                    model.Map();

                    return (true, model);

                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    throw new Exception($"{ex.Message}", ex);
                }
            });
            return (true, model);
        }
    }
}
