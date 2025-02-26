using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories
{
    public class BusinessRepository :IBusinessRepository
    {
        private readonly DataContext _context;
        public BusinessRepository(DataContext context)
        {
            _context = context;
        }
        public async Task<List<BusinessWorkerModel>> GetStaffByBusinessCode(long businessId)
        {
            var query = await (from b in _context.BusinessWorkers
                               join u in _context.Users
                               on b.UserId equals u.Id
                               where b.BusinessId == businessId
                               orderby  b.CreatedOn
                               select new BusinessWorkerModel
                               {
                                   BusinessId = businessId,
                                   BusinessWorkerId= b.BusinessWorkerId,
                                   Email=u.Email,
                                   FullName=u.Name,
                                   PhoneNumber=u.PhoneNumber,
                                   UserId=u.Id
                               }).ToListAsync();           


            if (query.Count > 0)
            {
                return query;
            }
            return null;
        }
        public async Task<int> BusinessCount()
        {
            return await _context.Set<Business>().CountAsync();
        }
        public async Task<BusinessModel> CreateBusiness(BusinessModel businessModel, string userId)
        {
            Business business = businessModel.Map();
            byte[] salt = EncryptionUtil.GetSalt();
            business.BusinessCodeSignageSalt = Convert.ToBase64String(salt);
            business.BusinessCodeSignage = business.BusinessCode.EncryptTransactionPin(salt);

            business.UserId = userId;
            _context.Set<Business>().Add(business);
            await _context.SaveChangesAsync();
            return business.Map();
        }


        public async Task<BusinessWorkerModel> CreateBusinessWorker(BusinessWorkerModel bWorker)
        {
            BusinessWorker businessWorker = bWorker.Map();
            await _context.AddAsync(businessWorker);
            await _context.SaveChangesAsync();

            return businessWorker.Map();
        }
   
        public async Task<List<BusinessModel>> GetBusinessesAsync(ListQueryParams query)
        {
            var business = await _context.Set<Business>().Include(b =>b.BusinessWorkers)
                .AsNoTracking()
                .OrderByDescending(b=>b.CreatedOn)
                 .Skip(query.PageNumber)
                .Take(query.PageSize)
              .Select(s => new BusinessModel
              {
                  TotalStaffCount= s.BusinessWorkers.Count,
                  BusinessName=s.BusinessName,
                  BusinessLogo=s.BusinessLogo,
                  BusinessStatus= s.BusinessStatus.ToEnum<BusinessStatus>(),
                  Address=s.Address,
                  BusinessEmail= s.BusinessEmail,
                  BusinessCode=s.BusinessCode,
                  PhoneNumber=s.PhoneNumber,
                  BusinessId=s.BusinessId,
                  Website=s.Website,
                  UserId=s.UserId,
              }).ToListAsync();
            if (business == null)
            {
                return null;
            }
            return business;
        }

        public async Task<BusinessModel> GetBusinessAsync(string businessName)
        {
            var business = await _context.Set<Business>()
                .FirstOrDefaultAsync(x =>  x.BusinessName == businessName && x.BusinessStatus != BusinessStatus.Deactivated.ToString());
            if (business == null)
            {
                return null;
            }
            return business.Map();
        }
        public async Task<BusinessModel> GetBusinessBySinageSaltAsync(string signagesalt)
        {
            var business = await _context.Set<Business>()
                .FirstOrDefaultAsync(x => x.BusinessCodeSignageSalt == signagesalt && x.BusinessStatus != BusinessStatus.Deactivated.ToString());
            if (business == null)
            {
                return null;
            }
            return business.Map();
        }

        public async Task<BusinessModel> GetBusinessByCodeAsync(string businessCode)
        {
            var business = await _context.Set<Business>()
                .FirstOrDefaultAsync(x => x.BusinessCode == businessCode && x.BusinessStatus != BusinessStatus.Deactivated.ToString());
            if (business == null)
            {
                return null;
            }
            return business.Map();
        }

        public async Task<BusinessModel> GetBusinessByUserIdAsync(string userId)
        {
            var business = await _context.Set<Business>()
                .FirstOrDefaultAsync(x => x.UserId == userId);
            if (business == null)
            {
                return null;
            }
            return business.Map();
        }

        public async Task<BusinessModel> GetBusinessByIdAsync(long businessId)
        {
            var business = await _context.Set<Business>()
                .FirstOrDefaultAsync(x => x.BusinessId == businessId && x.BusinessStatus != BusinessStatus.Deactivated.ToString());
            if (business == null)
            {
                return null;
            }
            return business.Map();
        }


    }
}
