using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories
{
    public class FarmerCreditScoreRepository : IFarmerCreditScoreRepository
    {
        private readonly DataContext _context;
        public FarmerCreditScoreRepository(DataContext context)
        {
            _context = context;
        }
        public async Task<CreditScoreWeightsModel> GetCreditScoreWeightByBusiness(string businessCode, string organizationCode=null)      
        {
            var query = _context.CreditScoreWeights
        .Where(f => f.BusinessCode == businessCode);

            if (!string.IsNullOrEmpty(organizationCode))
            {
                query = query.Where(f => f.OrganizationCode.Contains(organizationCode));
            }
            var result = await query.FirstOrDefaultAsync();

            return result.Map(); ;
        }
        public async Task SaveCreditScoreWeightAsync(CreditScoreWeightsModel score)
        {
            var existing = await _context.CreditScoreWeights.FindAsync(score.Id);
            if (existing != null)
            {
                existing.RepaymentWeight = score.RepaymentWeight;
                existing.SubcriptionWeight = score.SubcriptionWeight;
                existing.TimelinessWeight = score.TimelinessWeight;
            }
            else
            {
                var entity = score.Map();
                await _context.CreditScoreWeights.AddAsync(entity);
            }
            await _context.SaveChangesAsync();
        }
        public async Task<FarmerCreditScore?> GetByFarmerIdAsync(long farmerId)
        {
            return await _context.FarmerCreditScores.FindAsync(farmerId);
        }
        public async Task<PagedListResponse<List<FarmerCreditScoreModel>>> GetCreditScorebybusiness(
    string businessCode,
    int pageNumber = 1,
    int pageSize = 10,
    string organizationCode = null)
        {
            var query = from creditScore in _context.FarmerCreditScores
                        join farmer in _context.Farmers on creditScore.FarmerId equals farmer.Id
                        join cooperative in _context.Cooperatives on farmer.BelongToCooperativeId equals cooperative.Id
                        join subaccount in _context.SubAccounts on farmer.UserId equals subaccount.UserId into subaccGroup
                        from subaccount in subaccGroup.DefaultIfEmpty() // left join
                        select new
                        {
                            FarmerName=farmer.Name,
                            PhoneNumber=farmer.PhoneNumber,
                            CreditScore = creditScore,
                            FarmerNIN = subaccount.NIN,
                            CooperativeName = cooperative.Name,
                            SubaccountName = subaccount != null ? subaccount.SubAccountName : null
                        };

            if (!string.IsNullOrEmpty(organizationCode))
            {
                query = query.Where(x => x.CreditScore.OrganizationCode == organizationCode);
            }

            var totalCount = await query.CountAsync();

            if (totalCount == 0) return null;

            var pagedResult = await query
                .OrderByDescending(x => x.CreditScore.LastUpdated)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = pagedResult.Select(x =>
            {
                var model = x.CreditScore.Map();
                model.NIN = x.FarmerNIN;
                model.CooperativeName = x.CooperativeName;
                model.Name = x.FarmerName;
                model.PhoneNumber = x.PhoneNumber;
                return model;
            }).ToList();

            return new PagedListResponse<List<FarmerCreditScoreModel>>
            {
                Data = result,
                Count = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        //        public async Task<PagedListResponse<List<FarmerCreditScoreModel>>> GetCreditScorebybusiness(string businessCode, int pageNumber = 1,
        //  int pageSize = 10, 
        //            string organizationCode = null)
        //        {
        //            var query = _context.FarmerCreditScores

        //.AsQueryable();

        //            if (!string.IsNullOrEmpty(organizationCode))
        //            {
        //                query = query.Where(x => x.OrganizationCode == organizationCode);
        //            }
        //            if (query.Count() == 0) { return null; }
        //            var totalCount = await query.CountAsync();


        //            var pagedResult = await query
        //                .OrderByDescending(x => x.LastUpdated)
        //                .Skip((pageNumber - 1) * pageSize)
        //                .Take(pageSize)
        //               .ToListAsync();
        //            return new PagedListResponse<List<FarmerCreditScoreModel>>
        //            {
        //                Data = pagedResult.Select(x => x.Map()).ToList(),
        //                Count = totalCount,
        //                PageNumber = pageNumber,
        //                PageSize = pageSize
        //            };

        //        }
        public async Task<List<long>> GetAllFarmerIdsAsync()
        {
            return await _context.Farmers.Select(f => f.Id).ToListAsync();
        }
        public async Task SaveAsync(FarmerCreditScore score)
        {
            var existing = await _context.FarmerCreditScores.FindAsync(score.FarmerId);
            if (existing != null)
            {
                existing.Score = score.Score;
                existing.RiskLevel = score.RiskLevel;
                existing.LastUpdated = score.LastUpdated;
            }
            else
            {
                await _context.FarmerCreditScores.AddAsync(score);
            }
            await _context.SaveChangesAsync();
        }
       
    }
}
