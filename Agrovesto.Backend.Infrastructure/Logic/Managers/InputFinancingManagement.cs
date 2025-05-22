using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Common.WalletServiceRequest;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection.Metadata;
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
        // private readonly IWalletRepository _walletRepository;
        private readonly ISMSService _whisperSMSService;
        private readonly IFaceManager _faceManager;
        private readonly IConfiguration _configuration;
        private readonly IWalletManager _walletManager;
        public InputFinancingManagement(IInputFinancingManagementRepository repository,
            IFaceManager faceManager, IBusinessRepository businessRepository, IAuthenticationRepository authenticationRepository,
            DataContext context, ILogger<InputFinancingManagement> log, IWarehouseManagementRepository warehouseManagementRepository, ISMSService whisperSMSService,
            IFarmerRepository farmerRepository,
            ICafeOneWalletService cafeOneWalletService,
            // IWalletRepository walletRepository, 
            IConfiguration config, IWalletManager walletManager)
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
            // _walletRepository = walletRepository;
            _faceManager = faceManager;
            _whisperSMSService = whisperSMSService;
            _walletManager = walletManager;
        }
        public async Task<Response<List<SubscriptionData>>> FarmerBundleSubscriptionsMetric(ListQueryParams query)
        {
            try
            {
                var business = await _businessRepository.GetBusinessByUserIdAsync(query.UserId);

                if (business == null && !string.IsNullOrWhiteSpace(query.businessCode))
                {
                    business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
                }
                if (business == null)
                {
                    throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "No Record Found");
                }

                var bizEntities = await _repository.FarmerBundleSubscriptions(business.BusinessCode);
                if (bizEntities == null) return new Response<List<SubscriptionData>>
                {
                    Succeeded = false,
                    Data = null,
                    Message = "No Record Found"
                };
                return new Response<List<SubscriptionData>>
                {
                    Succeeded = true,
                    Data = bizEntities,
                    Message = "Retrieved Successfully"
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
        public async Task<PagedListResponse<List<BundleSubscriptionInterestModel>>> GetInterestRequestByAgent(BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
               ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            var requestsByBusiness = await _repository.GetInterestRequestByAgent(args.businessCode, args.UserId, args.PageNumber, args.PageSize);
            if (requestsByBusiness == null) return null;


            foreach (var request in requestsByBusiness.Data)
            {
                var farmer = await _farmerRepository.GetFarmerByUserId(request.FarmerUserId) ?? throw new Exception("Farmer does not exist");
                var userSterlinWallet = await _walletManager.GetUserSterlingAccount(farmer.UserId);
                if (userSterlinWallet != null)
                {
                    var accouuntBalance = await _walletManager.RetrieveCustomerWallet(request.FarmerUserId, args.Env);
                    if (accouuntBalance != null)
                    {
                        if (accouuntBalance != null)
                        {

                            request.AccountBalance = accouuntBalance.availableBalance.ToString();
                        }
                    }

                }

            }
            return requestsByBusiness;
        }


        //FarmerBundleSubscriptionInterests
        public async Task<Response<List<SubscriptionData>>> FarmerBundleSubscriptions(ListQueryParams query)
        {
            try
            {
                var business = await _businessRepository.GetBusinessByUserIdAsync(query.UserId);

                if (business == null && !string.IsNullOrWhiteSpace(query.businessCode))
                {
                    business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
                }
                if (business == null)
                {
                    throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "No Record Found");
                }

                var subscription = await _repository.FarmerBundleSubscriptions(business.BusinessCode);
                if (subscription == null) return new Response<List<SubscriptionData>>
                {
                    Succeeded = true,
                    Data = null,
                    Message = "No Record Found"
                };
                return new Response<List<SubscriptionData>>
                {
                    Succeeded = true,
                    Data = subscription,
                    Message = "Retrieved Successfully"
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

                if (business == null && !string.IsNullOrWhiteSpace(query.businessCode))
                {
                    business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
                }
                if (business == null)
                {
                    throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "No Record Found");
                }

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
                    Message = "Retrieved Successfully"
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

        public async Task<PagedListResponse<List<BundleSubscriptionInterestModel>>> GetInterestRequestByBusiness(BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
               ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            var requestsByBusiness = await _repository.GetSubscriptionInterestByBusiness(args.businessCode, args.PageNumber, args.PageSize, args.startDate, args.endDate, args.Location);
            if (requestsByBusiness == null) return null;


            foreach (var request in requestsByBusiness.Data)
            {
                var farmer = await _farmerRepository.GetFarmerByUserId(request.FarmerUserId) ?? throw new Exception("Farmer does not exist");
                var userSterlinWallet = await _walletManager.GetUserSterlingAccount(farmer.UserId);
                if (userSterlinWallet != null)
                {
                    var accouuntBalance = await _walletManager.RetrieveCustomerWallet(request.FarmerUserId, args.Env);
                    if (accouuntBalance != null)
                    {

                        request.AccountBalance = accouuntBalance.availableBalance.ToString();
                    }
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
                var userSterlinWallet = await _walletManager.GetUserSterlingAccount(farmer.UserId);
                if (userSterlinWallet != null)
                {
                    var accouuntBalance = await _walletManager.RetrieveCustomerWallet(request.FarmerUserId, args.Env);
                    if (accouuntBalance != null)
                    {

                        request.AccountBalance = accouuntBalance.availableBalance.ToString();
                    }
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
                var userSterlinWallet = await _walletManager.GetUserSterlingAccount(farmer.UserId);
                if (userSterlinWallet != null)
                {
                    var accouuntBalance = await _walletManager.RetrieveCustomerWallet(request.FarmerUserId, args.Env);
                    if (accouuntBalance != null)
                    {

                        request.AccountBalance = accouuntBalance.availableBalance.ToString();
                    }
                }

            }
            return requestsByBusiness;
        }

        public async Task<(bool status, BundleSubscriptionInterestModel data)> ConfirmEquityCommitmentPayment(BundleSubscriptionInterestModel model, BusinessRegistrationArgs args)
        {
            var farmerWallet = new CafeOneCustomerWalletResponse();
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                 ?? throw new Exception("Business does not exist");
            var getInputInventory = new BundleModel();
            args.UserId ??= business.UserId;
            var update = new BundleSubscriptionInterestModel();

            ///REFACTOR, LET IT RETURN THE SUBSCIRPTION THE FARMER MADE AND SHOULD INCLUDE THE SUBSCRIPTION DETAIL
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var farmer = await _farmerRepository.GetFarmerByUserId(model.FarmerUserId) ?? throw new Exception("Farmer does not exist");
                    var userSterlinWallet = await _walletManager.GetUserSterlingAccount(farmer.UserId);
                    var businessAccount = await _walletManager.GetUserSterlingAccount(business.UserId);
                    if (userSterlinWallet != null)
                    {
                        farmerWallet = await _walletManager.RetrieveCustomerWallet(model.FarmerUserId, args.Env);
                    }

                    var bundleOfInterest = await _repository.GetUnpaidSubscriptionInterestByFarmer(model);
                    getInputInventory = await _warehouseManagementRepository.GetBundleByIdAsync(business.BusinessCode, model.InterestedBundleId)
                            ?? throw new Exception("Bundle does not exist");
                    if (!bundleOfInterest.Any()) { throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST, "No pending equity payment for selected farmer"); }

                    var sterlingCharge = decimal.Parse(_configuration["Sterling:Charge"]);
                    var selectedBundle = bundleOfInterest.Where(x => x.InterestedBundleId == model.InterestedBundleId).FirstOrDefault();
                    var totalSettlment = selectedBundle.ExpectedTotalEquityPrice;


                    var settlementSplitAmountBusiness = getInputInventory.BundleEquityPrice != 0m
                    ? ((getInputInventory.BundleEquityPrice * selectedBundle.QuantityOfInterest) - (getInputInventory.CommissionPrice))
                    : (getInputInventory.BundlePrice + getInputInventory.CommissionPrice);

                    var settlementPlatformSplitAmount = getInputInventory.CommissionPrice;

                    //TODO: check if commission is enabled, then add the commission value, else don't


                    if (totalSettlment != (settlementSplitAmountBusiness + settlementPlatformSplitAmount))
                    {
                        throw new BaseException(Core.Enums.ExceptionType.OPERATION_FAILED, "Settlement Validation mismatched");
                    }

                    if (selectedBundle == null && ((decimal)(farmerWallet.availableBalance) < (selectedBundle.ExpectedTotalEquityPrice - sterlingCharge)))
                    {
                        throw new BaseException(Core.Enums.ExceptionType.OPERATION_FAILED, "Insufficient amount in user wallet for the selected bundle");
                    }
                    var reference = $"Bun{getInputInventory.Id}-{selectedBundle.QuantityOfInterest}-{business.BusinessCode}-{ReferenceUtil.GenerateTransferReference()}";


                    var createBusinessSettlement = await _walletManager.PayoutTransferFromUserAccountToBusinessAccount(new PostWalletToWalletTransferRequestModel
                    {
                        Amount = settlementSplitAmountBusiness,
                        sourceAccountName = farmerWallet.name,
                        sourceAccountNumber = farmerWallet.virtualAccount.accountNumber,
                        destinationAccountNumber = businessAccount.AccountNumber,
                        destinationAccountName = businessAccount.AccountName,

                        remarks = $"Farmer purchase for {selectedBundle.InterestedBundleName}",
                    }, args.Env);
                    if (createBusinessSettlement == null )
                    {
                        throw new BaseException(Core.Enums.ExceptionType.OPERATION_FAILED, "Settlement challenge: Could not confirm payment");
                    }
                    
                    _log.LogInformation($"Response from user transfer to User Business {business.BusinessCode} Wallet", JsonConvert.SerializeObject(createBusinessSettlement));
                    var @ref = $"Bun{getInputInventory.Id}-{selectedBundle.QuantityOfInterest}-{business.BusinessCode}-{ReferenceUtil.GenerateTransferReference()}";
                    if (settlementPlatformSplitAmount != 0M)
                    {
                        var createPlatformSettlement = await _walletManager.PayoutTransferFromUserAccountToAgrovestoAccount(new PostWalletToWalletTransferRequestModel
                        {
                            Amount = settlementPlatformSplitAmount,
                            sourceAccountName = farmerWallet.name,
                            sourceAccountNumber = farmerWallet.virtualAccount.accountNumber,

                            //AccountNumber = //businessAccount.AccountNumber,
                            remarks = $"Farmer purchase for {selectedBundle.InterestedBundleName}",
                        }, args.Env);


                        _log.LogInformation("Response from user transfer to Agrovesto Wallet", JsonConvert.SerializeObject(createPlatformSettlement));
                        if (createPlatformSettlement == null)
                        {
                            throw new BaseException(Core.Enums.ExceptionType.OPERATION_FAILED, "Settlement challenge: Could not confirm payment");
                        }
                        var platformstatus = createPlatformSettlement.success;
                        model.IsCommissionPaidOut = platformstatus ? true : false;

                    }



                    //log reeponse
                    selectedBundle.ExpectedEquityPricePerBundle = selectedBundle.ExpectedEquityPricePerBundle != 0m ? selectedBundle.ExpectedEquityPricePerBundle : 1;
                    var status = createBusinessSettlement.success;
                   

                    model.Status = status ? EquityPaymentStatus.Paid : EquityPaymentStatus.Failed;
                    model.PaymentDate = status ? DateTimeOffset.Now : null;
                          model.TotalQuantityPaidFor = (long)(totalSettlment / selectedBundle.ExpectedEquityPricePerBundle); //(long)(settlementSplitAmountBusiness / selectedBundle.ExpectedEquityPricePerBundle);
                    model.TotalEquityPaid = totalSettlment;


                    model.Reference = reference;

                    //update payment
                    //notify farmer
                    //notify business
                    model.EquitySubscriptionStatus = InputFinancingStatus.Requested;
                    model.MaturityDate = getInputInventory.MaturityDate;
                    update = await _repository.UpdateSubscriptionPayment(model);

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();
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

                    //responseObject = await RetrieveFarmerInputDisbursementReceipt(model, args);


                    return (true, update);

                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    throw new Exception($"{ex.Message}", ex);
                }
            });
            return (true, update);
        }

        public async Task<BundleInterestReceiptModel> RetrieveFarmerInputDisbursementReceipt(BundleSubscriptionInterestModel model, BusinessRegistrationArgs args)
        {

            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                 ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            // var subscription = await _repository.GetPaidSubscriptionByFarmer(business.BusinessCode, model.FarmerUserId, model.Reference);
            var subscription = await _repository.RetrieveFarmerInputDisbursementReceipt(business.BusinessCode, model.FarmerUserId, model.Reference);
            if (subscription == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "Subscription Detail not found");

            var bundle = await _warehouseManagementRepository.GetBundleByIdAsync(business.BusinessCode, subscription.SubscriptionInterest.InterestedBundleId)
                            ?? throw new Exception("Bundle does not exist");
            foreach (var item in bundle.BundlePackage)
            {
                var input = await _warehouseManagementRepository.GetInputBYIdAsync(business.BusinessCode, item.WarehouseInputInventoryId);
                item.Price = input.Price;
            }

            return await GenerateReceipt(subscription, bundle, business, subscription.SubscriptionInterest.CommissionPrice);

        }

        public async Task<BundleInterestReceiptModel> GenerateReceipt(BundleInterestReceiptModel subscription, BundleModel bundle, BusinessModel business, decimal settlementPlatformSplitAmount)
        {
            return new BundleInterestReceiptModel
            {
                Id = subscription.SubscriptionInterest.Id,
                Description = bundle.Description,
                EquityPaymentBalanceDeficit = (bundle.BundlePrice * subscription.QuantityOfInterest) - subscription.TotalEquityPaid,
                PaymentDate = subscription.SubscriptionInterest.PaymentDate,
                BundlePackage = bundle.BundlePackage,
                BundlePrice = bundle.BundlePrice * subscription.SubscriptionInterest.QuantityOfInterest,
                FarmerName = subscription.SubscriptionInterest.FarmerName,
                FarmerUserId = subscription.SubscriptionInterest.FarmerUserId,
                BundleType = bundle.Name,
                OtherBundlePaymentCosts = bundle.OtherBundlePaymentCosts,
                PaymentStatus = subscription.SubscriptionInterest.PaymentStatus,
                QuantityOfInterest = subscription.SubscriptionInterest.QuantityOfInterest,
                Status = subscription.SubscriptionInterest.Status,
                TotalEquityPaid = subscription.SubscriptionInterest.TotalEquityPaid + settlementPlatformSplitAmount,
                OnBoardedOn = subscription.SubscriptionInterest.OnBoardedOn,
                Farmer = subscription.Farmer,
                Cooperative = subscription.Cooperative,
                SubscriptionInterest = subscription.SubscriptionInterest,
                MaturityDate = subscription.SubscriptionInterest.MaturityDate,
                BelongToBusinessCode = subscription.SubscriptionInterest.BelongToBusinessCode,
                BusinessLogo = business.BusinessLogo,
                BusinessName = business.BusinessName,
                BusinessPhoneNumber = business.PhoneNumber,
                Reference = subscription.SubscriptionInterest.Reference,
                WarehouseName = bundle.WarehouseName,
                ExpectedEquity = subscription.SubscriptionInterest.ExpectedTotalEquityPrice,
            };
        }
        public async Task<(bool status, BundleSubscriptionInterestResponseModel data)> CreateInputFinancingInterest(BundleSubscriptionInterestModel model, BusinessRegistrationArgs args)
        {
            var response = new BundleSubscriptionInterestResponseModel();
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            var status = true;
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var existingInterest = await _repository.GetExistingSubscription(model);
                    if (existingInterest != null) { throw new Exception("You've indicated interest in this previously, kindly pay for the existing interest"); }
                    var getInputInventory = await _warehouseManagementRepository.GetBundleByIdAsync(business.BusinessCode, model.InterestedBundleId)
                         ?? throw new Exception("Bundle does not exist");
                    var farmer = await _farmerRepository.GetFarmerByUserId(model.FarmerUserId) ?? throw new Exception("Farmer does not exist");

                    var sterlingCharge = decimal.Parse(_configuration["Sterling:Charge"]);
                    model.BelongToBusinessCode = args.businessCode;
                    model.OfficerInChargeUserId = args.UserId;
                    model.InterestedBundleName = getInputInventory.Name;
                    model.Status = EquityPaymentStatus.Initial;
                    model.FarmerName = farmer.Name;
                    model.CooperativeId = farmer.BelongToCooperativeId;
                    model.Location = farmer.State;
                       if (getInputInventory.ShouldAddCommission)
                    {
                        model.ShouldAddCommission = true;
                        model.CommissionPrice = getInputInventory.CommissionPrice;
                    }

                    model.ExpectedEquityPricePerBundle = getInputInventory.BundleEquityPrice; //if equity price is available use equityprice

                    getInputInventory.BundleEquityPrice = getInputInventory.BundleEquityPrice != 0m ? getInputInventory.BundleEquityPrice : getInputInventory.BundlePrice;

                    //getInputInventory.BundleEquityPlusCommission = getInputInventory.BundleEquityPlusCommission != 0m ? getInputInventory.BundleEquityPlusCommission : getInputInventory.BundlePrice;

                    var expectedPayment = ((getInputInventory.BundleEquityPrice * model.QuantityOfInterest));// + model.CommissionPrice);

                    if (expectedPayment < (model.QuantityOfInterest * model.ExpectedEquityPricePerBundle))
                    {
                        throw new BaseException(Core.Enums.ExceptionType.OPERATION_FAILED, "Bundle equity value mismatched");
                    }
                    model.ExpectedTotalEquityPrice = expectedPayment;// + sterlingCharge;


                    model.EquityPaymentBalanceDeficit = (getInputInventory.BundlePrice * model.QuantityOfInterest) - (getInputInventory.BundleEquityPrice * model.QuantityOfInterest);


                    var userWallet = await _walletManager.GetUserSterlingAccount(farmer.UserId);

                    if (userWallet != null)
                    {
                        getInputInventory.TotalBundlePackageRequested = model.QuantityOfInterest;
                        var updateBundleSubscription = await _warehouseManagementRepository.UpdateBundleSubscriptions(getInputInventory);
                        //update bundke
                        var createWarehouse = await _repository.CreateInputFinancingInterestThroughBundle(model);

                        await _context.SaveChangesAsync();
                        await dbTransaction.CommitAsync();

                        response.RepaymentBalance = model.EquityPaymentBalanceDeficit;
                        response = await GenerateInputSubscriptionInterestPaymentSlip(response, userWallet, createWarehouse.data.ExpectedTotalEquityPrice);


                        await SendInputSubscriptionINterestSMS(response, getInputInventory, farmer);
                        status = true;
                    }

                    status = false;

                    //return (status, response);

                    // (status, response);
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    throw new Exception($"{ex.Message}", ex);
                }

            });
            return (status, response);
        }

        public async Task<BundleSubscriptionInterestResponseModel> RetrievePaymentSlip(BundleSubscriptionInterestModel model, BusinessRegistrationArgs args)
        {
            var response = new BundleSubscriptionInterestResponseModel();
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;

            var userWallet = await _walletManager.GetUserSterlingAccount(model.FarmerUserId);

            var bundleOfInterest = await _repository.GetFarmerSubscriptionInterestDetails(model);

            if (!bundleOfInterest.Any()) { throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST, "No pending equity payment for selected farmer"); }

            var selectedBundle = bundleOfInterest.Where(x => x.InterestedBundleId == model.InterestedBundleId).FirstOrDefault();

            var receipt = await GenerateInputSubscriptionInterestPaymentSlip(response, userWallet, selectedBundle.ExpectedTotalEquityPrice);

            return response;
        }
        private async Task<BundleSubscriptionInterestResponseModel> GenerateInputSubscriptionInterestPaymentSlip(BundleSubscriptionInterestResponseModel response,
           SterlingBankWalletModel userWallet,
           decimal ExpectedTotalEquityPrice)
        {
            response.AccountName = userWallet.AccountName;
            response.AccountNumber = userWallet.AccountNumber;
            response.BankName = userWallet.BankName;
            response.Amount = ExpectedTotalEquityPrice;
            response.RepaymentBalance = response.RepaymentBalance;

            return response;
        }

        private async Task SendInputSubscriptionINterestSMS(BundleSubscriptionInterestResponseModel response, BundleModel getInputInventory, FarmerModel farmer)
        {
            var messageBody = $"You have indicated interest in input package: {getInputInventory.Name}." +
                         $"Confirm your equity by crediting {response.Amount} into {response.AccountNumber} {response.AccountName} {response.BankName} ";

            var sms = await _whisperSMSService.HandleWhisperSMS(new WhisperSMSModel
            {
                campaign_name = "Input Notification",
                contacts = new List<string> { farmer.PhoneNumber },
                message = messageBody,
            });
            var sms1 = await _whisperSMSService.HandleAfricasTalkingSMS(new WhisperSMSModel
            {
                campaign_name = "Input Notification",
                contacts = new List<string> { $"+234{farmer.PhoneNumber.GetLast(10)}" },
                message = messageBody,
            });
            var sms2 = await _whisperSMSService.HandleTermiiSMS(new TermiiSmsRequest
            {
                to = $"234{farmer.PhoneNumber.GetLast(10)}",
                sms = messageBody
            });
        }

        public async Task<BundleSubscriptionInterestModel> ApproveEquityForDisbursement(BundleSubscriptionInterestModel model, BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
               ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;

            var updateSubsubcriptionStatus = new BundleSubscriptionInterestModel();

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {

                    var subscription = await _repository.GetPaidSubscriptionByFarmer(business.BusinessCode, model.FarmerUserId, model.Reference);
                    if (subscription == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "Subscription Detail not found");

                    var bundleByWaresbouse = await _warehouseManagementRepository.GetBundleByIdAsync(business.BusinessCode, model.InterestedBundleId);
                    if (bundleByWaresbouse == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "Bundle not found in warehouse");


                    var warehouseUSer = await _warehouseManagementRepository.GetWarehouseManagerByBusiness(args.businessCode, bundleByWaresbouse.WarehouseId);
                    if (warehouseUSer == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "Warehouse user not available");
                    var user = await _authenticationRepository.GetUserById(warehouseUSer.UserId);
                    if (!user.status) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "User not found");

                    var getInputInventory = await _warehouseManagementRepository.GetBundleByIdAsync(business.BusinessCode, model.InterestedBundleId)
                            ?? throw new Exception("Bundle does not exist");

                    subscription.EquitySubscriptionStatus = model.EquitySubscriptionStatus; //InputFinancingStatus.Approved;
                    updateSubsubcriptionStatus = await _repository.UpdateEquitySubcriiptonStatus(subscription);
                    if (model.EquitySubscriptionStatus == InputFinancingStatus.Approved)
                    {

                        SendDisbursementApprovalEmail(user.model, subscription, business, getInputInventory);
                        //notify agent via mail to disburse input
                    }
                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();
                    return updateSubsubcriptionStatus;

                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    throw new Exception($"{ex.Message}", ex);
                }
            });
            return updateSubsubcriptionStatus;
        }
        private static void SendDisbursementApprovalEmail(UserModel bWorker, BundleSubscriptionInterestModel args, BusinessModel business, BundleModel bundle)
        {
            var propagationUrl = AppSettingsHelper.Setting("propagation:url");
            var mailRequest = new InputDisbursementApprovalMailBody
            {
                To = bWorker.Email,
                ToName = $"{bWorker.FirstName}",
                Subject = $" {business.BusinessName} Input Disbursement Notification",
                From = business.BusinessEmail,
                FromName = business.BusinessName,
                Bundle = args.InterestedBundleName,
                Quantity = args.TotalQuantityPaidFor.ToString(),
                BundlePackage = bundle.Name,
                CompanyLogo = business.BusinessLogo,
                Reference = args.Reference,
                PropagateUrl = $"{propagationUrl}/agent/disbursement?userId={args.FarmerUserId}&reference={args.Reference}",
            };
            var mail = SendGridService.DynamicInputDisbursementApproval(mailRequest);
        }

        //method get all successful payment for input subscription
        //method disburse input bundle to farmers and mark disbursement as completed,, deplete inventory in the warehouse based on what has been disbursed
        public async Task<BundleDisbursementModel> DisburseInput(BundleDisbursementModel model, BusinessRegistrationArgs args)
        {

            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            var updateDisbursement = new BundleDisbursementModel();
            var strategy = _context.Database.CreateExecutionStrategy();
            var user = await _authenticationRepository.GetUserById(args.UserId);
            if (!user.status) throw new Exception("Business does not exist");
            await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var compareFace = await _faceManager.CompareFaces(model.FarmerImage, new ImageVerificationArgs
                    {
                        BusinessCode = business.BusinessCode,
                        FarmerUserId = model.FarmerUserId
                    });

                    if (compareFace != null && compareFace.IsSimilarFace)
                    {
                        //compare with what we have in AWS rekognition
                        //if image matches, proceed with disbursement else, terminate execution
                        var subscription = await _repository.GetPaidSubscriptionByFarmer(business.BusinessCode, model.FarmerUserId, model.Reference);
                        if (subscription == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "Subscription Detail not found");

                        var hasBeenDisbursed = await _repository.GetDisbursedSubscription(business.BusinessCode, model.FarmerUserId, model.Reference);
                        if (hasBeenDisbursed != null && hasBeenDisbursed.InputAlreadyCollected) throw new BaseException(Core.Enums.ExceptionType.OPERATION_FAILED, "Input already collected by farmer");

                        //get bundle detail
                        var bundle = await _warehouseManagementRepository.GetBundleByIdAsync(business.BusinessCode, model.InterestedBundleId);

                        // Validate that each item has a valid WarehouseInputInventoryId
                        foreach (var item in bundle.BundlePackage)
                        {
                            if (item.WarehouseInputInventoryId <= 0)
                                throw new ArgumentException($"Invalid inventory ID for item: {item.Name}");
                            var getInputInventory = await _warehouseManagementRepository.GetInputBYIdAsync(business.BusinessCode, item.WarehouseInputInventoryId);

                            if (getInputInventory == null || getInputInventory.IsAvailable == false)
                                throw new ArgumentException($"This item does not exist: {item.Name}");

                            item.Name = getInputInventory.InputName;
                            //depletes input based on bundle package quantity


                            var quantityToremove = item.Unit * subscription.TotalQuantityPaidFor;
                            if (getInputInventory.UnitBalance < quantityToremove)
                            {
                                throw new BaseException(Core.Enums.ExceptionType.OPERATION_FAILED, "No more stock available");
                            }
                            var depleteInput = await _warehouseManagementRepository.DepleteAsync(getInputInventory.Id, quantityToremove);
                        }
                        bundle.TotalBundlePackageDisbursed = subscription.TotalQuantityPaidFor;


                        var updateBundleSubscription = await _warehouseManagementRepository.UpdateBundleSubscriptions(bundle);
                        model.DisbursedBy = user.model.FirstName;
                        // subscription.Status = EquityPaymentStatus.Disbursed;
                        subscription.EquitySubscriptionStatus = InputFinancingStatus.Disbursed;
                        var updateSubsubcriptionStatus = await _repository.UpdateEquitySubcriiptonStatus(subscription);

                        model.InterestedBundleName = bundle.Name;
                        model.InterestedBundleId = bundle.Id;
                        model.Location = bundle.WarehouseName;
                        model.Reference = subscription.Reference;
                        model.CooperativeId = subscription.CooperativeId;
                        model.BelongToBusinessCode = business.BusinessCode;
                        model.DisbursedBy = model.DisbursedBy;
                        model.DisbursedOn = DateTimeOffset.Now;
                        model.FarmerName = subscription.FarmerName;
                        model.OfficerInChargeUserId = args.UserId;
                        model.TotalQuantityPaidFor = subscription.TotalQuantityPaidFor;
                        model.InputAlreadyCollected = true;

                        model.InputCollectedByPictureOf = compareFace.ImageKey;


                        updateDisbursement = await _repository.UpdateDisbursement(model);

                        await _context.SaveChangesAsync();
                        await dbTransaction.CommitAsync();
                        return updateDisbursement;
                    }
                    //get farmer imagekey from db
                    else
                    {
                        throw new Exception("This image does not match the farmer's profile previously submitted");
                    }

                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    throw new Exception($"{ex.Message}", ex);
                }
            });
            return updateDisbursement;
        }


        public async Task<List<BundleSubscriptionInterestModel>> GetPaidSubscriptionInterestByBusiness(BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
               ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            var requestsByBusiness = await _repository.GetPaidSubscriptionInterestByBusiness(args.businessCode);
            if (!requestsByBusiness.Any()) return null;
            foreach (var request in requestsByBusiness)
            {
                var farmer = await _farmerRepository.GetFarmerByUserId(request.FarmerUserId) ?? throw new Exception("Farmer does not exist");
                var userSterlinWallet = await _walletManager.GetUserSterlingAccount(farmer.UserId);
                if (userSterlinWallet != null)
                {
                    var accouuntBalance = await _walletManager.RetrieveCustomerWallet(request.FarmerUserId, args.Env);
                    if (accouuntBalance != null)
                    {

                        request.AccountBalance = accouuntBalance.availableBalance.ToString();
                    }
                }

            }
            return requestsByBusiness;
        }
        public async Task<List<BundleSubscriptionInterestModel>> GetPaidSubscriptionInterestByBusinessAgent(BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
               ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            var requestsByBusiness = await _repository.GetPaidSubscriptionInterestByBusinessAgent(args.businessCode, args.UserId);
            if (requestsByBusiness == null) return null;


            foreach (var request in requestsByBusiness)
            {
                var farmer = await _farmerRepository.GetFarmerByUserId(request.FarmerUserId) ?? throw new Exception("Farmer does not exist");
                var userSterlinWallet = await _walletManager.GetUserSterlingAccount(farmer.UserId);
                if (userSterlinWallet != null)
                {
                    var accouuntBalance = await _walletManager.RetrieveCustomerWallet(request.FarmerUserId, args.Env);
                    if (accouuntBalance != null)
                    {

                        request.AccountBalance = accouuntBalance.availableBalance.ToString();
                    }
                }

            }
            return requestsByBusiness;
        }

        public async Task<List<BundleDisbursementModel>> GetAllInputDisbursement(BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
               ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            var requestsByBusiness = await _repository.GetDisbursedSubscriptions(args.businessCode);
            if (!requestsByBusiness.Any()) return null;

            return requestsByBusiness;
        }


        public async Task<PagedListResponse<List<BundleDisbursementModel>>> GetDisbursementByBusiness(BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
               ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            var requestsByBusiness = await _repository.GetDisbursementByBusiness(args.businessCode, args.PageNumber, args.PageSize, args.startDate, args.endDate, args.Location, args.WarehouseManger);
            if (requestsByBusiness == null) return null;

            return requestsByBusiness;
        }

        public async Task<PagedListResponse<List<BundleDisbursementModel>>> GetDisbursementByAgent(BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
               ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            var requestsByBusiness = await _repository.GetDisbursementByAgent(args.businessCode, args.UserId, args.PageNumber, args.PageSize, args.startDate, args.endDate, args.Location, args.WarehouseManger);
            if (requestsByBusiness == null) return null;

            return requestsByBusiness;
        }
    }
}
