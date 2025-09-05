using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
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
        public async Task<List<SubscriptionData>> FarmerBundleSubscriptions(string businessCode)
        {
            var query = await _context.BundleSubscriptionInterests
                .Where(w => w.BelongToBusinessCode == businessCode)
                .GroupBy(b => 1) // Group everything together to get overall counts
                .Select(g => new
                {
                    ActiveSubscribers = g.Count(b => b.PaymentStatus == Enum.GetName(typeof(EquityPaymentStatus), EquityPaymentStatus.Paid) || b.PaymentStatus == Enum.GetName(typeof(EquityPaymentStatus), EquityPaymentStatus.AllowedDebit)),
                    PendingSubscribers = g.Count(b =>
                        b.PaymentStatus == Enum.GetName(typeof(EquityPaymentStatus), EquityPaymentStatus.Pending) ||
                        b.PaymentStatus == Enum.GetName(typeof(EquityPaymentStatus), EquityPaymentStatus.Initial)),
                    OverdueSubscribers = g.Count(b => b.PaymentStatus == Enum.GetName(typeof(EquityPaymentStatus), EquityPaymentStatus.Partial_Payment))
                })
                .FirstOrDefaultAsync(); // We expect a single object as output

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

        public async Task<List<BundleSubscriptionSummary>> FarmerBundleSubscriptionInterests(string businessCode)
        {
            var query = await _context.BundleSubscriptionInterests
                .Where(w => w.BelongToBusinessCode == businessCode)
                .GroupBy(b => b.InterestedBundleName) // Assuming BundleName represents the bundle type
                .Select(g => new BundleSubscriptionSummary
                {
                    Name = g.Key,
                    Value = g.Count() // Count number of farmers subscribed to each bundle
                })
                .ToListAsync();
            if (query.Count == 0) { return null; }
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

        public async Task<BundleInterestReceiptModel> RetrieveFarmerInputDisbursementReceipt(string businessCode, string farmerUserId, string reference)
        {
            var receipt = new BundleInterestReceiptModel();
            // Retrieve the bundle subscription interest
            var interest = await _context.BundleSubscriptionInterests
                .FirstOrDefaultAsync(x =>
                    x.BelongToBusinessCode == businessCode &&
                    x.FarmerUserId == farmerUserId &&
                    x.Reference == reference);

            if (interest == null)
            {
                return null;
            }

            // Fetch the farmer details
            var farmer = await _context.Farmers
                .FirstOrDefaultAsync(f => f.UserId == farmerUserId);

            if (farmer == null)
            {
                return null;
            }

            // Fetch the cooperative details
            var cooperative = await _context.Cooperatives
                .FirstOrDefaultAsync(c => c.Id == farmer.BelongToCooperativeId);

            // Map and return the result (assuming your model can handle these details)
            receipt.SubscriptionInterest = interest.Map(); // Assuming Map returns BundleSubscriptionInterestModel

            // If your model supports it, populate additional details
            receipt.Farmer = farmer.Map(); // You may need to create a mapping method
            receipt.Cooperative = cooperative?.Map(); // Optional chaining if cooperative is null

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
         string? location = null)
        {
            var query = _context.BundleSubscriptionInterests.Where(x => x.BelongToBusinessCode == businessCode
            ).AsQueryable();



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

        public async Task<PagedListResponse<List<BundleDisbursementModel>>> GetDisbursementByBusiness(string businessCode, int pageNumber = 1,
int pageSize = 10,
  DateTimeOffset? startDate = null,
  DateTimeOffset? endDate = null,
  string? location = null,
  string? disbursedBy =null)
        {
            var query = _context.BundleSubscriptionDisbursements.Where(x => x.BelongToBusinessCode == businessCode
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
                    RepaymentMode = Enum.GetName(typeof(RepaymentMode), i.RepaymentMode)

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
