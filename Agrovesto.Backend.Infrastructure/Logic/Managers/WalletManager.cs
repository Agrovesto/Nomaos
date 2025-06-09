using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Common.WalletServiceRequest;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Individuals;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        public async Task<SubAccountModel> GetSubAccount(string userId)
        {
            return await _walletRepository.GetSubAccount(userId);
        }
        #region intrabankpayout
        public async Task<CafeoneResponseModel<CafeOneCustomerWalletResponse>> PayoutTransferFromUserAccountToBusinessAccount(PostWalletToWalletTransferRequestModel request, string env)
        {
            try
            {

               
                var postIntrabank = await _cafeOneWalletService.PayoutTransferFromUserAccountToBusinessAccount(request, env);
                
               // if (postIntrabank == null) { throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "Not Found"); }

                return postIntrabank;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public async Task<CafeoneResponseModel<CafeOneCustomerWalletResponse>> PayoutTransferFromUserAccountToAgrovestoAccount(PostWalletToWalletTransferRequestModel request, string env)
        {
            try
            {

               
                var postIntrabank = await _cafeOneWalletService.PayoutTransferFromUserAccountToAgrovestoAccount(request, env);
                if (postIntrabank == null) { throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "Not Found"); }

                return postIntrabank;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion intrabankpayout
        public async Task<CafeoneResponseModel<CafeOneCustomerWalletResponse>> TransferFromUserAccountToBusinessAccount(WalletToWalletTransferRequestModel request, string env)
        {
            try
            {

                var userWallet = await _walletRepository.GetSterlingWallet(request.AgrovestoUserId);
                if (userWallet == null) { throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "Not Found"); }

                var retrieveWalletDetails = await _cafeOneWalletService.TransferFromUserAccountToBusinessAccount(request, env);
                if (retrieveWalletDetails == null) { throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "Not Found"); }

                return retrieveWalletDetails;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }  public async Task<CafeoneResponseModel<CafeOneCustomerWalletResponse>> TransferFromUserAccountToAgrovestoAccount(WalletToWalletTransferRequestModel request, string env)
        {
            try
            {

                var userWallet = await _walletRepository.GetSterlingWallet(request.AgrovestoUserId);
                if (userWallet == null) { throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "Not Found"); }

                var retrieveWalletDetails = await _cafeOneWalletService.TransferFromUserAccountToAgrovestoAccount(request, env);
                if (retrieveWalletDetails == null) { throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "Not Found"); }

                return retrieveWalletDetails;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<CafeOneCustomerWalletResponse> RetrieveCustomerWallet(string userId, string env)
        {
            try
            {

                var userWallet = await _walletRepository.GetSterlingWallet(userId);
                if (userWallet == null) { throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "Not Found"); }

                var retrieveWalletDetails = await _cafeOneWalletService.RetrieveCustomerWallet(userWallet.CustomerId, userWallet.WalletId, env);
                if(retrieveWalletDetails != null)
                {
                    var sterlingWallet = await _walletRepository.CreateSterlingWallet(new SterlingBankWalletModel
                    {
                        CustomerId = userWallet.CustomerId,
                        OwnerId = userWallet.OwnerId,
                        UserId = userWallet.UserId,
                        LedgerAccountNumber = userWallet.LedgerAccountNumber,
                        InternalAccountId = userWallet.InternalAccountId,
                        CreatedOn = userWallet.CreatedOn,
                        IsABusinessAccount = userWallet.IsABusinessAccount,
                        OrganizationId = userWallet.OrganizationId,
                        customerTypeId = retrieveWalletDetails.customerTypeId,
                        currencyId = retrieveWalletDetails.currencyId,
                        BankCode = retrieveWalletDetails.virtualAccount.bankCode,
                        walletClassificationId = retrieveWalletDetails.walletClassificationId,
                        AccountName = retrieveWalletDetails.name,
                        BankName = retrieveWalletDetails.virtualAccount.bankName,

                        AccountNumber = retrieveWalletDetails.virtualAccount.accountNumber,
                        AgrovestoUserId = userId,

                        WalletId = retrieveWalletDetails.Id,
                    });
                }


                return retrieveWalletDetails;
            }
            catch (Exception ex)
            {
                return null;
                //throw new Exception(ex.Message);
            }
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

//var bvnVerification = await _identityVerificationManager.VerifyBvn(new BVNDataModel
            //{
            //    number = model.Bvn
            //});
            //if (!bvnVerification.status)
            //{
            //    throw new BaseException(Core.Enums.ExceptionType.OPERATION_FAILED, "Could not verify BVN details");
            //}
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using (var dbTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {

                        var existingCustomer = await _walletRepository.GetSterlingWallet(args.UserId);
                        if (existingCustomer != null && existingCustomer.AccountNumber == null)
                        {
                            var auth = await _walletRepository.GetSubAccount(args.UserId);
                            var updateKYC = await _cafeOneWalletService.UpdateCustomerKyc(new CafeOneCustomerKYCRequest
                            {
                                firstName = auth.FirstName,
                               middleName=auth.MiddleName,
                                CustomerId =existingCustomer.CustomerId,
                                dob = auth.DOB,
                                lastName = auth.LastName,
                                Nin = auth.NIN
                            }, null, args.Env);
                            var createCustomerWallet = await _cafeOneWalletService.CreateCustomerWallet(
                                            new CafeOneCustomerWalletCreationRequest
                                            {
                                                Name = $"{model.FirstName} {model.LastName}",
                                                CustomerId = existingCustomer.CustomerId,
                                                WalletGroupId = null
                                            }
                                           , args.Env);
                     
                            if (createCustomerWallet != null)
                            {
                                var sterlingWallet = await _walletRepository.CreateSterlingWallet(new SterlingBankWalletModel
                                {

                                    CustomerId = existingCustomer.CustomerId,
                                    customerTypeId = createCustomerWallet.customerTypeId,
                                    currencyId = createCustomerWallet.currencyId,
                                    BankCode = createCustomerWallet.virtualAccount.bankCode,
                                    walletClassificationId = createCustomerWallet.walletClassificationId,
                                    AccountName = createCustomerWallet.name,
                                    BankName = createCustomerWallet.virtualAccount.bankName,

                                    AccountNumber = createCustomerWallet.virtualAccount.accountNumber,
                                    AgrovestoUserId = args.UserId,
                                    CreatedOn = DateTime.UtcNow,
                                    // InternalAccountId = createCustomerWallet.InternalAccountId,
                                    IsABusinessAccount = true,
                                    // LedgerAccountNumber = createCustomerWallet.LedgerAccountNumber,
                                    OrganizationId = existingCustomer.OrganizationId,
                                    OwnerId = existingCustomer.CustomerId,
                                    UserId = existingCustomer.CustomerId,
                                    WalletId = createCustomerWallet.Id,
                                });

                                model.WalletCode = createCustomerWallet.Id;
                                model.SubAccountName = $"{model.FirstName} {model.LastName}";
                                model.UserId = args.UserId;
                                model.IsMainAccount = true;
                                model.CreatedBy = business.BusinessName;
                                model.OnBoardedOn = DateTimeOffset.Now;
                                model.BusinessId = business.BusinessId;
                                
                                model.Status = BusinessStatus.Live.ToString();
                                var createSubaccount = await _walletRepository.UpdateSubAccount(model);

                            }

                        }
                        else
                        {

                          DateTime date = DateTime.ParseExact(model.DOB, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        string dobOfBirth = date.ToString("dd-MM-yyyy");
                         model.DOB = dobOfBirth;

                        model.DOB = ConvertedDate(model.DOB);
                        var createCustomer = await _cafeOneWalletService.CreateCustomer(new CafeoneCustomerRequest
                        {
                            mobileNumber =  model.PhoneNumber,
                            firstName = model.FirstName,
                            middleName=model.MiddleName,
                            lastName = model.LastName,
                            dob = model.DOB,
                            bvnNumber = model.Bvn,
                            EmailAddress = business.BusinessEmail,
                            nin = model.NIN,
                            Address = model.Address,
                            Alias = model.Alias,
                            City=model.City,
                            
                        }, args.Env);


                        //if successful, create subaccount
                        if (createCustomer != null)
                        {
                            var createCustomerWallet = await _cafeOneWalletService.CreateCustomerWallet(
                                new CafeOneCustomerWalletCreationRequest
                                {
                                    Name = $"{model.FirstName} {model.LastName}",
                                    CustomerId = createCustomer.Id,
                                    WalletGroupId = null
                                }
                               , args.Env);

                            if (createCustomerWallet != null)
                            {
                                var sterlingWallet = await _walletRepository.CreateSterlingWallet(new SterlingBankWalletModel
                                {                                 

                                    CustomerId = createCustomer.Id,
                                    customerTypeId = createCustomerWallet.customerTypeId,
                                    currencyId = createCustomerWallet.currencyId,
                                    BankCode = createCustomerWallet.virtualAccount.bankCode,
                                    walletClassificationId = createCustomerWallet.walletClassificationId,
                                    AccountName = createCustomerWallet.name,
                                    BankName = createCustomerWallet.virtualAccount.bankName,

                                    AccountNumber = createCustomerWallet.virtualAccount.accountNumber,
                                    AgrovestoUserId = args.UserId,
                                    CreatedOn = DateTime.UtcNow,
                                    // InternalAccountId = createCustomerWallet.InternalAccountId,
                                    IsABusinessAccount = true,
                                    // LedgerAccountNumber = createCustomerWallet.LedgerAccountNumber,
                                    OrganizationId = createCustomer.OrganizationId,
                                    OwnerId = createCustomer.Id,
                                    UserId = createCustomer.Id,
                                    WalletId = createCustomerWallet.Id,
                                });

                                model.WalletCode = createCustomerWallet.Id;
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

                        }

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
        public async Task<SterlingBankWalletModel> GetUserSterlingAccount(string userId)
        {
            return await _walletRepository.GetSterlingWallet(userId);
        }
        private static string ExtractDomain(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                return string.Empty;
            }
            string[] parts = email.Split('@');
            return parts.Length == 2 ? parts[1] : string.Empty;
        }
        private static string GenerateEmail(string fullName, string domain)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(domain))
            {
                throw new ArgumentException("Invalid input provided.");
            }
            domain = ExtractDomain(domain);
            fullName = Regex.Replace(fullName.Trim(), @"[^a-zA-Z\s]", "");

            string[] nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (nameParts.Length == 0)
            {
                throw new ArgumentException("Name must contain at least one word.");
            }

            string firstName = nameParts[0];
            string lastName = nameParts.Length > 1 ? nameParts[^1] : "user";

            firstName = firstName.ToLower();
            lastName = lastName.ToLower();

            return $"{firstName}.{lastName}@{domain}";
        }
        public async Task<SubAccountModel> UpdateUserProfile(SubAccountModel model, ParameterArgs args)
        {
            model.City = !string.IsNullOrEmpty(model.City) ? model.City : model.Address;
            var user = await _walletRepository.GetSubAccount(model.UserId);
            //if(user !=null && user.WalletCode !=null)
            //{
            //    var createSubaccount = await _walletRepository.UpdateSubAccount(model);                
            //}
            var business = await _businessRepository.GetBusinessByIdAsync(user.BusinessId);

            if (business == null) { throw new Exception($"Business does not exist"); }
            var strategy = _context.Database.CreateExecutionStrategy();
            model.Email = !string.IsNullOrEmpty(model.Email) ? model.Email : GenerateEmail(model.FirstName, business.BusinessEmail);

          
            string firstName = "";
            string lastName = "";
           
            //var nameParts = model.FirstName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            //if (nameParts.Length == 1)
            //{
            //    firstName = nameParts[0];
            //    model.FirstName = firstName;
            //}
            //else if (nameParts.Length >= 2)
            //{
            //    firstName = nameParts[0];
            //    model.FirstName = firstName;
            //    lastName = nameParts[^1];
            //    model.LastName = lastName;
            //}
            await strategy.ExecuteAsync(async () =>
            {
                using (var dbTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var existingCustomer = await _walletRepository.GetSterlingWallet(args.UserId);


                        if (existingCustomer != null && existingCustomer.AccountNumber == null)
                        {
                            var auth = await _walletRepository.GetSubAccount(args.UserId);
                            var updateKYC = await _cafeOneWalletService.UpdateCustomerKyc(new CafeOneCustomerKYCRequest
                            {
                                firstName = auth.FirstName,
                                middleName = auth.MiddleName,
                                CustomerId = existingCustomer.CustomerId,
                                dob = auth.DOB,
                                lastName = auth.LastName,
                                Nin = auth.NIN
                            }, null, args.Env);
                            var createCustomerWallet = await _cafeOneWalletService.CreateCustomerWallet(
                                            new CafeOneCustomerWalletCreationRequest
                                            {
                                                Name = $"{model.FirstName} {model.LastName}",
                                                CustomerId = existingCustomer.CustomerId,
                                                WalletGroupId = null
                                            }
                                           , args.Env);

                            if (createCustomerWallet != null)
                            {
                                var sterlingWallet = await _walletRepository.CreateSterlingWallet(new SterlingBankWalletModel
                                {

                                    CustomerId = existingCustomer.CustomerId,
                                    customerTypeId = createCustomerWallet.customerTypeId,
                                    currencyId = createCustomerWallet.currencyId,
                                    BankCode = createCustomerWallet.virtualAccount.bankCode,
                                    walletClassificationId = createCustomerWallet.walletClassificationId,
                                    AccountName = createCustomerWallet.name,
                                    BankName = createCustomerWallet.virtualAccount.bankName,

                                    AccountNumber = createCustomerWallet.virtualAccount.accountNumber,
                                    AgrovestoUserId = args.UserId,
                                    CreatedOn = DateTime.UtcNow,
                                    // InternalAccountId = createCustomerWallet.InternalAccountId,
                                    IsABusinessAccount = true,
                                    // LedgerAccountNumber = createCustomerWallet.LedgerAccountNumber,
                                    OrganizationId = existingCustomer.OrganizationId,
                                    OwnerId = existingCustomer.CustomerId,
                                    UserId = existingCustomer.CustomerId,
                                    WalletId = createCustomerWallet.Id,
                                });

                                model.WalletCode = createCustomerWallet.Id;
                                model.SubAccountName = $"{model.FirstName} {model.LastName}";
                                model.UserId = args.UserId;
                                model.IsMainAccount = true;
                                model.CreatedBy = business.BusinessName;
                                model.OnBoardedOn = DateTimeOffset.Now;
                                model.BusinessId = business.BusinessId;

                                model.Status = BusinessStatus.Live.ToString();
                                var createSubaccount = await _walletRepository.UpdateSubAccount(model);

                            }

                        }
                        else { 
                        var createCustomer = await _cafeOneWalletService.CreateCustomer(new CafeoneCustomerRequest
                        {
                            mobileNumber = model.PhoneNumber,
                            firstName = model.FirstName,
                            lastName = model.LastName,
                            middleName=model.MiddleName,
                            dob = model.DOB,
                            bvnNumber = model.Bvn,
                            EmailAddress = model.Email,
                            nin = model.NIN,
                            Address = model.Address,
                            Alias = model.Alias,
                            City = model.City,
                        }, args.Env);

                        //if successful, create subaccount
                        if (createCustomer != null)
                        {
                            var createCustomerWallet = await _cafeOneWalletService.CreateCustomerWallet( new CafeOneCustomerWalletCreationRequest
                            {
                                Name = $"{model.FirstName} {model.LastName}",
                                CustomerId= createCustomer.Id,
                                WalletGroupId=null
                            }
                               , args.Env);

                            if (createCustomerWallet != null)
                            {
                                model.WalletCode = createCustomerWallet.Id;
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
                                    CustomerId = createCustomer.Id,
                                    customerTypeId=createCustomerWallet.customerTypeId,
                                   currencyId=createCustomerWallet.currencyId,
                                   BankCode=createCustomerWallet.virtualAccount.bankCode,
                                   walletClassificationId=createCustomerWallet.walletClassificationId,
                                   AccountName= createCustomerWallet.name,
                                   BankName=createCustomerWallet.virtualAccount.bankName,
                                   
                                    AccountNumber = createCustomerWallet.virtualAccount.accountNumber,
                                    AgrovestoUserId = args.UserId,
                                    CreatedOn = DateTime.UtcNow,
                                   // InternalAccountId = createCustomerWallet.InternalAccountId,
                                    IsABusinessAccount = false,
                                   // LedgerAccountNumber = createCustomerWallet.LedgerAccountNumber,
                                    OrganizationId = createCustomer.OrganizationId,
                                    OwnerId = createCustomer.Id,
                                    UserId = createCustomer.Id,
                                    WalletId = createCustomerWallet.Id,
                                });

                            }

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
