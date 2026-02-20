using Agrovesto.Backend.Core.Models.Businesses.Agrovesto.Application.DTOs.Farms;
using Agrovesto.Backend.Core.Models.Businesses.Wallets;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories.V2
{
    public interface IUserWalletRepository
    {
        // Define methods that the UserWalletRepository should implement
        Task<bool> CloseUserWallet(long walletId, string closedByUserId, string reason = "User requested wallet closure");
        Task<UserWalletModel> CreateUserWallet(UserWalletModel request, UserWalletParams args);
    }
    public class UserWalletRepository : IUserWalletRepository
    {
        private readonly DataContext _context;
        public UserWalletRepository(DataContext context)
        {
            _context = context;

        }
        public async Task<List<UserWalletModel>> GetUserWallletsByUserId(UserWalletParams args)
        {
           var query = await _context.UserWallets
                .Where(uw => uw.UserId == args.UserId 
                && uw.BelongToBusinessCode == args.businessCode)
                .Select(uw => new UserWalletModel
                {
                    Id = uw.Id,
                    UserId = uw.UserId,
                    WalletProviderId = uw.WalletProviderId,
                    WalletBalance = uw.WalletBalance,
                    Currency = uw.Currency,
                    IsDefault = uw.IsDefault,
                    IsActive= uw.IsActive,
                    WalletNumber= uw.WalletNumber,
                    ProviderName= uw.WalletProvider.Name
                })
                .ToListAsync();
            return query;
        }
        public async Task<UserWalletModel> CreateUserWallet(
        UserWalletModel request,
        UserWalletParams args)
        {
            var exists = await _context.UserWallets.AnyAsync(x =>
        x.UserId == request.UserId &&
        x.WalletProviderId == request.WalletProviderId &&
        !x.IsDeleted);

            if (exists)
                throw new Exception("Wallet already exists for the same provider");

            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (request.IsDefault)
                {
                    var existingDefaults = await _context.UserWallets
                        .Where(x => x.UserId == request.UserId &&
                                    x.IsDefault &&
                                    !x.IsDeleted)
                        .ToListAsync();

                    foreach (var wallet in existingDefaults)
                    {
                        wallet.IsDefault = false;
                    }

                    await _context.SaveChangesAsync();
                }

                var userWallet = new UserWallet
                {
                    AddedOn = DateTimeOffset.UtcNow,
                    UserId = request.UserId,
                    WalletProviderId = request.WalletProviderId,
                    WalletBalance = request.WalletBalance,
                    Currency = request.Currency ?? "NGN",
                    IsDefault = request.IsDefault,
                    IsActive = true,
                    WalletNumber = request.WalletNumber,
                    BelongToBusinessCode = args.businessCode,
                    IsDeleted = false
                };

                _context.UserWallets.Add(userWallet);
                await _context.SaveChangesAsync();
       
                if (request.WalletBalance > 0)
                {
                    var walletTransaction = new UserWalletTransactions
                    {
                        UserWalletId = userWallet.Id,
                        WalletTransactionType = WalletTransactionType.Credit,
                        TransactionAmount = request.WalletBalance,
                        BalanceBefore = 0,
                        BalanceAfter = request.WalletBalance,
                        TransactionDate = DateTimeOffset.UtcNow,
                        WalletTransactionStatus = WalletTransactionStatus.Successful,
                        TransactionReference = Guid.NewGuid().ToString(),
                        Currency = userWallet.Currency,
                        TransactionDescription = "Opening balance",
                        ProviderStatus = "SYSTEM",
                        
                    };

                    _context.UserWalletTransactions.Add(walletTransaction);
                    await _context.SaveChangesAsync();
                }

                await dbTransaction.CommitAsync();

                request.Id = userWallet.Id;
                return request;
            }
            catch (DbUpdateException ex)
            {
                await dbTransaction.RollbackAsync();

                // Handle unique constraint (UserId + WalletProviderId)
                if (ex.InnerException?.Message.Contains("IX_UserWallets_UserId_WalletProviderId") == true)
                {
                    throw new Exception("Wallet already exists for this provider.");
                }

                throw new Exception("An error occurred while creating the wallet.");
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> CloseUserWallet(
    long walletId,
    string closedByUserId,
    string reason = "User requested wallet closure")
        {
            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var wallet = await _context.UserWallets
                    .FirstOrDefaultAsync(x => x.Id == walletId && !x.IsDeleted);

                if (wallet == null)
                    throw new Exception("Wallet not found.");

                if (!wallet.IsActive)
                    throw new Exception("Wallet is already inactive.");

                if (wallet.WalletBalance != 0)
                    throw new Exception("Wallet balance must be zero before closure.");

                wallet.IsActive = false;
                wallet.IsDeleted = true;


                var auditTransaction = new UserWalletTransactions
                {
                    UserWalletId = wallet.Id,
                    WalletTransactionType = WalletTransactionType.Debit, 
                    TransactionAmount = 0,
                    BalanceBefore = wallet.WalletBalance,
                    BalanceAfter = wallet.WalletBalance,
                    TransactionDate = DateTimeOffset.UtcNow,
                    WalletTransactionStatus = WalletTransactionStatus.Successful,
                    TransactionReference = Guid.NewGuid().ToString(),
                    Currency = wallet.Currency,
                    TransactionDescription = $"Wallet closed: {reason}",
                    ProviderStatus = "SYSTEM"
                };

                _context.UserWalletTransactions.Add(auditTransaction);

                await _context.SaveChangesAsync();

                await dbTransaction.CommitAsync();

                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                await dbTransaction.RollbackAsync();
                throw new Exception("Wallet was modified by another process. Please retry.");
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }


        public async Task ProcessWalletTransaction(
    string userId,
    long walletProviderId,
    decimal amount,
    WalletTransactionType type,
    string reference,
    string description,
    string chargedTo = null,
    string chargedToId = null)
        {
            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Idempotency check
                var existingTx = await _context.UserWalletTransactions
                    .FirstOrDefaultAsync(x => x.TransactionReference == reference);

                if (existingTx != null)
                    return; // already processed

                // 2. Get wallet (FOR UPDATE behavior via SaveChanges)
                var wallet = await _context.UserWallets
                    .FirstOrDefaultAsync(x =>
                        x.UserId == userId &&
                        x.WalletProviderId == walletProviderId &&
                        !x.IsDeleted);

                if (wallet == null)
                    throw new Exception("Wallet not found");

                var balanceBefore = wallet.WalletBalance;
                decimal balanceAfter;

                // 3. Business logic
                if (type == WalletTransactionType.Debit)
                {
                    if (wallet.WalletBalance < amount)
                        throw new Exception("Insufficient balance");

                    wallet.WalletBalance -= amount;
                }
                else
                {
                    wallet.WalletBalance += amount;
                }

                balanceAfter = wallet.WalletBalance;

                // 4. Create ledger entry
                var transaction = new UserWalletTransactions
                {
                    UserWalletId = wallet.Id,
                    WalletTransactionType = type,
                    TransactionAmount = amount,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = balanceAfter,
                    TransactionDate = DateTimeOffset.UtcNow,
                    WalletTransactionStatus = WalletTransactionStatus.Successful,
                    TransactionReference = reference,
                    Currency = wallet.Currency,
                    TransactionDescription = description,
                    ChargedTo = chargedTo,
                    ChargedToId = chargedToId,
                    ProviderStatus = "SYSTEM"
                };

                _context.UserWalletTransactions.Add(transaction);

                // 5. Save (this enforces RowVersion concurrency)
                await _context.SaveChangesAsync();

                await dbTransaction.CommitAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                await dbTransaction.RollbackAsync();
                throw new Exception("Wallet was updated by another process. Retry.");
            }
            catch (DbUpdateException ex)
            {
                await dbTransaction.RollbackAsync();

                // Unique constraint on TransactionReference
                if (ex.InnerException?.Message.Contains("TransactionReference") == true)
                    return; // idempotent retry

                throw;
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

    }
}
