using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
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
        private readonly IFaceManager _faceManager;
        private readonly IConfiguration _configuration;
        public InputFinancingManagement(IInputFinancingManagementRepository repository,
            IFaceManager faceManager,IBusinessRepository businessRepository, IAuthenticationRepository authenticationRepository,
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
            _faceManager = faceManager;
            _whisperSMSService = whisperSMSService;
        }
        public async Task<Response<List<SubscriptionData>>> FarmerBundleSubscriptionsMetric(ListQueryParams query)
        {
            try
            {
                var business = await _businessRepository.GetBusinessByUserIdAsync(query.UserId);

                if (business == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

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
        public async Task<List<BundleSubscriptionInterestModel>> GetInterestRequestByAgent(BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
               ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            var requestsByBusiness = await _repository.GetInterestRequestByAgent(args.businessCode, args.UserId);
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


        //FarmerBundleSubscriptionInterests
        public async Task<Response<List<SubscriptionData>>> FarmerBundleSubscriptions(ListQueryParams query)
        {
            try
            {
                var business = await _businessRepository.GetBusinessByUserIdAsync(query.UserId);

                if (business == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

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
                    if (!bundleOfInterest.Any()) { throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST, "No pending equity payment for selected farmer"); }
                  
                        var sterlingCharge = decimal.Parse(_configuration["Sterling:Charge"]);
                        var selectedBundle = bundleOfInterest.Where(x => x.InterestedBundleId == model.InterestedBundleId).FirstOrDefault();
                    var totalSettlment = getInputInventory.BundleEquityPlusCommission != 0m
   ? getInputInventory.BundleEquityPlusCommission
   : getInputInventory.BundlePrice;

                    var settlementSplitAmountBusiness = getInputInventory.BundleEquityPrice !=0m ? getInputInventory.BundleEquityPrice : 
                    getInputInventory.BundlePrice;
                        var settlementPlatformSplitAmount = getInputInventory.CommissionPrice; //- sterlingCharge;

                        //TODO: REMOVE THIS LINE WHEN DEPLOYING
                        //TODO: REMOVE THIS
                        farmerWallet.Balance = "100000";

                        if (totalSettlment != (settlementSplitAmountBusiness + settlementPlatformSplitAmount))
                        {
                            throw new BaseException(Core.Enums.ExceptionType.OPERATION_FAILED, "Settlement Validation mismatched");
                        }

                        if (selectedBundle == null && (decimal.Parse(farmerWallet.Balance) < (selectedBundle.ExpectedTotalEquityPrice - sterlingCharge)))
                        {
                            throw new BaseException(Core.Enums.ExceptionType.OPERATION_FAILED, "Insufficient amount in user wallet for the selected bundle");
                        }
                            var reference = $"Bun{getInputInventory.Id}-{selectedBundle.QuantityOfInterest}-{business.BusinessCode}-{ReferenceUtil.GenerateTransferReference()}";

                            var createBusinessSettlement = await _cafeOneWalletService.UserWalletToBusinessWalletTransfer(new WalletToWalletTransferRequest
                            {
                                Amount = settlementSplitAmountBusiness,
                                BusinessAccountNumber = businessAccount.InternalAccountId,
                                FromAccount = userSterlinWallet.InternalAccountId,
                                Env = args.Env,
                                Memo = $"Input finance subscription for {getInputInventory.Name} by {farmer.Name} on {DateTimeOffset.Now}",
                                OwnerId = userSterlinWallet.OwnerId,
                                Reference = reference
                            });
                            _log.LogInformation($"Response from user transfer to User Business {business.BusinessCode} Wallet", JsonConvert.SerializeObject(createBusinessSettlement));
                            var @ref = $"Bun{getInputInventory.Id}-{selectedBundle.QuantityOfInterest}-{business.BusinessCode}-{ReferenceUtil.GenerateTransferReference()}";

                            var createPlatformSettlement = await _cafeOneWalletService.UserWalletToAgrovestoTransfer(@ref,
                                userSterlinWallet.OwnerId, settlementPlatformSplitAmount, userSterlinWallet.InternalAccountId, args.Env);
                            _log.LogInformation("Response from user transfer to Agrovesto Wallet", JsonConvert.SerializeObject(createPlatformSettlement));
                        //log reeponse
                        if (createBusinessSettlement.Data == null && (createPlatformSettlement.Data == null))
                        {
                            throw new BaseException(Core.Enums.ExceptionType.OPERATION_FAILED, "Settlement challenge: Could not confirm payment");
                        }
                    selectedBundle.ExpectedEquityPricePerBundle = selectedBundle.ExpectedEquityPricePerBundle != 0m ? selectedBundle.ExpectedEquityPricePerBundle : 1;
                                var status = createBusinessSettlement.Status;
                                var platformstatus = createPlatformSettlement.Status;
                                model.Status = status == "success" ? EquityPaymentStatus.Paid : EquityPaymentStatus.Failed;
                                model.PaymentDate = status == "success" ? DateTimeOffset.Now : null;
                                model.IsCommissionPaidOut = platformstatus == "success" ? true : false;
                                model.TotalQuantityPaidFor = (long)(settlementSplitAmountBusiness / selectedBundle.ExpectedEquityPricePerBundle);
                                model.TotalEquityPaid = settlementSplitAmountBusiness;

                                model.Reference = reference;

                                //update payment
                                //notify farmer
                                //notify business

                                var update = await _repository.UpdateSubscriptionPayment(model);

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
                                return (true, "Payment Updated");   
               
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
            var status = true;
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {

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
                        model.CommissionPrice = getInputInventory.CommissionPrice;//(getInputInventory.CommissionPrice * model.QuantityOfInterest);
                                                                                  //model.EquityPlusCommission = (getInputInventory.BundlePrice * model.QuantityOfInterest) + getInputInventory.CommissionPrice
                    }

                    model.ExpectedEquityPricePerBundle = getInputInventory.BundleEquityPrice; //if equity price is available use equityprice

                    getInputInventory.BundleEquityPrice= getInputInventory.BundleEquityPrice != 0m
   ? getInputInventory.BundleEquityPrice
   : getInputInventory.BundlePrice;

                    getInputInventory.BundleEquityPlusCommission = getInputInventory.BundleEquityPlusCommission != 0m
   ? getInputInventory.BundleEquityPlusCommission
   : getInputInventory.BundlePrice;
                    var expectedPayment = ((getInputInventory.BundleEquityPrice * model.QuantityOfInterest) + model.CommissionPrice);

                    if (expectedPayment < getInputInventory.BundleEquityPlusCommission)
                    {
                        throw new BaseException(Core.Enums.ExceptionType.OPERATION_FAILED, "Bundle equity value mismatched");
                    }
                    model.ExpectedTotalEquityPrice = expectedPayment + sterlingCharge;


                    //after interest created, call, user sterling bank and display amount to pay
                    var userSterlinWallet = await _walletRepository.GetSterlingWallet(farmer.UserId);
                    if(userSterlinWallet == null)
                    {
                        throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "Customer account not found, kindly update your profile");
                    }

                        var userWallet = await _cafeOneWalletService.GetUserAccount(userSterlinWallet.InternalAccountId, args.Env);
                    if(userWallet.data == null)
                    {
                        throw new Exception(userWallet.message);
                    }   
                   
                            getInputInventory.TotalBundlePackageRequested = model.QuantityOfInterest;
                            var updateBundleSubscription = await _warehouseManagementRepository.UpdateBundleSubscriptions(getInputInventory);
                            //update bundke
                            var createWarehouse = await _repository.CreateInputFinancingInterestThroughBundle(model);

                            await _context.SaveChangesAsync();
                            await dbTransaction.CommitAsync();

                            response.AccountName = userWallet.data.ProviderAccountName;
                            response.AccountNUmber = userWallet.data.AccountNumber;
                            response.BankName = userWallet.data.ProviderBankName;
                            response.Amount = createWarehouse.data.ExpectedTotalEquityPrice ;
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
                            status = true;
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


        //method get all successful payment for input subscription
        //method disburse input bundle to farmers and mark disbursement as completed,, deplete inventory in the warehouse based on what has been disbursed
        public async Task<BundleDisbursementModel> UpdateInputDisbursement(BundleDisbursementModel model, BusinessRegistrationArgs args)
        {
            
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            var updateDisbursement = new BundleDisbursementModel(); ;
            var strategy = _context.Database.CreateExecutionStrategy();

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

                    if(compareFace !=null && compareFace.IsSimilarFace)
                    {                        
                        //compare with what we have in AWS rekognition
                        //if image matches, proceed with disbursement else, terminate execution

                        var hasBeenDisbursed = await _repository.GetDisbursedSubscription(business.BusinessCode, model.FarmerUserId, model.Reference);
                        if (hasBeenDisbursed != null || hasBeenDisbursed.InputAlreadyCollected) throw new BaseException(Core.Enums.ExceptionType.OPERATION_FAILED, "Input already collected by farmer");

                        var subscription = await _repository.GetPaidSubscriptionByFarmer(business.BusinessCode, model.FarmerUserId, model.Reference);
                        if (subscription == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "Subscription Detail not found");
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
                            var quantity = model.TotalQuantityPaidFor;
                            var quantityToremove = item.Unit * quantity;
                            if (getInputInventory.UnitBalance < quantityToremove)
                            {
                                throw new BaseException(Core.Enums.ExceptionType.OPERATION_FAILED, "No more stock available");
                            }
                            var depleteInput = await _warehouseManagementRepository.DepleteAsync(getInputInventory.Id, quantityToremove);
                        }
                        bundle.TotalBundlePackageDisbursed = model.TotalQuantityPaidFor;
                        var updateBundleSubscription = await _warehouseManagementRepository.UpdateBundleSubscriptions(bundle);

                        subscription.Status = EquityPaymentStatus.Disbursed;
                        var updateSubsubcriptionStatus = await _repository.UpdateEquitySubcriiptonStatus(subscription);

                        model.InterestedBundleName = bundle.Name;
                        model.InterestedBundleId = bundle.Id;
                        model.Location = model.Location;
                        model.Reference = subscription.Reference;
                        model.CooperativeId = subscription.CooperativeId;
                        model.InputCollectedByPictureOf = model.InputCollectedByPictureOf;
                        model.BelongToBusinessCode = business.BusinessCode;
                        model.DisbursedBy = model.DisbursedBy;
                        model.DisbursedOn = DateTimeOffset.Now;
                        model.FarmerName = subscription.FarmerName;
                        model.OfficerInChargeUserId = args.UserId;
                        model.TotalQuantityPaidFor = model.TotalQuantityPaidFor;
                        model.InputAlreadyCollected = true;
                        model.InputCollectedByPictureOf = compareFace.ImageKey;


                        updateDisbursement = await _repository.UpdateDisbursement(model);

                        await _context.SaveChangesAsync();
                        await dbTransaction.CommitAsync();
                      
                    }
                    //get farmer imagekey from db
                    else
                    {
                        throw new Exception("No record was founding matching the captured user image");
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
        public async Task<List<BundleDisbursementModel>> GetAllInputDisbursement(BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
               ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            var requestsByBusiness = await _repository.GetDisbursedSubscriptions(args.businessCode);
            if (!requestsByBusiness.Any()) return null;
           
            return requestsByBusiness;
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
                var userSterlinWallet = await _walletRepository.GetSterlingWallet(farmer.UserId);
                if (userSterlinWallet != null)
                {

                    var accouuntBalance = await _cafeOneWalletService.GetUserBalance(userSterlinWallet.WalletId, args.Env);
                    if (accouuntBalance != null)
                    {
                        request.AccountBalance = accouuntBalance.Balance;
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
                var userSterlinWallet = await _walletRepository.GetSterlingWallet(farmer.UserId);
                if (userSterlinWallet != null)
                {

                    var accouuntBalance = await _cafeOneWalletService.GetUserBalance(userSterlinWallet.WalletId, args.Env);
                    request.AccountBalance = accouuntBalance.Balance;
                }

            }
            return requestsByBusiness;
        }

    }
}
