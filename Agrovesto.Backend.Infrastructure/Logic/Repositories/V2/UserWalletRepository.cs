using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Agrovesto.Application.DTOs.Farms;
using Agrovesto.Backend.Core.Models.Businesses.Input;
using Agrovesto.Backend.Core.Models.Businesses.Wallets;
using Agrovesto.Backend.Core.Models.Common.WalletServiceRequest.MomoRequest;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.Utilities;
using BCrypt.Net;
using Core.Models.Payment;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto.Generators;
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
        Task CompleteWalletFunding(string transactionReference, bool isSuccessful, string? providerReference, string? providerStatus);
        Task<UserWalletModel> CreateUserWallet(UserWalletModel request, UserWalletParams args);
        Task<UserWallet> ExistingWalletForProvider(WalletQueryParams args, WalletContextType contextType, string? contextReferenceId, long providerId);
        Task<FarmerFinancialSummaryDto> GetUserFinancialSummary(UserWalletParams args);
        Task<List<LinkedInputPackageDto>> GetWalletLinkedPackages(UserWalletParams args);
        Task<UserWallet?> GetActiveWalletForUser(string userId, string businessCode);
        Task<PagedListResponse<List<FarmerTransactionDto>>> GetUserWalletTransaction(ListQueryParams args);
        Task<PagedListResponse<List<UserWalletResponseModel>>> GetUserWallletsByUserId(UserWalletParams args);
        Task<UserWalletDetailResponseModel> GetWalletById(UserWalletParams args);
        Task<UserWalletTransactions> GetWalletTransactionByRef(string transactionReference);
        Task<string> CreateWalletFundinAsync(long userWalletId, decimal amount, string? description = "Wallet Top-up"
            , string? reference = null);
        Task<string> InitiateWalletWithdrawal(long userWalletId, decimal amount, string? description = "Wallet Withdrawal", string? reference = null);
        Task CompleteWalletWithdrawal(string transactionReference, bool isSuccessful, string? providerReference, string? providerStatus);
        Task<string> ReserveWalletWithdrawal(long userWalletId, decimal amount, string? description, string reference);
        Task SettleWalletWithdrawal(string transactionReference, string? providerReference, string? providerStatus);
        Task ReverseWalletWithdrawal(string transactionReference, string? providerReference, string? providerStatus);
        Task MarkWithdrawalIndeterminate(string transactionReference, string? providerReference, string? providerStatus);
        Task ValidateTransactionPin(long walletId, string inputPin);
        Task UpdateTransactionPin(long walletId, string newPin);
        Task UpdateTransaction(UserWalletTransactions transaction);
        Task UpdateTransactionProviderInfo(string transactionReference, string? providerReference, string providerStatus, string? rawPayload);
        Task UpdateTransactionStatus(string transactionReference, WalletTransactionStatus status);
        Task UpdateWalletBalance(UserWallet wallet);
        Task<UserWalletDetailResponseModel> GetUserWalletByProviderId(UserWalletParams args);
        Task FlagForReconciliation(string debitReference, string creditReference, string reason);
        Task<WalletReconciliationLog?> GetReconciliationLog(string debitReference);
        Task<List<WalletReconciliationLog>> GetPendingReconciliations();
        Task ResolveReconciliation(long reconciliationId, string resolvedBy, string resolutionNote, ReconciliationStatus finalStatus);
        Task AdjustWalletBalanceAsync(long walletId, decimal delta);
        Task MarkPendingReconciliation(string reference, string reason);
    }
    public class UserWalletRepository : IUserWalletRepository
    {
        private static readonly SubscriptionStatus[] ActiveLoanStatuses =
        {
            SubscriptionStatus.Active,
            SubscriptionStatus.PendingDisbursement,
            SubscriptionStatus.PendingCommitment,
            SubscriptionStatus.PendingApproval
        };

        private readonly DataContext _context;
        private readonly IAWSService _notificationService;
        public UserWalletRepository(DataContext context, IAWSService aWSService)
        {
            _context = context;
            _notificationService = aWSService;
        }
        public async Task<decimal> TotalUserWalletsBalance(string userId, string businessCode)
        {
            var totalBalance = await _context.UserWallets
                .Where(w =>
                    w.UserId == userId &&
                    w.BelongToBusinessCode == businessCode &&
                    !w.IsDeleted)
                .SumAsync(w => (decimal?)w.WalletBalance) ?? 0m;
            return totalBalance;
        }
        public async Task AdjustWalletBalanceAsync(long walletId, decimal delta)
        {
            var wallet = await _context.UserWallets.FirstOrDefaultAsync(x => x.Id == walletId);

            if (wallet == null)
                throw new BaseException(ExceptionType.NOTFOUND, "Wallet not found");

            wallet.WalletBalance += delta;

            await _context.SaveChangesAsync();
        }
        public async Task<UserWalletDetailResponseModel> GetUserWalletByProviderId(UserWalletParams args)
        {

            var query = _context.UserWallets
                .Where(uw =>
                    uw.UserId == args.UserId &&
                    uw.WalletProviderId == args.Id &&
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

       
        
        public async Task<UserWallet?> GetActiveWalletForUser(string userId, string businessCode)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            return await _context.UserWallets
                .Where(w =>
                    w.UserId == userId &&
                    w.BelongToBusinessCode == businessCode &&
                    w.IsActive &&
                    !w.IsDeleted)
                .OrderByDescending(w => w.WalletContexts.Any(c => c.IsDefault))
                .ThenBy(w => w.AddedOn)
                .FirstOrDefaultAsync();
        }

        public async Task<List<LinkedInputPackageDto>> GetWalletLinkedPackages(UserWalletParams args)
        {
            if (string.IsNullOrWhiteSpace(args.UserId))
                throw new ArgumentException("UserId is required");

            var walletIds = await _context.UserWallets
                .AsNoTracking()
                .Where(w =>
                    w.UserId == args.UserId &&
                    w.BelongToBusinessCode == args.businessCode &&
                    !w.IsDeleted &&
                    (args.Id <= 0 || w.Id == args.Id))
                .Select(w => w.Id)
                .ToListAsync();

            if (!walletIds.Any())
                return new List<LinkedInputPackageDto>();

            var packages = await _context.InputPackages
                .AsNoTracking()
                .Where(p => p.UserWalletId.HasValue && walletIds.Contains(p.UserWalletId.Value))
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    WalletId = p.UserWalletId.Value
                })
                .ToListAsync();

            if (!packages.Any())
                return new List<LinkedInputPackageDto>();

            var packageIds = packages.Select(p => p.Id).ToList();

            var activeLoans = await _context.InputPackageSubscriptions
                .AsNoTracking()
                .Where(s =>
                    packageIds.Contains(s.InputPackageId) &&
                    ActiveLoanStatuses.Contains(s.Status))
                .GroupBy(s => s.InputPackageId)
                .Select(g => new
                {
                    PackageId = g.Key,
                    Count = g.Count()
                })
                .ToDictionaryAsync(x => x.PackageId, x => x.Count);

            var outstanding = await _context.RepaymentSchedules
                .AsNoTracking()
                .Where(x =>
                    packageIds.Contains(x.InputPackageSubscription.InputPackageId) &&
                    !x.IsPaid &&
                    !x.IsDeleted)
                .GroupBy(x => x.InputPackageSubscription.InputPackageId)
                .Select(g => new
                {
                    PackageId = g.Key,
                    Amount = g.Sum(x => x.Amount - x.AmountPaid)
                })
                .ToDictionaryAsync(x => x.PackageId, x => x.Amount);

            return packages
                .Select(p => new LinkedInputPackageDto
                {
                    PackageId = p.Id,
                    WalletId = p.WalletId,
                    PackageName = p.Name,
                    ActiveLoans = activeLoans.TryGetValue(p.Id, out var count) ? count : 0,
                    OutstandingPortfolio = outstanding.TryGetValue(p.Id, out var amount) ? amount : 0
                })
                .OrderBy(p => p.PackageName)
                .ToList();
        }

        public async Task<FarmerFinancialSummaryDto> GetUserFinancialSummary(UserWalletParams args)
        {
            var subscriberUserId = args.UserId;

            if (string.IsNullOrWhiteSpace(subscriberUserId))
                throw new ArgumentException("SubscriberUserId is required");

            var totalWalletBalance = await _context.UserWallets
               .Where(w => w.UserId == subscriberUserId &&
                           w.BelongToBusinessCode == args.businessCode &&
                           !w.IsDeleted)
               .SumAsync(w => (decimal?)w.WalletBalance) ?? 0m;

            var subscriptionIds = await _context.InputPackageSubscriptions
                .AsNoTracking()
                .Where(x =>
                    x.SubscriberUserId == subscriberUserId 
                   // &&
                   // x.Status == SubscriptionStatus.Active
                    )
                .Select(x => x.Id)
                .ToListAsync();

            if (!subscriptionIds.Any())
            {
                return new FarmerFinancialSummaryDto
                {
                    OutstandingInputDebt = 0,
                    NextRepaymentAmount = 0,
                    NextRepaymentDate = null
                };
            }

            var outstandingInputDebt = await _context.RepaymentSchedules
                .Where(x =>
                    subscriptionIds.Contains(x.InputPackageSubscriptionId) &&
                    !x.IsPaid)
                .SumAsync(x => (decimal?)(
                    x.Amount - x.AmountPaid
                )) ?? 0;

            var nextRepayment = await _context.RepaymentSchedules
                .Where(x =>
                    subscriptionIds.Contains(x.InputPackageSubscriptionId) &&
                    !x.IsPaid)
                .OrderBy(x => x.DueDate)
                .Select(x => new
                {
                    x.DueDate,
                    RemainingAmount = x.Amount - x.AmountPaid
                })
                .FirstOrDefaultAsync();

            return new FarmerFinancialSummaryDto
            {
                TotalWalletBalance= totalWalletBalance,
                OutstandingInputDebt = outstandingInputDebt,
                NextRepaymentAmount = nextRepayment?.RemainingAmount ?? 0,
                NextRepaymentDate = nextRepayment?.DueDate
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
                    DateAdded = uw.AddedOn,

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

        public async Task UpdateTransactionPin(long walletId, string newPin)
        {
            var security = await _context.WalletSecurities
                .FirstOrDefaultAsync(x => x.UserWalletId == walletId);

            if (security == null)
                throw new BaseException(ExceptionType.NOTFOUND, "Wallet security record not found");

            security.TransactionPinHash = BCrypt.Net.BCrypt.HashPassword(newPin);
            security.PinFailedAttempts = 0;
            security.PinLockedUntil = null;
            security.LastPinChangeDate = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<UserWalletModel> CreateUserWallet(
       UserWalletModel request,
       UserWalletParams args)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync((Func<Task<UserWalletModel>>)(async () =>
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
                        MaxCheckSum = request.MaxCheckSum
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
                            WalletTransactionStatus = WalletTransactionStatus.Completed,
                            TransactionReference = Guid.NewGuid().ToString(),
                            Currency = userWallet.Currency,
                            TransactionDescription = "Opening balance",
                            ProviderStatus = "SYSTEM",
                        };

                        _context.UserWalletTransactions.Add(walletTransaction);
                        await _context.SaveChangesAsync();
                    }

                    await dbTransaction.CommitAsync();

                    request.WalletId = userWallet.Id;
                    return request;
                }
                catch
                {
                    await dbTransaction.RollbackAsync();
                    throw;
                }
            }));
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
                    WalletTransactionStatus = WalletTransactionStatus.Completed,
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
                    WalletTransactionStatus = WalletTransactionStatus.Completed,
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


        public async Task<string> CreateWalletFundinAsync(
    long userWalletId,
    decimal amount,
    string? description = "Wallet Top-up"
            , string? reference =null)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero");

            var wallet = await _context.UserWallets
                .FirstOrDefaultAsync(x => x.Id == userWalletId && !x.IsDeleted);

            if (wallet == null)
                throw new Exception("Wallet not found");

            var transactionReference =!string.IsNullOrEmpty(reference) ? reference :  Guid.NewGuid().ToString();

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
                WalletTransactionStatus = WalletTransactionStatus.DebitPending,
                TransactionReference = transactionReference,
                BalanceBefore = wallet.WalletBalance,
                BalanceAfter = wallet.WalletBalance, // unchanged for now
                ProviderStatus = "INITIATED"
            };

            _context.UserWalletTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            return transactionReference;
        }

        private const int ProviderStatusMaxLength = 50;
        private const int ProviderReferenceMaxLength = 100;
        private const int TransactionReferenceMaxLength = 50;
        private const int TransactionDescriptionMaxLength = 500;

        private static string? Clip(string? value, int maxLength) =>
            string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];

        public async Task<string> ReserveWalletWithdrawal(
            long userWalletId, decimal amount, string? description, string reference)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero");

            if (string.IsNullOrWhiteSpace(reference))
                throw new ArgumentException("A transaction reference is required");

            if (reference.Length > TransactionReferenceMaxLength)
                throw new ArgumentException(
                    $"Transaction reference must be {TransactionReferenceMaxLength} characters or fewer");

            var wallet = await _context.UserWallets
                .FirstOrDefaultAsync(x => x.Id == userWalletId && !x.IsDeleted);

            if (wallet == null)
                throw new Exception("Wallet not found");

            if (wallet.WalletBalance < amount)
                throw new Exception("Insufficient balance");

            var balanceBefore = wallet.WalletBalance;
            wallet.WalletBalance -= amount;

            var transaction = new UserWalletTransactions
            {
                UserWalletId = wallet.Id,
                WalletTransactionType = WalletTransactionType.Debit,
                TransactionAmount = amount,
                CommissionFee = 0,
                TransactionFee = 0,
                Currency = wallet.Currency,
                TransactionDescription = Clip(description, TransactionDescriptionMaxLength),
                TransactionDate = DateTimeOffset.UtcNow,
                WalletTransactionStatus = WalletTransactionStatus.DebitPending,
                TransactionReference = reference,
                BalanceBefore = balanceBefore,
                BalanceAfter = wallet.WalletBalance,
                ProviderStatus = "RESERVED"
            };

            _context.UserWallets.Update(wallet);
            _context.UserWalletTransactions.Add(transaction);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new Exception(
                    "This wallet was updated by another transaction. Please check your balance and try again.");
            }

            return reference;
        }

        private async Task<UserWalletTransactions> GetReservedWithdrawal(string transactionReference)
        {
            var transaction = await _context.UserWalletTransactions
                .FirstOrDefaultAsync(x => x.TransactionReference == transactionReference);

            if (transaction == null)
                throw new Exception("Transaction not found");

            if (transaction.WalletTransactionStatus != WalletTransactionStatus.DebitPending)
                throw new Exception("Transaction already processed");

            return transaction;
        }

        public async Task SettleWalletWithdrawal(
            string transactionReference, string? providerReference, string? providerStatus)
        {
            var transaction = await GetReservedWithdrawal(transactionReference);

            transaction.ProviderReference = Clip(providerReference, ProviderReferenceMaxLength);
            transaction.ProviderStatus = Clip(providerStatus, ProviderStatusMaxLength);
            transaction.WalletTransactionStatus = WalletTransactionStatus.Completed;

            _context.UserWalletTransactions.Update(transaction);
            await _context.SaveChangesAsync();
        }

        public async Task ReverseWalletWithdrawal(
            string transactionReference, string? providerReference, string? providerStatus)
        {
            var transaction = await GetReservedWithdrawal(transactionReference);

            var wallet = await _context.UserWallets
                .FirstOrDefaultAsync(x => x.Id == transaction.UserWalletId);

            if (wallet == null)
                throw new Exception("Wallet not found");

            wallet.WalletBalance += transaction.TransactionAmount;

            transaction.BalanceAfter = wallet.WalletBalance;
            transaction.ProviderReference = Clip(providerReference, ProviderReferenceMaxLength);
            transaction.ProviderStatus = Clip(providerStatus, ProviderStatusMaxLength);
            transaction.WalletTransactionStatus = WalletTransactionStatus.Failed;

            _context.UserWallets.Update(wallet);
            _context.UserWalletTransactions.Update(transaction);
            await _context.SaveChangesAsync();
        }

        public async Task MarkWithdrawalIndeterminate(
            string transactionReference, string? providerReference, string? providerStatus)
        {
            var transaction = await GetReservedWithdrawal(transactionReference);

            transaction.ProviderReference = Clip(providerReference, ProviderReferenceMaxLength);
            transaction.ProviderStatus = Clip(providerStatus, ProviderStatusMaxLength);
            transaction.WalletTransactionStatus = WalletTransactionStatus.ReconciliationRequired;

            _context.UserWalletTransactions.Update(transaction);
            await _context.SaveChangesAsync();
        }

        public async Task<string> InitiateWalletWithdrawal(long userWalletId, decimal amount, string? description = "Wallet Withdrawal", string? reference = null)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero");

            var wallet = await _context.UserWallets
                .FirstOrDefaultAsync(x => x.Id == userWalletId && !x.IsDeleted);

            if (wallet == null)
                throw new Exception("Wallet not found");

            if (wallet.WalletBalance < amount)
                throw new Exception("Insufficient balance");

            var transactionReference = !string.IsNullOrEmpty(reference) ? reference : Guid.NewGuid().ToString();

            var transaction = new UserWalletTransactions
            {
                UserWalletId = wallet.Id,
                WalletTransactionType = WalletTransactionType.Debit,
                TransactionAmount = amount,
                CommissionFee = 0,
                TransactionFee = 0,
                Currency = wallet.Currency,
                TransactionDescription = description,
                TransactionDate = DateTimeOffset.UtcNow,
                WalletTransactionStatus = WalletTransactionStatus.DebitPending,
                TransactionReference = transactionReference,
                BalanceBefore = wallet.WalletBalance,
                BalanceAfter = wallet.WalletBalance, // unchanged until processed
                ProviderStatus = "INITIATED"
            };

            _context.UserWalletTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            return transactionReference;
        }

        public async Task CompleteWalletWithdrawal(
            string transactionReference,
            bool isSuccessful,
            string? providerReference,
            string? providerStatus)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

          //  await strategy.ExecuteAsync(async () =>
            //{
              //  await using var dbTransaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var transaction = await _context.UserWalletTransactions
                        .FirstOrDefaultAsync(x => x.TransactionReference == transactionReference);

                    if (transaction == null)
                        throw new Exception("Transaction not found");

                    if (transaction.WalletTransactionStatus != WalletTransactionStatus.DebitPending)
                        throw new Exception("Transaction already processed");

                    var wallet = await _context.UserWallets
                        .FirstOrDefaultAsync(x => x.Id == transaction.UserWalletId);

                    if (wallet == null)
                        throw new Exception("Wallet not found");

                    transaction.ProviderReference = Clip(providerReference, ProviderReferenceMaxLength);
                    transaction.ProviderStatus = Clip(providerStatus, ProviderStatusMaxLength);

                    if (!isSuccessful)
                    {
                        transaction.WalletTransactionStatus = WalletTransactionStatus.Failed;
                        _context.UserWalletTransactions.Update(transaction);
                        await _context.SaveChangesAsync();
                      //  await dbTransaction.CommitAsync();
                        return;
                    }

                    var balanceBefore = wallet.WalletBalance;
                    var netAmount = transaction.TransactionAmount
                                    + transaction.TransactionFee
                                    + transaction.CommissionFee;

                    if (wallet.WalletBalance < netAmount)
                        throw new Exception("Insufficient balance");

                    wallet.WalletBalance -= netAmount;

                    transaction.BalanceBefore = balanceBefore;
                    transaction.BalanceAfter = wallet.WalletBalance;
                    transaction.WalletTransactionStatus = WalletTransactionStatus.Completed;

                    _context.UserWallets.Update(wallet);
                    _context.UserWalletTransactions.Update(transaction);

                    await _context.SaveChangesAsync();
                    //await dbTransaction.CommitAsync();
                }
                catch
                {
                  //  await dbTransaction.RollbackAsync();
                    throw;
                }
            //});
        }

        public async Task<UserWalletTransactions> GetWalletTransactionByRef(string transactionReference)
        {
            var transaction = await _context.UserWalletTransactions
                .Include(x=>x.UserWallet)
                .FirstOrDefaultAsync(x => x.TransactionReference == transactionReference);
            if (transaction == null)
                return null;
            return transaction;
        }

        public async Task CompleteWalletFunding(
    string transactionReference,
    bool isSuccessful,
    string? providerReference,
    string? providerStatus)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            //await strategy.ExecuteAsync(async () =>
            //{
            //    await using var dbTransaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var transaction = await _context.UserWalletTransactions
                        .FirstOrDefaultAsync(x => x.TransactionReference == transactionReference);

                    if (transaction == null)
                        throw new Exception("Transaction not found");

                    if (transaction.WalletTransactionStatus != WalletTransactionStatus.DebitPending)
                        throw new Exception("Transaction already processed");

                    var wallet = await _context.UserWallets
                        .FirstOrDefaultAsync(x => x.Id == transaction.UserWalletId);

                    if (wallet == null)
                        throw new Exception("Wallet not found");

                    transaction.ProviderReference = Clip(providerReference, ProviderReferenceMaxLength);
                    transaction.ProviderStatus = Clip(providerStatus, ProviderStatusMaxLength);

                    if (!isSuccessful)
                    {
                        transaction.WalletTransactionStatus = WalletTransactionStatus.Failed;
                        _context.UserWalletTransactions.Update(transaction);
                        await _context.SaveChangesAsync();
                       // await dbTransaction.CommitAsync();
                        return;
                    }


                    var balanceBefore = wallet.WalletBalance;
                    var netAmount = transaction.TransactionAmount
                                    - transaction.TransactionFee
                                    - transaction.CommissionFee;

                    wallet.WalletBalance += netAmount;
                    
                    transaction.BalanceBefore = balanceBefore;
                    transaction.BalanceAfter = wallet.WalletBalance;
                    transaction.WalletTransactionStatus = WalletTransactionStatus.Completed;
                    transaction.CreditWalletId = transaction.UserWalletId;
                    _context.UserWallets.Update(wallet);
                    _context.UserWalletTransactions.Update(transaction);

                    await _context.SaveChangesAsync();
                  //  await dbTransaction.CommitAsync();
                }
                catch
                {
                   // await dbTransaction.RollbackAsync();
                    throw;
                }
            //});
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

            transaction.ProviderReference = Clip(providerReference, ProviderReferenceMaxLength);
            transaction.ProviderStatus = Clip(providerStatus, ProviderStatusMaxLength);
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

            if (transaction.WalletTransactionStatus == WalletTransactionStatus.Completed &&
                status != WalletTransactionStatus.Reversed)
            {
                return;
            }

            transaction.WalletTransactionStatus = status;
            transaction.TransactionDate = DateTimeOffset.UtcNow;

            _context.UserWalletTransactions.Update(transaction);
           // await _context.SaveChangesAsync();
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
           // await _context.SaveChangesAsync();
        }

        public async Task UpdateTransaction(UserWalletTransactions transaction)
        {
            var existingTransaction = await _context.UserWalletTransactions
                .FirstOrDefaultAsync(x => x.Id == transaction.Id);

            if (existingTransaction == null)
                return;

            if (existingTransaction.WalletTransactionStatus == WalletTransactionStatus.Completed)
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
            existingTransaction.WalletTransactionStatus =
    transaction.WalletTransactionStatus;
            _context.UserWalletTransactions.Update(existingTransaction);
           // await _context.SaveChangesAsync();
        }

        public async Task<PagedListResponse<List<FarmerTransactionDto>>> GetUserWalletTransaction(ListQueryParams args)
        {
            var pageSize = args.PageSize <= 0 ? 10 : args.PageSize;
            var pageNumber = args.PageNumber <= 0 ? 1 : args.PageNumber;

            var query = _context.UserWalletTransactions
                .AsNoTracking()
                .Where(x => x.UserWalletId == args.Id);

            if (!string.IsNullOrWhiteSpace(args.TransactionReference))
            {
                query = query.Where(x =>
                    x.TransactionReference.Contains(args.TransactionReference));
            }

            if (args.TransactionDate.HasValue)
            {
                // The caller's stated calendar date, matched as a UTC day. Transactions are
                // always written with DateTimeOffset.UtcNow, so this is the day they belong to.
                //
                // A half-open range replaces CONVERT(date, column) == date, which had two
                // problems: the function around the column made the predicate non-SARGable,
                // and it compared the date in whatever offset each row was stored with rather
                // than a fixed one. Comparing instants avoids both.
                var dayStart = new DateTimeOffset(args.TransactionDate.Value.Date, TimeSpan.Zero);
                var dayEnd = dayStart.AddDays(1);

                query = query.Where(x =>
                    x.TransactionDate >= dayStart && x.TransactionDate < dayEnd);
            }

            if (args.TransactionType.HasValue)
            {
                var txnType = (WalletTransactionType)args.TransactionType;

                query = query.Where(x =>
                    x.WalletTransactionType == txnType);
            }

            if (args.TransactionStatus.HasValue)
            {
                var txnStatus = (WalletTransactionStatus)args.TransactionStatus;

                query = query.Where(x =>
                    x.WalletTransactionStatus == txnStatus);
            }

            var totalCount = await query.CountAsync();

            if (totalCount == 0)
            {
                return new PagedListResponse<List<FarmerTransactionDto>>
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    Count = 0,
                    Data = new List<FarmerTransactionDto>()
                };
            }

            var pagedData = await query
                .OrderByDescending(x => x.TransactionDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new FarmerTransactionDto
                 {
                     TransactionId = t.Id,
                     TransactionReference = t.TransactionReference,
                     Amount = t.TransactionAmount,
                     BalanceAfter = t.BalanceAfter,
                     TransactionType = EnumExtensions.GetDisplayName(t.WalletTransactionType),
                     Status = EnumExtensions.GetDisplayName(t.WalletTransactionStatus),
                     Timestamp = t.TransactionDate,
                     Description = t.TransactionDescription
                 })
                .ToListAsync();

            return new PagedListResponse<List<FarmerTransactionDto>>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Count = totalCount,
                Data = pagedData
            };
        }

        public async Task MarkPendingReconciliation(string reference, string reason)
        {
            var tx = await _context.UserWalletTransactions
                .FirstOrDefaultAsync(x => x.TransactionReference == reference);

            if (tx == null)
                return; 

            tx.WalletTransactionStatus = WalletTransactionStatus.ReconciliationRequired;
            //tx.IsSuccessful = null; // explicitly unknown, not false
            //tx.FailureReason = reason;
            //tx.UpdatedOn = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task FlagForReconciliation(
    string debitReference,
    string creditReference,
    string reason)
        {
            // Guard against duplicate flags for the same debit
            var existing = await _context.WalletReconciliationLogs
                .FirstOrDefaultAsync(x => x.DebitReference == debitReference);

            if (existing != null)
            {
                // Update reason if it was already flagged, rather than creating a duplicate
                existing.Reason = $"{existing.Reason} | Re-flagged: {reason}";
                existing.FlaggedAt = DateTimeOffset.UtcNow;
                _context.WalletReconciliationLogs.Update(existing);
                await _context.SaveChangesAsync();
                return;
            }

            var log = new WalletReconciliationLog
            {
                DebitReference = debitReference,
                CreditReference = creditReference,
                Reason = reason,
                Status = ReconciliationStatus.Pending,
                FlaggedAt = DateTimeOffset.UtcNow
            };

            await _context.WalletReconciliationLogs.AddAsync(log);
            await _context.SaveChangesAsync();

            // Notify the team — fire and forget, never let this break the main flow
            try
            {
                await _notificationService.SendReconciliationAlertAsync(new ReconciliationAlertRequest
                {
                    DebitReference = debitReference,
                    CreditReference = creditReference,
                    Reason = reason,
                    FlaggedAt = log.FlaggedAt
                });
            }
            catch
            {
                // Notification failure must never swallow or re-throw —
                // the reconciliation log is already saved at this point
            }
        }

        public async Task<WalletReconciliationLog?> GetReconciliationLog(string debitReference)
        {
            return await _context.WalletReconciliationLogs
                .FirstOrDefaultAsync(x => x.DebitReference == debitReference);
        }

        public async Task<List<WalletReconciliationLog>> GetPendingReconciliations()
        {
            return await _context.WalletReconciliationLogs
                .Where(x => x.Status == ReconciliationStatus.Pending
                         || x.Status == ReconciliationStatus.InReview)
                .OrderBy(x => x.FlaggedAt)
                .ToListAsync();
        }

        public async Task ResolveReconciliation(
            long reconciliationId,
            string resolvedBy,
            string resolutionNote,
            ReconciliationStatus finalStatus)
        {
            var log = await _context.WalletReconciliationLogs
                .FirstOrDefaultAsync(x => x.Id == reconciliationId);

            if (log == null)
                throw new BaseException(
                    ExceptionType.NOTFOUND,
                    $"Reconciliation log {reconciliationId} not found");

            if (log.Status == ReconciliationStatus.Resolved ||
                log.Status == ReconciliationStatus.Reversed)
                throw new BaseException(
                    ExceptionType.OPERATION_FAILED,
                    $"Reconciliation {reconciliationId} is already {log.Status}");

            log.Status = finalStatus;
            log.ResolvedAt = DateTimeOffset.UtcNow;
            log.ResolvedBy = resolvedBy;
            log.ResolutionNote = resolutionNote;

            _context.WalletReconciliationLogs.Update(log);
            await _context.SaveChangesAsync();
        }
    }
}
