using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers
{
    public class InputFinancingManagement : IInputFinancingManagement
    {
        private readonly IInputFinancingManagementRepository _repository;
        private readonly IWarehouseManagementRepository _warehouseManagementRepository;
        private readonly IFarmerRepository _farmerRepository;
        private readonly IBusinessRepository _businessRepository;
        private readonly IAuthenticationRepository _authenticationRepository;
        private readonly DataContext _context;
        private readonly ILogger<InputFinancingManagement> _log;
        private readonly ICafeOneWalletService _cafeOneWalletService;
        private readonly IWalletRepository _walletRepository;
        private readonly ISMSService _whisperSMSService;

        private readonly IConfiguration _configuration;
        public InputFinancingManagement(IInputFinancingManagementRepository repository, IBusinessRepository businessRepository, IAuthenticationRepository authenticationRepository,
            DataContext context, ILogger<InputFinancingManagement> log, IWarehouseManagementRepository warehouseManagementRepository, ISMSService whisperSMSService,
            IFarmerRepository farmerRepository, ICafeOneWalletService cafeOneWalletService, IWalletRepository walletRepository, IConfiguration config)
        {
            _repository = repository;
            _businessRepository = businessRepository;
            _authenticationRepository = authenticationRepository;
            _context = context;
            _log = log;
            _warehouseManagementRepository = warehouseManagementRepository;
            _farmerRepository = farmerRepository;
            _cafeOneWalletService = cafeOneWalletService;
            _configuration = config;
            _walletRepository = walletRepository;

            _whisperSMSService = whisperSMSService;
        }
        //FarmerBundleSubscriptionInterests
        public async Task<Response<SubscriptionMetrics>> FarmerBundleSubscriptions(ListQueryParams query)
        {
            try
            {
                var business = await _businessRepository.GetBusinessByUserIdAsync(query.UserId);

                if (business == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                var subscription = await _repository.FarmerBundleSubscriptions(business.BusinessCode);
                if (subscription == null) return new Response<SubscriptionMetrics>
                {
                    Succeeded = true,
                    Data = null,
                    Message = "No Record Found"
                };
                return new Response<SubscriptionMetrics>
                {
                    Succeeded = true,
                    Data = subscription,
                    Message = "Retrieved Successfulluy"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Inventory: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }

        public async Task<Response<List<BundleSubscriptionSummary>>> GetBundleSubscriptionAnalysisByBusiness(ListQueryParams query)
        {
            try
            {
                var business = await _businessRepository.GetBusinessByUserIdAsync(query.UserId);

                if (business == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                var subscription = await _repository.FarmerBundleSubscriptionInterests(business.BusinessCode);
                if (subscription == null) return new Response<List<BundleSubscriptionSummary>>
                {
                    Succeeded = true,
                    Data = null,
                    Message = "No Record Found"
                };
                return new Response<List<BundleSubscriptionSummary>>
                {
                    Succeeded = true,
                    Data = subscription,
                    Message = "Retrieved Successfulluy"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Inventory: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }

        public async Task<List<BundleSubscriptionInterestModel>> GetInterestRequestByBusiness(BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
               ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            var requestsByBusiness = await _repository.GetSubscriptionInterestByBusiness(args.businessCode);
            if (!requestsByBusiness.Any()) return null;
            foreach (var request in requestsByBusiness)
            {
                var farmer = await _farmerRepository.GetFarmerByUserId(request.FarmerUserId) ?? throw new Exception("Farmer does not exist");
                var userSterlinWallet = await _walletRepository.GetSterlingWallet(farmer.UserId);
                if (userSterlinWallet != null)
                {

                    var accouuntBalance = await _cafeOneWalletService.GetUserBalance(userSterlinWallet.WalletId, args.Env);
                    request.AccountBalance = accouuntBalance.Balance;
                }

            }
            return requestsByBusiness;
        }

        public async Task<List<BundleSubscriptionInterestModel>> GetInterestRequestByCooperative(BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
               ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            var requestsByBusiness = await _repository.GetSubscriptionInterestByCooperative(args.businessCode, args.CooperativeId);
            if (!requestsByBusiness.Any()) return null;


            foreach (var request in requestsByBusiness)
            {
                var farmer = await _farmerRepository.GetFarmerByUserId(request.FarmerUserId) ?? throw new Exception("Farmer does not exist");
                var userSterlinWallet = await _walletRepository.GetSterlingWallet(farmer.UserId);
                if (userSterlinWallet != null)
                {

                    var accouuntBalance = await _cafeOneWalletService.GetUserBalance(userSterlinWallet.WalletId, args.Env);
                    request.AccountBalance = accouuntBalance.Balance;
                }

            }
            return requestsByBusiness;
        }
        public async Task<List<BundleSubscriptionInterestModel>> GetInterestRequestByLocation(BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
               ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            var requestsByBusiness = await _repository.GetSubscriptionInterestByLocation(args.businessCode, args.Location);
            if (!requestsByBusiness.Any()) return null;
            foreach (var request in requestsByBusiness)
            {
                var farmer = await _farmerRepository.GetFarmerByUserId(request.FarmerUserId) ?? throw new Exception("Farmer does not exist");
                var userSterlinWallet = await _walletRepository.GetSterlingWallet(farmer.UserId);
                if (userSterlinWallet != null)
                {

                    var accouuntBalance = await _cafeOneWalletService.GetUserBalance(userSterlinWallet.WalletId, args.Env);
                    request.AccountBalance = accouuntBalance.Balance;
                }

            }
            return requestsByBusiness;
        }

        public async Task<(bool status, string message)> ConfirmEquityCommitmentPayment(BundleSubscriptionInterestModel model, BusinessRegistrationArgs args)
        {
            var farmerWallet = new CafeOneData();
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                 ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var farmer = await _farmerRepository.GetFarmerByUserId(model.FarmerUserId) ?? throw new Exception("Farmer does not exist");
                    var userSterlinWallet = await _walletRepository.GetSterlingWallet(farmer.UserId);
                    var businessAccount = await _walletRepository.GetSterlingWallet(business.UserId);
                    if (userSterlinWallet != null)
                    {

                        farmerWallet = await _cafeOneWalletService.GetUserBalance(userSterlinWallet.WalletId, args.Env);
                    }

                    var bundleOfInterest = await _repository.GetUnpaidSubscriptionInterestByFarmer(model);
                    var getInputInventory = await _warehouseManagementRepository.GetBundleByIdAsync(business.BusinessCode, model.InterestedBundleId)
                            ?? throw new Exception("Bundle does not exist");
                    if (bundleOfInterest.Any())
                    {
                        var sterlingCharge = decimal.Parse(_configuration["Sterling:Charge"]);
                        var selectedBundle = bundleOfInterest.Where(x => x.InterestedBundleId == model.InterestedBundleId).FirstOrDefault();
                        var totalSettlment = getInputInventory.BundleEquityPlusCommission;
                        var settlementSplitAmountBusiness = getInputInventory.BundleEquityPrice;
                        var settlementPlatformSplitAmount = getInputInventory.CommissionPrice - sterlingCharge;

                        //TODO: REMOVE THIS LINE WHEN DEPLOYING
                       

                        if (totalSettlment != (settlementSplitAmountBusiness + settlementPlatformSplitAmount))
                        {
                            throw new BaseException(Core.Enums.ExceptionType.OPERATION_FAILED, "Settlement Validation mismatched");
                        }
                        if (selectedBundle != null && (decimal.Parse(farmerWallet.Balance) > (selectedBundle.ExpectedTotalEquityPrice - sterlingCharge)))
                        {
                            var reference = $"Bun{getInputInventory.Id}-{selectedBundle.QuantityOfInterest}-{business.BusinessCode}";

                            var createBusinessSettlement = await _cafeOneWalletService.UserWalletToBusinessWalletTransfer(new WalletToWalletTransferRequest
                            {
                                Amount = settlementSplitAmountBusiness,
                                BusinessAccountNumber = businessAccount.AccountNumber,
                                FromAccount = userSterlinWallet.AccountNumber,
                                Env = args.Env,
                                Memo = $"Input finance subscription for {getInputInventory.Name} by {farmer.Name} on {DateTimeOffset.Now}",
                                OwnerId = userSterlinWallet.OwnerId,
                                Reference = reference
                            });
                            _log.LogInformation($"Response from user transfer to User Business {business.BusinessCode} Wallet", JsonConvert.SerializeObject(createBusinessSettlement));
                            var createPlatformSettlement = await _cafeOneWalletService.UserWalletToAgrovestoTransfer(reference,
                                userSterlinWallet.OwnerId, settlementPlatformSplitAmount, userSterlinWallet.AccountNumber, args.Env);
                            _log.LogInformation("Response from user transfer to Agrovesto Wallet", JsonConvert.SerializeObject(createPlatformSettlement));
                            //log reeponse

                            if (createBusinessSettlement.Data != null && (createPlatformSettlement.Data != null))
                            {
                                var status = createBusinessSettlement.Status;
                                var platformstatus = createPlatformSettlement.Status;
                                model.PaymentStatus = status == "Succeeded" ? EquityPaymentStatus.Paid : EquityPaymentStatus.Failed;
                                model.PaymentDate = status == "Succeeded" ? DateTimeOffset.Now : null;
                                model.IsCommissionPaidOut = platformstatus == "Suceeded" ? true : false;
                                model.TotalQuantityPaidFor = (long)(settlementSplitAmountBusiness / selectedBundle.ExpectedEquityPricePerBundle);
                                model.TotalEquityPaid = settlementSplitAmountBusiness;

                                model.Reference = reference;

                                //update payment
                                //notify farmer
                                //notify business


                            }

                            var update = await _repository.UpdateSubscriptionPayment(model);

                            var messageBody = $"You have successfully completed your subscription for input package: {getInputInventory.BundlePackage}." +
                                              $"You will receive an update when distribution begins ";

                            var sms = await _whisperSMSService.HandleWhisperSMS(new WhisperSMSModel
                            {
                                campaign_name = "Input Notification",
                                contacts = new List<string> { farmer.PhoneNumber },
                                message = messageBody,
                            });
                            var sms2 = await _whisperSMSService.HandleTermiiSMS(new TermiiSmsRequest
                            {
                                to = $"234{farmer.PhoneNumber.GetLast(10)}",
                                sms = messageBody
                            });
                            return (true, "Payment Updated");
                        }
                    }

                    return (false, "Could not retrieve bundle");
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    throw new Exception($"{ex.Message}", ex);
                }
            });
            return (true, "Payment Updated");
        }
        public async Task<(bool status, BundleSubscriptionInterestResponseModel data)> CreateInputFinancingInterest(BundleSubscriptionInterestModel model, BusinessRegistrationArgs args)
        {
            var response = new BundleSubscriptionInterestResponseModel();
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {

                    var getInputInventory = await _warehouseManagementRepository.GetBundleByIdAsync(business.BusinessCode, model.InterestedBundleId)
                        ?? throw new Exception("Bundle does not exist");
                    var farmer = await _farmerRepository.GetFarmerByUserId(model.FarmerUserId) ?? throw new Exception("Farmer does not exist");

                    model.BelongToBusinessCode = args.businessCode;
                    model.OfficerInChargeUserId = args.UserId;
                    model.InterestedBundleName = getInputInventory.Name;
                    model.PaymentStatus = EquityPaymentStatus.Initial;
                    model.FarmerName = farmer.Name;
                    model.CooperativeId = args.CooperativeId;
                    model.Location = farmer.State;
                    if (getInputInventory.ShouldAddCommission)
                    {
                        model.ShouldAddCommission = true;
                        model.CommissionPrice = getInputInventory.CommissionPrice;//(getInputInventory.CommissionPrice * model.QuantityOfInterest);
                                                                                  //model.EquityPlusCommission = (getInputInventory.BundlePrice * model.QuantityOfInterest) + getInputInventory.CommissionPrice
                    }

                    model.ExpectedEquityPricePerBundle = getInputInventory.BundleEquityPrice; //if equity price is availa
                    var expectedPayment = ((getInputInventory.BundleEquityPrice * model.QuantityOfInterest) + model.CommissionPrice);

                    if (expectedPayment < getInputInventory.BundleEquityPlusCommission)
                    {
                        throw new BaseException(Core.Enums.ExceptionType.OPERATION_FAILED, "Bundle equity value mismatched");
                    }
                    model.ExpectedTotalEquityPrice = expectedPayment;
                    var createWarehouse = await _repository.CreateInputFinancingInterestThroughBundle(model);

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    //after interest created, call, user sterling bank and display amount to pay
                    var userSterlinWallet = await _walletRepository.GetSterlingWallet(farmer.UserId);
                    if (userSterlinWallet != null)
                    {

                        var userWallet = await _cafeOneWalletService.GetUserAccount(userSterlinWallet.InternalAccountId, args.Env);
                        if (userWallet != null)
                        {

                            response.AccountName = userWallet.ProviderAccountName;
                            response.AccountNUmber = userWallet.AccountNumber;
                            response.BankName = userWallet.ProviderBankName;
                            response.Amount = createWarehouse.data.ExpectedTotalEquityPrice;

                        }
                    }
                    var messageBody = $"You have indicated interest in input package: {getInputInventory.BundlePackage}." +
                    $"Confirm your equity by crediting {response.Amount} into {response.AccountNUmber} {response.AccountName} {response.BankName} ";

                    var sms = await _whisperSMSService.HandleWhisperSMS(new WhisperSMSModel
                    {
                        campaign_name = "Input Notification",
                        contacts = new List<string> { farmer.PhoneNumber },
                        message = messageBody,
                    });
                    var sms2 = await _whisperSMSService.HandleTermiiSMS(new TermiiSmsRequest
                    {
                        to = $"234{farmer.PhoneNumber.GetLast(10)}",
                        sms = messageBody
                    });
                    return (true, response);
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    throw new Exception($"{ex.Message}", ex);
                }

            });
            return (true, response);
        }


        //method get all successful payment for input subscription
        //method disburse input bundle to farmers and mark disbursement as completed,, deplete inventory in the warehouse based on what has been disbursed

    }
}
