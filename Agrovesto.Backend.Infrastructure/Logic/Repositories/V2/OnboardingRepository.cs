using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.Utilities;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using PhoneNumbers;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories.V2
{
    public interface IOnboardingRepository
    {
        Task<(UserModel model, bool status, string message)> OnRegistrationAsync(UserModel model, string roleName);
        Task RemoveUserFromOrganization(string userId, string organizationCode);
        Task TrackUserRoleAssignment(string userId, string userType) ;
        Task<List<string>> GetOrganizationsForUser(string userId);
        Task<List<User>> GetUsersInOrganization(string organizationCode);
        Task<(UserModel model, bool status, string message)> GetUserById(string userId);
        Task<(bool Status, string Message)> ForgotPassword(string username);
        Task<(bool Status, string Message)> ResetPassword(PasswordResetModel model);
        Task<(bool Status, string Message)> ChangePassword(ChnagePasswordModel model);
        Task<UserModel> FindByPhoneNumberAsync(string phoneNumber);
        Task AddUserToOrganizationAsync(User user, string organizationCode, string phoneNumber);
        Task<(OnboardingRequestModel model, bool status, string message, bool IsExisting)> RegisterUser(OnboardingRequestModel model, OnBoardingArgs args);
        Task ConfirmUserEmailOrPassword(string userId);
       // Task<(bool Status, string Message, TokenModel Response)> Login(LoginModel model, string businessCode);
        Task<(bool Status, string Message, TokenModel Response)> Login(LoginModel model);
        //Task<(bool status, string message)> OnUserLevelUpdate(string userId, UserLevel level);
        Task<bool> OnNINBVNUpdate(UserModel model);
        Task OnCreateKYC(KYCDetails kYC);
        Task<UsersProfileResponse> GetUserProfile(string userId);
        Task<UserModel> UpdateUserProfileAsync(string userId, ProfileRequestModel model);
        Task<User?> FindExistingUserAsync(OnboardingRequestModel model);
        Task<UsersProfile> FindUserById(string userId);
        Task<bool> IsUserInRoleAsync(string userId, string role);
        Task UpdateUserDetails(OnboardingRequestModel model, OnBoardingArgs args);
    }
    public class OnboardingRepository : IOnboardingRepository
    {
        private readonly DataContext _context;
        private readonly RoleManager<Role> _roleManager;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OnboardingRepository> _log;
        public OnboardingRepository(DataContext context,
            IConfiguration config, RoleManager<Role> roleManager, UserManager<User> userManager,
            ILogger<OnboardingRepository> log)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _configuration = config;
            _log = log;
        }


        public async Task<bool> IsUserInRoleAsync(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var roles = await _userManager.GetRolesAsync(user);
            return roles.Contains(role, StringComparer.OrdinalIgnoreCase);
        }

        public async Task<UsersProfile> FindUserById(string userId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null)
                throw new BaseException(ExceptionType.NO_RECORD_FOUND, "Profile not found");

            var response = new UsersProfile
            {
                UserId = user.Id,
                UserType = user.UserType,
                Email = user.Email,
                Phone = user.PhoneNumber,
                Name=user.Name
            };

           

            return response;
        }


        //public async Task<TokenModel> RefreshAsync(string refreshToken, string deviceId)
        //{
        //    var stored = await _context.RefreshTokens
        //        .FirstOrDefaultAsync(x =>
        //            x.Token == refreshToken &&
        //            !x.IsRevoked &&
        //            x.ExpiresAt > DateTime.UtcNow &&
        //            x.DeviceId == deviceId);

        //    if (stored == null)
        //        throw new BaseException(ExceptionType.UNAUTHORIZED, "Invalid refresh token");

        //    var user = await _userManager.FindByIdAsync(stored.UserId);

        //    stored.IsRevoked = true;
        //    var newRefreshToken = CreateRefreshToken(user.Id, deviceId);

        //    _context.RefreshTokens.Add(newRefreshToken);
        //    await _context.SaveChangesAsync();

        //    var claims = BuildClaims(user, ...);

        //    return new TokenModel
        //    {
        //        Token = GenerateJwtToken(claims),
        //        RefreshToken = newRefreshToken.Token
        //    };
        //}
        private RefreshToken CreateRefreshToken(string userId, string deviceId)
        {
            return new RefreshToken
            {
                UserId = userId,
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                DeviceId = deviceId
            };
        }

        private async Task<User?> ResolveUserAsync(LoginModel model)
        {
            User? user = null;

            switch (model.LoginMethod)
            {
                case LoginType.Password:
                    if (string.IsNullOrWhiteSpace(model.Email))
                        return null;

                    user = await _userManager.FindByNameAsync(model.Email);
                    break;

                case LoginType.Pin:
                    if (string.IsNullOrWhiteSpace(model.Phonenumber))
                        return null;

                    user = await _userManager.Users
                        .FirstOrDefaultAsync(x =>
                            x.PhoneNumber == model.Phonenumber 
                            //&&
                            //x.BusinessCode == businessCode
                            );
                    break;
            }

            //if (user == null || user.BusinessCode != businessCode)
            //    return null;

            return user;
        }

        private bool HasPendingOnboarding(User user)
        {
            return string.IsNullOrWhiteSpace(user.Email)
                || string.IsNullOrWhiteSpace(user.PhoneNumber)
                || string.IsNullOrWhiteSpace(user.UserType);
        }
        private List<Claim> BuildClaims(
    User user,
    IList<string>? roles,
    UserBusiness activeBusiness,
    List<OrganizationDto>? organizations,
    bool hasPendingOnboarding,
    List<string>? modules = null)
        {
            var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(ClaimTypes.NameIdentifier, user.Id),

        // Business info
        new Claim("BusinessId", activeBusiness.BusinessId.ToString()),
        new Claim("BusinessCode", activeBusiness.Business?.BusinessCode ?? ""),
        new Claim("BusinessRole", activeBusiness.Role ?? ""),

        new Claim("HasPendingOnboarding", hasPendingOnboarding.ToString())
    };
            var principalName =
                user.Name ??
                user.Email ??
                user.PhoneNumber ??
                user.Id;

            claims.Add(new Claim(ClaimTypes.Name, principalName));

            if (!string.IsNullOrWhiteSpace(user.Email))
                claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));

            if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
                claims.Add(new Claim("PhoneNumber", user.PhoneNumber));

            claims.Add(new Claim(
                "DisplayName",
                user.Name ??
                user.Email ??
                user.PhoneNumber ??
                "User"
            ));

            // User Roles
            if (roles?.Any() == true)
            {
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                claims.Add(new Claim("Roles", string.Join(",", roles)));
            }

            if (!string.IsNullOrWhiteSpace(user.UserType))
                claims.Add(new Claim("UserType", user.UserType));

            if (user.LastLoginDate.HasValue)
                claims.Add(new Claim("LastLogin", user.LastLoginDate.Value.ToString("O")));

            claims.Add(new Claim("MemberSince", user.CreatedOn.ToString("O")));

            // OPTIONAL: Modules that can be accessed
            if (modules?.Any() == true)
            {
                claims.Add(new Claim(
                    "Modules",
                    JsonConvert.SerializeObject(modules)
                ));
            }

            // OPTIONAL: User organizations (you should phase this out)
            if (organizations?.Any() == true)
            {
                claims.Add(new Claim(
                    "Organizations",
                    JsonConvert.SerializeObject(
                        organizations.Select(o => new
                        {
                            o.OrganizationCode,
                            o.OrganizationName
                        })
                    )
                ));
            }

            return claims;
        }
        /**  private List<Claim> BuildClaims(
        User user,
        IList<string>? roles,
        List<OrganizationDto>? organizations,
        bool hasPendingOnboarding)
          {
              var claims = new List<Claim>
      {

          new Claim(JwtRegisteredClaimNames.Sub, user.Id),
          new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
          new Claim(ClaimTypes.NameIdentifier, user.Id),
       
          new Claim("HasPendingOnboarding", hasPendingOnboarding.ToString())
      };
              var principalName =
      user.Name ??
      user.Email ??
      user.PhoneNumber ??
      user.Id;

              claims.Add(new Claim(ClaimTypes.Name, principalName));

              if (!string.IsNullOrWhiteSpace(user.Email))
                  claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));

              if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
                  claims.Add(new Claim("PhoneNumber", user.PhoneNumber));



              claims.Add(new Claim(
      "DisplayName",
      user.Name ??
      user.Email ??
      user.PhoneNumber ??
      "User"
));
              if (roles?.Any() == true)
              {
                  foreach (var role in roles)
                  {
                      claims.Add(new Claim(ClaimTypes.Role, role));
                  }
                  claims.Add(new Claim("Roles", string.Join(",", roles)));
              }
                if (!string.IsNullOrWhiteSpace(user.UserType))
                  claims.Add(new Claim("UserType", user.UserType));

              if (user.LastLoginDate.HasValue)
                  claims.Add(new Claim("LastLogin", user.LastLoginDate.Value.ToString("O")));
         
              claims.Add(new Claim("MemberSince", user.CreatedOn.ToString("O")));

              if (organizations?.Any() == true)
              {
                  claims.Add(new Claim(
                      "Organizations",
                      JsonConvert.SerializeObject(
                          organizations.Select(o => new
                          {
                              o.OrganizationCode,
                              o.OrganizationName
                          })
                      )
                  ));
              }

              return claims;
          }
          **/

        private async Task<List<OrganizationDto>> GetUserOrganizationsAsync(string userId)
        {
            var orgCodes = await _context.UserOrganizations
                .Where(x => x.UserId == userId)
                .Select(x => x.OrganizationCode)
                .Distinct()
                .ToListAsync();

            return await _context.Organizations
                .Where(o => orgCodes.Contains(o.OrganizationCode))
                .Select(o => new OrganizationDto
                {
                    OrganizationCode = o.OrganizationCode,
                    OrganizationName = o.CompanyName
                })
                .ToListAsync();
        }

        private bool ValidateUserPin(User user, string plainPin)
        {
            var salt = Convert.FromBase64String(user.Salt);
            var hashedPin = plainPin.HashSecret(salt);

            return user.PinEncrypt == hashedPin;
        }

        public async Task<TokenModel> SwitchBusiness(string userId, long businessId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                throw new Exception("User not found");

            var membership = await _context.UserBusinesses
                .Include(x => x.Business)
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.BusinessId == businessId);

            if (membership == null)
                throw new Exception("User not assigned to this business");

            var roles = await _userManager.GetRolesAsync(user);
            var organizations = await (from uo in _context.UserOrganizations
                                       join bo in _context.Organizations
                                       on uo.OrganizationCode equals bo.OrganizationCode
                                       where uo.UserId == user.Id
                                       select new OrganizationDto
                                       {
                                           OrganizationCode = bo.OrganizationCode,
                                           OrganizationName = bo.CompanyName
                                       }).ToListAsync();

            var hasPendingOnboarding =
                string.IsNullOrWhiteSpace(user.Email) ||
                string.IsNullOrWhiteSpace(user.PhoneNumber) ||
                string.IsNullOrWhiteSpace(user.UserType);

            var modules = await _context.BusinessLicenseModules
                .Where(x =>
                    x.BusinessLicense.BusinessId == businessId &&
                    x.IsEnabled)
                .Select(x => x.SystemModule.Code)
                .ToListAsync();

            var claims = BuildClaims(
                user,
                roles,
                membership,          
                organizations,
                hasPendingOnboarding,
                modules
            );

            var token = GenerateJwtToken(claims);

            return new TokenModel
            {
                Id = user.Id,
                Token = token,
                Email = user.Email,
                Name = user.Name,
                BusinessCode = membership.Business.BusinessCode,
                Roles = roles.ToList()
            };
        }
        public async Task<(bool Status, string Message, TokenModel Response)> Login(
      LoginModel model)
        {
            var user = await ResolveUserAsync(model);
            if (user == null)
                return (false, "Invalid credentials", null);
            //var user = await _userManager.Users
            //    .FirstOrDefaultAsync(x =>
            //        x.Email == model.Email ||
            //        x.UserName == model.Email ||
            //        x.UserName == model.Phonenumber ||
            //        x.PhoneNumber == model.Phonenumber);

            //if (user == null)
            //    return (false, "Invalid credentials", null);

            var credentialResult = await ValidateCredentialsAsync(user, model);
            if (!credentialResult.Success)
                return (false, credentialResult.Message, null);
            //var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);

            //if (!passwordValid)
            //    return (false, "Invalid credentials", null);

            var userBusinesses = await _context.UserBusinesses
                .Include(x => x.Business)
                .Where(x => x.UserId == user.Id && x.IsActive)
                .ToListAsync();

            if (!userBusinesses.Any())
                return (false, "User is not assigned to any business", null);

            var activeBusiness = userBusinesses.FirstOrDefault(x => x.IsDefault)
                                 ?? userBusinesses.First();

            var roles = await _userManager.GetRolesAsync(user);

            var organizations = await (from uo in _context.UserOrganizations
                                       join bo in _context.Organizations
                                       on uo.OrganizationCode equals bo.OrganizationCode
                                       where uo.UserId == user.Id
                                       select new OrganizationDto
                                       {
                                           OrganizationCode = bo.OrganizationCode,
                                           OrganizationName = bo.CompanyName
                                       }).ToListAsync();

            var hasPendingOnboarding =
                string.IsNullOrWhiteSpace(user.Email) ||
                string.IsNullOrWhiteSpace(user.PhoneNumber) ||
                string.IsNullOrWhiteSpace(user.UserType);

            var modules = await _context.BusinessLicenseModules
                .Where(x =>
                    x.BusinessLicense.BusinessId == activeBusiness.BusinessId &&
                    x.IsEnabled)
                .Select(x => x.SystemModule.Code)
                .ToListAsync();

            var claims = BuildClaims(
                user,
                roles,
                activeBusiness,   
                organizations,
                hasPendingOnboarding,
                modules
            );

            var token = GenerateJwtToken(claims);

            return (true, "Successful", new TokenModel
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Token = token,
                Roles = roles.ToList(),
                Organizations= organizations,
                phone_number = user.PhoneNumber,
                TokenType =TokenType.AccessToken,
                BusinessCode = activeBusiness.Business.BusinessCode,
                Businesses = userBusinesses.Select(b => new BusinessModel
                {
                  BusinessId=  b.BusinessId,
                   BusinessCode= b.Business.BusinessCode,
                   BusinessName= b.Business.BusinessName
                }).ToList()
            });
        }  /**   public async Task<(bool Status, string Message, TokenModel Response)> Login(
       LoginModel model,
       string businessCode)
           {
               var business = await _context.Businesses
                   .AsNoTracking()
                   .FirstOrDefaultAsync(x => x.BusinessCode == businessCode);

               if (business == null || business.BusinessStatus == BusinessStatus.Deactivated.ToString())
                   return (false, "Business is inactive or not found", null);

               var user = await ResolveUserAsync(model, businessCode);
               if (user == null)
                   return (false, "Invalid credentials", null);

               var credentialResult = await ValidateCredentialsAsync(user, model);
               if (!credentialResult.Success)
                   return (false, credentialResult.Message, null);

               var roles = await (from ur in _context.UserRoles.AsNoTracking()
                                  join r in _context.Roles.AsNoTracking() on ur.RoleId equals r.Id
                                  where ur.UserId == user.Id
                                  select r.Name)
                                 .ToListAsync();

               if (!roles.Any())
                   return (false, "User has no assigned roles", null);

               var hasPendingOnboarding = HasPendingOnboarding(user);

               try
               {
                   var needUpdate = !user.LastLoginDate.HasValue || (DateTimeOffset.UtcNow - user.LastLoginDate.Value) > TimeSpan.FromMinutes(1);
                   if (needUpdate)
                   {
                       await _context.Users
                           .Where(u => u.Id == user.Id)
                           .ExecuteUpdateAsync(s => s.SetProperty(u => u.LastLoginDate, u => DateTimeOffset.UtcNow));
                   }
               }
               catch (Exception ex)
               {
                   try { _log.LogWarning(ex, "Failed to update LastLoginDate for user {userId}", user.Id); } catch { }
               }

               var organizations = await (from uo in _context.UserOrganizations.AsNoTracking()
                                          join bo in _context.BusinessOrganizations.AsNoTracking() on uo.OrganizationCode equals bo.OrganizationCode
                                          where uo.UserId == user.Id
                                          select new OrganizationDto
                                          {
                                              OrganizationCode = bo.OrganizationCode,
                                              OrganizationName = bo.CompanyName
                                          }).ToListAsync();

               var claims = BuildClaims(user, roles, organizations, hasPendingOnboarding);

               var token = GenerateJwtToken(claims);
               var time = DateTime.UtcNow.AddDays(1);

               return (true, "Successful", new TokenModel
               {
                   Id = user.Id,
                   Email = user.Email,
                   Name = user.Name,
                   phone_number = user.PhoneNumber,
                   Token = token,
                   TokenType = TokenType.AccessToken,
                   ExpiresOn = time,
                   Roles = roles.ToList(),
                   Organizations = organizations
               });
           }
         
           **/
        private bool HasPassword(User user)
        {
            return !string.IsNullOrWhiteSpace(user.PasswordHash);
        }
         
        private bool HasPin(User user)
        {
            return !string.IsNullOrWhiteSpace(user.PinEncrypt);
        }
        private async Task<(bool Success, string Message)> ValidateCredentialsAsync(
   User user,
   LoginModel model)
        {
            switch (model.LoginMethod)
            {
                case LoginType.Password:
                    if (!HasPassword(user))
                        return (false, "Password login is not enabled for this account");

                    if (!await _userManager.CheckPasswordAsync(user, model.Password))
                        return (false, "Invalid email or password");
                    break;

                case LoginType.Pin:
                    if (!HasPin(user))
                        return (false, "PIN login is not enabled for this account");

                    if (!ValidateUserPin(user, model.Pin))
                        return (false, "Invalid phone number or PIN");
                    break;
                //case LoginType.Otp:

                //    var otpValid = await _otpService.ValidateOtp(user.Id, model.Otp);

                //    if (!otpValid)
                //        return (false, "Invalid or expired OTP");

                //    break;

                default:
                    return (false, "Invalid login method");
            }

            return (true, "OK");
        }
        private string GenerateJwtToken(IEnumerable<Claim> claims)
        {
            var key = _configuration["JWT:Key"];
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("JWT key is not configured");

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:Issuer"],
                audience: _configuration["JWT:Audience"],
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: new SigningCredentials(
                    signingKey,
                    SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        public async Task ConfirmUserEmailOrPassword(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            user.IsActive = true;
            user.EmailConfirmed = true;

            await _userManager.UpdateAsync(user);
        }

        public async Task<UsersProfileResponse> GetUserProfile(string userId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null)
                throw new BaseException(ExceptionType.NO_RECORD_FOUND, "Profile not found");

            var response = new UsersProfileResponse
            {
                UserId = user.Id,
                UserType = user.UserType,
                Email = user.Email,
                Phone = user.PhoneNumber
            };

            switch (user.UserType)
            {
                case "Farmer":
                    response.Farmer = await GetFarmerProfile(userId);
                    break;

                case "Agent":
                    response.Agent = await GetAgentProfile(userId);
                    break;

                case "MemberShip":
                    response.Membership = await GetCooperativeProfile(userId);
                    break;

                default:
                    throw new BaseException(ExceptionType.BAD_REQUEST, "Invalid user type");
            }

            return response;
        }

        private async Task<FarmerProfileDto> GetFarmerProfile(string userId)
        {

            var farmer = await _context.Farmers.FirstOrDefaultAsync(f => f.UserId == userId);
            if (farmer == null)
                throw new BaseException(ExceptionType.NO_RECORD_FOUND, "Farmer profile not found");


            var subAccount = await _context.SubAccounts.FirstOrDefaultAsync(s => s.UserId == userId);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            var image = await _context.FarmerImages
                .Where(i => i.FarmerUserId == userId)
                .FirstOrDefaultAsync();

            var cooperatives = await (
                from fc in _context.FarmerCooperatives
                join c in _context.MemberShips on fc.CooperativeId equals c.Id
                where fc.FarmerId == farmer.Id
                select new FarmerMembershipProfileDto
                {
                    CooperativeId = c.Id,
                    CooperativeName = c.Name,
                    OnboardedBy = fc.OnboardedByUserId,
                    DateAdded = c.OnBoardedOn,
                    FullName = $"{c.MembershipLeaderFirstName} {c.MembershipLeaderMiddleName} {c.MembershipLeaderLastName}"
                }
            ).ToListAsync();

            var profile = new FarmerProfileDto
            {
                PersonalInformation = new FarmerPersonalInfoDto
                {
                    
                    UserId = farmer.UserId,
                    Name = farmer.Name,
                    Gender = farmer.Gender,
                    DateOfBirth = farmer.DateOfBirth,
                    PhoneNumber = farmer.PhoneNumber,
                    Email = user?.Email,
                    BVN = user?.BVN,
                    NIN = user?.NIN,
                    DateRegistered = farmer.OnBoardedOn,
                    FarmerImage = image?.IMageKey,
                    FarmerLevel = EnumExtensions.GetDisplayName(farmer.Level), //user != null ? user.UserLevel.ToString() : null,
                    IsActive = user?.IsActive ?? false,
                },

                Location = new FarmerProfileLocationDto
                {
                    State = farmer.State,
                    Lga = farmer.Lga,
                    Ward = farmer.Ward,
                    Location = farmer.Location,
                },
                CooperativeInformation = cooperatives
            }; 

           
            return profile;
        }

        private async Task<AgentModel> GetAgentProfile(string userId)
        {
            return await _context.Agents
                .Where(a => a.UserId == userId)
                .Select(a => new AgentModel
                {
                    Id = a.Id,
                    Name = a.Name,
                    BelongToBusinessCode=a.BelongToBusinessCode,
                    Email=a.Email,
                    Lga=a.Lga,
                    State=a.State,
                    Location=a.Location,
                    SupervisorUserId=a.SupervisorUserId,
                    OnboardedById=a.OnboardedById,
                    OnBoardedOn=a.OnBoardedOn,
                   // OrganizationCode=a.OrganizationCode,
                    PhoneNumber=a.PhoneNumber,
                    Ward=a.Ward,
                    UserId=userId
                })
                .FirstOrDefaultAsync();
        }
        private async Task<MemberShipModel> GetCooperativeProfile(string userId)
        {
             var memberShip = await _context.MemberShips
                .AsNoTracking()
                .Include(m => m.MemberShipType)
                .Include(m => m.Lga)
                .Include(m => m.State)
                .Include(m => m.Ward)
                .FirstOrDefaultAsync(f => f.UserId == userId);

            if (memberShip == null)
                throw new BaseException(ExceptionType.NO_RECORD_FOUND, "Membership profile not found");

            var agents = await _context.MemberShipAgents
                .AsNoTracking()
                .Where(msa => msa.MemberShipId == memberShip.Id)
                .Select(msa => msa.AgentId) 
                .ToListAsync();

            var profile = new MemberShipModel
            {
                Id = memberShip.Id,
                Name = memberShip.Name,
                MemberShipTypeId = memberShip.MemberShipTypeId,
                MembershipType = memberShip.MemberShipType?.Name,
                BelongToBusinessCode = memberShip.BelongToBusinessCode,
                ContactPerson = memberShip.ContactPerson,
                MembershipLeaderFirstName = memberShip.MembershipLeaderFirstName,
                MembershipLeaderGender = memberShip.MembershipLeaderGender,
                MembershipLeaderLastName = memberShip.MembershipLeaderLastName,
                MembershipLeaderMiddleName = memberShip.MembershipLeaderMiddleName,
             //   OrganizationCode = memberShip.OrganizationCode,
                Lga = memberShip.Lga,
                Email = memberShip.Email,
                HasbeenAssignedAgent = memberShip.HasbeenAssignedAgent,
                Location = memberShip.Location,
                OnboardedById = memberShip.OnboardedById,
                OnBoardedOn = memberShip.OnBoardedOn,
                PhoneNumber = memberShip.PhoneNumber,
                State = memberShip.State,
                Tier = EnumExtensions.GetDisplayName(memberShip.Tier),
                UserId = userId,
                Ward = memberShip.Ward,
                AgentIds = agents.Select(a => a).ToList()
            };

            return profile;
        }

       
        public async Task<UserModel> UpdateUserProfileAsync(
    string userId,
    ProfileRequestModel model)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                throw new BaseException(
                    ExceptionType.NOTFOUND,
                    "User not found");


            var resolvedName =
         !string.IsNullOrWhiteSpace(model.FullName)
             ? model.FullName
             : string.Join(" ",
                 new[] { model.FirstName, model.MiddleName, model.LastName }
                     .Where(x => !string.IsNullOrWhiteSpace(x)));

                if (!string.IsNullOrWhiteSpace(resolvedName) && string.IsNullOrEmpty(user.Name))
                    user.Name = resolvedName;
        

            if (!string.IsNullOrEmpty(model.PhoneNumber) && string.IsNullOrEmpty(user.PhoneNumber))
            {
                    user.PhoneNumber = model.PhoneNumber;
            }
       

            if (!string.IsNullOrWhiteSpace(model.Email) && string.IsNullOrEmpty( user.Email))
                user.Email = model.Email;


            if (!string.IsNullOrWhiteSpace(model.NIN) && string.IsNullOrEmpty(user.NIN))
                user.NIN = model.NIN;

            if (!string.IsNullOrWhiteSpace(model.Bvn) && string.IsNullOrEmpty(user.BVN))
                user.BVN = model.Bvn;

            user.ModifiedBy = userId;
            if(user.UserType ==  UserType.Farmer.ToString())
            {
                var farmer = await _context.Farmers.FirstOrDefaultAsync(f => f.UserId == userId);
                if (farmer != null)
                {
                    user.FarmerOnboardingStatus = FarmerOnboardingStatus.Completed;
                }
            }
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                throw new BaseException(
                    ExceptionType.BAD_REQUEST,
                    result.Errors.First().Description);

            await LinkUserToOrganizationIfNeeded(user.Id, model.organizationCode);

            return user.Map();
        }
        private async Task UpdateMissingCredentials(User existingUser, OnboardingRequestModel model)
        {
            bool updated = false;

         
            if (!string.IsNullOrWhiteSpace(model.Pin))
            {
                var salt = EncryptionUtil.GetSalt();

                existingUser.PinEncrypt = model.Pin.EncryptTransactionPin(salt);
                existingUser.Salt = Convert.ToBase64String(salt);

                existingUser.HasDefaultPin = false;

                updated = true;
            }

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                var removeResult = await _userManager.RemovePasswordAsync(existingUser);

                if (!removeResult.Succeeded)
                {
                    throw new Exception("Failed to remove old password");
                }

                var addResult = await _userManager.AddPasswordAsync(existingUser, model.Password);

                if (!addResult.Succeeded)
                {
                    throw new Exception("Failed to set new password");
                }

                existingUser.HasDefaultPassword = false;

                updated = true;
            }

            if (updated)
            {
                await _context.SaveChangesAsync();
            }
        }
        public async Task<(OnboardingRequestModel model, bool status, string message, bool IsExisting)> RegisterUser(
    OnboardingRequestModel model, OnBoardingArgs args)
        {
            ValidateUserType(args.userType);

            ValidateUserInput(model);

            var role = await GetRoleAsync(args.userType);

            var existingUser = await FindExistingUserAsync(model);

            if (existingUser != null)
            {
                await UpdateMissingCredentials(existingUser, model);
                return await HandleExistingUser(existingUser, model, args, role);
            }
            return await CreateNewUser(model, args, role);
        }
        private void ValidateUserType(string userType)
        {
            switch (userType)
            {
                case "Farmer":
                case "Agent":
                case "MemberShip":
                case "Organization":
                    return;

                default:
                    throw new BaseException(
                        ExceptionType.BAD_REQUEST,
                        "Invalid user type");
            }
        }
        private void ValidateUserInput(OnboardingRequestModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) &&
                string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                throw new BaseException(
                    ExceptionType.BAD_REQUEST,
                    "Either email or phone number must be provided.");
            }
        }
        private async Task<IdentityRole> GetRoleAsync(string roleName)
        {
            var role = await _roleManager.FindByNameAsync(roleName);

            if (role == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Specified role does not exist.");

            return role;
        }

        public async Task<User?> FindExistingUserAsync(OnboardingRequestModel model)
        {
            if (!string.IsNullOrWhiteSpace(model.Email))
                return await _userManager.FindByNameAsync(model.Email);

            if (!string.IsNullOrWhiteSpace(model.PhoneNumber))
                return await _context.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == model.PhoneNumber);

            return null;
        }
        public async Task UpdateUserDetails(
         OnboardingRequestModel model,
         OnBoardingArgs args)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == args.UserId);

            if (user == null)
                throw new BaseException(
                    ExceptionType.NO_RECORD_FOUND,
                    "User record not found");
            var fullName = string.Join(" ", new[]
            {
    model.FirstName,
    model.MiddleName,
    model.LastName
}
            .Where(x => !string.IsNullOrWhiteSpace(x)));

            if (!string.IsNullOrWhiteSpace(fullName))
            {
                user.Name = fullName;
            }

            if (!string.IsNullOrWhiteSpace(model.FullName))
                user.Name = model.FullName;

            if (!string.IsNullOrWhiteSpace(model.PhoneNumber))
                user.PhoneNumber = model.PhoneNumber;

            if (!string.IsNullOrWhiteSpace(model.Email))
                user.Email = model.Email;

            if (!string.IsNullOrWhiteSpace(model.Bvn))
                user.BVN = model.Bvn;

            if (!string.IsNullOrWhiteSpace(model.Nin))
                user.NIN = model.Nin;

            user.DateModified = DateTimeOffset.UtcNow;
            user.ModifiedBy = args.UserId;

            await _context.SaveChangesAsync();
        }
        private async Task<(OnboardingRequestModel model, bool status, string message, bool IsExisting)>
