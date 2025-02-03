using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
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
        public async Task<int> BusinessCount()
        {
            return await _context.Set<Business>().CountAsync();
        }
        public async Task<BusinessModel> CreateBusiness(BusinessModel businessModel, string userId)
        {
            Business business = businessModel.Map();
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



    }
}
