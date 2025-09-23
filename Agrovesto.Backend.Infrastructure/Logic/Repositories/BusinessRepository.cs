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
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories
{
    public class BusinessRepository :IBusinessRepository
    {
        private readonly DataContext _context;
        public BusinessRepository(DataContext context)
        {
            _context = context;
        }
        public async Task<int> OrganizationCount()
        {
            return await _context.Set<Organization>().CountAsync();
        }
        public async Task<OrganizationModel> CreateOrganization(OrganizationModel orgModel)
        {
            var orgExist = await _context.BusinessOrganizations
              .FirstOrDefaultAsync(x => x.OrganizationCode == orgModel.OrganizationCode && x.OrganizationName == orgModel.OrganizationName);

            if (orgExist != null)
            {
                // Organization already exists, return the existing one
                return orgExist.Map();
            }
            Organization org = orgModel.Map();

            _context.Set<Organization>().Add(org);
           // await _context.SaveChangesAsync();
            return org.Map();
        }

        public async Task<OrganizationModel> GetOrganizationAsync(string organizationCode)
        {
            var org = await _context.Set<Organization>()
                .FirstOrDefaultAsync(x => x.OrganizationCode == organizationCode);
            if (org == null)
            {
                return null;
            }
            return org.Map();
        }

        public async Task<List<OrganizationModel>> GetBusinessOrganizationAsync(string businessCode)
        {
            var org = await _context.Set<Organization>()
                .Where(x => x.BusinessCode == businessCode).ToListAsync();
            if (org == null)
            {
                return null;
            }
            return org.Select(x=>x.Map()).ToList();
        }

        public async Task<List<BusinessWorkerModel>> GetStaffByBusinessCode(long businessId, string organizationCode)
        {
            var query = from b in _context.BusinessWorkers
                        join u in _context.Users on b.UserId equals u.Id
                        join ur in _context.UserRoles on u.Id equals ur.UserId into userRoles
                        from ur in userRoles.DefaultIfEmpty()
                        join rl in _context.Roles on ur.RoleId equals rl.Id into roles
                        from role in roles.DefaultIfEmpty()
                        where b.BusinessId == businessId
                        select new
                        {
                            b,
                            u,
                            ur,
                            role
                        };

            if (!string.IsNullOrWhiteSpace(organizationCode))
            {
                query = query.Where(x => x.b.OrganizationCode == organizationCode);
            }

            var result = await query
                .OrderBy(x => x.b.CreatedOn)
                .Select(x => new BusinessWorkerModel
                {
                    BusinessId = x.b.BusinessId,
                    BusinessWorkerId = x.b.BusinessWorkerId,
                    Email = x.u.Email,
                    FullName = x.u.Name,
                    PhoneNumber = x.u.PhoneNumber,
                    UserId = x.u.Id,
                    RoleId = x.ur.RoleId,
                    RoleName = x.role.Name ?? "No Role Assigned",
                    CreatedOn = x.b.CreatedOn
                })
                .ToListAsync();

            var distinctUsers = result
                .GroupBy(x => x.UserId)
                .Select(g => g.First())
                .Take(50)
                .ToList();

            return distinctUsers.Any() ? distinctUsers : null;
        }

        public async Task<int> BusinessCount()
        {
            return await _context.Set<Business>().CountAsync();
        }
        public async Task<BusinessModel> CreateBusiness(BusinessModel businessModel, string userId)
        {
            var businessExist = await _context.Businesses
              .FirstOrDefaultAsync(x => x.UserId == userId);

            if (businessExist != null)
            {
                return  businessExist.Map();
            }
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
   
        public async Task<List<BusinessModel>> GetBusinessesAsync()
        {
            var business = await _context.Set<Business>().Include(b =>b.BusinessWorkers)
                .AsNoTracking()
                .OrderByDescending(b=>b.CreatedOn)
                // .Skip(query.PageNumber)
                //.Take(query.PageSize)
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
                  DisputeEmail=s.DisputeEmail,
                  SupportEmail=s.SupportEmail,
                  BusinessCodeSignage=s.BusinessCodeSignage,
                  BusinessCodeSignageSalt
                  =s.BusinessCodeSignageSalt,
                  CreatedOn=s.CreatedOn,
                  BusinessWorkers=s.BusinessWorkers.Select(x=> new BusinessWorkerModel
                  {
                      BusinessId=x.BusinessId,
                      BusinessWorkerId=x.BusinessWorkerId,
                      UserId= x.UserId,
                     
                  }).ToArray(),
                  BVN = s.BVN,
                  Category=s.Category.ToEnum<Category>(),
                  ContactEmail=s.ContactEmail,
                  NIN=s.NIN,
                  
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
