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
using Microsoft.Data.SqlClient;
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
        Task<UserLookupResult?> FindExistingUserAsync(OnboardingRequestModel model);


        Task<(UserModel model, bool status, string message)> OnRegistrationAsync(UserModel model, string roleName);
        Task RemoveUserFromOrganization(string userId, string organizationCode);
        Task TrackUserRoleAssignment(string userId, string userType) ;
        Task<List<string>> GetOrganizationsForUser(string userId);
        Task<List<User>> GetUsersInOrganization(string organizationCode);
        Task<(UserModel model, bool status, string message)> GetUserById(string userId);
        Task<(bool Status, string Message, string token)> ForgotPassword(string username);
        Task<(bool Status, string Message)> ResetPassword(PasswordResetModel model);
        Task<(bool Status, string Message)> ChangePassword(ChnagePasswordModel model);
        Task<UserModel> FindByPhoneNumberAsync(string phoneNumber);
        Task AddUserToOrganizationAsync(User user, string organizationCode, string phoneNumber);
        Task<CreateUserResult> RegisterUser(OnboardingRequestModel model, OnBoardingArgs args);
        Task ConfirmUserEmailOrPassword(string userId, OneTimePassword otp);
       // Task<(bool Status, string Message, TokenModel Response)> Login(LoginModel model, string businessCode);
        Task<(bool Status, string Message, TokenModel Response)> Login(LoginModel model);
        //Task<(bool status, string message)> OnUserLevelUpdate(string userId, UserLevel level);
        Task<bool> OnNINBVNUpdate(UserModel model, bool IsVerified);
        Task OnCreateKYC(KYCDetails kYC);
        Task<UsersProfileResponse> GetUserProfile(string userId);
        Task<NotificationPreferenceResponse> GetNotificationPreference(string userId);
        Task<NotificationPreferenceResponse> SaveNotificationPreference(string userId, NotificationPreferenceRequest model);
        Task<UserModel> UpdateUserProfileAsync(string userId, ProfileRequestModel model);
        Task<UsersProfile> FindUserById(string userId);
        Task<bool> IsUserInRoleAsync(string userId, string role);
        Task UpdateUserDetails(OnboardingRequestModel model, OnBoardingArgs args);
        Task<UsersProfileResponse> GetUseridentity(string userId);
        Task<KYCDetails> GetKYCDetails(string userId, KYCVerificationType verificationType);
        Task ResetPin(User user,  string newPin);
        Task<bool> IsRevokedAsync(string jti);
        Task RevokeAsync(string jti, string userId, DateTime tokenExpiry);
        Task<CreateUserResult> HandleExistingUser(User user, OnboardingRequestModel model, OnBoardingArgs args, IdentityRole role);
        Task<CreateUserResult> CreateNewUser(OnboardingRequestModel model, OnBoardingArgs args, IdentityRole role);
        Task<IdentityRole> GetRoleAsync(string roleName);
    }
    public class OnboardingRepository : IOnboardingRepository
    {
        private readonly DataContext _context;
        private readonly RoleManager<Role> _roleManager;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OnboardingRepository> _log;
        public OnboardingRepository(DataContext context,
            SignInManager<User> signInManager,

        IConfiguration config, RoleManager<Role> roleManager, UserManager<User> userManager,
            ILogger<OnboardingRepository> log)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _configuration = config;
            _log = log;
            _signInManager = signInManager;
        }
        public async Task RevokeAsync(string jti, string userId, DateTime tokenExpiry)
        {
            var alreadyRevoked = await _context.RevokedTokens
                .AnyAsync(x => x.Jti == jti);

            if (alreadyRevoked) return;

            _context.RevokedTokens.Add(new RevokedToken
            {
                Jti = jti,
                UserId = userId,
                ExpiresAt = tokenExpiry,
                RevokedOn= DateTimeOffset.UtcNow
            });

            await _context.SaveChangesAsync();
        }
        public async Task<bool> IsRevokedAsync(string jti)
        {
            return await _context.RevokedTokens.AnyAsync(x => x.Jti == jti);
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

            var roleEmail = await ResolveRoleEmailAsync(userId);

            var response = new UsersProfile
            {
                UserId = user.Id,
                UserType = user.UserType,
                Email = !string.IsNullOrWhiteSpace(roleEmail) ? roleEmail : user.Email,
                Phone = user.PhoneNumber,
                Name=user.Name
            };

           

            return response;
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
                            
                            );
                    break;
            }


            return user;
        }

         private List<Claim> BuildClaims(
        User user,
        IList<string>? roles,
        UserBusiness activeBusiness,
        List<OrganizationDto>? organizations,
        bool hasPendingOnboarding,
        List<string>? modules = null,
        bool requiresCredentialChange = false)
        {
            var claims = new List<Claim>
    {
        new Claim("HasPendingOnboarding", hasPendingOnboarding.ToString()),
        new Claim("RequiresCredentialChange", requiresCredentialChange.ToString()),

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
                    claims.Add(new Claim("Roles", role));
                }
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
      
      
        private bool ValidateUserPin(User user, string plainPin)
        {
            var salt = Convert.FromBase64String(user.Salt);
            var hashedPin = plainPin.HashSecret(salt);

            return user.PinEncrypt == hashedPin;
        }

         private (bool IsWeak, string Message) CheckWeakCredentials(User user, LoginModel model)
        {
            switch (model.LoginMethod)
            {
                case LoginType.Pin:
                    if (model.Pin == "1234")
                        return (true, "Your PIN is set to a default value. Please change it for security.");
                    break;

                case LoginType.Password:
                    if (!string.IsNullOrWhiteSpace(model.Password) &&
                        !string.IsNullOrWhiteSpace(user.PhoneNumber) &&
                        model.Password == user.PhoneNumber)
                        return (true, "Your password matches your phone number. Please change it for security.");
                    break;
            }

            return (false, null);
        }
        public async Task<(bool Status, string Message, TokenModel Response)> Login(
        LoginModel model)
        {
            var user = await ResolveUserAsync(model);
            if (user == null)
                return (false, "Invalid credentials", null);
            
            var credentialResult = await ValidateCredentialsAsync(user, model);
            if (!credentialResult.Success)
                return (false, credentialResult.Message, null);

            var weakCredentialResult = CheckWeakCredentials(user, model);

            var userBusinesses = await _context.UserBusinesses
                .Include(x => x.Business)
                .Where(x => x.UserId == user.Id && x.IsActive)
                .ToListAsync();

            if (!userBusinesses.Any())
                return (false, "User is not assigned to any business", null);

            UserBusiness activeBusiness;


            if (!string.IsNullOrWhiteSpace(model.BusinessCode))
            {
                activeBusiness = userBusinesses
                    .FirstOrDefault(x => x.Business.BusinessCode == model.BusinessCode);

                if (activeBusiness == null)
                    return (false, "Access denied: you do not has access to this platform", null);
            }
            else
            {
                activeBusiness = userBusinesses.FirstOrDefault(x => x.IsDefault)
                                  ?? userBusinesses.First();
            }

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
                modules,
                weakCredentialResult.IsWeak
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
                Businesses = userBusinesses.Select(b => new BusinessClaimModel
                {
                  BusinessId=  b.BusinessId,
                   BusinessCode= b.Business.BusinessCode,
                   BusinessName= b.Business.BusinessName
                }).ToList(),
                RequiresCredentialChange = weakCredentialResult.IsWeak,
                SecurityNotice = weakCredentialResult.Message
            });
        } 
        
         private async Task<(bool Success, string Message)> ValidateCredentialsAsync(
        User user,
        LoginModel model)
        {
            switch (model.LoginMethod)
            {
                case LoginType.Password:
                    //if (!HasPassword(user))
                    //    return (false, "Password login is not enabled for this account");

                    if (!await _userManager.CheckPasswordAsync(user, model.Password))
                        return (false, "Invalid email or password");
                    break;

                case LoginType.Pin:
                    //if (!HasPin(user))
                    //    return (false, "PIN login is not enabled for this account");

                    if (!ValidateUserPin(user, model.Pin))
                        return (false, "Invalid phone number or PIN");
                    break;

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


        public async Task ConfirmUserEmailOrPassword(string userId, OneTimePassword otp)
        {
            var user = await _userManager.FindByIdAsync(userId);
            user.IsActive = true;
            if(otp.Target == OtpTarget.Email)
            {
                user.IsEmailVerified = true;               
                user.EmailConfirmed = true;
            }
            if(otp.Target == OtpTarget.Phone)
            {
                user.IsPhoneVerified = true;
                user.PhoneNumberConfirmed = true;
            }

            await _userManager.UpdateAsync(user);
        }
        public async Task<UsersProfileResponse> GetUseridentity(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new BaseException(ExceptionType.BAD_REQUEST, "UserId is required");

            var userWithKyc = await (
                from u in _context.Users
                join kyc in _context.KYCDetails on u.Id equals kyc.UserId into kycGroup
                from kyc in kycGroup.DefaultIfEmpty() 
                where u.Id == userId
                select new
                {
                    u.Id,
                    u.UserType,
                    u.Email,
                    u.PhoneNumber,
                    u.Name,
                    u.NIN,
                    u.BVN,
                    Kyc = kyc 
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (userWithKyc == null)
                throw new BaseException(ExceptionType.NO_RECORD_FOUND, "Profile not found");

            var roles = await (
                from ur in _context.UserRoles
                join r in _context.Roles on ur.RoleId equals r.Id
                where ur.UserId == userId
                select new UserRoleDto
                {
                    RoleId = r.Id,
                    RoleName = r.Name
                })
                .AsNoTracking()
                .ToListAsync();

            var roleEmail = await ResolveRoleEmailAsync(userId);

            return new UsersProfileResponse
            {
                UserId = userWithKyc.Id,
                UserType = userWithKyc.UserType,
                Email = !string.IsNullOrWhiteSpace(roleEmail) ? roleEmail : userWithKyc.Email,
                Phone = userWithKyc.PhoneNumber,
                Name = userWithKyc.Name,
                NIN = userWithKyc.NIN,
                BVN = userWithKyc.BVN,
                Roles = roles,
                KYC = userWithKyc.Kyc == null ? null : new KYCDto
                {
                    NIN = userWithKyc.Kyc.Number,
                    BVN = userWithKyc.Kyc.Number,
                    KycType = userWithKyc.Kyc.VerificationType,
                    Base64 = userWithKyc.Kyc.Photourl
                    // add other KYC fields you actually need here
                }
            };
        }
        public async Task<UsersProfileResponse> GetUserProfile(string userId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null)
                throw new BaseException(ExceptionType.NO_RECORD_FOUND, "Profile not found");

               var additionalRoles = await (
                from ur in _context.UserRoles
                join r in _context.Roles on ur.RoleId equals r.Id
                where ur.UserId == userId
                select r.Name 
            ).ToListAsync();

            var response = new UsersProfileResponse
            {
                UserId = user.Id,
                UserType = user.UserType,
                Email = user.Email,
                Phone = user.PhoneNumber,
                IsEmailVerified=user.IsEmailVerified,
                IsPhoneVerified= user.IsPhoneVerified,
                NIN = user.NIN,
                BVN = user.BVN,
                DateOfBirth= user.DateOfBirth,
                IsBvnVerified=user.IsBvnVerified,
                IsNinVerified=user.IsNinVerified,
                Roles = additionalRoles.Select(r => new UserRoleDto { RoleName = r }).ToList()
            };

            var allRoleTypes = new List<string> { user.UserType };
            allRoleTypes.AddRange(additionalRoles);
            allRoleTypes = allRoleTypes.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            foreach (var roleType in allRoleTypes)
            {
                await PopulateRoleProfile(response, roleType, userId);
            }

            if (string.IsNullOrWhiteSpace(response.Email))
                response.Email = await ResolveRoleEmailAsync(userId);

            return response;
        }
        public async Task<NotificationPreferenceResponse> GetNotificationPreference(string userId)
        {
            var preference = await _context.UserNotificationPreferences
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (preference == null)
            {
                return new NotificationPreferenceResponse
                {
                    UserId = userId,
                    IsDefault = true,
                    MappingStatusUpdates = false,
                    AgentVisitReminders = false,
                    MembershipAnnouncements = false,
                    DocumentApprovalUpdates = false,
                    GeneralPlatformUpdates = false,
                    Sms = false,
                    Email = false,
                    InAppNotification = false
                };
            }

            return MapNotificationPreference(preference);
        }

        public async Task<NotificationPreferenceResponse> SaveNotificationPreference(string userId, NotificationPreferenceRequest model)
        {
            var preference = await _context.UserNotificationPreferences
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (preference == null)
            {
                preference = new UserNotificationPreference
                {
                    UserId = userId,
                    CreatedOn = DateTimeOffset.UtcNow
                };

                await _context.UserNotificationPreferences.AddAsync(preference);
            }
            else
            {
                preference.DateModified = DateTimeOffset.UtcNow;
            }

            preference.MappingStatusUpdates = model.MappingStatusUpdates;
            preference.AgentVisitReminders = model.AgentVisitReminders;
            preference.MembershipAnnouncements = model.MembershipAnnouncements;
            preference.DocumentApprovalUpdates = model.DocumentApprovalUpdates;
            preference.GeneralPlatformUpdates = model.GeneralPlatformUpdates;
            preference.Sms = model.Sms;
            preference.Email = model.Email;
            preference.InAppNotification = model.InAppNotification;

            await _context.SaveChangesAsync();

            return MapNotificationPreference(preference);
        }

        private static NotificationPreferenceResponse MapNotificationPreference(UserNotificationPreference preference) =>
            new()
            {
                UserId = preference.UserId,
                IsDefault = false,
                MappingStatusUpdates = preference.MappingStatusUpdates,
                AgentVisitReminders = preference.AgentVisitReminders,
                MembershipAnnouncements = preference.MembershipAnnouncements,
                DocumentApprovalUpdates = preference.DocumentApprovalUpdates,
                GeneralPlatformUpdates = preference.GeneralPlatformUpdates,
                Sms = preference.Sms,
                Email = preference.Email,
                InAppNotification = preference.InAppNotification,
                LastUpdatedOn = preference.DateModified ?? preference.CreatedOn
            };

        private async Task<string?> ResolveRoleEmailAsync(string userId)
        {
            var farmerEmail = await _context.Farmers
                .AsNoTracking()
                .Where(f => f.UserId == userId)
                .Select(f => f.Email)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrWhiteSpace(farmerEmail))
                return farmerEmail;

            var agentEmail = await _context.Agents
                .AsNoTracking()
                .Where(a => a.UserId == userId)
                .Select(a => a.Email)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrWhiteSpace(agentEmail))
                return agentEmail;

            var membershipEmail = await _context.MemberShips
                .AsNoTracking()
                .Where(m => m.UserId == userId)
                .Select(m => m.Email)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrWhiteSpace(membershipEmail))
                return membershipEmail;

            return await _context.Organizations
                .AsNoTracking()
                .Where(o => o.UserId == userId)
                .Select(o => o.OrganizationEmail)
                .FirstOrDefaultAsync();
        }
        private async Task PopulateRoleProfile(UsersProfileResponse response, string roleType, string userId)
        {
            switch (roleType)
            {
                case "Farmer":
                    response.Farmer = await GetFarmerProfile(userId);
                    response.Name = response.Farmer?.PersonalInformation?.Name ?? response.Name;
                    response.Gender = response.Farmer?.PersonalInformation.Gender ?? "NA";
                    response.Email = !string.IsNullOrWhiteSpace(response.Farmer?.PersonalInformation?.Email)
                        ? response.Farmer.PersonalInformation.Email
                        : response.Email;
                    break;

                case "Agent":
                    response.Agent = await GetAgentProfile(userId);
                    response.Name = response.Agent?.Name ?? response.Name;
                    response.Email = !string.IsNullOrWhiteSpace(response.Agent?.Email)
                        ? response.Agent.Email
                        : response.Email;
                    break;

                case "MemberShip":
                    response.Membership = await GetCooperativeProfile(userId); 
                    response.Gender = response.Membership?.MembershipLeaderGender ?? "NA";
                    response.Email = !string.IsNullOrWhiteSpace(response.Membership?.Email)
                        ? response.Membership.Email
                        : response.Email;
                    break;

                case "Organization":
                    response.Organization = await GetOrganizationProfile(userId); // fixed: was overwriting Membership
                    response.Name = response.Organization?.OrganizationName ?? response.Name;
                    response.Email = !string.IsNullOrWhiteSpace(response.Organization?.OrganizationEmail)
                        ? response.Organization.OrganizationEmail
                        : response.Email;
                    break;

                default:
                    break;
            }
        }
        private async Task<OrganizationModel> GetOrganizationProfile(string userId)
        {
            var organization = await _context.Organizations
                .Include(x=>x.State)
                .Include(x => x.LGA)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.UserId == userId);
            if (organization == null)
                throw new BaseException(ExceptionType.NO_RECORD_FOUND, "Organization profile not found");
            var profile = new OrganizationModel
            {
                OrganizaionId = organization.OrganizationId,
                OrganizationName = organization.CompanyName,
                OrganizationCode = organization.OrganizationCode,
                OrganizationEmail = organization.OrganizationEmail,
                PhoneNumber = organization.PhoneNumber,
                State = organization.State.State,
                Lga = organization.LGA.Name,
                Location = organization.CompanyAddress,
             
            };
            return profile;
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
                    Email = !string.IsNullOrWhiteSpace(farmer.Email) ? farmer.Email : user?.Email,
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
                Tier = EnumExtensions.GetDisplayName(memberShip.TierLevel),
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
        

            if (!string.IsNullOrEmpty(model.PhoneNumber))
               {
                    user.PhoneNumber = model.PhoneNumber;
            }
       

            if (!string.IsNullOrWhiteSpace(model.Email))
                user.Email = model.Email;

            if (!string.IsNullOrWhiteSpace(model.NIN))
            {
                if (!string.IsNullOrWhiteSpace(user.NIN))
                {
                    if (!string.Equals(user.NIN, model.NIN, StringComparison.Ordinal))
                    {
                        throw new BaseException(
                            ExceptionType.BAD_REQUEST,
                            "The NIN you provided does not match the NIN already linked to your OnNINBVNUpdateaccount. If you believe this is incorrect, please contact support.");
                    }
                }
                else
                {
                    var existingNinUser = await _userManager.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id != user.Id && x.NIN == model.NIN);

                    if (existingNinUser != null)
                    {
                        throw new BaseException(
                            ExceptionType.ALREADY_EXIST,
                            "This NIN is already linked to another account. Please contact support if you believe this is an error.");
                    }

                    user.NIN = model.NIN;
                }
            }

            if (!string.IsNullOrWhiteSpace(model.Bvn))
            {
                if (!string.IsNullOrWhiteSpace(user.BVN))
                {
                    if (!string.Equals(user.BVN, model.Bvn, StringComparison.Ordinal))
                    {
                        throw new BaseException(
                            ExceptionType.BAD_REQUEST,
                            "The BVN you provided does not match the BVN already linked to your account. If you believe this is incorrect, please contact support.");
                    }
                }
                else
                {
                    var existingBvnUser = await _userManager.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id != user.Id && x.BVN == model.Bvn);

                    if (existingBvnUser != null)
                    {
                        throw new BaseException(
                            ExceptionType.ALREADY_EXIST,
                            "This BVN is already linked to another account. Please contact support if you believe this is an error.");
                    }

                    user.BVN = model.Bvn;
                }
            }
            user.DateOfBirth ??= model.DateOfBirth;
            user.ModifiedBy = userId;
            if(user.UserType ==  UserType.Farmer.ToString())
            {
                var farmer = await _context.Farmers.FirstOrDefaultAsync(f => f.UserId == userId);
                if (farmer != null)
                {
                    user.FarmerOnboardingStatus = FarmerOnboardingStatus.Completed;
                }
            }
            try
            {


                await _context.SaveChangesAsync();
                //
                //var result = await _userManager.UpdateAsync(user);

                //if (!result.Succeeded)
                //    throw new BaseException(
                //        ExceptionType.BAD_REQUEST,
                //        result.Errors.First().Description);
                await LinkUserToOrganizationIfNeeded(user.Id, model.organizationCode);

                return user.Map();
            }
            catch (DbUpdateException ex) when (IsDuplicateKeyViolation(ex, out var conflictField))
            {
                // Surface a clear ALREADY_EXIST error for unique constraint violations
                throw new BaseException(
                    ExceptionType.ALREADY_EXIST,
                    $"This {conflictField} is already linked to a different account. Please contact support to merge your accounts.");
            }
            catch (Exception ex) { 
            
                throw new BaseException(
                    ExceptionType.INTERNAL_SERVER_ERROR,
                    $"An error occurred while updating the user profile: {ex.Message}");
            }
           
        }
        public async Task<CreateUserResult> RegisterUser(
        OnboardingRequestModel model, OnBoardingArgs args)
        {
            
            var role = await GetRoleAsync(args.userType);

            model.Id = args.UserId;

            var lookup = await FindExistingUserAsync(model);

            if (lookup == null)
            {
                return await CreateNewUser(model, args, role);
            }
            if (lookup.MatchedById)
            {
                return await HandleExistingUser(lookup.User, model, args, role);
            }
            var user = lookup.User;

            bool submittedAsEmailAccount = !string.IsNullOrWhiteSpace(model.Email) && !string.IsNullOrWhiteSpace(model.Password);
            bool submittedAsPhoneAccount = !string.IsNullOrWhiteSpace(model.PhoneNumber) && !string.IsNullOrWhiteSpace(model.Pin);

            bool isGenuineOwner =
                (user.SignupMethod == SignupMethod.Email && lookup.MatchedByEmail && submittedAsEmailAccount)
                || (user.SignupMethod == SignupMethod.PhoneNumber && lookup.MatchedByPhone && submittedAsPhoneAccount);

            if (!isGenuineOwner)
            {
                var field = lookup.MatchedByPhone ? "phone number" : "email";
                throw new BaseException(ExceptionType.CONFLICT, $"This {field} is already linked to another account.");
            }

            return await HandleExistingUser(user, model, args, role);
        }
     
        public async Task<IdentityRole> GetRoleAsync(string roleName)
        {
            var role = await _roleManager.FindByNameAsync(roleName);

            if (role == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Specified role does not exist.");

            return role;
        }
        public async Task<UserLookupResult?> FindExistingUserAsync(OnboardingRequestModel model)
        {
            var userId = model.Id?.Trim();

            if (!string.IsNullOrWhiteSpace(userId))
            {
                var userById = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (userById != null)
                    return new UserLookupResult { User = userById, MatchedById = true };

            }

            var phone = model.PhoneNumber?.Trim();
            var email = model.Email?.Trim();
            var normalizedEmail = email?.ToUpperInvariant();

            var normalizedUserNameCandidate = (email ?? phone)?.Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(email))
                return null;

            var user = await _context.Users.FirstOrDefaultAsync(u =>
                (!string.IsNullOrWhiteSpace(phone) && u.PhoneNumber == phone) ||
                (!string.IsNullOrWhiteSpace(email) && u.Email == email) ||
                (!string.IsNullOrWhiteSpace(normalizedEmail) && u.NormalizedEmail == normalizedEmail) ||
                (!string.IsNullOrWhiteSpace(normalizedUserNameCandidate) && u.NormalizedUserName == normalizedUserNameCandidate));

            if (user == null) return null;

            return new UserLookupResult
            {
                User = user,
                MatchedByPhone = !string.IsNullOrWhiteSpace(phone) && string.Equals(user.PhoneNumber, phone, StringComparison.OrdinalIgnoreCase),
                MatchedByEmail = !string.IsNullOrWhiteSpace(email) && string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)
            };
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
        public async Task<CreateUserResult> HandleExistingUser(
        User user,
        OnboardingRequestModel model,
        OnBoardingArgs args,
        IdentityRole role)
        {
     
            if (!string.IsNullOrEmpty(model.DateOfBirth) && user.DateOfBirth != model.DateOfBirth)
            {
                user.DateOfBirth = model.DateOfBirth;
            }
            if (!string.IsNullOrWhiteSpace(model.PhoneNumber)
                && !string.Equals(model.PhoneNumber, user.PhoneNumber, StringComparison.OrdinalIgnoreCase))
            {
                bool phoneTakenByAnotherUser = await _context.Users
                    .AnyAsync(u => u.Id != user.Id && u.PhoneNumber == model.PhoneNumber);

                if (phoneTakenByAnotherUser)
                {
                    throw new BaseException(ExceptionType.CONFLICT, "This phone number is already linked to a different account. Please contact support to merge your accounts.");
                    //return new CreateUserResult
                    //{
                    //    model = model,
                    //    status = false,
                    //    IsExisting = true,
                    //    message = "This phone number is already linked to a different account. Please contact support to merge your accounts.",
                    //    ShouldLogin = false,
                    //};
                }

                user.PhoneNumber = model.PhoneNumber;
            }

            if (!string.IsNullOrWhiteSpace(model.Email)
                && !string.Equals(model.Email, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                bool emailTakenByAnotherUser = await _context.Users
                    .AnyAsync(u => u.Id != user.Id && u.Email == model.Email);

                if (emailTakenByAnotherUser)
                {
                    throw new BaseException(ExceptionType.CONFLICT, "This email is already linked to a different account. Please contact support to merge your accounts.");
                    //return new CreateUserResult
                    //{
                    //    model = model,
                    //    status = false,
                    //    IsExisting = true,
                    //    message = "This email is already linked to a different account. Please contact support to merge your accounts.",
                    //    ShouldLogin = false,
                    //};
                }
                

                user.Email = model.Email;
            }
         
            if (string.IsNullOrWhiteSpace(args.businessCode))
            {
                throw new BaseException(ExceptionType.ACCESS_DENIED, "Business context is required");
                //return new CreateUserResult
                //{
                //    model = model,
                //    status = false,
                //    IsExisting = true,
                //    message = "Business context is required.",
                //    ShouldLogin = !requiresProfileCompletion,
                //};
            }

            var business = await _context.Businesses
                .FirstOrDefaultAsync(x => x.BusinessCode == args.businessCode);

            if (business == null)
            {
                throw new BaseException(ExceptionType.ACCESS_DENIED, "Invalid business");

                //return new CreateUserResult
                //{
                //    model = model,
                //    status = false,
                //    IsExisting = true,
                //    message = "Invalid business.",
                //    ShouldLogin = !requiresProfileCompletion,
                //};
            }

            var identityRoles = await (
                from ur in _context.UserRoles
                join r in _context.Roles on ur.RoleId equals r.Id
                where ur.UserId == user.Id
                select r.Name
            ).ToListAsync();

            var businessMemberships = await _context.UserBusinesses
                .Where(x => x.UserId == user.Id && x.BusinessId == business.BusinessId)
                .ToListAsync();

            var businessRoles = businessMemberships
                .Select(x => x.Role)
                .ToList();

            _log.LogInformation(
                "User {UserId} roles: Identity={IdentityRoles}, Business={BusinessRoles}",
                user.Id,
                string.Join(",", identityRoles),
                string.Join(",", businessRoles));

            bool isOrganizationRequest =
                string.Equals(args.userType, "Organization", StringComparison.OrdinalIgnoreCase);

            bool hasOtherRolesInBusiness = businessRoles.Any(r =>
                !string.Equals(r, "Organization", StringComparison.OrdinalIgnoreCase));

            bool alreadyOrganizationInBusiness = businessRoles.Any(r =>
                string.Equals(r, "Organization", StringComparison.OrdinalIgnoreCase));

            if (isOrganizationRequest && hasOtherRolesInBusiness)
            {
                throw new BaseException (ExceptionType.OPERATION_FAILED, "This account already exists under another role in this business. Organization accounts must use a separate identity.");

                //return new CreateUserResult
                //{
                //    model = model,
                //    status = false,
                //    IsExisting = true,
                //    message =
                //        "This account already exists under another role in this business. " +
                //        "Organization accounts must use a separate identity.",
                //    ShouldLogin = requiresProfileCompletion
                //};
            }

            if (!isOrganizationRequest && alreadyOrganizationInBusiness)
            {
                throw new BaseException(ExceptionType.OPERATION_FAILED, "This account is registered as an Organization in this business and cannot be used for other roles.");

                //return new CreateUserResult
                //{
                //    model = model,
                //    status = false,
                //    IsExisting = true,
                //    message =
                //        "This account is registered as an Organization in this business and cannot be used for other roles.",
                //    ShouldLogin = !requiresProfileCompletion
                //};
            }

            await LinkUserToOrganizationIfNeeded(user.Id, args.organizationCode);

            bool hasIdentityRole = identityRoles.Contains(role.Name);
             bool isNewRoleForUser = !hasIdentityRole;




            if (!hasIdentityRole)
            {
                var roleResult = await _userManager.AddToRoleAsync(user, role.Name);

                if (!roleResult.Succeeded)
                {
                    throw new BaseException(ExceptionType.OPERATION_FAILED, string.Join(", ", roleResult.Errors.Select(e => e.Description)));

                    //return new CreateUserResult
                    //{
                    //    model = model,
                    //    status = false,
                    //    IsExisting = true,
                    //    message = string.Join(", ", roleResult.Errors.Select(e => e.Description)),
                    //    ShouldLogin = !requiresProfileCompletion
                    //};
                }
            }

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

            var hasDefault = await _context.UserBusinesses
                .AnyAsync(x => x.UserId == user.Id && x.IsDefault);

            if (!hasDefault)
            {
                newMembership.IsDefault = true;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsDuplicateKeyViolation(ex, out var conflictField))
            {
               throw new BaseException(ExceptionType.OPERATION_FAILED, $"This {conflictField} is already linked to a different account. Please contact support to merge your accounts.");

                //return new CreateUserResult
                //{
                //    model = model,
                //    status = false,
                //    IsExisting = true,
                //    message = $"This {conflictField} is already linked to a different account. Please contact support to merge your accounts.",
                //    ShouldLogin = !requiresProfileCompletion
                //};
            }

            bool isNewBusinessMembership = !businessMemberships.Any();
            bool hasOnboardedById = !string.IsNullOrEmpty(args.OnboardedByUserId);

            // If the user just picked up a role or business membership they didn't already
            // have, force them into profile completion instead of straight login.
            bool requiresProfileCompletion = isNewRoleForUser || isNewBusinessMembership;

            var msg = !hasOnboardedById
         ? (isNewBusinessMembership
             ? "Welcome back. Continue onboarding to set up access for this business."
             : "Welcome back. Continue onboarding to complete setup for your new role in this business.")
         : (isNewBusinessMembership
             ? "User added successfully to this business"
             : $"User added successfully as a {role.Name}.");

            return new CreateUserResult
            {
                model = MapUserResponse(user, model),
                status = true,
                IsExisting = true,
                message = msg,
                ShouldLogin = !requiresProfileCompletion
            };
        }

        public async Task<CreateUserResult> CreateNewUser(
        OnboardingRequestModel model,
        OnBoardingArgs args,
        IdentityRole role)
        {
            var salt = EncryptionUtil.GetSalt();
            model.SignupMethod = !string.IsNullOrWhiteSpace(model.Email)
    ? SignupMethod.Email
    : SignupMethod.PhoneNumber;
            string defaultPin = "1234";
            string defaultPassword =string.IsNullOrEmpty(model.PhoneNumber) ? "P@55w0rd*" : model.PhoneNumber;

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
            user.SignupMethod = model.SignupMethod;
            if (!string.IsNullOrEmpty(pinToUse))
            {
                user.PinEncrypt = pinToUse.EncryptTransactionPin(salt);
            }

            user.HasDefaultPassword = isDefaultPassword;
            user.HasDefaultPin = isDefaultPin;

            IdentityResult createUser;

            try
            {
                if (model.SignupMethod == SignupMethod.Email)
                {
                    createUser = await _userManager.CreateAsync(user, passwordToUse);
                }
                else
                {
                    createUser = await _userManager.CreateAsync(user);
                }
            }
            catch (DbUpdateException ex) when (IsDuplicateKeyViolation(ex, out var conflictField))
            {
                _log.LogInformation(
                    "Duplicate {ConflictField} on user creation for phone {Phone}/email {Email} — " +
                    "likely a concurrent onboarding request for the same person. Treating as existing user, not a failure.",
                    conflictField, model.PhoneNumber, model.Email);

                var conflictingUser = await FindExistingUserAsync(model);

                if (conflictingUser != null)
                {
                    return await HandleExistingUser(conflictingUser.User, model, args, role);
                }

                _log.LogError(
                    "Duplicate {ConflictField} detected on user creation but no matching user could be " +
                    "found via FindExistingUserAsync for phone {Phone}/email {Email}. Data inconsistency.",
                    conflictField, model.PhoneNumber, model.Email);

                return new CreateUserResult
                {
                    model = model,
                    status = false,
                    IsExisting = true,
                    message = $"An account with this {conflictField} already exists," +
                    $" but we couldn't retrieve it. Please contact support.",
                    ShouldLogin=false,
                };
            }
            if (!createUser.Succeeded)
            {
                var errors = string.Join(", ", createUser.Errors.Select(e => e.Description));
                return new CreateUserResult
                {
                    model = model,
                    status = false,
                    message = errors,
                    IsExisting = false,
                    ShouldLogin=false
                };
            }

            await _userManager.AddToRoleAsync(user, role.Name);
            Business? business = null;

            if (!string.IsNullOrWhiteSpace(args.businessCode))
            {
                business = await _context.Businesses
                    .FirstOrDefaultAsync(x => x.BusinessCode == args.businessCode);
            }

            if (business == null)
            {
                return new CreateUserResult
                {
                    model = model,
                    status = false,
                    message = "Business not found.",
                    IsExisting = false,
                    ShouldLogin=false
                };
            }

            var exists = await _context.UserBusinesses
                .AnyAsync(x => x.UserId == user.Id && x.BusinessId == business.BusinessId);

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

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException ex) when (IsDuplicateKeyViolation(ex, out var conflictField))
                {
                    _log.LogInformation(
                        "Duplicate UserBusiness membership detected for user {UserId} in business {BusinessId} - " +
                        "treating as existing membership, not a failure.",
                        user.Id, business.BusinessId);

                    return await HandleExistingUser(user, model, args, role);
                }
            }

            await LinkUserToOrganizationIfNeeded(user.Id, args.organizationCode);

            user.Pin = pinToUse;
            user.Password = passwordToUse;

            model.Pin = pinToUse;
            model.Password = passwordToUse;

            return new CreateUserResult
            {
                model = MapUserResponse(user, model),
                status = true,
                message = "You're almost done. Please continue to complete your profile.",
                IsExisting = false,
                ShouldLogin=false
            };
        }

        private static bool IsDuplicateKeyViolation(DbUpdateException ex, out string conflictField)
        {
            conflictField = "details";

            var sqlEx = ex.InnerException as SqlException ?? ex.GetBaseException() as SqlException;

            if (sqlEx == null)
                return false;

            // 2627 = violation of PRIMARY KEY or UNIQUE constraint
            // 2601 = cannot insert duplicate key row (unique index)
            bool isDuplicateKey = sqlEx.Errors
                .Cast<SqlError>()
                .Any(e => e.Number == 2627 || e.Number == 2601);

            if (!isDuplicateKey)
                return false;

            var message = sqlEx.Message;

            if (message.Contains("IX_AspNetUsers_PhoneNumber", StringComparison.OrdinalIgnoreCase) 
                //||
              //  message.Contains("IX_Users_PhoneNumber", StringComparison.OrdinalIgnoreCase) ||
                //message.Contains("PhoneNumber", StringComparison.OrdinalIgnoreCase)
                )
                conflictField = "phone number";
            else if (message.Contains("IX_AspNetUsers_Email", StringComparison.OrdinalIgnoreCase) ||
                    // message.Contains("IX_Users_Email", StringComparison.OrdinalIgnoreCase) ||
                     message.Contains("UserNameIndex", StringComparison.OrdinalIgnoreCase) 
                     //||
                     //message.Contains("Email", StringComparison.OrdinalIgnoreCase)
                     )
                conflictField = "email";
            else if (message.Contains("IX_AspNetUsers_BVN", StringComparison.OrdinalIgnoreCase) ||
                    // message.Contains("IX_Users_BVN", StringComparison.OrdinalIgnoreCase) ||
                     message.Contains("BVN", StringComparison.OrdinalIgnoreCase))
                conflictField = "BVN";
            else if (message.Contains("IX_AspNetUsers_NIN", StringComparison.OrdinalIgnoreCase) ||
                   //  message.Contains("IX_Users_NIN", StringComparison.OrdinalIgnoreCase) ||
                     message.Contains("NIN", StringComparison.OrdinalIgnoreCase))
                conflictField = "NIN";

            return true;
        }
        private User BuildUserEntity(OnboardingRequestModel model, OnBoardingArgs args, byte[] salt)
        {
            return new User
            {
                Id = GenerateUserId(args),
                DateOfBirth=model.DateOfBirth,
                UserName = model.Email ?? model.PhoneNumber,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Salt = Convert.ToBase64String(salt),
                CreatedOn = DateTimeOffset.Now,
                IsActive = true,
                UserType = args.userType,
                BusinessCode = args.businessCode,
                CreatedBy = args.UserId,
                Name = !string.IsNullOrWhiteSpace(model.FullName)
       ? model.FullName
       : (string.IsNullOrWhiteSpace(model.FirstName) && string.IsNullOrWhiteSpace(model.LastName)
           ? null
           : string.Join(" ", new[] { model.FirstName, model.MiddleName, model.LastName }
               .Where(x => !string.IsNullOrWhiteSpace(x))))
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

            if (!_context.UserOrganizations.Local.Any(x => x.UserId == user.Id && x.OrganizationCode == organizationCode))
            {
                _context.UserOrganizations.Add(new UserOrganization
                {
                    UserId = user.Id,
                    OrganizationCode = organizationCode
                });

                _log.LogInformation($"User {phoneNumber} added to organization {organizationCode}.");
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
        public async Task<(bool Status, string Message, string token)> ForgotPassword(string username)
        {
            var user = await _userManager.FindByEmailAsync(username);
            if (user != null)
            {
                var generatedToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                
                return (true, "User Found", generatedToken);
            }
            return (false, "User Not found", null);
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
            var userExist = await _userManager.FindByEmailAsync(model.Email);
            if (userExist != null)
            {
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
            if (string.IsNullOrEmpty(userExist.Email)) throw new BaseException(ExceptionType.OPERATION_FAILED, "Cannot change password for an account with no email address, Kindly update your profile");

            throw new BaseException(ExceptionType.OPERATION_FAILED, "Invalid Email");
         
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
        public async Task<bool> OnNINBVNUpdate(UserModel model, bool IsVerified)
        {
            var user = await _context.Users.Where(x => x.Id == model.Id).FirstOrDefaultAsync();
            if (user == null)
            {
                return false;
            }
            try {
            if (!string.IsNullOrEmpty(model.NIN)) {

                user.NIN = model.NIN;
                user.IsNinVerified = IsVerified;
            }
            if (!string.IsNullOrEmpty(model.Bvn)) {

                user.BVN = model.Bvn;
                user.IsBvnVerified = IsVerified;
            }
           

            await _context.SaveChangesAsync();

                return true;
            }
            catch (DbUpdateException ex) when (IsDuplicateKeyViolation(ex, out var conflictField))
            {
                throw new BaseException(
                    ExceptionType.ALREADY_EXIST,
                    $"This {conflictField} is already linked to a different account. Please contact support to merge your accounts.");
            }
        }

        public async Task<KYCDetails> GetKYCDetails(string userId, KYCVerificationType verificationType)
        {
            var kycDetails = await _context.KYCDetails
                .FirstOrDefaultAsync(k => k.UserId == userId && k.VerificationType == verificationType);
            return kycDetails;
        }
        public async Task OnCreateKYC(KYCDetails kYC)
        {
            var existingKYC = await _context.KYCDetails
                .FirstOrDefaultAsync(k => k.UserId == kYC.UserId && k.VerificationType == kYC.VerificationType);

            if(existingKYC == null)
            {
                await _context.KYCDetails.AddAsync(kYC);
             
            }
            else
            {
                existingKYC.Birthcountry = kYC.Birthcountry;
                existingKYC.Birthlga = kYC.Birthlga;
                existingKYC.Birthstate = kYC.Birthstate;
                existingKYC.Birthdate = kYC.Birthdate;
                existingKYC.Photourl = kYC.Photourl;
                existingKYC.Firstname = kYC.Firstname;
                existingKYC.Middlename = kYC.Middlename;
                existingKYC.Surname = kYC.Surname;
                existingKYC.Number=kYC.Number;
                existingKYC.VerifiedBy = kYC.VerifiedBy;
                existingKYC.Phonenumber=kYC.Phonenumber;
                existingKYC.VerificationType = kYC.VerificationType;
            }
            await _context.SaveChangesAsync();
        }


        public async Task ResetPin(User user, string newPin)
        {
            var salt = EncryptionUtil.GetSalt();
            user.Pin = newPin;
            user.Salt = Convert.ToBase64String(salt);
            user.PinEncrypt = newPin.EncryptTransactionPin(salt);
            user.HasDefaultPin = false;
           
            await _context.SaveChangesAsync();
            await _signInManager.SignOutAsync();
        }
 

    }
}
