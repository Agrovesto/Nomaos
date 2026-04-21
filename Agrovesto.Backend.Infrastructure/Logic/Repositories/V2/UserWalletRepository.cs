using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Agrovesto.Application.DTOs.Farms;
using Agrovesto.Backend.Core.Models.Businesses.Wallets;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.Utilities;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto.Generators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories.V2
{
    public interface IUserWalletRepository
    {
        // Define methods that the UserWalletRepository should implement
        Task<bool> CloseUserWallet(long walletId, string closedByUserId, string reason = "User requested wallet closure");
        Task CompleteWalletTopUp(string transactionReference, bool isSuccessful, string? providerReference, string? providerStatus);
        Task<UserWalletModel> CreateUserWallet(UserWalletModel request, UserWalletParams args);
        Task<UserWallet> ExistingWalletForProvider(WalletQueryParams args, WalletContextType contextType, string? contextReferenceId, long providerId);
        Task<FarmerFinancialSummaryDto> GetUserFinancialSummary(UserWalletParams args);
        Task<PagedListResponse<List<UserWalletResponseModel>>> GetUserWallletsByUserId(UserWalletParams args);
        Task<UserWalletDetailResponseModel> GetWalletById(UserWalletParams args);
        Task<UserWalletTransactions> GetWalletTransactionByRef(string transactionReference);
        Task<string> InitiateWalletTopUp(long userWalletId, decimal amount, string? description = "Wallet Top-up");
        Task UpdateTransaction(UserWalletTransactions transaction);
        Task UpdateTransactionProviderInfo(string transactionReference, string? providerReference, string providerStatus, string? rawPayload);
        Task UpdateTransactionStatus(string transactionReference, WalletTransactionStatus status);
        Task UpdateWalletBalance(UserWallet wallet);
    }
    public class UserWalletRepository : IUserWalletRepository
    {
        private readonly DataContext _context;
        public UserWalletRepository(DataContext context)
        {
            _context = context;

        }

        public async Task<UserWalletDetailResponseModel> GetWalletById(UserWalletParams args)
        {
           
            var query = _context.UserWallets
                .Where(uw =>
                    uw.UserId == args.UserId &&
                    uw.Id== args.Id &&
                    uw.BelongToBusinessCode == args.businessCode &&
                    !uw.IsDeleted);

          

            var wallet = await query
                .Select(uw => new UserWalletDetailResponseModel
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
                .FirstOrDefaultAsync();

            return wallet;
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

        public async Task<PagedListResponse<List<UserWalletResponseModel>>> GetUserWallletsByUserId(UserWalletParams args)
        {
            args.pageNumber = args.pageNumber <= 0 ? 1 : args.pageNumber;
            args.pageSize = args.pageSize <= 0 ? 10 : args.pageSize;

            var query = _context.UserWallets
                .Where(uw =>
                    uw.UserId == args.UserId &&
                    uw.BelongToBusinessCode == args.businessCode &&
                    !uw.IsDeleted);

            var totalRecords = await query.CountAsync();

            var wallets = await query
                .OrderByDescending(x => x.Id)
                .Skip((args.pageNumber - 1) * args.pageSize)
                .Take(args.pageSize)
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

            return new PagedListResponse<List<UserWalletResponseModel>>
            {
                PageNumber = args.pageNumber,
                PageSize = args.pageSize,
                Count = totalRecords,
                
                Data = wallets
            };
        }
        public async Task ValidateTransactionPin(long walletId, string inputPin)
        {
            var security = await _context.WalletSecurities
                .FirstOrDefaultAsync(x => x.UserWalletId == walletId);

            if (security == null)
                throw new Exception("Wallet security record not found");

            if (security.PinLockedUntil != null &&
                security.PinLockedUntil > DateTimeOffset.UtcNow)
            {
                throw new Exception("Wallet is locked due to multiple invalid PIN attempts");
            }

            var valid = BCrypt.Net.BCrypt.Verify(inputPin, security.TransactionPinHash);

            if (!valid)
            {
                security.PinFailedAttempts++;

                if (security.PinFailedAttempts >= 5)
                {
                    security.PinLockedUntil = DateTimeOffset.UtcNow.AddMinutes(10);
                    security.PinFailedAttempts = 0;
                }

                await _context.SaveChangesAsync();

                throw new Exception("Invalid transaction PIN");
            }

            security.PinFailedAttempts = 0;
            security.PinLockedUntil = null;

            await _context.SaveChangesAsync();
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
                        IsDeleted = false,
                         };

                    _context.UserWallets.Add(userWallet);
                    await _context.SaveChangesAsync();
                    var walletSecurity = new WalletSecurity
                    {
                        UserWalletId = userWallet.Id,
                        Txn= request.TransactionPin,
                        TransactionPinHash = BCrypt.Net.BCrypt.HashPassword(request.TransactionPin)
                    };

                    _context.WalletSecurities.Add(walletSecurity);
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


        public async Task<string> InitiateWalletTopUp(
    long userWalletId,
    decimal amount,
    string? description = "Wallet Top-up")
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero");

            var wallet = await _context.UserWallets
                .FirstOrDefaultAsync(x => x.Id == userWalletId && !x.IsDeleted);

            if (wallet == null)
                throw new Exception("Wallet not found");

            var transactionReference = Guid.NewGuid().ToString();

            var transaction = new UserWalletTransactions
            {
                UserWalletId = wallet.Id,
                WalletTransactionType = WalletTransactionType.Credit,
                TransactionAmount = amount,
                CommissionFee = 0,
                TransactionFee = 0,
                Currency = wallet.Currency,
                TransactionDescription = description,
                TransactionDate = DateTimeOffset.UtcNow,
                WalletTransactionStatus = WalletTransactionStatus.Pending,
                TransactionReference = transactionReference,
                BalanceBefore = wallet.WalletBalance,
                BalanceAfter = wallet.WalletBalance, // unchanged for now
                ProviderStatus = "INITIATED"
            };

            _context.UserWalletTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            return transactionReference;
        }

        public async Task<UserWalletTransactions> GetWalletTransactionByRef(string transactionReference)
        {
            var transaction = await _context.UserWalletTransactions
                .FirstOrDefaultAsync(x => x.TransactionReference == transactionReference);
            if (transaction == null)
                return null;
            return transaction;
        }

        public async Task CompleteWalletTopUp(
    string transactionReference,
    bool isSuccessful,
    string? providerReference,
    string? providerStatus)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var transaction = await _context.UserWalletTransactions
                        .FirstOrDefaultAsync(x => x.TransactionReference == transactionReference);

                    if (transaction == null)
                        throw new Exception("Transaction not found");

                    if (transaction.WalletTransactionStatus != WalletTransactionStatus.Pending)
                        throw new Exception("Transaction already processed");

                    var wallet = await _context.UserWallets
                        .FirstOrDefaultAsync(x => x.Id == transaction.UserWalletId);

                    if (wallet == null)
                        throw new Exception("Wallet not found");

                    transaction.ProviderReference = providerReference;
                    transaction.ProviderStatus = providerStatus;

                    if (!isSuccessful)
                    {
                        transaction.WalletTransactionStatus = WalletTransactionStatus.Failed;
                        await _context.SaveChangesAsync();
                        await dbTransaction.CommitAsync();
                        return;
                    }

                 
                    var balanceBefore = wallet.WalletBalance;
                    var netAmount = transaction.TransactionAmount
                                    - transaction.TransactionFee
                                    - transaction.CommissionFee;

                    wallet.WalletBalance += netAmount;

                    transaction.BalanceBefore = balanceBefore;
                    transaction.BalanceAfter = wallet.WalletBalance;
                    transaction.WalletTransactionStatus = WalletTransactionStatus.Successful;

                    _context.UserWallets.Update(wallet);
                    _context.UserWalletTransactions.Update(transaction);

                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();
                }
                catch
                {
                    await dbTransaction.RollbackAsync();
                    throw;
                }
            });
        }



        public async Task UpdateTransactionProviderInfo(
    string transactionReference,
    string? providerReference,
    string providerStatus,
    string? rawPayload)
        {
            var transaction = await _context.UserWalletTransactions
                .FirstOrDefaultAsync(x => x.TransactionReference == transactionReference);

            if (transaction == null)
                return;

            transaction.ProviderReference = providerReference;
            transaction.ProviderStatus = providerStatus;
            transaction.TransactionDate = DateTimeOffset.UtcNow;

            if (!string.IsNullOrWhiteSpace(rawPayload))
            {
                // If you later add a column for it:
                // transaction.RawWebhookPayload = rawPayload;
            }

            _context.UserWalletTransactions.Update(transaction);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateTransactionStatus(
    string transactionReference,
    WalletTransactionStatus status)
        {
            var transaction = await _context.UserWalletTransactions
                .FirstOrDefaultAsync(x => x.TransactionReference == transactionReference);

            if (transaction == null)
                return;

            if (transaction.WalletTransactionStatus == status)
                return;

            if (transaction.WalletTransactionStatus == WalletTransactionStatus.Successful &&
                status != WalletTransactionStatus.Reversed)
            {
                return;
            }

            transaction.WalletTransactionStatus = status;
            transaction.TransactionDate = DateTimeOffset.UtcNow;

            _context.UserWalletTransactions.Update(transaction);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateWalletBalance(UserWallet wallet)
        {
            var existingWallet = await _context.UserWallets
                .FirstOrDefaultAsync(x => x.Id == wallet.Id && !x.IsDeleted);

            if (existingWallet == null)
                return;

            if (wallet.WalletBalance < 0)
                throw new Exception("Wallet balance cannot be negative");

            existingWallet.WalletBalance = wallet.WalletBalance;
            existingWallet.IsActive = wallet.IsActive;

            _context.Entry(existingWallet).OriginalValues["RowVersion"] = existingWallet.RowVersion;

            _context.UserWallets.Update(existingWallet);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateTransaction(UserWalletTransactions transaction)
        {
            var existingTransaction = await _context.UserWalletTransactions
                .FirstOrDefaultAsync(x => x.Id == transaction.Id);

            if (existingTransaction == null)
                return;

            if (existingTransaction.WalletTransactionStatus == WalletTransactionStatus.Successful)
                return;

            existingTransaction.TransactionAmount = transaction.TransactionAmount;
            existingTransaction.TransactionFee = transaction.TransactionFee;
            existingTransaction.CommissionFee = transaction.CommissionFee;

            existingTransaction.BalanceBefore = transaction.BalanceBefore;
            existingTransaction.BalanceAfter = transaction.BalanceAfter;

            existingTransaction.ProviderReference = transaction.ProviderReference;
            existingTransaction.ProviderStatus = transaction.ProviderStatus;

            existingTransaction.TransactionDescription = transaction.TransactionDescription;

            existingTransaction.TransactionDate = DateTimeOffset.UtcNow;

            _context.UserWalletTransactions.Update(existingTransaction);
            await _context.SaveChangesAsync();
        }


    }
}
