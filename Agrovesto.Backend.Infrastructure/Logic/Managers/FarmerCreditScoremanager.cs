using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers
{
    public class FarmerCreditScoremanager : IFarmerCreditScoremanager
    {
        private readonly IFarmerRepository _farmerRepo;
        private readonly IInputFinancingManagementRepository _inputFinanceRepo;

        private readonly IBusinessRepository _businessRepository;
        private readonly IFarmerCreditScoreRepository _scoreRepo;
        private readonly DataContext _context;
        public FarmerCreditScoremanager(DataContext context,IInputFinancingManagementRepository inputFinanceRepo,IFarmerRepository farmerRepo, IBusinessRepository businessRepository,IFarmerCreditScoreRepository scoreRepo)
        {
            _farmerRepo = farmerRepo;
            _scoreRepo = scoreRepo;
            _businessRepository = businessRepository;
            _inputFinanceRepo= inputFinanceRepo;
            _context= context;
        }

        public async Task<PagedListResponse<List<FarmerCreditScoreModel>>> GetFarmerScoreAsync(BusinessRegistrationArgs args)
        {
            try
            {
                var score = await _scoreRepo.GetCreditScoreByBusiness(args.businessCode,args.PageNumber,args.PageSize,args.organizationCode);
            if (score == null) return null;

            if (score == null) return new PagedListResponse<List<FarmerCreditScoreModel>>
            {
                Succeeded = true,
                Data = null,
                Count = 0,
                Message = "No Record Found"
            };
            return new PagedListResponse<List<FarmerCreditScoreModel>>
            {
                Succeeded = true,
                Data = score.Data,
                Count = score.Count,
                Message = "Retrieved Successfully"
            };
        }
            catch (BaseException ex)
            {
               // _log.LogError($"Error retrieving Agents: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
    }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
}
        }
        public async Task<long> CalculateScoreAsync(BusinessRegistrationArgs args)
        {
            var farmer = await _farmerRepo.GetFarmerDetails(args.UserId);
            if (farmer == null) return 0;

            var repaymentWeights = await _scoreRepo.GetCreditScoreWeightByBusiness(args.businessCode, args.organizationCode);
            if (repaymentWeights == null) return 0;

            long totalScore = 0;

            // 1. Repayment history: Calculate partial points for partial repayments
            if (farmer.Repayments != null && farmer.Repayments.Any())
            {
                double totalRepaymentScorePercent = 0;
                // Flatten all RepaymentBreakdown lists into one enumerable
                var allBreakdowns = farmer.Repayments
                    .Where(r => r.RepaymentBreakdown != null) // optional null check
                    .SelectMany(r => r.RepaymentBreakdown);

                // Sum the amount paid from all breakdown entries
                var totalAmountPaid = allBreakdowns.Sum(b => b.AmountOrEquivalent);

                foreach (var repayment in farmer.Repayments)
                {
                    if (repayment.IsComplete)
                    {
                        // Full points for completed repayment
                        totalRepaymentScorePercent += 100;
                    }
                    else if (repayment.AmountToRepay > 0 && repayment.AmountPaid > 0)
                    {
                        // Partial points proportional to amount paid
                        var partialPercent = (double)(totalAmountPaid / repayment.AmountToRepay) * 100;
                        // You may want to cap partialPercent to 100 in case of overpayment
                        partialPercent = Math.Min(partialPercent, 100);
                        totalRepaymentScorePercent += partialPercent;
                    }
                    else
                    {
                        // No payment made, no points
                        totalRepaymentScorePercent += 0;
                    }
                }

                // Average score percentage across repayments
                var repaymentScorePercent = totalRepaymentScorePercent / farmer.Repayments.Count;

                totalScore += (long)((repaymentScorePercent * repaymentWeights.RepaymentWeight) / 100);
            }
            else
            {
                // No repayments: default partial score?
                totalScore += (repaymentWeights.RepaymentWeight / 2); // e.g. 50% of repayment weight
            }

            // 6. Timeliness of repayment: count repayments completed on or before due date
            var timelyRepaymentsCount = farmer.Repayments
                .Count(r => r.IsComplete && r.CompletedOn.HasValue && r.CompletedOn.Value.Date <= r.PaymentDueDate.Date);

            int timelinessScore = (farmer.Repayments.Any())
                ? (timelyRepaymentsCount * 100 / farmer.Repayments.Count())
                : 50;

            totalScore += (timelinessScore * repaymentWeights.TimelinessWeight) / 100;

            // Add subscription weight (assuming it's a fixed bonus)
            totalScore += repaymentWeights.SubcriptionWeight;

            return totalScore;
        }
        public string GetRiskLevel(long score)
        {
            if (score >= 70) return "Low";
            if (score >= 40) return "Moderate";
            return "High";
        }
        public async Task UpdateFarmerScores(BusinessRegistrationArgs args)
        {
            try
            {
                var farmers = await _context.Farmers.Where(f => f.UserId == args.UserId).FirstOrDefaultAsync();

                var score = await CalculateScoreAsync(args);
                var riskLevel = GetRiskLevel(score);

                var creditRecord = await _context.FarmerCreditScores
                    .FirstOrDefaultAsync(f => f.FarmerId == farmers.Id);

                if (creditRecord != null)
                {
                    creditRecord.Score = score;
                    creditRecord.RiskLevel = riskLevel;
                    creditRecord.LastUpdated = DateTime.UtcNow;

                }
                else
                {
                    _context.FarmerCreditScores.Add(new FarmerCreditScore
                    {
                        FarmerId = farmers.Id,
                        Score = score,
                        RiskLevel = riskLevel,
                        BusinessCode = args.businessCode,
                        OrganizationCode = args.organizationCode,
                        LastUpdated = DateTime.UtcNow,
                        
                    });
                }


                await _context.SaveChangesAsync();
            }
            catch(Exception ex)
            {
                throw new BaseException(Core.Enums.ExceptionType.OPERATION_FAILED, "Could not update credit score,  try again");
            }
        }


        public async Task UpdateAllFarmerScores(BusinessRegistrationArgs args)
        {
            var farmers = await _context.Farmers.Select(f => f.Id).ToListAsync();
            foreach (var id in farmers)
            {
                var score = await CalculateScoreAsync(args);
                var riskLevel = GetRiskLevel(score);

                var creditRecord = await _context.FarmerCreditScores
                    .FirstOrDefaultAsync(f => f.FarmerId == id);

                if (creditRecord != null)
                {
                    creditRecord.Score = score;
                    creditRecord.RiskLevel = riskLevel;
                    creditRecord.LastUpdated = DateTime.UtcNow;

                }
                else
                {
                    _context.FarmerCreditScores.Add(new FarmerCreditScore
                    {
                        FarmerId = id,
                        Score = score,
                        RiskLevel = riskLevel,
                        BusinessCode=args.businessCode,
                        OrganizationCode=args.organizationCode,
                        LastUpdated = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

    }
}
