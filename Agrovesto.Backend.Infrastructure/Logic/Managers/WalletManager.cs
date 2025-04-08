using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Individuals;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers
{
    public class WalletManager : IWalletManager
    {
        private readonly ICafeOneWalletService _cafeOneWalletService;
        private readonly IBusinessRepository _businessRepository;
        private readonly IAuthenticationRepository _authenticationRepository;
        private readonly IWalletRepository _walletRepository;
        private readonly DataContext _context;
        private readonly IIdentityVerificationManager _identityVerificationManager;
        public WalletManager(ICafeOneWalletService cafeOneWalletService, IBusinessRepository businessRepository,
            IIdentityVerificationManager identityVerificationManager,
            IAuthenticationRepository authenticationRepository,
            DataContext context, IWalletRepository walletRepository)
        {
                _cafeOneWalletService = cafeOneWalletService;
            _businessRepository = businessRepository;
            _authenticationRepository = authenticationRepository;
            _identityVerificationManager = identityVerificationManager;
            _context = context;
            _walletRepository = walletRepository;   
        }

        private string ConvertedDate(string date)
        {
            string[] dateFormats = {
            "dd-MMM-yyyy", "dd-MM-yyyy", "dd/MM/yyyy", "yyyy-MM-dd",
            "MM/dd/yyyy", "MMM dd, yyyy", "yyyy/MM/dd"
        };

            string dateOfBirth = date; // Example input, change to test other formats

            if (DateTime.TryParseExact(dateOfBirth, dateFormats,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                string formattedDate = parsedDate.ToString("yyyy-MM-dd");
                return formattedDate;
            }
            else
            {
                throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST, "Invalid date of birth");
            }
        }
        public async Task<SubAccountModel> UpdateBusinessProfile(SubAccountModel model, ParameterArgs args)
        {

            //var user = await _authenticationRepository.GetUserById(args.UserId);
            var business = await _businessRepository.GetBusinessByUserIdAsync(args.UserId);

            if (business == null) { throw new Exception($"Business does not exist"); }

            var bvnVerification = await _identityVerificationManager.VerifyBvn(new BVNDataModel
            {
                number = model.Bvn
            });
            if (!bvnVerification.status)
            {
                throw new BaseException(Core.Enums.ExceptionType.OPERATION_FAILED, "Could not verify BVN details");
            }
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using (var dbTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var createCustomer = await _cafeOneWalletService.CreateCustomer(new CafeoneCustomerRequest
                        {
                            phoneNumber =  model.PhoneNumber,
                            firstName = model.FirstName,
                            lastName = model.LastName,
                            dob = model.DOB,
                            bvnNumber = model.Bvn,
                            email = business.BusinessEmail,
                            nin = model.NIN
                        }, args.Env);

                        //if successful, create subaccount
                        if (createCustomer != null)
                        {
                            var createCustomerWallet = await _cafeOneWalletService.CreateWallet(
                                business.BusinessName, args.Env);

                            if (createCustomerWallet != null)
                            {
                                var sterlingWallet = await _walletRepository.CreateSterlingWallet(new SterlingBankWalletModel
                                {
                                    AccountNumber = createCustomerWallet.AccountNumber,
                                    AgrovestoUserId = args.UserId,
                                    CreatedOn = DateTime.UtcNow,
                                    InternalAccountId = createCustomerWallet.InternalAccountId,
                                    IsABusinessAccount = true,
                                    LedgerAccountNumber = createCustomerWallet.LedgerAccountNumber,
                                    OrganizationId = createCustomer.OrganizationId,
                                    OwnerId = createCustomer.UserId,
                                    UserId = createCustomer.UserId,
                                    WalletId = createCustomerWallet.WalletId,
                                });

                                model.WalletCode = createCustomerWallet.WalletId;
                                model.SubAccountName = $"{model.FirstName} {model.LastName}";
                                model.UserId = args.UserId;
                                model.IsMainAccount = true;
                                model.CreatedBy = business.BusinessName;
                                model.OnBoardedOn = DateTimeOffset.Now;
                                model.BusinessId = business.BusinessId;

                                model.Status = BusinessStatus.Live.ToString();
                                var createSubaccount = await _walletRepository.CreateSubAccount(model);

                            }
                        }

                        await _context.SaveChangesAsync();
                        await dbTransaction.CommitAsync();


                        //send email to business on their wallet creation
                        return model;
                    }
                    catch (Exception ex)
                    {

                        await dbTransaction.RollbackAsync();

                        throw new Exception(ex.Message);
                    }
                }
            });
            return model;
        }

        public async Task<SubAccountModel> CreateSubAccount(SubAccountModel model)
        {
            return await _walletRepository.CreateSubAccount(model);
        }
        public async Task<SubAccountModel> GetSubAccountByUserId(string userId)
        {
            return await _walletRepository.GetSubAccount(userId);
        }
        public async Task<SubAccountModel> UpdateUserProfile(SubAccountModel model, ParameterArgs args)
        {

            var user = await _walletRepository.GetSubAccount(model.UserId);
            if(user !=null && user.WalletCode !=null)
            {
                var createSubaccount = await _walletRepository.UpdateSubAccount(model);                
            }
            var business = await _businessRepository.GetBusinessByIdAsync(user.BusinessId);

            if (business == null) { throw new Exception($"Business does not exist"); }
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using (var dbTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {

                        var createCustomer = await _cafeOneWalletService.CreateCustomer(new CafeoneCustomerRequest
                        {
                            phoneNumber = model.PhoneNumber,
                            firstName = model.FirstName,
                            lastName = model.LastName,
                            dob = model.DOB,
                            bvnNumber = model.Bvn,
                            email = model.Email,
                            nin = model.NIN
                        }, args.Env);

                        //if successful, create subaccount
                        if (createCustomer != null)
                        {
                            var createCustomerWallet = await _cafeOneWalletService.CreateBusinessUserWallet(
                                $"{model.FirstName} {model.LastName}", args.Env, business.UserId);

                            if (createCustomerWallet != null)
                            {
                                model.WalletCode = createCustomerWallet.WalletId;
                                model.SubAccountName = $"{model.FirstName} {model.LastName}";
                                model.UserId = args.UserId;
                                model.IsMainAccount = true;
                                model.CreatedBy = business.BusinessName;
                                model.OnBoardedOn = DateTimeOffset.Now;
                               // model.SubAccountType = Enum.GetName(typeof(SubAccountType), model.s)
                                //  model.BusinessId = business.BusinessId;

                                var createSubaccount = await _walletRepository.UpdateSubAccount(model);
                                var sterlingWallet = await _walletRepository.CreateSterlingWallet(new SterlingBankWalletModel
                                {
                                    AccountNumber = createCustomerWallet.AccountNumber,
                                    AgrovestoUserId = args.UserId,
                                    CreatedOn = DateTime.UtcNow,
                                    InternalAccountId = createCustomerWallet.InternalAccountId,
                                    IsABusinessAccount = true,
                                    LedgerAccountNumber = createCustomerWallet.LedgerAccountNumber,
                                    OrganizationId = createCustomer.OrganizationId,
                                    OwnerId = createCustomer.UserId,
                                    UserId = createCustomer.UserId,
                                    WalletId = createCustomerWallet.WalletId,
                                });

                            }

                        }

                        await _context.SaveChangesAsync();
                        await dbTransaction.CommitAsync();


                        //send email to business on their wallet creation
                        return model;
                    }
                    catch (Exception ex)
                    {

                        await dbTransaction.RollbackAsync();

                        throw new Exception(ex.Message);
                    }
                }
            });
            return model;
        }
    }
}
