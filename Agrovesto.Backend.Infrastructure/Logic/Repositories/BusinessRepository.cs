using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.V2.Onboarding;
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
        public async Task<LocationTypeModel> CreateLocationTypeAsync(LocationTypeModel model)
        {
            var orgExist = await _context.LocationTypes
              .FirstOrDefaultAsync(x => x.Type == model.Type);

            if (orgExist != null)
            {
                return orgExist.ViewMap();
            }
            LocationType org = model.Map();

            _context.Set<LocationType>().Add(org);
            await _context.SaveChangesAsync();
            var response = org.ViewMap();
            return response;
        }
        public async Task<List<LocationTypeModel>> GetLocationAsync(string businessCode)
        {
            var org = await _context.Set<LocationType>()
                .Where(x => x.BusinessCode == businessCode).ToListAsync();
           
            return org.Select(x=>x.ViewMap()).ToList();
        }
        public async Task<LocationTypeModel> GetLocationByIAsync(long Id)
        {
            var org = await _context.Set<LocationType>()
                .FirstOrDefaultAsync(x => x.Id == Id);
            if (org == null)
            {
                return null;
            }
            return org.ViewMap();
        }


        public async Task<int> OrganizationCount()
        {
            return await _context.Set<Organization>().CountAsync();
        }

        public async Task<OrganizationResponseModel> CreateOrUpdateOrganization(
       OrganizationOnboardingModel orgModel,
       OrganizationOnboardingArgs args)
        {
            // STEP 1: Resolve Business
            var business = await _context.Businesses
                .FirstOrDefaultAsync(x => x.BusinessCode == args.businessCode);

            if (business == null)
                throw new Exception("Invalid business");

            // STEP 2: Check for existing organization
            var orgExist = await _context.Organizations
                .Include(x => x.OrganizationBusinesses)
                .FirstOrDefaultAsync(x =>
                    x.OrganizationCode == orgModel.OrganizationCode ||
                    x.CompanyName == orgModel.CompanyName);

            if (orgExist != null)
            {
                UpdateOrganizationFields(orgExist, orgModel);
                LinkBusinessIfMissing(orgExist, business.BusinessId);

                await _context.SaveChangesAsync();
                return orgExist.ViewOnboardMap();
            }

            // STEP 3: Create new organization
            orgModel.OrganizationCode = await GenerateOrganizationCodeAsync(orgModel.CompanyName);

            var org = orgModel.OnboardMap();
            org.UserId = args.userId;
            org.OrganizationCode = orgModel.OrganizationCode;
            org.BusinessCode = args.businessCode;
            org.OrganizationBusinesses.Add(new OrganizationBusiness
            {
                BusinessId = business.BusinessId,
                IsActive = true,
                CreatedOn = DateTimeOffset.Now
            });

            _context.Organizations.Add(org);
            await _context.SaveChangesAsync();

            return org.ViewOnboardMap();
        }

        private static void UpdateOrganizationFields(Organization orgExist, OrganizationOnboardingModel orgModel)
        {
            if (!string.IsNullOrWhiteSpace(orgModel.ContactEmail))
                orgExist.ContactEmail = orgModel.ContactEmail;

            if (!string.IsNullOrWhiteSpace(orgModel.CompanyEmail))
                orgExist.OrganizationEmail = orgModel.CompanyEmail;

            if (!string.IsNullOrWhiteSpace(orgModel.CompanyName))
                orgExist.CompanyName = orgModel.CompanyName;

            if (!string.IsNullOrWhiteSpace(orgModel.FirstName))
                orgExist.FirstName = orgModel.FirstName;

            if (!string.IsNullOrWhiteSpace(orgModel.LastName))
                orgExist.LastName = orgModel.LastName;

            if (!string.IsNullOrWhiteSpace(orgModel.PhoneNumber))
                orgExist.PhoneNumber = orgModel.PhoneNumber;

            if (!string.IsNullOrWhiteSpace(orgModel.OrganizationPhone))
                orgExist.OrganizationPhone = orgModel.OrganizationPhone;

            if (orgModel.StateId > 0)
                orgExist.StateId = orgModel.StateId;

            if (orgModel.LgaId > 0)
                orgExist.LgaId = orgModel.LgaId;

            if (!string.IsNullOrWhiteSpace(orgModel.CompanyAddress))
                orgExist.CompanyAddress = orgModel.CompanyAddress;

            if (orgModel.CompanyType.HasValue)
                orgExist.CompanyType = orgModel.CompanyType.ToString();

            if (!string.IsNullOrWhiteSpace(orgModel.CACNumber))
                orgExist.CACNumber = orgModel.CACNumber;

            if (orgModel.RoleInOrganization.HasValue)
                orgExist.RoleInOrganization = orgModel.RoleInOrganization.ToString();
        }

        private static void LinkBusinessIfMissing(Organization orgExist, long businessId)
        {
            bool alreadyLinked = orgExist.OrganizationBusinesses
                .Any(x => x.BusinessId == businessId);

            if (!alreadyLinked)
            {
                orgExist.OrganizationBusinesses.Add(new OrganizationBusiness
                {
                    BusinessId = businessId,
                    IsActive = true,
                    CreatedOn = DateTimeOffset.Now
                });
            }
        } //   public async Task<OrganizationResponseModel> CreateOrUpdateOrganization(
        //OrganizationOnboardingModel orgModel,
        //OrganizationOnboardingArgs args)
        //   {
        //       var orgExist = await _context.BusinessOrganizations //organization
        //           .FirstOrDefaultAsync(x =>
        //               x.OrganizationCode == orgModel.OrganizationCode &&
        //               x.CompanyName == orgModel.CompanyName);

        //       if (orgExist != null)
        //       {
        //           if (!string.IsNullOrWhiteSpace(orgModel.ContactEmail))
        //               orgExist.ContactEmail = orgModel.ContactEmail;

        //           if (!string.IsNullOrWhiteSpace(orgModel.CompanyEmail))
        //               orgExist.OrganizationEmail = orgModel.CompanyEmail;

        //           if (!string.IsNullOrWhiteSpace(orgModel.CompanyName))
        //               orgExist.CompanyName = orgModel.CompanyName;

        //           if (!string.IsNullOrWhiteSpace(orgModel.FirstName))
        //               orgExist.FirstName = orgModel.FirstName;

        //           if (!string.IsNullOrWhiteSpace(orgModel.LastName))
        //               orgExist.LastName = orgModel.LastName;

        //           if (!string.IsNullOrWhiteSpace(orgModel.PhoneNumber))
        //               orgExist.PhoneNumber = orgModel.PhoneNumber;

        //           if (!string.IsNullOrWhiteSpace(orgModel.OrganizationPhone))
        //               orgExist.OrganizationPhone = orgModel.OrganizationPhone;

        //           if (orgModel.StateId > 0)
        //               orgExist.StateId = orgModel.StateId;

        //           if (orgModel.LgaId > 0)
        //               orgExist.LgaId = orgModel.LgaId;

        //           if (!string.IsNullOrWhiteSpace(orgModel.CompanyAddress))
        //               orgExist.CompanyAddress = orgModel.CompanyAddress;

        //           if (orgModel.CompanyType.HasValue)
        //               orgExist.CompanyType = orgModel.CompanyType.ToString();

        //           if (!string.IsNullOrWhiteSpace(orgModel.CACNumber))
        //               orgExist.CACNumber = orgModel.CACNumber;

        //           if (orgModel.RoleInOrganization.HasValue)
        //               orgExist.RoleInOrganization = orgModel.RoleInOrganization.ToString();

        //           await _context.SaveChangesAsync();

        //           return orgExist.ViewOnboardMap();
        //       }
        //       var orgEmail = string.IsNullOrWhiteSpace(orgModel.CompanyEmail)
        //                  ? orgModel.ContactEmail
        //                  : orgModel.CompanyEmail;
        //       orgModel.OrganizationCode = await GenerateOrganizationCodeAsync(orgModel.CompanyName);
        //       Organization org = orgModel.OnboardMap();
        //       org.UserId = args.userId;
        //       org.OrganizationCode = orgModel.OrganizationCode;
        //       org.BusinessCode = args.businessCode;

        //       _context.Set<Organization>().Add(org);
        //       //await _context.SaveChangesAsync();

        //       return org.ViewOnboardMap();
        //   }

        private async Task<string> GenerateOrganizationCodeAsync(string companyName)
        {
            var cleanedName = companyName?
                .Replace(" ", string.Empty)
                .ToUpperInvariant() ?? "ORG";

            var prefix = cleanedName.Length > 3
                ? cleanedName[..3]
                : cleanedName;

            var uniqueSegment = Guid.NewGuid()
                .ToString("N")
                .Substring(0, 4)
                .ToUpperInvariant();

            return $"{prefix}{DateTimeOffset.UtcNow:MM}{uniqueSegment}";
        }

        public async Task<OrganizationModel> CreateOrganization(OrganizationModel orgModel)
        {
            var orgExist = await _context.Organizations
              .FirstOrDefaultAsync(x => x.OrganizationCode == orgModel.OrganizationCode && x.CompanyName == orgModel.OrganizationName);

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
        public async Task<OrganizationModel> GetOrganizationByCodeAsync(string organizationCode)
        {
            var org = await _context.Set<Organization>()
                .FirstOrDefaultAsync(x => x.OrganizationCode == organizationCode);
            if (org == null)
            {
                return null;
            }
            return org.Map();
        }
        public async Task<OrganizationModel> GetOrganizationByUserId(string userId)
        {
            var org = await _context.Set<Organization>()
                .FirstOrDefaultAsync(x => x.UserId == userId);
            if (org == null)
            {
                return null;
            }
            return org.Map();
        }
        public async Task<OrganizationModel> GetOrganizationAsync(string organizationEmail)
        {
            var org = await _context.Set<Organization>()
                .FirstOrDefaultAsync(x => x.OrganizationEmail == organizationEmail);
            if (org == null)
            {
                return null;
            }
            return org.Map();
        }
        public async Task<BusinessOrganizationDashboardResponse> GetBusinessDashboardAsync(ListQueryParams args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            if (string.IsNullOrEmpty(args.businessCode))
                throw new ValidationException("Invalid Business");

            try
            {
                var totalOrgs = await _context.Set<Organization>()
                   
                    .Where(x => x.BusinessCode == args.businessCode).ToListAsync();
                

                var seed = args.Id.GetHashCode();
                var random = new Random(seed);

                var totalEquityPaid = 0;//(decimal)random.Next(100000, 500000);
                var totalProduceSold = 0; //(decimal)random.Next(200000, 800000);

                return new BusinessOrganizationDashboardResponse
                {
                    TotalOrganizations = totalOrgs.Count,
                    TotalActiveOrganizations= totalOrgs.Where(x=>x.IsActive).Count(),

                   
                    TotalEquityPaid = totalEquityPaid,
                    TotalProduceSold = totalProduceSold
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving organization dashboard", ex);
            }
        }

        public async Task<List<OrganizationModel>> GetBusinessOrganizationAsync(string businessCode)
        {
            var query = await (
                from org in _context.Organizations

                    // Creator (Organization Owner)
                join creator in _context.Set<User>()
                    on new { org.BusinessCode, Email = org.OrganizationEmail }
                    equals new { creator.BusinessCode, Email = creator.Email }

                    // LEFT JOIN OrganizationUsers
                join orgUser in _context.UserOrganizations
                    on org.OrganizationCode equals orgUser.OrganizationCode into orgUsersGroup
                from orgUser in orgUsersGroup.DefaultIfEmpty()

                    // LEFT JOIN Users (members)
                join user in _context.Set<User>()
                    on orgUser.UserId equals user.Id into usersGroup
                from user in usersGroup.DefaultIfEmpty()

                where org.BusinessCode == businessCode

                select new
                {
                    Org = org,
                    Creator = creator,
                    User = user
                }
            ).ToListAsync();

            if (query == null || query.Count == 0)
                return null;

            var result = query
                .GroupBy(x => x.Org.OrganizationId)
                .Select(g =>
                {
                    var org = g.First().Org;
                    var creator = g.First().Creator;

                    var users = g
                        .Where(x => x.User != null)
                        .Select(x => x.User)
                        .Distinct();

                    return new OrganizationModel
                    {
                        OrganizaionId = org.OrganizationId,
                        OrganizationName = org.CompanyName,
                        BusinessCode = org.BusinessCode,
                        OrganizationCode = org.OrganizationCode,
                        ContacPerson = org.FullName,
                        Contact = org.PhoneNumber,
                        ContactEmail = org.ContactEmail,
                        Location = org.CompanyAddress,
                        OrganizationEmail = org.OrganizationEmail,
                        PhoneNumber= org.PhoneNumber,
                        State= org.State.State,
                        Lga=org.LGA.Name,
                        // ✅ Total users in organization
                        UserCount = users.Count(),

                        // ✅ Total ACTIVE users
                        ActiveUserCount = users.Count(u => u.IsActive),

                        OnboardedOn = creator.CreatedOn
                    };
                })
                .ToList();

            return result;
        }
        //    public async Task<List<OrganizationModel>> GetBusinessOrganizationAsync(string businessCode)
        //    {
        //        var organizations = await (
        //            from org in _context.BusinessOrganizations
        //            join user in _context.Set<User>()
        //on new { org.BusinessCode, Email = org.OrganizationEmail }
        //equals new { user.BusinessCode, Email = user.Email }
        //            where org.BusinessCode == businessCode
        //            select new
        //            {
        //                Org = org,
        //                User = user
        //            }
        //        ).ToListAsync();

        //        if (organizations == null || organizations.Count == 0)
        //            return null;

        //        var result = organizations
        //            .GroupBy(x => x.Org.OrganizationId)
        //            .Select(g =>
        //            {
        //                var first = g.First();
        //                var org = first.Org;
        //                var creatorUser = g
        //                    .OrderBy(u => u.User.CreatedOn)
        //                    .First().User;

        //                return new OrganizationModel
        //                {
        //                    OrganizaionId = org.OrganizationId,
        //                    OrganizationName = org.CompanyName,
        //                    BusinessCode = org.BusinessCode,
        //                    OrganizationCode = org.OrganizationCode,
        //                    ContacPerson = org.FullName,
        //                    Contact = org.PhoneNumber,
        //                    ContactEmail = org.ContactEmail,
        //                    Location = org.CompanyAddress,
        //                    OrganizationEmail = org.OrganizationEmail,

        //                    // Pulled from Users table
        //                    Kick = creatorUser.Password,          // Your “kick” field
        //                    OnboardedOn = creatorUser.CreatedOn   // Date they were onboarded
        //                };
        //            })
        //            .ToList();

        //        return result;
        //    }

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
