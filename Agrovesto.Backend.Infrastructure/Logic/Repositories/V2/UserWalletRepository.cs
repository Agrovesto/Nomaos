using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Agrovesto.Application.DTOs.Farms;
using Agrovesto.Backend.Core.Models.Businesses.Wallets;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.Utilities;
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
        Task<UserWallet> ExistingWalletForProvider(WalletQueryParams args, WalletContextType contextType, string? contextReferenceId, long providerId);
        Task<FarmerFinancialSummaryDto> GetUserFinancialSummary(UserWalletParams args);
        Task<List<UserWalletResponseModel>> GetUserWallletsByUserId(UserWalletParams args);
    }
    public class UserWalletRepository : IUserWalletRepository
    {
        private readonly DataContext _context;
        public UserWalletRepository(DataContext context)
        {
            _context = context;

        }
        public async Task<FarmerFinancialSummaryDto> GetUserFinancialSummary(UserWalletParams args)
        {
            var userId = args.UserId;

            var totalWalletBalance = await _context.UserWallets
                .Where(w => w.UserId == userId &&
                            w.BelongToBusinessCode == args.businessCode &&
                            !w.IsDeleted)
                .SumAsync(w => (decimal?)w.WalletBalance) ?? 0m;

            var inputStats = await _context.InputTransactions
                .Where(t => t.Farmer.UserId == userId)
                .GroupBy(t => 1)
                .Select(g => new
                {
                    TotalDisbursement = g
                        .Where(x => x.TransactionType == InputTransactionType.Disbursement)
                        .Sum(x => (decimal?)x.Amount) ?? 0m,

                    TotalRepayment = g
                        .Where(x => x.TransactionType == InputTransactionType.Repayment)
                        .Sum(x => (decimal?)x.Amount) ?? 0m
                })
                .FirstOrDefaultAsync();

            var totalDisbursement = inputStats?.TotalDisbursement ?? 0m;
            var totalRepayment = inputStats?.TotalRepayment ?? 0m;
            var outstandingDebt = totalDisbursement - totalRepayment;

            var nextRepayment = await _context.InputTransactions
                .Where(t => t.Farmer.UserId == userId &&
                            t.TransactionType == InputTransactionType.Repayment &&
                            t.TransactionDate > DateTime.UtcNow)
                .OrderBy(t => t.TransactionDate)
                .Select(t => new { t.Amount, t.TransactionDate })
                .FirstOrDefaultAsync();

            return new FarmerFinancialSummaryDto
            {
                TotalWalletBalance = totalWalletBalance,
                OutstandingInputDebt = outstandingDebt,
                NextRepaymentAmount = nextRepayment?.Amount ?? 0m,
                NextRepaymentDate = nextRepayment?.TransactionDate
            };
        }
        public  async Task<UserWallet> ExistingWalletForProvider(WalletQueryParams args, WalletContextType contextType, string? contextReferenceId, long providerId)
        {
            return await _context.UserWallets
                .Where(w =>
                    w.UserId == args.UserId &&
                    w.WalletProviderId == providerId &&
                    !w.IsDeleted &&
                    w.WalletContexts.Any(c =>
                        c.ContextType == contextType &&
                        c.ContextReferenceId == contextReferenceId))
                .FirstOrDefaultAsync();
        }

        public async Task<List<UserWalletResponseModel>> GetUserWallletsByUserId(UserWalletParams args)
        {
            var wallets = await _context.UserWallets
                .Where(uw =>
                    uw.UserId == args.UserId &&
                    uw.BelongToBusinessCode == args.businessCode &&
                    !uw.IsDeleted)
                .Select(uw => new UserWalletResponseModel
                {
                    Id = uw.Id,
                    UserId = uw.UserId,
                    WalletProviderId = uw.WalletProviderId,
                    ProviderName = uw.WalletProvider.Name,
                    WalletNumber = uw.WalletNumber,
                    WalletBalance = uw.WalletBalance,
                    Currency = uw.Currency,
                    IsActive = uw.IsActive,
                    BelongToBusinessCode = uw.BelongToBusinessCode,

                    Contexts = uw.WalletContexts
                        .Select(c => new UserWalletContextModel
                        {
                            ContextType = c.ContextType.ToString(),
                            ContextReferenceId = c.ContextReferenceId,
                            IsDefault = c.IsDefault
                        })
                        .ToList()
                })
                .ToListAsync();

            return wallets;
        }
        public async Task<UserWalletModel> CreateUserWallet(
       UserWalletModel request,
       UserWalletParams args)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                var exists = await _context.UserWallets.AnyAsync(x =>
                    x.UserId == request.UserId &&
                    x.WalletProviderId == request.WalletProviderId &&
                    !x.IsDeleted);

                if (exists)
                    throw new Exception("Wallet already exists for the same provider");

                await using var dbTransaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var userWallet = new UserWallet
                    {
                        AddedOn = DateTimeOffset.UtcNow,
                        UserId = request.UserId,
                        WalletProviderId = request.WalletProviderId,
                        WalletBalance = request.WalletBalance,
                        Currency = request.Currency ?? "NGN",
                        IsActive = true,
                        WalletNumber = request.WalletNumber,
                        BelongToBusinessCode = args.businessCode,
                        IsDeleted = false
                    };

                    _context.UserWallets.Add(userWallet);
                    await _context.SaveChangesAsync();

                    // Handle default for context
                    if (request.SetAsDefaultForContext)
                    {
                        var existingDefaults = await _context.UserWalletContexts
                            .Where(x =>
                                x.UserId == request.UserId &&
                                x.ContextType == request.ContextType.ToEnum<WalletContextType>() &&
                                x.ContextReferenceId == request.ContextReferenceId &&
                                x.IsDefault)
                            .ToListAsync();

                        foreach (var context in existingDefaults)
                        {
                            context.IsDefault = false;
                        }

                        await _context.SaveChangesAsync();
                    }

                    // Create context mapping
                    var walletContext = new UserWalletContext
                    {
                        UserId = request.UserId,
                        UserWalletId = userWallet.Id,
                        ContextType = request.ContextType.ToEnum<WalletContextType>(),
                        ContextReferenceId = request.ContextReferenceId,
                        IsDefault = request.SetAsDefaultForContext
                    };

                    _context.UserWalletContexts.Add(walletContext);
                    await _context.SaveChangesAsync();

                    // Opening balance transaction
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
                catch
                {
                    await dbTransaction.RollbackAsync();
                    throw;
                }
            });
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


        private bool ValidateTransactionPin(User user, string plainPin)
        {
            var salt = Convert.FromBase64String(user.Salt);
            var hashedPin = plainPin.HashSecret(salt);

            return user.PinEncrypt == hashedPin;
        }
    }
}