HandleExistingUser(
    User user,
    OnboardingRequestModel model,
    OnBoardingArgs args,
    IdentityRole role)
        {
            bool hasRole = await _context.UserRoles
                .AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);

            if (!hasRole)
            {
                var roleResult = await _userManager.AddToRoleAsync(user, role.Name);

                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    return (model, false, errors, true);
                }
            }

            if (string.IsNullOrWhiteSpace(args.organizationCode))
            {
                return (model, false, "Business context is required.", true);
            }

            var business = await _context.Businesses
                .FirstOrDefaultAsync(x => x.BusinessCode == args.organizationCode);

            if (business == null)
            {
                return (model, false, "Invalid business.",true);
            }

            var membership = await _context.UserBusinesses
                .FirstOrDefaultAsync(x =>
                    x.UserId == user.Id &&
                    x.BusinessId == business.BusinessId);

            // CASE 1: User already in this business
            if (membership != null)
            {
                return (
                    MapUserResponse(user, model),
                    true,
                    "User already exists in this business. Please login with your credentials.",
                    true
                );
            }

            // CASE 2: User exists but NOT in business, add them to the new business
            var newMembership = new UserBusiness
            {
                UserId = user.Id,
                BusinessId = business.BusinessId,
                Role = args.userType,
                IsActive = true,
                IsDefault = false,
                JoinedOn = DateTimeOffset.UtcNow
            };

            _context.UserBusinesses.Add(newMembership);
            await _context.SaveChangesAsync();

            var hasDefault = await _context.UserBusinesses
                .AnyAsync(x => x.UserId == user.Id && x.IsDefault);

            if (!hasDefault)
            {
                newMembership.IsDefault = true;
                await _context.SaveChangesAsync();
            }

            await LinkUserToOrganizationIfNeeded(user.Id, args.organizationCode);

            return (
                MapUserResponse(user, model),
                true,
                "User already exists. Access has been granted to this business. Please login.",
                true
            );
        }
        /** private async Task<(OnboardingRequestModel, bool status, string message)> HandleExistingUser(
        User user,
        OnboardingRequestModel model,
        OnBoardingArgs args,
        IdentityRole role)
        {
            
            bool hasRole = await _context.UserRoles
                .AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);

            if (!hasRole)
            {
                var result = await _userManager.AddToRoleAsync(user, role.Name);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return (model, false, errors);
                }
            }

            await LinkUserToOrganizationIfNeeded(user.Id, args.organizationCode);

            return (
                MapUserResponse(user, model),
                true,
                "User already exists"
            );
        }
        **/
        private async Task<(OnboardingRequestModel, bool, string, bool IsExisting)> CreateNewUser(
       OnboardingRequestModel model,
       OnBoardingArgs args,
       IdentityRole role)
        {
            var salt = EncryptionUtil.GetSalt();

            string defaultPin = "0000";
            string defaultPassword = "Admin123";

            var passwordToUse = string.IsNullOrWhiteSpace(model.Password)
                ? defaultPassword
                : model.Password;

            var pinToUse = string.IsNullOrWhiteSpace(model.Pin) &&
                           !string.IsNullOrWhiteSpace(model.PhoneNumber)
                ? defaultPin
                : model.Pin;

            bool isDefaultPassword = string.IsNullOrWhiteSpace(model.Password);
            bool isDefaultPin = string.IsNullOrWhiteSpace(model.Pin) &&
                                !string.IsNullOrWhiteSpace(model.PhoneNumber);

            var user = BuildUserEntity(model, args, salt);

            if (!string.IsNullOrEmpty(pinToUse))
            {
                user.PinEncrypt = pinToUse.EncryptTransactionPin(salt);
            }

            user.HasDefaultPassword = isDefaultPassword;
            user.HasDefaultPin = isDefaultPin;

            // STEP 1: CREATE USER
            var createUser = await _userManager.CreateAsync(user, passwordToUse);

            if (!createUser.Succeeded)
            {
                var errors = string.Join(", ", createUser.Errors.Select(e => e.Description));
                return (model, false, errors, false);
            }

            await _userManager.AddToRoleAsync(user, role.Name);

            // STEP 2: GET BUSINESS (IMPORTANT CHANGE)
            Business? business = null;

            if (!string.IsNullOrWhiteSpace(args.organizationCode))
            {
                business = await _context.Businesses
                    .FirstOrDefaultAsync(x =>
                        x.BusinessCode == args.organizationCode);
            }

            if (business == null)
            {
                return (model, false, "Invalid business context.", false);
            }

            // STEP 3: LINK USER TO BUSINESS (NEW TABLE)
            var exists = await _context.UserBusinesses
                .AnyAsync(x =>
                    x.UserId == user.Id &&
                    x.BusinessId == business.BusinessId);

            if (!exists)
            {
                _context.UserBusinesses.Add(new UserBusiness
                {
                    UserId = user.Id,
                    BusinessId = business.BusinessId,
                    IsDefault = true,
                    IsActive = true,
                    Role = args.userType,
                    JoinedOn = DateTimeOffset.UtcNow
                });

                await _context.SaveChangesAsync();
            }

            // STEP 4: (Optional) organization mapping stays separate
            await LinkUserToOrganizationIfNeeded(user.Id, args.organizationCode);

            user.Pin = pinToUse;
            user.Password = passwordToUse;

            model.Pin = pinToUse;
            model.Password = passwordToUse;

            return (
                MapUserResponse(user, model),
                true,
                "Registration Successful.",
                false
            );
        }

        /** private async Task<(OnboardingRequestModel, bool, string)> CreateNewUser(
        OnboardingRequestModel model,
        OnBoardingArgs args,
        IdentityRole role)
        {
            var salt = EncryptionUtil.GetSalt();

            string defaultPin = "0000";
            string defaultPassword = "Admin123";

            var passwordToUse = string.IsNullOrWhiteSpace(model.Password)
        ? defaultPassword
        : model.Password;

            var pinToUse = string.IsNullOrWhiteSpace(model.Pin) && !string.IsNullOrWhiteSpace(model.PhoneNumber)
                ? defaultPin
                : model.Pin;

            bool isDefaultPassword = string.IsNullOrWhiteSpace(model.Password);
            bool isDefaultPin = string.IsNullOrWhiteSpace(model.Pin) && !string.IsNullOrWhiteSpace(model.PhoneNumber);


            var user = BuildUserEntity(model, args, salt);

            if (!string.IsNullOrEmpty(pinToUse))
            {
                user.PinEncrypt = pinToUse.EncryptTransactionPin(salt);
            }

            user.HasDefaultPassword = isDefaultPassword;
            user.HasDefaultPin = isDefaultPin;

            var createUser = await _userManager.CreateAsync(user, passwordToUse);

            if (!createUser.Succeeded)
            {
                var errors = string.Join(", ", createUser.Errors.Select(e => e.Description));
                return (model, false, errors);
            }

            await _userManager.AddToRoleAsync(user, role.Name);

            await LinkUserToOrganizationIfNeeded(user.Id, args.organizationCode);
            user.Pin = pinToUse;
            user.Password = passwordToUse;
            model.Pin = pinToUse;
            model.Password = passwordToUse;
            return (
                MapUserResponse(user, model),
                true,
                "Registration Successful."
            );
        }
      **/
        private User BuildUserEntity(OnboardingRequestModel model, OnBoardingArgs args, byte[] salt)
        {
            return new User
            {
                Id = GenerateUserId(args),
                UserName = model.Email ?? model.PhoneNumber,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Salt = Convert.ToBase64String(salt),
                CreatedOn = DateTimeOffset.Now,
                IsActive = true,
                UserType = args.userType,
                BusinessCode = args.businessCode,
                CreatedBy = args.UserId,
                Name = model.FullName,
            };
        }
        private string GenerateUserId(OnBoardingArgs args)
        {
            var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
            var randomDigits = new Random().Next(100, 999);

            if (!string.IsNullOrEmpty(args.businessCode))
                return $"{args.businessCode}{datePart}{randomDigits}";

            if (!string.IsNullOrEmpty(args.organizationCode))
                return $"{args.organizationCode}{datePart}{randomDigits}";

            return Guid.NewGuid().ToString();
        }
        private async Task LinkUserToOrganizationIfNeeded(string userId, string organizationCode)
        {
            if (string.IsNullOrEmpty(organizationCode))
                return;

            bool exists = await _context.UserOrganizations
                .AnyAsync(x => x.UserId == userId && x.OrganizationCode == organizationCode);

            if (!exists)
            {
                _context.UserOrganizations.Add(new UserOrganization
                {
                    UserId = userId,
                    OrganizationCode = organizationCode
                });
            }
        }
        private OnboardingRequestModel MapUserResponse(User user, OnboardingRequestModel model)
        {
            return new OnboardingRequestModel
            {
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Id = user.Id,
                Pin= user.Pin,
                Password=user.Password,
                HasDefaultPin=user.HasDefaultPin,
                HasDefaultPassword=user.HasDefaultPassword,
            };
        }
        public async Task AddUserToOrganizationAsync(User user, string organizationCode, string phoneNumber)
        {
            if (string.IsNullOrEmpty(organizationCode))
            {
                _log.LogInformation($"User {phoneNumber} is a direct business user (no organization).");
                return;
            }

            bool alreadyInOrg = await _context.UserOrganizations
                .AnyAsync(x => x.UserId == user.Id && x.OrganizationCode == organizationCode);

            if (alreadyInOrg)
                throw new BaseException(Core.Enums.ExceptionType.CONFLICT,
                    $"A user with phone number {phoneNumber} already exists in this organization.");

            // Check if already tracked locally to avoid duplicates
            if (!_context.UserOrganizations.Local.Any(x => x.UserId == user.Id && x.OrganizationCode == organizationCode))
            {
                _context.UserOrganizations.Add(new UserOrganization
                {
                    UserId = user.Id,
                    OrganizationCode = organizationCode
                });

                _log.LogInformation($"User {phoneNumber} added to organization {organizationCode}.");
              //  await NotifyUserOfOrganizationAddition(phoneNumber, organizationCode);
            }
        }
        public async Task<UserModel> FindByPhoneNumberAsync(string phoneNumber)
        {
            var user = new User();
            var users = await _context.Set<User>().Where(u => u.PhoneNumber == phoneNumber).ToListAsync();
            if (users.Count > 0)
            {
                if (users.Count > 1)
                {
                    user = users.Where(u => u.UserType == UserType.MemberShip.ToString()).FirstOrDefault();
                }
                else
                {
                    user = users[0];
                }
            }
            else
            {
                return null;
            }


            return user.Map();
        }
        public async Task RemoveUserFromOrganization(string userId, string organizationCode)
        {
            var userOrg = await _context.UserOrganizations
                .FirstOrDefaultAsync(x => x.UserId == userId && x.OrganizationCode == organizationCode);

            if (userOrg == null)
            {
                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "User is not part " +
                    "of the specified organization.");
            }

            _context.UserOrganizations.Remove(userOrg);
            await _context.SaveChangesAsync();

            _log.LogInformation($"User {userId} removed from organization {organizationCode}.");
        }
        public async Task TrackUserRoleAssignment(string userId, string userType)
        {
            var model = await _userManager.FindByIdAsync(userId);
            try
            {
            
                if (!await _userManager.IsInRoleAsync(model, userType))
                {
                    await _userManager.AddToRoleAsync(model, userType);

                }

                _log.LogInformation($"Assigned role '{userType}' to user  under the business .");
            }
            catch (Exception ex)
            {
                _log.LogError(ex, $"Failed to track user role assignment for user {JsonConvert.SerializeObject(model)}");
            }
        }
        public async Task<List<string>> GetOrganizationsForUser(string userId)
        {
            return await _context.UserOrganizations
                .Where(x => x.UserId == userId)
                .Select(x => x.OrganizationCode)
                .ToListAsync();
        }
        public async Task<List<User>> GetUsersInOrganization(string organizationCode)
        {
            return await _context.Users
                .Where(u => u.UserOrganizations.Any(o => o.OrganizationCode == organizationCode))
                .ToListAsync();
        }
        public async Task<(UserModel model, bool status, string message)> GetUserById(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return (null, false, "No record found");
            return (user.Map(), true, "Retrieved Successfully");
        }
        public async Task<(bool Status, string Message)> ForgotPassword(string username)
        {
            var user = await _userManager.FindByEmailAsync(username);
            if (user != null)
            {
                var generatedToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var tokenGeneratedByte = Encoding.UTF8.GetBytes(generatedToken);

                var encodedToken = WebEncoders.Base64UrlEncode(tokenGeneratedByte);
                string callbackUrl = $"{_configuration["ResetPasswordLink"]}?email={user.Email}&token={encodedToken}";


                var message = $"Please have your password reset by <a href='{callbackUrl}'>clicking here</a>.";

                //send mail to user
                var mailRequest = new MailBody
                {
                    To = user.Email,
                    ToName = user.UserName,
                    Subject = "Password Reset",
                    Body = callbackUrl,
                    Token = callbackUrl,
                };

                var mailUtil = SendGridService.SendDynamicForgotPasswordMail(mailRequest);

                return (true, "An Email has been sent to your registered email");
            }
            return (false, "Invalid username");
        }
        public async Task<(bool Status, string Message)> ResetPassword(PasswordResetModel model)
        {
            var userExist = await _userManager.FindByEmailAsync(model.Username);
            if (userExist != null)
            {
                var decodedBytes = WebEncoders.Base64UrlDecode(model.Token);
                model.Token = Encoding.UTF8.GetString(decodedBytes);
                var resetPassword = await _userManager.ResetPasswordAsync(userExist, model.Token, model.Password);
                if (resetPassword.Succeeded)
                {
                    return (true, resetPassword.Succeeded.ToString());
                }
                else
                {
                    return (false, resetPassword.Errors.ToString());
                }

            }
            return (false, "Invalid username");
        }
        public async Task<(bool Status, string Message)> ChangePassword(ChnagePasswordModel model)
        {
            var userExist = await _userManager.FindByEmailAsync(model.UserName);
            if (userExist != null)
            {
                //TODO: rework such that if user is using pin, it should just go ahead and change pin instead of password
                var resetPassword = await _userManager.ChangePasswordAsync(userExist, model.OldPassword, model.NewPassword);
                if (resetPassword.Succeeded)
                {
                    return (true, resetPassword.Succeeded.ToString());
                }
                else
                {
                    return (false, resetPassword.Errors.ToString());
                }

            }
            return (false, "Invalid username");
        }
        public async Task<(UserModel model, bool status, string message)> OnRegistrationAsync(UserModel model, string roleName)
        {
            // Use phone number as email if email is not provided
            model.Email = !string.IsNullOrWhiteSpace(model.Email) ? model.Email : model.PhoneNumber;

            // Generate salt for PIN encryption
            byte[] salt = EncryptionUtil.GetSalt();
            model.Salt = Convert.ToBase64String(salt);

            // Check if the user already exists
            var existingUser = await _userManager.FindByNameAsync(model.Email);

            if (existingUser != null)
            {
                // Only handle organization linkage if organizationCode is provided
                if (!string.IsNullOrEmpty(model.OrganizationCode))
                {
                    bool alreadyInOrg = await _context.UserOrganizations
                        .AnyAsync(x => x.UserId == existingUser.Id && x.OrganizationCode == model.OrganizationCode);

                    if (alreadyInOrg)
                        return (model, false, "User already exists in this organization.");

                    if (!_context.UserOrganizations.Local.Any(x => x.UserId == existingUser.Id && x.OrganizationCode == model.OrganizationCode))
                    {
                        _context.UserOrganizations.Add(new UserOrganization
                        {
                            UserId = existingUser.Id,
                            OrganizationCode = model.OrganizationCode
                        });

                        _log.LogInformation($"Existing user {model.PhoneNumber} added to organization {model.OrganizationCode}.");
                    }
                }
                else
                {
                    _log.LogInformation($"Existing user {model.PhoneNumber} is a direct business user (no organization).");
                }

                return (existingUser.Map(), true, "User already exists, linked to organization if applicable.");
            }

            // New user registration
            var entity = model.Map();
            entity.PinEncrypt = string.IsNullOrEmpty(model.Pin) ? null : model.Pin.EncryptTransactionPin(salt);
            entity.IsActive = true;
            entity.UserType = model.UserType;
            entity.NIN = model.NIN;
            entity.BVN = model.Bvn;

            // Generate custom user ID
            var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
            var randomDigits = new Random().Next(100, 999);
            entity.Id = !string.IsNullOrEmpty(model.BusinessCode)
                ? $"{model.BusinessCode}{datePart}{randomDigits}"
                : !string.IsNullOrEmpty(model.OrganizationCode)
                    ? $"{model.OrganizationCode}{datePart}{randomDigits}"
                    : Guid.NewGuid().ToString();

            // Check for phone number conflict within the same organization
            if (!string.IsNullOrEmpty(model.OrganizationCode))
            {
                var existingPhoneInOrg = await _context.Users
                    .Include(u => u.UserOrganizations)
                    .Where(u => u.PhoneNumber == model.PhoneNumber &&
                                u.UserOrganizations.Any(o => o.OrganizationCode == model.OrganizationCode))
                    .FirstOrDefaultAsync();

                if (existingPhoneInOrg != null)
                    return (model, false, "A user with the same phone number exists in this organization.");
            }

            // Register user in Identity
            IdentityResult createUser = string.IsNullOrEmpty(model.Password)
                ? await _userManager.CreateAsync(entity)
                : await _userManager.CreateAsync(entity, model.Password);

            if (!createUser.Succeeded)
            {
                var errors = string.Join(", ", createUser.Errors.Select(e => e.Description));
                return (model, false, $"Could not create account: {errors}");
            }

            // Assign role
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
                return (model, false, "Specified role does not exist.");

            var roleResult = await _userManager.AddToRoleAsync(entity, role.Name);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                return (model, false, $"Could not assign role: {errors}");
            }

            // Add user to organization if applicable
            if (!string.IsNullOrEmpty(model.OrganizationCode) &&
                !_context.UserOrganizations.Local.Any(x => x.UserId == entity.Id && x.OrganizationCode == model.OrganizationCode))
            {
                _context.UserOrganizations.Add(new UserOrganization
                {
                    UserId = entity.Id,
                    OrganizationCode = model.OrganizationCode
                });

                _log.LogInformation($"New user {model.PhoneNumber} added to organization {model.OrganizationCode}.");
            }
            else if (string.IsNullOrEmpty(model.OrganizationCode))
            {
                _log.LogInformation($"New user {model.PhoneNumber} registered as a direct business user (no organization).");
            }


            return (entity.Map(), true, "User successfully registered and linked to organization if applicable.");
        }
        private async Task TrackDeviceAsync(
    string userId,
    string deviceId,
    string deviceName,
    string ip)
        {
            var device = await _context.UserDevices
                .FirstOrDefaultAsync(x => x.UserId == userId && x.DeviceId == deviceId);

            if (device == null)
            {
                _context.UserDevices.Add(new UserDevice
                {
                    UserId = userId,
                    DeviceId = deviceId,
                    DeviceName = deviceName,
                    IPAddress = ip,
                    LastSeen = DateTime.UtcNow
                });
            }
            else
            {
                device.LastSeen = DateTime.UtcNow;
                device.IPAddress = ip;
            }

            await _context.SaveChangesAsync();
        }
        public async Task<bool> OnNINBVNUpdate(UserModel model)
        {
            var user = await _context.Users.Where(x => x.Id == model.Id).FirstOrDefaultAsync();
            if (user == null)
            {
                return false;
            }
            user.NIN=model.NIN;
                user.BVN=model.Bvn;
                await _context.SaveChangesAsync();

            return true;
        }
        public async Task OnCreateKYC(KYCDetails kYC)
        {

            await _context.KYCDetails.AddAsync(kYC);
            await _context.SaveChangesAsync();

            
        }
       

    }
}
