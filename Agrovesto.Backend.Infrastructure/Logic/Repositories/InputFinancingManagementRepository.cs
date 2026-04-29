using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Input;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using static Amazon.S3.Util.S3EventNotification;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories
{
    public class InputFinancingManagementRepository : IInputFinancingManagementRepository
    {
        private readonly DataContext _context;
        public InputFinancingManagementRepository(DataContext context)
        {
            _context = context;
        }
        #region inputfinancing


        public async Task<PagedListResponse<List<FarmerInputObligationDto>>> GetFarmerInputObligations(
            ListQueryParams args)
        {
            var allocations = await _context.InputAllocations
                .AsNoTracking()
                .Include(a => a.Input)
                .Where(a => a.FarmerId == args.farmerId)
                .ToListAsync();

            if (!allocations.Any())
            {
                return new PagedListResponse<List<FarmerInputObligationDto>>
                {
                    Data = new List<FarmerInputObligationDto>(),
                    Count = 0,
                    PageNumber = args.PageNumber,
                    PageSize = args.PageSize
                };
            }

            var allocationIds = allocations.Select(a => a.Id).ToList();

            var query = _context.InputTransactions
                .AsNoTracking()
                .Where(t =>
                    t.FarmerId == args.farmerId &&
                    t.TransactionType == InputTransactionType.Repayment &&
                    !t.IsDeleted);

            var totalCount = await query.CountAsync();

            var pageNumber = args.PageNumber <= 0 ? 1 : args.PageNumber;
            var pageSize = args.PageSize <= 0 ? 20 : args.PageSize;

            var skip = (pageNumber - 1) * pageSize;

            var allRepaymentsUpToPage = await query
                .OrderBy(t => t.TransactionDate)
                .Take(skip + pageSize)
                .ToListAsync();

            var items = new List<FarmerInputObligationDto>();

            foreach (var allocation in allocations)
            {
                var loanAmount = allocation.TotalCost;

                decimal runningTotal = allRepaymentsUpToPage
                    .Take(skip)
                    .Sum(x => x.Amount);

                foreach (var payment in allRepaymentsUpToPage.Skip(skip))
                {
                    runningTotal += payment.Amount;

                    var balanceRemaining = loanAmount - runningTotal;

                    if (balanceRemaining < 0)
                        balanceRemaining = 0;

                    decimal repaymentProgress = 0;

                    if (loanAmount > 0)
                    {
                        repaymentProgress = (runningTotal / loanAmount) * 100;
                    }

                    if (repaymentProgress > 100)
                        repaymentProgress = 100;

                    items.Add(new FarmerInputObligationDto
                    {
                        Date = payment.TransactionDate,
                        TotalCost = loanAmount,
                        AmountPaid = payment.Amount,
                        RemainigBalance = balanceRemaining,
                        Name = allocation.Input?.Name,
                        RepaymentProgress = Math.Round(repaymentProgress, 2)
                    });
                }
            }

            return new PagedListResponse<List<FarmerInputObligationDto>>
            {
                Data = items
                    .OrderByDescending(x => x.Date)
                    .ToList(),

                Count = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
        public async Task<LoanDetailDto> GetLoanDetailAsync(long inputAllocationId)
        {
            var allocation = await _context.InputAllocations
                .Include(a => a.Input)
                .Include(a => a.Farmer)
                .FirstOrDefaultAsync(a => a.Id == inputAllocationId);

            if (allocation == null)
                return null;

            var inputs = await _context.InputAllocations
                .Where(a => a.Id == inputAllocationId)
                .Select(a => new InputBreakdownDto
                {
                    InputName = a.Input.Name,
                    Quantity = a.Quantity,
                    UnitPrice = a.UnitPrice,
                    TotalCost = a.TotalCost
                })
                .ToListAsync();

            var loanAmount = allocation.TotalCost;

            var installments = await _context.InputTransactions
                .Where(t => t.FarmerId == allocation.FarmerId
                            && t.TransactionType == InputTransactionType.Repayment
                            && !t.IsDeleted)
                .OrderBy(t => t.TransactionDate)
                .Select(t => new InstallmentDto
                {
                    PaymentDate = t.TransactionDate,
                    Amount = t.Amount,
                    Reference = t.ReferenceNumber
                })
                .ToListAsync();

            var totalRepaid = installments.Sum(x => x.Amount);

            var interestRate = 0.05m; // default 5% (replace with DB field if available)
            var interestAmount = loanAmount * interestRate;
            var totalRepayable = loanAmount + interestAmount;

            var dueDate = allocation.AllocatedDate.AddDays(90);

            var installmentCount = 3;
            var expectedInstallment = totalRepayable / installmentCount;

            var schedule = new List<RepaymentScheduleDto>();

            for (int i = 1; i <= installmentCount; i++)
            {
                var due = allocation.AllocatedDate.AddDays(30 * i);

                var paidSoFar = totalRepaid >= (expectedInstallment * i);

                schedule.Add(new RepaymentScheduleDto
                {
                    InstallmentNo = i,
                    DueDate = due,
                    ExpectedAmount = expectedInstallment,
                    IsPaid = paidSoFar
                });
            }

            return new LoanDetailDto
            {
                LoanName = allocation.Input.Name,
                LoanAmount = loanAmount,
                DateIssued = allocation.AllocatedDate,
                DueDate = dueDate,
                InterestRate = interestRate,

                Inputs = inputs,
                InstallmentsPaid = installments,
                RepaymentSchedule = schedule
            };
        }
      
        public async Task<PagedListResponse<List<FinancingBreakdownDto>>> GetFinancingBreakdownAsync(ListQueryParams args)
        {
            var query = _context.InputAllocations
                .AsNoTracking()
                .Where(a => a.InputTransactionId != null);

            if (args.Id > 0)
                query = query.Where(a => a.FarmerId == args.Id);

            var totalCount = await query.CountAsync();

            var pageNumber = args.PageNumber <= 0 ? 1 : args.PageNumber;
            var pageSize = args.PageSize <= 0 ? 20 : args.PageSize;

            var skip = (pageNumber - 1) * pageSize;

            var data = await query
                .OrderByDescending(a => a.AllocatedDate)
                .Skip(skip)
                .Take(pageSize)
                .Select(a => new
                {
                    LoanName = a.Input.Name,
                    Amount = a.TotalCost,
                    DateIssued = a.AllocatedDate,

                    TotalRepaid = _context.InputTransactions
                        .Where(t => t.FarmerId == a.FarmerId
                                    && t.TransactionType == InputTransactionType.Repayment
                                    && !t.IsDeleted)
                        .Sum(t => (decimal?)t.Amount) ?? 0,

                    TotalDisbursed = _context.InputTransactions
                        .Where(t => t.FarmerId == a.FarmerId
                                    && t.TransactionType == InputTransactionType.Disbursement
                                    && !t.IsDeleted)
                        .Sum(t => (decimal?)t.Amount) ?? 0
                })
                .ToListAsync();

            var items = data.Select(x =>
            {
                var repaymentPercentage = x.TotalDisbursed == 0
                    ? 0
                    : (x.TotalRepaid / x.TotalDisbursed) * 100;

                string status =
                    repaymentPercentage >= 100 ? "Paid" :
                    repaymentPercentage > 0 ? "Partially Paid" :
                    "Unpaid";

                return new FinancingBreakdownDto
                {
                    LoanName = x.LoanName,
                    Amount = x.Amount,
                    DateIssued = x.DateIssued,
                    DueDate = x.DateIssued.AddDays(90),
                    RepaymentPercentage = Math.Round(repaymentPercentage, 2),
                    Status = status
                };
            }).ToList();

            return new PagedListResponse<List<FinancingBreakdownDto>>
            {
                Data = items,
                Count = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
        public async Task<InputLoanSummaryDto> GetInputLoanSummaryAsync(ListQueryParams args)
        {
            var totalLoansIssued = await _context.InputAllocations
                .Where(x => x.InputTransactionId != null && x.FarmerId == args.Id)
                .SumAsync(x => (decimal?)((x.Quantity * x.UnitPrice))) ?? 0;

            var totalRepaid = await _context.InputTransactions
                .Where(t => t.TransactionType == InputTransactionType.Repayment
                            && t.FarmerId == args.Id
                            && !t.IsDeleted)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var totalActiveLoans = await _context.InputTransactions
                .Where(t => t.TransactionType == InputTransactionType.Disbursement
                            && t.FarmerId == args.Id
                            && t.PaymentStatus != PaymentStatus.Paid
                            && !t.IsDeleted)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var outstanding = await _context.InputTransactions
                .Where(t => !t.IsDeleted && t.FarmerId == args.Id)
                .GroupBy(t => t.FarmerId)
                .Select(g => new
                {
                    Disbursed = g.Where(x => x.TransactionType == InputTransactionType.Disbursement)
                                 .Sum(x => (decimal?)x.Amount) ?? 0,

                    Repaid = g.Where(x => x.TransactionType == InputTransactionType.Repayment)
                              .Sum(x => (decimal?)x.Amount) ?? 0
                })
                .SumAsync(x => x.Disbursed - x.Repaid);

            return new InputLoanSummaryDto
            {
                TotalLoansIssued = totalLoansIssued,
                TotalActiveLoans = totalActiveLoans,
                TotalRepaid = totalRepaid,
                TotalOutstandingBalance = outstanding
            };
        }

        public async Task<PagedListResponse<List<RepaymentBreakdownDto>>> GetRepaymentBreakdownAsync(
      long inputAllocationId,
      ListQueryParams args)
        {
            var allocation = await _context.InputAllocations
                .AsNoTracking()
                .Include(a => a.Input)
                .FirstOrDefaultAsync(a => a.Id == inputAllocationId);

            if (allocation == null)
            {
                return new PagedListResponse<List<RepaymentBreakdownDto>>
                {
                    Data = new List<RepaymentBreakdownDto>(),
                    Count = 0,
                    PageNumber = args.PageNumber,
                    PageSize = args.PageSize
                };
            }

            var loanAmount = allocation.TotalCost;

            var query = _context.InputTransactions
                .AsNoTracking()
                .Where(t => t.FarmerId == allocation.FarmerId
                            && t.TransactionType == InputTransactionType.Repayment
                            && !t.IsDeleted);

            var totalCount = await query.CountAsync();

            var pageNumber = args.PageNumber <= 0 ? 1 : args.PageNumber;
            var pageSize = args.PageSize <= 0 ? 20 : args.PageSize;

            var skip = (pageNumber - 1) * pageSize;

            var pagedRepayments = await query
                .OrderBy(t => t.TransactionDate)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            var allRepaymentsUpToPage = await query
                .OrderBy(t => t.TransactionDate)
                .Take(skip + pageSize)
                .ToListAsync();

            decimal runningTotal = 0;

            var items = new List<RepaymentBreakdownDto>();

            foreach (var payment in allRepaymentsUpToPage.Skip(skip))
            {
                runningTotal += payment.Amount;

                var balanceRemaining = loanAmount - runningTotal;
                if (balanceRemaining < 0)
                    balanceRemaining = 0;

                items.Add(new RepaymentBreakdownDto
                {
                    Date = payment.TransactionDate,
                    LoanAmount = loanAmount,
                    AmountPaid = payment.Amount,
                    MethodOfPayment = string.IsNullOrWhiteSpace(payment.Description)
                        ? payment.ReferenceNumber
                        : payment.Description,
                    BalanceRemaining = balanceRemaining
                });
            }

            return new PagedListResponse<List<RepaymentBreakdownDto>>
            {
                Data = items,
                Count = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
        public async Task<FinancingRepaymentSummaryDto> GetFinancingRepaymentSummaryAsync(long inputAllocationId)
        {
            var allocation = await _context.InputAllocations
                .AsNoTracking()
                .Include(a => a.Input)
                .FirstOrDefaultAsync(a => a.Id == inputAllocationId);

            if (allocation == null)
            {
                return new FinancingRepaymentSummaryDto
                {
                    TotalExpectedRepaymentValue = 0m,
                    TotalPaid = 0m,
                    TotalOutstanding = 0m,
                    RepaymentPercentage = 0m
                };
            }
            var totalExpected = (allocation.Quantity * allocation.UnitPrice);

            var totalPaid = await _context.InputTransactions
                .Where(t => t.FarmerId == allocation.FarmerId
                            && t.TransactionType == InputTransactionType.Repayment
                            && !t.IsDeleted)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            var totalOutstanding = totalExpected - totalPaid;
            if (totalOutstanding < 0)
                totalOutstanding = 0m;

            var repaymentPercentage = totalExpected == 0
                ? 0m
                : (totalPaid / totalExpected) * 100m;

            return new FinancingRepaymentSummaryDto
            {
                TotalExpectedRepaymentValue = totalExpected,
                TotalPaid = totalPaid,
                TotalOutstanding = totalOutstanding,
                RepaymentPercentage = Math.Round(repaymentPercentage, 2)
            };
        }
      
        
        #endregion inputfinancing


        public async Task<List<SubscriptionData>> FarmerBundleSubscriptions(ListQueryParams args)
        {
            var queryable = _context.BundleSubscriptionInterests
                .Where(w => w.BelongToBusinessCode == args.businessCode);

            // Conditionally apply organizationCode filter
            if (!string.IsNullOrEmpty(args.organizationCode))
            {
                queryable = queryable.Where(w => w.OrganizationCode == args.organizationCode);
            }

            var query = await queryable
                .GroupBy(b => 1) // Group everything together to get overall counts
                .Select(g => new
                {
                    ActiveSubscribers = g.Count(b =>
                        b.PaymentStatus == Enum.GetName(typeof(EquityPaymentStatus), EquityPaymentStatus.Paid) ||
                        b.PaymentStatus == Enum.GetName(typeof(EquityPaymentStatus), EquityPaymentStatus.AllowedDebit)),
                    PendingSubscribers = g.Count(b =>
                        b.PaymentStatus == Enum.GetName(typeof(EquityPaymentStatus), EquityPaymentStatus.Pending) ||
                        b.PaymentStatus == Enum.GetName(typeof(EquityPaymentStatus), EquityPaymentStatus.Initial)),
                    OverdueSubscribers = g.Count(b =>
                        b.PaymentStatus == Enum.GetName(typeof(EquityPaymentStatus), EquityPaymentStatus.Partial_Payment))
                })
                .FirstOrDefaultAsync();

            if (query == null)
            {
                return new List<SubscriptionData>();
            }

            return new List<SubscriptionData>
    {
        new SubscriptionData { Name = "Active Subscribers", Value = query.ActiveSubscribers },
        new SubscriptionData { Name = "Pending Subscribers", Value = query.PendingSubscribers },
        new SubscriptionData { Name = "Overdue Subscribers", Value = query.OverdueSubscribers }
    };
        }


        #region report
        public async Task<List<BundleSubscriptionInterestModel>> FilterSubscriptionForDownload(
string? businessCode,
string? cooperativeName,
string? state,
DateTime? startDate,
DateTime? endDate)
        {
            var query = _context.BundleSubscriptionInterests.AsQueryable();

            if (!string.IsNullOrWhiteSpace(businessCode))
            {
                query = query.Where(x => x.BelongToBusinessCode == businessCode);
            }

            if (!string.IsNullOrWhiteSpace(state))
            {
                query = query.Where(x => x.Location == state);
            }

            if (startDate.HasValue)
            {
                query = query.Where(x => x.PaymentDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(x => x.PaymentDate <= endDate.Value);
            }

            var subscriptions = await query.ToListAsync();

            if (subscriptions == null || !subscriptions.Any())
            {
                throw new Exception("No record found");
            }

            return subscriptions.Select(x => x.Map()).ToList();
        }


        public async Task<List<BundleDisbursementModel>> FilterDisbursementForDownload(
string? businessCode,
string? disbursedBy,
string? state,
DateTime? startDate,
DateTime? endDate)
        {
            var query = _context.BundleSubscriptionDisbursements.AsQueryable();

            if (!string.IsNullOrWhiteSpace(businessCode))
            {
                query = query.Where(x => x.BelongToBusinessCode == businessCode);
            }

            if (!string.IsNullOrWhiteSpace(state))
            {
                query = query.Where(x => x.Location == state);
            }  if (!string.IsNullOrWhiteSpace(disbursedBy))
            {
                query = query.Where(x => x.DisbursedBy == disbursedBy);
            }

            if (startDate.HasValue)
            {
                query = query.Where(x => x.DisbursedOn >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(x => x.DisbursedOn <= endDate.Value);
            }

            var subscriptions = await query.ToListAsync();

            if (subscriptions == null || !subscriptions.Any())
            {
                throw new Exception("No record found");
            }

            return subscriptions.Select(x => x.Map()).ToList();
        }

        #endregion report

        public async Task<List<BundleSubscriptionSummary>> FarmerBundleSubscriptionInterests(ListQueryParams args)
        {
            var queryable = _context.BundleSubscriptionInterests
                .Where(w => w.BelongToBusinessCode == args.businessCode);

            // Conditionally apply organizationCode filter
            if (!string.IsNullOrEmpty(args.organizationCode))
            {
                queryable = queryable.Where(w => w.OrganizationCode == args.organizationCode);
            }

            var query = await queryable
                .GroupBy(b => b.InterestedBundleName) // Group by bundle type
                .Select(g => new BundleSubscriptionSummary
                {
                    Name = g.Key,
                    Value = g.Count() // Count number of farmers subscribed to each bundle
                })
                .ToListAsync();

            if (query.Count == 0)
            {
                return null;
            }

            return query;
        }
        public async Task<(bool status, BundleSubscriptionInterestModel data)> CreateInputFinancingInterestThroughBundle(BundleSubscriptionInterestModel data)
        {
            
            var entity = data.Map();
            await _context.AddAsync(entity);
            await _context.SaveChangesAsync();

            return (true, entity.Map());
        }
        public async Task<List<BundleSubscriptionInterestModel>> isIdentifierPresent(BundleSubscriptionInterestModel data)
        {
            var interest = await _context.BundleSubscriptionInterests.Where(x => x.FarmerUserId == data.FarmerUserId).ToListAsync();

            if (!interest.Any()) { return null; }

            return interest.Select(x => x.Map()).ToList();
        }
        public async Task<List<BundleSubscriptionInterestModel>> GetExistingSubscription(BundleSubscriptionInterestModel data)
        {
            var interest = await _context.BundleSubscriptionInterests.Where(x => x.FarmerUserId == data.FarmerUserId && x.PaymentStatus != EquityPaymentStatus.Paid.ToString()
            && x.InterestedBundleId == data.InterestedBundleId).ToListAsync();

            if (!interest.Any()) { return null; }

            return interest.Select(x => x.Map()).ToList();
        }
        public async Task<List<BundleSubscriptionInterestModel>> GetFarmerSubscriptionInterestDetails(BundleSubscriptionInterestModel data)
        {
            var interest = await _context.BundleSubscriptionInterests.Where(x => x.FarmerUserId == data.FarmerUserId).ToListAsync();

            if (!interest.Any()) { return null; }

            return interest.Select(x => x.Map()).ToList();
        }
        public async Task<List<BundleSubscriptionInterestModel>> GetUnpaidSubscriptionInterestByFarmer(BundleSubscriptionInterestModel data)
        {
            var interest = await _context.BundleSubscriptionInterests.Where(x => x.FarmerUserId == data.FarmerUserId && x.PaymentStatus != EquityPaymentStatus.Paid.ToString()).ToListAsync();

            if (!interest.Any()) { return null; }

            return interest.Select(x => x.Map()).ToList();
        }
        public async Task<List<BundleSubscriptionInterestModel>> GetSubscriptionInterestByLocation(string businessCode, string Location)
        {
            var interest = await _context.BundleSubscriptionInterests.Where(x => x.BelongToBusinessCode == businessCode && x.Location == Location).ToListAsync();

            if (!interest.Any()) { return null; }

            return interest.Select(x => x.Map()).ToList();
        }
        public async Task<List<BundleSubscriptionInterestModel>> GetSubscriptionInterestByCooperative(string businessCode, long CooperativeId)
        {
            var interest = await _context.BundleSubscriptionInterests.Where(x => x.BelongToBusinessCode == businessCode && x.CooperativeId == CooperativeId).ToListAsync();

            if (!interest.Any()) { return null; }

            return interest.Select(x => x.Map()).ToList();
        }
        public async Task<PagedListResponse<List<BundleSubscriptionInterestModel>>> GetInterestRequestByAgent(
      string businessCode,
      string userId,
      int pageNumber = 1,
      int pageSize = 10,
      string organizationCode = null)
        {
            var interestQuery = _context.BundleSubscriptionInterests
                .Where(x => x.BelongToBusinessCode == businessCode && x.OfficerInChargeUserId == userId);

            // Optional organization filter
            if (!string.IsNullOrWhiteSpace(organizationCode))
            {
                interestQuery = interestQuery.Where(x => x.OrganizationCode == organizationCode);
            }

            var totalCount = await interestQuery.CountAsync();

            if (totalCount == 0)
            {
                return null;
            }

            var pagedResult = await interestQuery
                .OrderByDescending(x => x.CreatedOn)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedListResponse<List<BundleSubscriptionInterestModel>>
            {
                Data = pagedResult.Select(x => x.Map()).ToList(),
                Count = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<BundleInterestReceiptModel> RetrieveFarmerInputDisbursementReceipt(
           string businessCode,
           string farmerUserId,
           string reference)
        {
            // Retrieve the bundle subscription interest
            var interest = await _context.BundleSubscriptionInterests
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.BelongToBusinessCode == businessCode &&
                    x.FarmerUserId == farmerUserId &&
                    x.Reference == reference);

            if (interest == null) return null;

            // Fetch the farmer details
            var farmer = await _context.Farmers
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.UserId == farmerUserId);

            if (farmer == null) return null;

            // Fetch all cooperatives linked via FarmerCooperatives
            var cooperatives = await (
                from fc in _context.FarmerCooperatives.AsNoTracking()
                join coop in _context.MemberShips.AsNoTracking()
                    on fc.CooperativeId equals coop.Id
                where fc.FarmerId == farmer.Id
                select coop
            ).ToListAsync();

            // Map the receipt
            var receipt = new BundleInterestReceiptModel
            {
                SubscriptionInterest = interest.Map(),
                Farmer = farmer.Map(),
                Cooperatives = cooperatives.Select(c => c.Map()).ToList()
            };

            return receipt;
        }

        public async Task<BundleSubscriptionInterestModel> GetPaidSubscriptionByFarmer(string businessCode, string farmerUserId, string reference)
        {
            var interest = await _context.BundleSubscriptionInterests.
                Where(x => x.BelongToBusinessCode == businessCode && x.FarmerUserId == farmerUserId && x.Reference == reference)
                .FirstOrDefaultAsync();

            if (interest == null) { return null; }

            return interest.Map();
        }
        public async Task<PagedListResponse<List<BundleSubscriptionInterestModel>>> GetSubscriptionInterestByBusiness(string businessCode, int pageNumber = 1,
      int pageSize = 10,
         DateTimeOffset? startDate = null,
         DateTimeOffset? endDate = null,
         string? location = null,
         string? organizationCode =null)
        {
            var query = _context.BundleSubscriptionInterests.Where(x => x.BelongToBusinessCode == businessCode
            ).AsQueryable();

            if (!string.IsNullOrEmpty(organizationCode))
            {
                query=query.Where(x=>x.OrganizationCode==organizationCode);
            }

            if (startDate.HasValue)
            {
                query = query.Where(x => x.CreatedOn >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(x => x.CreatedOn <= endDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(x => x.Location == location);
            }
            if (query.Count() == 0) { return null; }
            var totalCount = await query.CountAsync();
            var data = await query.OrderByDescending(x => x.CreatedOn)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => x.Map()).ToListAsync();
            return new PagedListResponse<List<BundleSubscriptionInterestModel>>
            {
                Data = data,//pagedResult.Select(x => x.Map()).ToList(),
                Count = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

        }
        public async Task<BundleSubscriptionInterestModel> UpdateSubscriptionPayment(BundleSubscriptionInterestModel model)
        {
            var interest = await _context.BundleSubscriptionInterests.Where(x => x.FarmerUserId == model.FarmerUserId && x.InterestedBundleId == model.InterestedBundleId).FirstOrDefaultAsync();

            if (interest == null) return null;

            interest.PaymentStatus = Enum.GetName(typeof(EquityPaymentStatus), model.Status);
            interest.SubscriptionStatus = Enum.GetName(typeof(InputFinancingStatus), model.EquitySubscriptionStatus);
            interest.PaymentDate = model.PaymentDate;
            interest.IsCommissionPaidOut = model.IsCommissionPaidOut;
            interest.TotalQuantityPaidFor = model.TotalQuantityPaidFor;
            interest.TotalEquityPaid = model.TotalEquityPaid;
            interest.Reference = model.Reference;
            interest.MaturityDate = model.MaturityDate;
            interest.IsDebitted = model.IsDebitted;
            await _context.SaveChangesAsync();

            return interest.Map();


        }



        public async Task<BundleSubscriptionInterestModel> SettlePendingFarmerBundleSubscriptionEquityToBusiness(BundleSubscriptionInterestModel model)
        {
            var interest = await _context.BundleSubscriptionInterests.Where(x => x.FarmerUserId == model.FarmerUserId && x.InterestedBundleId == model.InterestedBundleId).FirstOrDefaultAsync();

            if (interest == null) return null;

            interest.PaymentStatus = Enum.GetName(typeof(EquityPaymentStatus), model.Status);
            interest.IsDebitted = model.IsDebitted;

            await _context.SaveChangesAsync();

            return interest.Map();


        }
        public async Task<BundleDisbursementModel> UpdateDisbursement(BundleDisbursementModel model)
        {
            var disbursement = model.Map();
            disbursement.PaymentStatus = Enum.GetName(typeof(EquityPaymentStatus), EquityPaymentStatus.Paid);
            disbursement.TotalQuantityReceived = model.TotalQuantityPaidFor;
            await _context.AddAsync(disbursement);
            await _context.SaveChangesAsync();

            return disbursement.Map();
        }
        public async Task<List<BundleDisbursementModel>> GetDisbursedSubscriptions(string businessCode)
        {
            var interest = await _context.BundleSubscriptionDisbursements.Where(x => x.BelongToBusinessCode == businessCode).ToListAsync();

            if (interest.Count == 0) { return null; }

            return interest.Select(x => x.Map()).ToList();
        }
     

        public async Task<List<BundleSubscriptionInterestModel>> GetPaidSubscriptionInterestByBusiness(string businessCode)
        {
            var interest = await _context.BundleSubscriptionInterests.Where(x => x.BelongToBusinessCode == businessCode && x.PaymentStatus == "Paid").ToListAsync();

            if (!interest.Any()) { return null; }

            return interest.Select(x => x.Map()).ToList();
        }
        public async Task<List<BundleSubscriptionInterestModel>> GetSubscriptionInterestByBusinessAgent(string businessCode, string userId)
        {
            var interest = await _context.BundleSubscriptionInterests.Where(x => x.BelongToBusinessCode == businessCode && x.OfficerInChargeUserId == userId && x.PaymentStatus != "Paid")
                .OrderByDescending(x => x.CreatedOn).ToListAsync();

            if (!interest.Any()) { return null; }

            return interest.Select(x => x.Map()).ToList();
        }

        public async Task<List<BundleSubscriptionInterestModel>> GetPaidSubscriptionInterestByBusinessAgent(string businessCode, string userId)
        {
            var interest = await _context.BundleSubscriptionInterests.Where(x => x.BelongToBusinessCode == businessCode && x.OfficerInChargeUserId == userId && x.PaymentStatus == "Paid")
                .OrderByDescending(x => x.CreatedOn).ToListAsync();

            if (!interest.Any()) { return null; }

            return interest.Select(x => x.Map()).ToList();
        }

        public async Task<BundleSubscriptionInterestModel> UpdateEquitySubcriiptonStatus(BundleSubscriptionInterestModel model)
        {
            var interest = await _context.BundleSubscriptionInterests.Where(x => x.FarmerUserId == model.FarmerUserId && x.InterestedBundleId == model.InterestedBundleId).FirstOrDefaultAsync();

            if (interest != null)
            {
                // interest.PaymentStatus = Enum.GetName(typeof(EquityPaymentStatus), model.Status); 
                   interest.PaymentStatus = Enum.GetName(typeof(EquityPaymentStatus), model.Status);
                interest.SubscriptionStatus = Enum.GetName(typeof(InputFinancingStatus), model.EquitySubscriptionStatus);
                interest.PaymentDate = model.PaymentDate;
                interest.IsCommissionPaidOut = model.IsCommissionPaidOut;
                interest.TotalQuantityPaidFor = model.TotalQuantityPaidFor;
                interest.TotalEquityPaid = model.TotalEquityPaid;
                interest.Reference = model.Reference;
                interest.MaturityDate = model.MaturityDate;

                await _context.SaveChangesAsync();
                return interest.Map();
            }
            return null;

        }

        public async Task<BundleSubscriptionInterestModel> ApproveEquityForDisbursement(BundleSubscriptionInterestModel model)
        {
            var interest = await _context.BundleSubscriptionInterests.Where(x => x.FarmerUserId == model.FarmerUserId && x.InterestedBundleId == model.InterestedBundleId).FirstOrDefaultAsync();

            if (interest != null)
            {
                interest.SubscriptionStatus = Enum.GetName(typeof(InputFinancingStatus), model.EquitySubscriptionStatus);

                await _context.SaveChangesAsync();
                return interest.Map();
            }
            return null;

        }

        public async Task<BundleDisbursementModel> GetDisbursedSubscription(string businessCode, string farmerUserId, string reference)
        {
            var interest = await _context.BundleSubscriptionDisbursements.Where(x => x.BelongToBusinessCode == businessCode && x.FarmerUserId == farmerUserId && x.Reference == reference).FirstOrDefaultAsync();

            if (interest == null) { return null; }

            return interest.Map();
        }

        public async Task<PagedListResponse<List<BundleDisbursementModel>>> GetDisbursementByBusiness(
    string businessCode,
    int pageNumber = 1,
    int pageSize = 10,
    DateTimeOffset? startDate = null,
    DateTimeOffset? endDate = null,
    string? location = null,
    string? disbursedBy = null,
    string? organizationCode = null)
        {
            var query = _context.BundleSubscriptionDisbursements
                .Where(x => x.BelongToBusinessCode == businessCode)
                .AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(x => x.DisbursedOn >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(x => x.DisbursedOn <= endDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(x => x.Location == location);
            }

            if (!string.IsNullOrWhiteSpace(disbursedBy))
            {
                query = query.Where(x => x.DisbursedBy == disbursedBy);
            }

            if (!string.IsNullOrWhiteSpace(organizationCode))
            {
                query = query.Where(x => x.OrganizationCode == organizationCode);
            }

            var totalCount = await query.CountAsync();
            if (totalCount == 0) { return null; }

            var data = await query
                .OrderByDescending(x => x.DisbursedOn)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => x.Map())
                .ToListAsync();

            return new PagedListResponse<List<BundleDisbursementModel>>
            {
                Data = data,
                Count = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<PagedListResponse<List<BundleDisbursementModel>>> GetDisbursementByAgent(string businessCode, string warehouseManger,int pageNumber = 1,
int pageSize = 10,
DateTimeOffset? startDate = null,
DateTimeOffset? endDate = null,
string? location = null,
string? disbursedBy = null)
        {
            var query = _context.BundleSubscriptionDisbursements.Where(x => x.BelongToBusinessCode == businessCode && x.OfficerInChargeUserId == warehouseManger
            ).AsQueryable();



            if (startDate.HasValue)
            {
                query = query.Where(x => x.DisbursedOn >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(x => x.DisbursedOn <= endDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(x => x.Location == location);
            }
            if (!string.IsNullOrWhiteSpace(disbursedBy))
            {
                query = query.Where(x => x.DisbursedBy == disbursedBy);
            }
            if (query.Count() == 0) { return null; }
            var totalCount = await query.CountAsync();
            var data = await query.OrderByDescending(x => x.DisbursedOn)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => x.Map()).ToListAsync();
            return new PagedListResponse<List<BundleDisbursementModel>>
            {
                Data = data,//pagedResult.Select(x => x.Map()).ToList(),
                Count = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

        }


        public async Task<BundleSubscriptionInterestModel> GetSingleSubscription(string farmerUserId, long bundleId)
        {
            var interest = await _context.BundleSubscriptionInterests.Where(x => x.FarmerUserId == farmerUserId 
            && x.InterestedBundleId == bundleId && x.SubscriptionStatus == InputFinancingStatus.Disbursed.ToString()).FirstOrDefaultAsync();

            if (interest == null) { return null; }

            return interest.Map();
        }

        #region repayment


        public async Task SaveBundleSubscriptionRepayment(RepaymentModel model)
        {

            var existing = await _context.Repayments.Where(x => x.InterestedBundleId == model.InterestedBundleId).FirstOrDefaultAsync(); ;
            if (existing != null)
            {
                if (existing.AmountToRepay == (existing.AmountPaid + model.AmountPaid))
                {
                    existing.IsComplete = true;
                    existing.CompletedOn = DateTimeOffset.UtcNow;

                }
                    existing.AmountPaid+= model.AmountPaid;
                existing.RepaymentBreakdown = model.RepaymentBreakdown.Select(i => new RepaymentBreakdown
                {

                    AmountOrEquivalent = i.AmountOrEquivalent,
                    PaidOn =  DateTimeOffset.Now,

                    RepaymentId = existing.Id,
                    RepaymentMode = Enum.GetName(typeof(Entities.Businesses.RepaymentMode), i.RepaymentMode)

                }).ToList();
       
            }
            else
            {
                var entity = model.Map();
                entity.IsComplete = false;
                entity.CompletedOn = null; 
                await _context.Repayments.AddAsync(entity);
            }
            await _context.SaveChangesAsync();
         
        }

        #endregion repayment
    }
}
