using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories
{
    public  class WalletRepository : IWalletRepository
    {
        private readonly DataContext _context;
        public WalletRepository(DataContext context)
        {
            _context = context;
        }
        public async Task<SubAccountModel> CreateSubAccount(SubAccountModel model)
        {
            SubAccount entity = model.Map();
            _context.Set<SubAccount>().Add(entity);

            int count = await _context.SaveChangesAsync();
            if (count < 1)
            {
                return null;
            }
            return entity.Map();
        }

        public async Task<SubAccountModel> UpdateSubAccount(SubAccountModel model)
        {
            var account = await _context.Set<SubAccount>().Where(x => x.UserId == model.UserId).FirstOrDefaultAsync();
           
            if (account == null)
            {
                return null;
            }
            account.BankAccount = model.BankAccount;
            account.BankName = model.BankName;
            account.Bvn = model.Bvn;
            account.DOB = model.DOB;
            account.FirstName = model.FirstName;
            account.LastName = model.LastName;
            account.MiddleName = model.MiddleName;
            account.WalletCode = model.WalletCode;
            account.IsMainAccount = false;
            account.NIN = model.NIN;
            account.Status = BusinessStatus.Registered.ToString();//model.Status;
            account.SubAccountName = model.SubAccountName;  
           

            await _context.SaveChangesAsync();
            return account.Map();
        }

        public async Task<SubAccountModel> GetSubAccount(string userId)
        {
            var entity = await _context.Set<SubAccount>()

                .FirstOrDefaultAsync(x => x.UserId == userId);
            if (entity == null)
            {
                return null;
            }
            return entity.Map();
        }
       
        public async Task<SterlingBankWalletModel> CreateSterlingWallet(SterlingBankWalletModel model)
        {
            
            //SterlingBankWallet entity = model.Map();
            var entity = await _context.SterlingBankWallets.Where(x => x.AgrovestoUserId == model.AgrovestoUserId).FirstOrDefaultAsync();
            if(entity == null)
            {
                entity = model.Map();
                _context.Set<SterlingBankWallet>().Add(entity);
            }
            else
            {
                entity.AccountName = model.AccountName;
                entity.AccountNumber = model.AccountNumber;
                entity.BankCode = model.BankCode;
                entity.BankName = model.BankName;
                entity.currencyId = model.currencyId;
                entity.CustomerId = model.CustomerId;
                entity.customerTypeId = model.customerTypeId;
                entity.InternalAccountId = model.InternalAccountId;
                entity.LedgerAccountNumber = model.LedgerAccountNumber;
                entity.OwnerId = model.OwnerId;
                entity.UserId = model.UserId;
                entity.walletClassificationId = model.walletClassificationId;
                entity.WalletId = model.WalletId;
                
            }
          

            int count = await _context.SaveChangesAsync();
            if (count < 1)
            {
                return null;
            }
            return entity.Map();
        }

        public async Task<SterlingBankWalletModel[]> GetSterlingWallets()
        {
            SterlingBankWallet[] entities = await _context.Set<SterlingBankWallet>()

                .OrderBy(x => x.CreatedOn).ToArrayAsync();

            return entities.Select(x => x.Map()).ToArray();
        }

       

        public async Task<SterlingBankWalletModel> GetSterlingWallet(string agrovestoUserId)
        {
            SterlingBankWallet entity = await _context.Set<SterlingBankWallet>()

                .FirstOrDefaultAsync(x => x.AgrovestoUserId == agrovestoUserId);
            if (entity == null)
            {
                return null;
            }
            return entity.Map();
        }



        public async Task<WalletModel> CreateUserWallet(WalletModel model)
        {
            Wallet entity = model.Map();
            _context.Set<Wallet>().Add(entity);

            int count = await _context.SaveChangesAsync();
            if (count < 1)
            {
                return null;
            }
            return entity.Map();
        }

        public async Task<WalletModel> GetUserWalletByProviderAsync(string userId, string walletProviderCode)
        {
            var entity = await _context.Set<Wallet>()

                .FirstOrDefaultAsync(x => x.UserId == userId && x.WalletProviderCode== walletProviderCode);
            if (entity == null)
            {
                return null;
            }
            return entity.Map();
        }

    }
}
