using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Individuals;
using System;
using System.Collections.Generic;
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
        public WalletManager(ICafeOneWalletService cafeOneWalletService, IBusinessRepository businessRepository, IAuthenticationRepository authenticationRepository,
            DataContext context, IWalletRepository walletRepository)
        {
                _cafeOneWalletService = cafeOneWalletService;
            _businessRepository = businessRepository;
            _authenticationRepository = authenticationRepository;
            _context = context;
            _walletRepository = walletRepository;   
        }

        public async Task<SubAccountModel> UpdateBusinessProfile(SubAccountModel model, ParameterArgs args)
        {

            //var user = await _authenticationRepository.GetUserById(args.UserId);
            var business = await _businessRepository.GetBusinessByUserIdAsync(args.UserId);

            if (business == null) { throw new Exception($"Business does not exist"); }

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
                                AccountNumber= createCustomerWallet.AccountNumber,
                                AgrovestoUserId =args.UserId,
                                CreatedOn = DateTime.UtcNow,
                                InternalAccountId = createCustomerWallet.InternalAccountId,
                                IsABusinessAccount = true,
                                LedgerAccountNumber = createCustomerWallet.LedgerAccountNumber,
                                OrganizationId = createCustomer.OrganizationId,
                                OwnerId= createCustomer.UserId,
                                UserId = createCustomer.UserId,
                                WalletId= createCustomerWallet.WalletId,
                            });

                            model.WalletCode = createCustomerWallet.WalletId;
                            model.SubAccountName = $"{model.FirstName} {model.LastName}";
                            model.UserId = args.UserId;
                            model.IsMainAccount = true;
                            model.CreatedBy = business.BusinessName;
                            model.CreatedOn = DateTimeOffset.Now;
                            model.BusinessId = business.BusinessId;

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

        }


        public async Task<SubAccountModel> UpdateUserProfile(SubAccountModel model, ParameterArgs args)
        {

            var user = await _walletRepository.GetSubAccount(args.UserId);

            var business = await _businessRepository.GetBusinessByIdAsync(user.BusinessId);

            if (business == null) { throw new Exception($"Business does not exist"); }

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
                        email = business.BusinessEmail,
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
                            model.CreatedOn = DateTimeOffset.Now;
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

        }
    }
}
