using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Admin;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.Logic.Managers;
using Agrovesto.Backend.Infrastructure.Logic.Services;
using Agrovesto.Backend.Infrastructure.Utilities;
using Amazon.Runtime.Internal.Util;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;
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
using System.Net.Mail;
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
        Task TrackUserRoleAssignment(string userId, string userType, string roleId, long businessId) ;
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
        Task<(bool Status, string Message, TokenModel Response)> Login(
                LoginModel model, string ipAddress = null);
        //Task<(bool status, string message)> OnUserLevelUpdate(string userId, UserLevel level);
        Task<bool> OnNINBVNUpdate(UserModel model, bool IsVerified);
        Task OnCreateKYC(KYCDetails kYC);
        Task<UsersProfileResponse> GetUserProfile(string userId);
        Task<NotificationPreferenceResponse> GetNotificationPreference(string userId, NotificationUserRole role);
        Task<NotificationPreferenceResponse> SaveNotificationPreference(string userId, NotificationUserRole role, NotificationPreferenceRequest model);
        Task<UserModel> UpdateUserProfileAsync(string userId, ProfileRequestModel model);
        Task<UsersProfile> FindUserById(string userId);
        Task<bool> IsUserInRoleAsync(string userId, string role);
        Task UpdateUserDetails(OnboardingRequestModel model, OnBoardingArgs args);
        Task<UsersProfileResponse> GetUseridentity(string userId);
        Task<KYCDetails> GetKYCDetails(string userId, KYCVerificationType verificationType);
        Task ResetPin(User user,  string newPin);
        Task<bool> ValidateUserPinAsync(string userId, string plainPin);
        Task<bool> IsRevokedAsync(string jti);
        Task RevokeAsync(string jti, string userId, DateTime tokenExpiry);
        Task<CreateUserResult> HandleExistingUser(User user, OnboardingRequestModel model, OnBoardingArgs args, Role role);
        Task<CreateUserResult> CreateNewUser(OnboardingRequestModel model, OnBoardingArgs args, Role role);
        Task<Role> GetRoleAsync(string roleName);
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> work);
        Task<UserModel> OnUpdateUserProfileAsync(string userId, ProfileRequestModel model, string modifiedBy = null);
        Task<(bool Status, string Message, TokenModel Response)> RefreshToken(string refreshToken, string ipAddress = null);
        Task LogoutAsync(string userId, string jti, DateTime accessTokenExpiry, string? sessionId);
        Task<AdminLoginResult> AdminLogin(AdminLoginModel model, string? businessCode, string? ipAddress);
    }
    public class OnboardingRepository : IOnboardingRepository
    {
     
        private const string OrganizationRole = "Organization";
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
         string sessionId,
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
          new Claim("SessionId", sessionId), 
        // Business info
        new Claim("BusinessId", activeBusiness.BusinessId.ToString()),
        new Claim("BusinessCode", activeBusiness.Business?.BusinessCode ?? ""),
        new Claim("BusinessRole", activeBusiness.Role ?? ""),
        new Claim("AuthMethod", user.SignupMethod.ToString()),

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

            if (!string.IsNullOrWhiteSpace(user.Email))
                claims.Add(new Claim(ClaimTypes.Email, user.Email));

            if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
                claims.Add(new Claim(ClaimTypes.MobilePhone, user.PhoneNumber));

           
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
        LoginModel model, string ipAddress = null)
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
                    return (false, "Access denied: you do not have access to this platform", null);
            }
            else
            {
                activeBusiness = userBusinesses.FirstOrDefault(x => x.IsDefault)
                                 ?? userBusinesses.First();
            }

            var tokens = await IssueTokensAsync(
                user,
                activeBusiness,
                weakCredentialResult.IsWeak,
                weakCredentialResult.Message,
                familyId: Guid.NewGuid().ToString("N"),
                ipAddress);

            return (true, "Successful", tokens);
        }


        public async Task<(bool Status, string Message, TokenModel Response)> RefreshToken(
          string refreshToken, string ipAddress = null)
        {
            const string invalid = "Invalid or expired refresh token";

            if (string.IsNullOrWhiteSpace(refreshToken))
                return (false, invalid, null);

            var hash = HashToken(refreshToken);
            var stored = await _context.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash);

            if (stored == null)
                return (false, invalid, null);

            // A revoked token being presented again = possible theft -> kill the whole family
            if (stored.RevokedOn != null)
            {
                await RevokeFamilyAsync(stored.FamilyId, "Reuse detected");
                return (false, invalid, null);
            }

            if (stored.ExpiresOn <= DateTime.UtcNow)
                return (false, invalid, null);

            var user = await _userManager.FindByIdAsync(stored.UserId);
            if (user == null || await _userManager.IsLockedOutAsync(user))
            {
                await RevokeFamilyAsync(stored.FamilyId, "User unavailable");
                return (false, invalid, null);
            }

            var activeBusiness = await _context.UserBusinesses
                .Include(x => x.Business)
                .FirstOrDefaultAsync(x => x.UserId == user.Id
                                       && x.BusinessId == stored.BusinessId
                                       && x.IsActive);

            if (activeBusiness == null)
            {
                await RevokeFamilyAsync(stored.FamilyId, "Business access removed");
                return (false, invalid, null);
            }

            // ADMIN: re-validate admin sessions against the database on every refresh, BEFORE rotating.
            // Permissions are re-read here, so a role or permission change takes effect at the next refresh.
            AdminAccess? adminAccess = null;

            if (stored.IsAdminSession)
            {
                // ASSUMPTION: same "account is active" property used in AdminLogin. Remove if you don't have one.
                if (!user.IsActive)
                {
                    await RevokeFamilyAsync(stored.FamilyId, "Admin account inactive");
                    return (false, invalid, null);
                }

                adminAccess = await GetAdminAccessAsync(user.Id, stored.BusinessId);

                if (!adminAccess.HasAccess)
                {
                    await RevokeFamilyAsync(stored.FamilyId, "Admin access removed");
                    return (false, invalid, null);
                }
            }

            // Atomic rotation: only one concurrent request can win this update
            var rows = await _context.RefreshTokens
                .Where(x => x.Id == stored.Id && x.RevokedOn == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.RevokedOn, DateTime.UtcNow)
                    .SetProperty(x => x.RevokedReason, "Rotated"));

            if (rows == 0)
            {
                await RevokeFamilyAsync(stored.FamilyId, "Concurrent reuse");
                return (false, invalid, null);
            }

            var tokens = await IssueTokensAsync(
                user,
                activeBusiness,
                stored.RequiresCredentialChange,   // carried forward; we can't re-check the password here
                stored.RequiresCredentialChange
                    ? "Your credentials are weak or default. Please change them for security."
                    : null,
                stored.FamilyId,
                ipAddress,
                adminAccess);                      // ADMIN: null for normal sessions; IssueTokensAsync copies IsAdminSession onto the new row

            return (true, "Successful", tokens);
        }

        public async Task RevokeAllForUserAsync(string userId, string reason)
        {
            await _context.RefreshTokens
                .Where(x => x.UserId == userId && x.RevokedOn == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.RevokedOn, DateTime.UtcNow)
                    .SetProperty(x => x.RevokedReason, reason));
        }

        public async Task LogoutAsync(string userId, string jti, DateTime accessTokenExpiry, string? sessionId)
        {
            // 1. Kill the current access token right away (existing mechanism)
            await RevokeAsync(jti, userId, accessTokenExpiry);

            // 2. Kill the refresh tokens for this login session so it can't be refreshed
            if (!string.IsNullOrEmpty(sessionId))
            {
                await _context.RefreshTokens
                    .Where(x => x.UserId == userId && x.FamilyId == sessionId && x.RevokedOn == null)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.RevokedOn, DateTime.UtcNow)
                        .SetProperty(x => x.RevokedReason, "Logout"));
            }
        }

        private Task RevokeFamilyAsync(string familyId, string reason) =>
            _context.RefreshTokens
                .Where(x => x.FamilyId == familyId && x.RevokedOn == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.RevokedOn, DateTime.UtcNow)
                    .SetProperty(x => x.RevokedReason, reason));

        private async Task<TokenModel> IssueTokensAsync(
         User user,
         UserBusiness activeBusiness,
         bool requiresCredentialChange,
         string securityNotice,
         string familyId,
         string ipAddress,
         AdminAccess? adminAccess = null)                                  // ADMIN: new, optional
        {
            // ADMIN: business-scoped, active-only roles for admin sessions
            IList<string> roles = adminAccess != null
                ? adminAccess.Roles
                : await _userManager.GetRolesAsync(user);

            var organizations = await (from uo in _context.UserOrganizations
                                       join bo in _context.Organizations
                                       on uo.OrganizationCode equals bo.OrganizationCode
                                       where uo.UserId == user.Id
                                       select new OrganizationDto
                                       {
                                           OrganizationCode = bo.OrganizationCode,
                                           OrganizationName = bo.CompanyName
                                       }).ToListAsync();

            // ADMIN: don't trap staff in the onboarding flow
            var hasPendingOnboarding = adminAccess == null && (
                string.IsNullOrWhiteSpace(user.Email) ||
                string.IsNullOrWhiteSpace(user.PhoneNumber) ||
                string.IsNullOrWhiteSpace(user.UserType));

            var modules = await _context.BusinessLicenseModules
                .Where(x => x.BusinessLicense.BusinessId == activeBusiness.BusinessId && x.IsEnabled)
                .Select(x => x.SystemModule.Code)
                .ToListAsync();

            // .ToList() so we can append (works whether BuildClaims returns List, array or IEnumerable)
            var claims = BuildClaims(user, roles, activeBusiness, organizations,
                                     hasPendingOnboarding, familyId, modules, requiresCredentialChange)
                         .ToList();

            // ADMIN: permission claims (one claim per permission; the JWT stores them as an array)
            if (adminAccess != null)
            {
                claims.AddRange(adminAccess.Permissions
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(p => new Claim(AdminPermissions.ClaimType, p)));

                claims.Add(new Claim("portal", "admin"));
            }

            var accessExpiry = DateTime.UtcNow.Add(GetAccessTokenLifetime(roles));
            var accessToken = GenerateJwtToken(claims, accessExpiry);

            // Refresh token
            var rawRefresh = GenerateRefreshTokenValue();
            var refreshDays = _configuration.GetValue("JWT:RefreshTokenDays", 7);
            var refreshExpiry = DateTime.UtcNow.AddDays(refreshDays);

            _context.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                BusinessId = activeBusiness.BusinessId,
                TokenHash = HashToken(rawRefresh),
                FamilyId = familyId,
                CreatedOn = DateTime.UtcNow,
                ExpiresOn = refreshExpiry,
                RequiresCredentialChange = requiresCredentialChange,
                CreatedByIp = ipAddress,
                IsAdminSession = adminAccess != null                            // ADMIN: remember the session type
            });

            user.LastLoginDate = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            return new TokenModel
            {
                Permissions = adminAccess?.Permissions ?? new List<string>(),
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                ExpiresOn = accessExpiry,
                Token = accessToken,
                RefreshToken = rawRefresh,
                RefreshTokenExpiresOn = refreshExpiry,
                Roles = roles.ToList(),
                Organizations = organizations,
                phone_number = user.PhoneNumber,
                TokenType = TokenType.AccessToken,
                BusinessCode = activeBusiness.Business.BusinessCode,
                SecurityNotice = securityNotice,
                Authmethod = user.SignupMethod.ToString(),
                RequiresCredentialChange = requiresCredentialChange,
                TwoFactorEnabled = false
            };
        }
        private TimeSpan GetAccessTokenLifetime(IEnumerable<string> roles)
        {
            var isOrganization = roles.Any(r =>
                string.Equals(r, OrganizationRole, StringComparison.OrdinalIgnoreCase));

            return isOrganization
                ? TimeSpan.FromMinutes(_configuration.GetValue("JWT:OrganizationAccessTokenMinutes", 30))
                : TimeSpan.FromMinutes(_configuration.GetValue("JWT:AccessTokenMinutes", 60));
        }

        private static string GenerateRefreshTokenValue() =>
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        private static string HashToken(string token) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
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
        private string GenerateJwtToken(IEnumerable<Claim> claims, DateTime validity)
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
                expires: validity,
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
            var (firstName, middleName, lastName, fullName) = ResolveDisplayName(user);

            var response = new UsersProfileResponse
            {
                UserId = user.Id,
                UserType = user.UserType,
                Email = user.Email,
                Phone = user.PhoneNumber,
                FirstName = firstName,
                MiddleName = middleName,
                LastName = lastName,
                Name = fullName,
                IsEmailVerified = user.IsEmailVerified,
                IsPhoneVerified = user.IsPhoneVerified,
                NIN = user.NIN,
                BVN = user.BVN,
                DateOfBirth = user.DateOfBirth,
                IsBvnVerified = user.IsBvnVerified,
                IsNinVerified = user.IsNinVerified,
              
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
        public async Task<NotificationPreferenceResponse> GetNotificationPreference(string userId, NotificationUserRole role)
        {
            var preference = await _context.UserNotificationPreferences
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId && x.UserRole == role);

            if (preference == null)
            {
                return new NotificationPreferenceResponse
                {
                    UserId = userId,
                    UserRole = role,
                    IsDefault = true,
                    MappingStatusUpdates = false,
                    AgentVisitReminders = false,
                    MembershipAnnouncements = false,
                    DocumentApprovalUpdates = false,
                    GeneralPlatformUpdates = false,
                    NewFarmerAssigned = false,
                    NewFarmCreated = false,
                    BulkUploadCompleted = false,
                    WalletRepaymentDue = false,
                    Sms = false,
                    Email = false,
                    PushNotification = false
                };
            }

            return MapNotificationPreference(preference);
        }

        public async Task<NotificationPreferenceResponse> SaveNotificationPreference(string userId, NotificationUserRole role, NotificationPreferenceRequest model)
        {
            var preference = await ResolvePreferenceForWriteAsync(userId, role);

            switch (role)
            {
                case NotificationUserRole.Farmer:
                    preference.MappingStatusUpdates = model.MappingStatusUpdates;
                    preference.AgentVisitReminders = model.AgentVisitReminders;
                    preference.MembershipAnnouncements = model.MembershipAnnouncements;
                    preference.DocumentApprovalUpdates = model.DocumentApprovalUpdates;
                    preference.GeneralPlatformUpdates = model.GeneralPlatformUpdates;
                    break;

                case NotificationUserRole.Agent:
                    preference.NewFarmerAssigned = model.NewFarmerAssigned;
                    preference.NewFarmCreated = model.NewFarmCreated;
                    preference.BulkUploadCompleted = model.BulkUploadCompleted;
                    preference.WalletRepaymentDue = model.WalletRepaymentDue;
                    break;
            }

            preference.Sms = model.Sms;
            preference.Email = model.Email;
            preference.PushNotification = model.PushNotification;

            await _context.SaveChangesAsync();

            return MapNotificationPreference(preference);
        }

        private async Task<UserNotificationPreference> ResolvePreferenceForWriteAsync(string userId, NotificationUserRole role)
        {
            var preference = await _context.UserNotificationPreferences
                .FirstOrDefaultAsync(x => x.UserId == userId && x.UserRole == role);

            if (preference == null)
            {
                preference = new UserNotificationPreference
                {
                    UserId = userId,
                    UserRole = role,
                    CreatedOn = DateTimeOffset.UtcNow
                };

                await _context.UserNotificationPreferences.AddAsync(preference);
            }
            else
            {
                preference.DateModified = DateTimeOffset.UtcNow;
            }

            return preference;
        }

        private static NotificationPreferenceResponse MapNotificationPreference(UserNotificationPreference preference) =>
            new()
            {
                UserId = preference.UserId,
                UserRole = preference.UserRole,
                IsDefault = false,
                MappingStatusUpdates = preference.MappingStatusUpdates,
                AgentVisitReminders = preference.AgentVisitReminders,
                MembershipAnnouncements = preference.MembershipAnnouncements,
                DocumentApprovalUpdates = preference.DocumentApprovalUpdates,
                GeneralPlatformUpdates = preference.GeneralPlatformUpdates,
                NewFarmerAssigned = preference.NewFarmerAssigned,
                NewFarmCreated = preference.NewFarmCreated,
                BulkUploadCompleted = preference.BulkUploadCompleted,
                WalletRepaymentDue = preference.WalletRepaymentDue,
                Sms = preference.Sms,
                Email = preference.Email,
                PushNotification = preference.PushNotification,
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
        private static (string? First, string? Middle, string? Last, string? Full) ResolveDisplayName(User user)
        {
            var first = string.IsNullOrWhiteSpace(user.FirstName) ? null : user.FirstName.Trim();
            var middle = string.IsNullOrWhiteSpace(user.MiddleName) ? null : user.MiddleName.Trim();
            var last = string.IsNullOrWhiteSpace(user.LastName) ? null : user.LastName.Trim();

            // Preferred: build from the parts
            var fromParts = string.Join(" ", new[] { first, middle, last }.Where(x => x != null));
            if (!string.IsNullOrWhiteSpace(fromParts))
                return (first, middle, last, fromParts);

            // Fallback: legacy Name column, split so the parts aren't empty in the response
            if (!string.IsNullOrWhiteSpace(user.Name))
            {
                var (f, m, l) = MigrationNameSplitter.Split(user.Name);
                return (f, m, l, user.Name.Trim());
            }

            // Nothing available
            return (null, null, null, null);
        }
        private async Task PopulateRoleProfile(UsersProfileResponse response, string roleType, string userId)
        {
            switch (roleType)
            {
                case "Farmer":
                    response.Farmer = await GetFarmerProfile(userId);
                   // response.Name = response.Farmer?.PersonalInformation?.Name ?? response.Name;
                    response.Gender = response.Farmer?.PersonalInformation.Gender ?? "NA";
                    response.Email = !string.IsNullOrWhiteSpace(response.Farmer?.PersonalInformation?.Email)
                        ? response.Farmer.PersonalInformation.Email
                        : response.Email;
                    break;

                case "Agent":
                    response.Agent = await GetAgentProfile(userId);
                   // response.Name = response.Agent?.Name ?? response.Name;
                    response.Email = !string.IsNullOrWhiteSpace(response.Agent?.Email)
                        ? response.Agent.Email
                        : response.Email;
                    response.Gender= response.Gender ?? "NA"; // Assuming Agent model doesn't have gender, so we keep existing gender or set to "NA"
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
                    //response.Name = response.Organization?.OrganizationName ?? response.Name;
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
                .Include(x => x.Ward)
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
                State = organization.State?.State,
                Lga = organization.LGA?.Name,
                Location = organization.CompanyAddress,
                OnboardedOn=organization.OnboardedOn,
                Ward=organization.Ward?.Name,

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
                .Include(x=>x.MemberShipType)
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


            var resolvedName = !string.IsNullOrWhiteSpace(model.FullName)
       ? model.FullName
       : string.Join(" ",
           new[] { model.FirstName, model.MiddleName, model.LastName }
               .Where(x => !string.IsNullOrWhiteSpace(x)));

            if (!string.IsNullOrWhiteSpace(resolvedName))
            {
                var isIdentityVerified = user.IsNinVerified || user.IsBvnVerified;
                var hasExistingName = !string.IsNullOrWhiteSpace(user.Name);

                if (hasExistingName && isIdentityVerified && !EnumExtensions.NamesMatch(user.Name, resolvedName))
                {
                    throw new BaseException(
                        ExceptionType.BAD_REQUEST,
                        "Your name cannot be changed because your NIN or BVN has been verified. " +
                        "Please contact support and provide relevant documentation (e.g. a valid ID or " +
                        "legal name-change document) to request a name update.");
                }

                // Work out the parts: use what the client sent if present, otherwise split the full name
                string first;
                string? middle, last;

                if (!string.IsNullOrWhiteSpace(model.FirstName) || !string.IsNullOrWhiteSpace(model.LastName))
                {
                    first = model.FirstName?.Trim() ?? string.Empty;
                    middle = string.IsNullOrWhiteSpace(model.MiddleName) ? null : model.MiddleName.Trim();
                    last = string.IsNullOrWhiteSpace(model.LastName) ? null : model.LastName.Trim();
                }
                else
                {
                    (first, middle, last) = MigrationNameSplitter.Split(resolvedName);
                }

                // Set if empty, or if the user has no verified identity locking it.
                if (!hasExistingName || !isIdentityVerified)
                {
                    user.Name = resolvedName.Trim();
                    user.FirstName = first;
                    user.MiddleName = middle;
                    user.LastName = last;
                }
                else if (string.IsNullOrWhiteSpace(user.FirstName))
                {
                    // Verified user whose name matches: the name stays locked,
                    // but fill the new part columns if they were never populated.
                    user.FirstName = first;
                    user.MiddleName = middle;
                    user.LastName = last;
                }
            }

            if (!string.IsNullOrEmpty(model.PhoneNumber))
            {
                if(!string.IsNullOrEmpty(user.PhoneNumber) && user.PhoneNumber != model.PhoneNumber)
                {
                    //add to error to be displayed to user
                    throw new BaseException(
                        ExceptionType.BAD_REQUEST,
                        "Phone number cannot be changed. Please contact support if you believe this is an error.");
                }
            }
       

            if (!string.IsNullOrWhiteSpace(model.Email))
            //check first if this email is not tied to any other user on the system
            {
                var existingEmailUser = await _userManager.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id != user.Id && x.Email == model.Email);
                if (existingEmailUser != null)
                {
                    throw new BaseException(
                        ExceptionType.ALREADY_EXIST,
                        "This email is unavailable, use another email or contact support if you believe this is an error.");
                }
            }
            user.Email = model.Email;

            if (!string.IsNullOrWhiteSpace(model.NIN))
            {
                if (!string.IsNullOrWhiteSpace(user.NIN) && user.IsNinVerified)
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

            if (!string.IsNullOrWhiteSpace(model.BVN))
            {
                if (!string.IsNullOrWhiteSpace(user.BVN) && user.IsBvnVerified)
                {
                    if (!string.Equals(user.BVN, model.BVN, StringComparison.Ordinal))
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
                        .FirstOrDefaultAsync(x => x.Id != user.Id && x.BVN == model.BVN);

                    if (existingBvnUser != null)
                    {
                        throw new BaseException(
                            ExceptionType.ALREADY_EXIST,
                            "This BVN is already linked to another account. Please contact support if you believe this is an error.");
                    }

                    user.BVN = model.BVN;
                }
            }
            bool identityLocked = user.IsBvnVerified; // add || user.IsNinVerified if you have that flag

            if (!string.IsNullOrWhiteSpace(model.DateOfBirth))
            {
                var incoming = model.DateOfBirth.Trim();

                if (identityLocked
                    && !string.IsNullOrWhiteSpace(user.DateOfBirth)
                    && !SameDate(incoming, user.DateOfBirth))
                {
                    throw new BaseException(ExceptionType.BAD_REQUEST,
                        "Date of birth cannot be changed after BVN or NIN verification. Please contact support if you believe this is an error.");
                }

                if (!identityLocked || string.IsNullOrWhiteSpace(user.DateOfBirth))
                    user.DateOfBirth = incoming;
            }

            if (!string.IsNullOrWhiteSpace(model.Gender))
            {
                var incoming = model.Gender.Trim();

                if (identityLocked
                    && !string.IsNullOrWhiteSpace(user.Gender)
                    && !string.Equals(incoming, user.Gender, StringComparison.OrdinalIgnoreCase))
                {
                    throw new BaseException(ExceptionType.BAD_REQUEST,
                        "Gender cannot be changed after BVN or NIN verification. Please contact support if you believe this is an error.");
                }

                if (!identityLocked || string.IsNullOrWhiteSpace(user.Gender))
                    user.Gender = incoming;
            }



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
                user.ModifiedBy = model.OnboardedByUserId;
                user.DateModified = DateTimeOffset.UtcNow;
               

                await _context.SaveChangesAsync();
                //
                //var result = await _userManager.UpdateAsync(user);

                //if (!result.Succeeded)
                //    throw new BaseException(
                //        ExceptionType.BAD_REQUEST,
                //        result.Errors.First().Description);
                await LinkUserToOrganizationIfNeeded(user.Id, model.OrganizationCode);

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
        private static bool SameDate(string a, string b) =>
    DateTime.TryParse(a, out var da) && DateTime.TryParse(b, out var db)
        ? da.Date == db.Date
        : string.Equals(a, b, StringComparison.OrdinalIgnoreCase);


        public async Task<CreateUserResult> RegisterUser(
        OnboardingRequestModel model, OnBoardingArgs args)
        {
            
            var role = await GetRoleAsync(args.userType);

           

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
     
        public async Task<Role> GetRoleAsync(string roleName)
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
        private static (string First, string? Middle, string? Last) ResolveNameParts(
      string? firstName, string? middleName, string? lastName, string fullName)
        {
            if (!string.IsNullOrWhiteSpace(firstName) || !string.IsNullOrWhiteSpace(lastName))
            {
                return (
                    firstName?.Trim() ?? string.Empty,
                    string.IsNullOrWhiteSpace(middleName) ? null : middleName.Trim(),
                    string.IsNullOrWhiteSpace(lastName) ? null : lastName.Trim());
            }

            return MigrationNameSplitter.Split(fullName);
        }
        public async Task<CreateUserResult> HandleExistingUser(
        User user,
        OnboardingRequestModel model,
        OnBoardingArgs args,
        Role role )
        {
     
            if (!string.IsNullOrEmpty(model.DateOfBirth) && user.DateOfBirth != model.DateOfBirth)
            {
                user.DateOfBirth = model.DateOfBirth;
            }
            var incomingName = !string.IsNullOrWhiteSpace(model.FullName)
    ? model.FullName
    : string.Join(" ",
        new[] { model.FirstName, model.MiddleName, model.LastName }
            .Where(x => !string.IsNullOrWhiteSpace(x)));

            bool hasName = !string.IsNullOrWhiteSpace(user.Name);
            bool hasParts = !string.IsNullOrWhiteSpace(user.FirstName);

            if (!hasName && !string.IsNullOrWhiteSpace(incomingName))
            {
                user.Name = incomingName.Trim();
            }

            if (!hasParts)
            {
                if (hasName)
                {
                    // Name already on file but parts never populated: derive them from the stored name
                    // so Name and the part columns always agree.
                    var (f, m, l) = MigrationNameSplitter.Split(user.Name);
                    user.FirstName = f;
                    user.MiddleName = m;
                    user.LastName = l;
                }
                else if (!string.IsNullOrWhiteSpace(incomingName))
                {
                    var (f, m, l) = ResolveNameParts(model.FirstName, model.MiddleName, model.LastName, incomingName);
                    user.FirstName = f;
                    user.MiddleName = m;
                    user.LastName = l;
                }
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

            if (user.IsBvnVerified
                  && !string.IsNullOrEmpty(model.DateOfBirth)
                      && model.DateOfBirth != user.DateOfBirth)
            {
                throw new BaseException(
                            ExceptionType.BAD_REQUEST, "Date of birth cannot be changed after BVN or NIN verification.");

            }
            if (user.IsBvnVerified
                    && !string.IsNullOrEmpty(model.Gender)
                        && model.Gender != user.Gender)
            {
                throw new BaseException(
                            ExceptionType.BAD_REQUEST, "Gender cannot be changed after BVN or NIN verification.Please contact support if you believe this is an error.");

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
                throw new BaseException (ExceptionType.OPERATION_FAILED, $"This account {user.PhoneNumber} {user.Email} already exists under another role in this business. Organization accounts must use a separate identity.");

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
                throw new BaseException(ExceptionType.OPERATION_FAILED, "This account {user.PhoneNumber} {user.Email} is registered as an Organization in this business and cannot be used for other roles.");

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
                var roleResult = await AssignRoleAsync(user.Id, role.Id, business.BusinessId);//await _userManager.AddToRoleAsync(user, role.Name);

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
        Role role)
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

            //await _userManager.AddToRoleAsync(user, role.Name);
            Business? business = null;

            if (!string.IsNullOrWhiteSpace(args.businessCode))
            {
                business = await _context.Businesses
                    .FirstOrDefaultAsync(x => x.BusinessCode == args.businessCode);
            }
            await AssignRoleAsync(user.Id, role.Id, business.BusinessId);


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
                Gender=model.Gender,
                UserName = model.Email ?? model.PhoneNumber,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Salt = Convert.ToBase64String(salt),
                CreatedOn = DateTimeOffset.Now,
                IsActive = true,
                UserType = args.userType,
                BusinessCode = args.businessCode,
                CreatedBy = args.UserId,
                FirstName = model.FirstName,
                MiddleName = model.MiddleName,
                LastName = model.LastName,
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
            string prefix = !string.IsNullOrEmpty(args.businessCode)
                ? args.businessCode
                : !string.IsNullOrEmpty(args.organizationCode)
                    ? args.organizationCode
                    : "USR";

            var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
            var randomPart = GenerateSecureRandomString(16); // ~96 bits of entropy

            return $"{prefix}{datePart}{randomPart}";
        }
        private static string GenerateSecureRandomString(int length)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789"; // no ambiguous chars
            var bytes = RandomNumberGenerator.GetBytes(length);
            var result = new char[length];
            for (int i = 0; i < length; i++)
                result[i] = chars[bytes[i] % chars.Length];
            return new string(result);
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
        public async Task TrackUserRoleAssignment(string userId, string userType, string roleId, long businessId)
        {
            var model = await _userManager.FindByIdAsync(userId);
            try
            {
            
                if (!await UserExistinRole(userId, roleId, businessId)
                    //!await _userManager.IsInRoleAsync(model, userType)
                    )
                {
                    await AssignRoleAsync(userId, roleId, businessId);
                   // await _userManager.AddToRoleAsync(model, userType);

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
                var tokenGeneratedByte = Encoding.UTF8.GetBytes(generatedToken);

                var encodedToken = WebEncoders.Base64UrlEncode(tokenGeneratedByte);
                string callbackUrl = $"{_configuration["ResetPasswordLink"]}?t={encodedToken}&u={user.Id}";

                return (true, "User Found", callbackUrl);
            }
            return (false, "User Not found", null);
        }
        public async Task<(bool Status, string Message)> ResetPassword(PasswordResetModel model)
        {
            var userExist = await _userManager.FindByIdAsync(model.UserId);
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
            if (string.IsNullOrWhiteSpace(model.Email))
                throw new BaseException(ExceptionType.OPERATION_FAILED, "Cannot change password for an account with no email address, Kindly update your profile");

            var userExist = await _userManager.FindByEmailAsync(model.Email);

            if (userExist == null)
                throw new BaseException(ExceptionType.OPERATION_FAILED, "Invalid Email");

            if (string.IsNullOrWhiteSpace(model.OldPassword))
                throw new BaseException(ExceptionType.BAD_REQUEST, "Your current password is required.");

            if (string.IsNullOrWhiteSpace(model.NewPassword))
                throw new BaseException(ExceptionType.BAD_REQUEST, "Your new password is required.");

            if (!string.Equals(model.NewPassword, model.ConfirmPassword, StringComparison.Ordinal))
                throw new BaseException(ExceptionType.BAD_REQUEST, "Confirm Password must match New Password.");

            if (string.Equals(model.OldPassword, model.NewPassword, StringComparison.Ordinal))
                throw new BaseException(ExceptionType.BAD_REQUEST, "Your new password must be different from your current password.");

            var resetPassword = await _userManager.ChangePasswordAsync(userExist, model.OldPassword, model.NewPassword);

            if (!resetPassword.Succeeded)
            {
                var reason = string.Join(" ", resetPassword.Errors.Select(x => x.Description));

                throw new BaseException(
                    ExceptionType.BAD_REQUEST,
                    string.IsNullOrWhiteSpace(reason)
                        ? "The current password you entered is incorrect."
                        : reason);
            }

            userExist.HasDefaultPassword = false;
            await _context.SaveChangesAsync();

            return (true, "Password changed successfully");
        }
        public async Task<(UserModel model, bool status, string message)> OnRegistrationAsync(UserModel model, string roleName)
        {
            var business = await _context.Businesses.FirstOrDefaultAsync(x => x.BusinessCode == model.BusinessCode);

            if(business == null)
            {
                return (model, false, "Invalid business");
            }
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

            var roleResult = await AssignRoleAsync(entity.Id, role.Id, business.BusinessId);  //_userManager.AddToRoleAsync(entity, role.Name);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                return (model, false, $"Could not assign role: {errors}");
            }
            var exists = await _context.UserBusinesses
      .AnyAsync(x => x.UserId == entity.Id && x.BusinessId == business.BusinessId);

            if (!exists)
            {
                _context.UserBusinesses.Add(new UserBusiness
                {
                    UserId = entity.Id,
                    BusinessId = business.BusinessId,
                    IsDefault = true,
                    IsActive = true,
                    Role = roleName,
                    JoinedOn = DateTimeOffset.UtcNow
                });

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException ex) when (IsDuplicateKeyViolation(ex, out var conflictField))
                {
                    _log.LogInformation(
                        "Duplicate UserBusiness detected for user {UserId} in business {BusinessId} - " +
                        "treating as existing membership, not a failure.",
                        entity.Id, business.BusinessId);

                    //return await HandleExistingUser(entity, model, args, role);
                }
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


        public async Task<bool> ValidateUserPinAsync(string userId, string plainPin)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null || string.IsNullOrWhiteSpace(user.Salt) || string.IsNullOrWhiteSpace(user.PinEncrypt))
                return false;

            return ValidateUserPin(user, plainPin);
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

        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> work)
        {
            if (_context.Database.CurrentTransaction != null)
                return await work();

            var strategy = _context.Database.CreateExecutionStrategy();
            var attempt = 0;

            return await strategy.ExecuteAsync(async () =>
            {
                if (attempt++ > 0) _context.ChangeTracker.Clear(); // discard stale state from a failed attempt

                await using var tx = await _context.Database.BeginTransactionAsync();
                var result = await work();
                await tx.CommitAsync();
                return result;
            });
        }
        public async Task<UserModel> OnUpdateUserProfileAsync(
    string userId,
    ProfileRequestModel model,
    string modifiedBy = null)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new BaseException(ExceptionType.BAD_REQUEST, "User id is required.");
            if (model == null)
                throw new BaseException(ExceptionType.BAD_REQUEST, "Profile details are required.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new BaseException(ExceptionType.NOTFOUND, "User not found");

            // ── Name: only fill it in when the user has none yet ────────────────
            var resolvedName = ResolveFullName(model);
            if (!string.IsNullOrWhiteSpace(resolvedName) && string.IsNullOrWhiteSpace(user.Name))
                user.Name = resolvedName;

            // ── Phone: immutable (compared by digits so 0803… == +234803…) ──────
            if (!string.IsNullOrEmpty(user.PhoneNumber) && (!string.IsNullOrWhiteSpace(model.PhoneNumber) &&
                !PhoneNumbersMatch(user.PhoneNumber, model.PhoneNumber)))
            {
                throw new BaseException(
                    ExceptionType.BAD_REQUEST,
                    "Phone number cannot be changed. Please contact support if you believe this is an error.");
            }

            // ── Email: only touched when one is supplied AND it really changes ──
            var email = model.Email?.Trim();
            if (!string.IsNullOrEmpty(email))
            {
                if (!IsValidEmail(email))
                    throw new BaseException(ExceptionType.BAD_REQUEST, "The email address is not valid.");

                var normalizedEmail = _userManager.NormalizeEmail(email);
                if (!string.Equals(user.NormalizedEmail, normalizedEmail, StringComparison.Ordinal))
                {
                    var emailTaken = await _context.Users.AsNoTracking()
                        .AnyAsync(x => x.Id != user.Id && x.NormalizedEmail == normalizedEmail);
                    if (emailTaken)
                    {
                        throw new BaseException(
                            ExceptionType.ALREADY_EXIST,
                            "This email is unavailable, use another email or contact support if you believe this is an error.");
                    }

                    user.Email = email;
                    user.NormalizedEmail = normalizedEmail;
                    user.EmailConfirmed = false;   // a new address must be re-confirmed (what SetEmailAsync does)
                }
            }

            // ── NIN ──────────────────────────────────────────────────────────────
            var nin = model.NIN?.Trim();
            if (ShouldWriteIdentifier("NIN", nin, user.NIN, user.IsNinVerified))
            {
                var taken = await _context.Users.AsNoTracking()
                    .AnyAsync(x => x.Id != user.Id && x.NIN == nin);
                if (taken)
                {
                    throw new BaseException(
                        ExceptionType.ALREADY_EXIST,
                        "This NIN is already linked to another account. Please contact support if you believe this is an error.");
                }
                user.NIN = nin;
            }

            // ── BVN ──────────────────────────────────────────────────────────────
            var bvn = model.BVN?.Trim();
            if (ShouldWriteIdentifier("BVN", bvn, user.BVN, user.IsBvnVerified))
            {
                var taken = await _context.Users.AsNoTracking()
                    .AnyAsync(x => x.Id != user.Id && x.BVN == bvn);
                if (taken)
                {
                    throw new BaseException(
                        ExceptionType.ALREADY_EXIST,
                        "This BVN is already linked to another account. Please contact support if you believe this is an error.");
                }
                user.BVN = bvn;
            }

            // ── Date of birth: set once, never overwritten ──────────────────────
            user.DateOfBirth ??= model.DateOfBirth;

            // Audit: who made the change (the acting user when known, else the user themself)
            user.ModifiedBy = string.IsNullOrWhiteSpace(modifiedBy) ? userId : modifiedBy;

            try
            {
                await _context.SaveChangesAsync();

                if (!string.IsNullOrWhiteSpace(model.OrganizationCode))
                    await LinkUserToOrganizationIfNeeded(user.Id, model.OrganizationCode);

                return user.Map();
            }
            catch (BaseException)
            {
                throw; // never re-wrap our own errors
            }
            catch (DbUpdateException ex) when (DbErrors.IsDuplicateKey(ex, out var conflictField))
            {
                // Race-condition backstop: the DB unique index caught what the pre-checks could not.
                throw new BaseException(
                    ExceptionType.ALREADY_EXIST,
                    $"This {conflictField} is already linked to a different account. Please contact support to merge your accounts.");
            }
        }

        /// <summary>
        /// Decides whether an incoming NIN/BVN should be written.
        /// false => nothing to do; true => caller must check uniqueness then write; throws if it conflicts with a VERIFIED value.
        /// </summary>
        private static bool ShouldWriteIdentifier(string label, string incoming, string current, bool isVerified)
        {
            if (string.IsNullOrEmpty(incoming)) return false;                                   // not provided
            if (string.Equals(current, incoming, StringComparison.Ordinal)) return false;       // unchanged

            if (isVerified && !string.IsNullOrWhiteSpace(current))
            {
                throw new BaseException(
                    ExceptionType.BAD_REQUEST,
                    $"The {label} you provided does not match the {label} already linked to your account. If you believe this is incorrect, please contact support.");
            }
            return true;
        }

        private static string ResolveFullName(ProfileRequestModel model)
        {
            if (!string.IsNullOrWhiteSpace(model.FullName)) return model.FullName.Trim();

            return string.Join(" ",
                new[] { model.FirstName, model.MiddleName, model.LastName }
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim()));
        }

        private static bool IsValidEmail(string email)
        {
            try { return new MailAddress(email).Address == email; }
            catch (FormatException) { return false; }
        }

        // Compares the last 10 digits (Nigerian numbers), so format differences don't cause false mismatches.
        private static string PhoneKey(string phone)
        {
            var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
            return digits.Length > 10 ? digits[^10..] : digits;
        }

        private static bool PhoneNumbersMatch(string a, string b)
        {
            var keyA = PhoneKey(a);
            return keyA.Length > 0 && keyA == PhoneKey(b);
        }




        #region admin
        public async Task<AdminLoginResult> AdminLogin(AdminLoginModel model, string? businessCode, string? ipAddress)
        {
            var loginModel = new LoginModel
            {
                Email = model.Email!.Trim(),
                Password = model.Password,
                LoginMethod = /* ASSUMPTION: the value that means "email + password" */ default,
                BusinessCode = businessCode
            };

            // 1. Who is this? Unknown user and wrong password return the SAME message, so the
            //    endpoint can't be used to find out which emails exist.
            var user = await ResolveUserAsync(loginModel);
            if (user == null)
                return AdminLoginResult.Fail(AdminLoginStatus.InvalidCredentials, AdminMessages.InvalidCredentials);

            var credentialResult = await ValidateCredentialsAsync(user, loginModel);
            if (!credentialResult.Success)
                return AdminLoginResult.Fail(AdminLoginStatus.InvalidCredentials, AdminMessages.InvalidCredentials);
            // If ValidateCredentialsAsync also reports lockout, return its message for that case only.

            // 2. Credentials are correct, so it is now safe to say why access is refused.
            if (!user.IsActive) // ASSUMPTION: property name for "account is active"
                return AdminLoginResult.Fail(AdminLoginStatus.AccountInactive, AdminMessages.AccountInactive);

            if(user.Status == AdminUserStatus.Deactivated)
                return AdminLoginResult.Fail(AdminLoginStatus.AccountInactive, AdminMessages.Deactivated);

            if(user.Status == AdminUserStatus.Suspended)
                return AdminLoginResult.Fail(AdminLoginStatus.AccountInactive, AdminMessages.Suspended);

            var userBusinesses = await _context.UserBusinesses
                .Include(x => x.Business)
                .Where(x => x.UserId == user.Id && x.IsActive)
                .ToListAsync();

            var candidates = string.IsNullOrWhiteSpace(businessCode)
                ? userBusinesses.OrderByDescending(x => x.IsDefault).ToList()
                : userBusinesses.Where(x => x.Business.BusinessCode == businessCode).ToList();

            // 3. Pick the first business where this user actually holds Admin Panel access.
            UserBusiness? activeBusiness = null;
            AdminAccess? access = null;

            foreach (var candidate in candidates)
            {
                var found = await GetAdminAccessAsync(user.Id, candidate.BusinessId);
                if (found.HasAccess)
                {
                    activeBusiness = candidate;
                    access = found;
                    break;
                }
            }

            if (activeBusiness == null || access == null)
                return AdminLoginResult.Fail(AdminLoginStatus.NoAdminAccess, AdminMessages.NoAdminAccess);

            // 4. Only now are tokens issued.
            var weak = CheckWeakCredentials(user, loginModel);

            var tokens = await IssueTokensAsync(
                user,
                activeBusiness,
                weak.IsWeak,
                weak.Message,
                familyId: Guid.NewGuid().ToString("N"),
                ipAddress,
                adminAccess: access);

            return AdminLoginResult.Ok(new AdminLoginResponseModel
            {
                Token= tokens.Token,
                RefreshToken = tokens.RefreshToken,
                TwoFactorEnabled = user.TwoFactorEnabled,
               // Authmethod = tokens.Authmethod,
                //BusinessCode= activeBusiness.Business.BusinessCode,
                //Email = user.Email,
                //ExpiresOn = tokens.ExpiresOn,
                //Id = user.Id,
                //Name = user.Name,
                //Permissions = access.Permissions,
                //phone_number = user.PhoneNumber,
                RefreshTokenExpiresOn = tokens.RefreshTokenExpiresOn,
                //Roles = access.Roles,
                RequiresCredentialChange = weak.IsWeak,
                SecurityNotice = weak.IsWeak ? weak.Message : null,
                TokenType = tokens.TokenType,
                //Tokens = tokens,
                //User = new AdminUserProfile
                //{
                //    Id = user.Id.ToString(),
                //    FirstName = user.FirstName,
                //    LastName = user.LastName,
                //    Email = user.Email
                //},
                ////Roles = access.Roles,
                ////Permissions = access.Permissions,
                ////RequiresCredentialChange = weak.IsWeak,
                //CredentialMessage = weak.IsWeak ? weak.Message : null
            });
        }

        /// <summary>
        /// ASSUMPTION: roles live in UserRoles -> Role -> Permissions, scoped per business.
        /// Adapt the query to however you store roles. The only requirement is that it returns
        /// the user's role names, their permission names, and whether AdminPanel.Access is among them.
        /// Being a farmer / agent / organization / cooperative profile must NOT count as admin access.
        /// </summary>
        private async Task<AdminAccess> GetAdminAccessAsync(string userId, long businessId)
        {
            var rows = await _context.UserRoles
                .Where(ur => ur.UserId == userId && ur.BusinessId == businessId && ur.IsActive)
                .Select(ur => new
                {
                    RoleName = ur.Role.Name,
                    Permissions = ur.Role.RolePermissions.Select(rp => rp.Permission.Name).ToList()
                })
                .ToListAsync();

            var roles = rows.Select(r => r.RoleName).Distinct().ToList();
            var permissions = rows.SelectMany(r => r.Permissions).Distinct().ToList();

            // A user with several roles gets the UNION of their permissions.
            var hasAccess = permissions.Contains(Permissions.AdminPanel.Access, StringComparer.OrdinalIgnoreCase);
            return new AdminAccess(hasAccess, roles, permissions);
        }


        #endregion admin

        public async Task<IdentityResult> AssignRoleAsync(string userId, string roleId, long businessId, CancellationToken ct = default)
        {
            try
            {
                var row = await _context.UserRoles.FirstOrDefaultAsync(
                    ur => ur.UserId == userId && ur.RoleId == roleId && ur.BusinessId == businessId, ct);

                if (row == null)
                    _context.UserRoles.Add(new ApplicationUserRole
                    {
                        UserId = userId,
                        RoleId = roleId,
                        BusinessId = businessId,
                        IsActive = true
                    });
                else
                    row.IsActive = true; // re-activate instead of inserting a duplicate

                await _context.SaveChangesAsync(ct);
                return IdentityResult.Success;
            }
            catch (DbUpdateException dbEx)
            {
                // Database error while assigning role
                return IdentityResult.Failed(new IdentityError { Description = dbEx.InnerException?.Message ?? dbEx.Message });
            }
            catch (Exception ex)
            {
                return IdentityResult.Failed(new IdentityError { Description = ex.Message });
            }
        }

        /// <summary>Soft revoke: keeps the history and is picked up by the IsActive filter at the next login/refresh.</summary>
        public async Task RevokeRoleAsync(string userId, string roleId, long businessId, CancellationToken ct = default)
        {
            var row = await _context.UserRoles.FirstOrDefaultAsync(
                ur => ur.UserId == userId && ur.RoleId == roleId && ur.BusinessId == businessId, ct);

            if (row == null) return;

            row.IsActive = false;
            await _context.SaveChangesAsync(ct);
        }
        public async Task<bool> UserExistinRole(string userId, string roleId, long businessId, CancellationToken ct = default)
        {
            var row = await _context.UserRoles.FirstOrDefaultAsync(
                ur => ur.UserId == userId && ur.RoleId == roleId && ur.BusinessId == businessId, ct);

            if (row == null) return false;
            return true;

            
        }
    }

}


