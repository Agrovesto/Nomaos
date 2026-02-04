using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.DB;
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
        Task<(OnboardingRequestModel model, bool status, string message)> RegisterUser(OnboardingRequestModel model, OnBoardingArgs args);
        Task ConfirmUserEmailOrPassword(string userId);
        Task<(bool Status, string Message, TokenModel Response)> Login(LoginModel model, string businessCode);
        //Task<(bool status, string message)> OnUserLevelUpdate(string userId, UserLevel level);
        Task<bool> OnNINBVNUpdate(UserModel model);
        Task OnCreateKYC(KYCDetails kYC);
        Task<UsersProfileResponse> GetUserProfile(string userId);
        Task<UserModel> UpdateUserProfileAsync(string userId, ProfileRequestModel model);
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

        private async Task<User?> ResolveUserAsync(LoginModel model, string businessCode)
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
                            x.PhoneNumber == model.Phonenumber &&
                            x.BusinessCode == businessCode);
                    break;
            }

            if (user == null || user.BusinessCode != businessCode)
                return null;

            return user;
        }


     //   public async Task<(bool Status, string Message, TokenModel Response)> Login(
     //LoginModel model,
     //string businessCode
     //       //,
     //       // string header
     //       )
     //   {
     //       //if (string.IsNullOrWhiteSpace(header))
     //       //  return (false, "Unauthorized request", null);

     //       var business = await _context.Businesses
     //           .AsNoTracking()
     //           .FirstOrDefaultAsync(x => x.BusinessCode == businessCode);

     //       if (business == null || business.BusinessStatus == BusinessStatus.Deactivated.ToString())
     //           return (false, "Business is inactive or not found", null);

     //       User user = null;

     //       // 🔹 Resolve user by username first (authoritative identity)
     //       user = await _userManager.FindByNameAsync(model.Username);

     //       if (user == null || user.BusinessCode != businessCode)
     //           return (false, "Invalid credentials", null);

     //       // 🔹 Validate credential
     //       var credentialResult = await ValidateCredentialsAsync(user, model);
     //       if (!credentialResult.Success)
     //           return (false, credentialResult.Message, null);

     //       // 🔹 Retrieve roles
     //       var roles = await _userManager.GetRolesAsync(user);
     //       if (roles == null || !roles.Any())
     //           return (false, "User has no assigned roles", null);

     //       // 🔹 Check onboarding completeness
     //       var hasPendingOnboarding = HasPendingOnboarding(user);

     //       // 🔹 Update last login
     //       user.LastLoginDate = DateTimeOffset.UtcNow;
     //       await _context.SaveChangesAsync();

     //       // 🔹 Organizations
     //       var organizations = await GetUserOrganizationsAsync(user.Id);

     //       // 🔹 Build claims
     //       var claims = BuildClaims(user, roles, organizations, hasPendingOnboarding);

     //       var token = GenerateJwtToken(claims);

     //       return (true, "Successful", new TokenModel
     //       {
     //           Id = user.Id,
     //           Token = token,
     //           TokenType = TokenType.AccessToken,
     //           ExpiresOn = DateTime.UtcNow.AddDays(1),
     //           Roles = roles.ToList(),
     //           Organizations = organizations,
     //           //  HasPendingOnboarding = hasPendingOnboarding
     //       });
     //   }


        private bool HasPendingOnboarding(User user)
        {
            return string.IsNullOrWhiteSpace(user.Email)
                || string.IsNullOrWhiteSpace(user.PhoneNumber)
                || string.IsNullOrWhiteSpace(user.UserType);
        }

        private List<Claim> BuildClaims(
      User user,
      IList<string>? roles,
      List<OrganizationDto>? organizations,
      bool hasPendingOnboarding)
        {
            var claims = new List<Claim>
    {

        // 🔹 Identity / JWT core
        new Claim(JwtRegisteredClaimNames.Sub, user.Id),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(ClaimTypes.NameIdentifier, user.Id),
       // new Claim(ClaimTypes.Name, user.UserName),

        // 🔹 State
        new Claim("HasPendingOnboarding", hasPendingOnboarding.ToString())
    };
            var principalName =
    user.Name ??
    user.Email ??
    user.PhoneNumber ??
    user.Id;

            claims.Add(new Claim(ClaimTypes.Name, principalName));

            // 🔹 Optional identity fields
            if (!string.IsNullOrWhiteSpace(user.Email))
                claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));

            if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
                claims.Add(new Claim("PhoneNumber", user.PhoneNumber));

            //claims.Add(new Claim(
            //    "DisplayName",
            //    user.Name ?? user.UserName
            //));
            claims.Add(new Claim(
    "DisplayName",
    user.Name ??
    user.Email ??
    user.PhoneNumber ??
    "User"
));

            // 🔹 Roles
            if (roles?.Any() == true)
            {
                // For ASP.NET authorization
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                // For frontend convenience
                claims.Add(new Claim("Roles", string.Join(",", roles)));
            }
          //  claims.Add(new Claim("UserLevel", user.UserLevel.ToString()));



            // 🔹 User metadata
            if (!string.IsNullOrWhiteSpace(user.UserType))
                claims.Add(new Claim("UserType", user.UserType));

            if (user.LastLoginDate.HasValue)
                claims.Add(new Claim("LastLogin", user.LastLoginDate.Value.ToString("O")));
       
            claims.Add(new Claim("MemberSince", user.CreatedOn.ToString("O")));

            // 🔹 Organizations (JSON)
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


        private async Task<List<OrganizationDto>> GetUserOrganizationsAsync(string userId)
        {
            var orgCodes = await _context.UserOrganizations
                .Where(x => x.UserId == userId)
                .Select(x => x.OrganizationCode)
                .Distinct()
                .ToListAsync();

            return await _context.BusinessOrganizations
                .Where(o => orgCodes.Contains(o.OrganizationCode))
                .Select(o => new OrganizationDto
                {
                    OrganizationCode = o.OrganizationCode,
                    OrganizationName = o.OrganizationName
                })
                .ToListAsync();
        }

        private bool ValidateUserPin(User user, string plainPin)
        {
            var salt = Convert.FromBase64String(user.Salt);
            var hashedPin = plainPin.HashSecret(salt);

            return user.PinEncrypt == hashedPin;
        }
        public async Task<(bool Status, string Message, TokenModel Response)> Login(
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

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Any())
                return (false, "User has no assigned roles", null);

            var hasPendingOnboarding = HasPendingOnboarding(user);

            user.LastLoginDate = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            var organizations = await GetUserOrganizationsAsync(user.Id);

            var claims = BuildClaims(user, roles, organizations, hasPendingOnboarding);

            var token = GenerateJwtToken(claims);
            var time = DateTime.UtcNow.AddDays(1);
            return (true, "Successful", new TokenModel
            {
                Id = user.Id,
                Email=user.Email,
                Name=user.Name,
                phone_number=user.PhoneNumber,
             
                Token = token,
                TokenType = TokenType.AccessToken,
                ExpiresOn = time,//DateTime.UtcNow.AddDays(1),
                Roles = roles.ToList(),
                Organizations = organizations
            });
        }

        private async Task<(bool Success, string Message)> ValidateCredentialsAsync(
      User user,
      LoginModel model)
        {
            switch (model.LoginMethod)
            {
                case LoginType.Password:
                    if (!await _userManager.CheckPasswordAsync(user, model.Password))
                        return (false, "Invalid email or password");
                    break;

                case LoginType.Pin:
                    if (!ValidateUserPin(user, model.Pin))
                        return (false, "Invalid phone number or PIN");
                    break;
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

        //public async Task<UserProfileResponse> GetUserProfile(string userId)
        //{
        //    var user = await _context.Users.Where(x => x.Id == userId).FirstOrDefaultAsync();
        //    if (user == null)
        //    {
        //        throw new BaseException(ExceptionType.NO_RECORD_FOUND, "Profile not found");
        //    }
        //    return new UserProfileResponse
        //    {
        //        Email = user.Email,
        //        FullName = user.Name,
        //        PhoneNumber = user.PhoneNumber,
        //        UserId = user.Id,
        //       // Level = EnumExtensions.GetDisplayName(user.UserLevel)//Enum.GetName(typeof(UserLevel),user.UserLevel)//user.UserLevel.ToString()
        //    };
        //}


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


            // Personal/subaccount info
            var subAccount = await _context.SubAccounts.FirstOrDefaultAsync(s => s.UserId == userId);

            // User (for level and active status)
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            // Image (latest entry)
            var image = await _context.FarmerImages
                .Where(i => i.FarmerUserId == userId)
                //.OrderByDescending(i => i.)
                .FirstOrDefaultAsync();

            // Cooperatives
            var cooperatives = await (
                from fc in _context.FarmerCooperatives
                join c in _context.MemberShips on fc.CooperativeId equals c.Id
                where fc.FarmerId == farmer.Id
                select c
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
                CooperativeInformation = cooperatives.Select(c => c.Map()).ToList()
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
                    OrganizationCode=a.OrganizationCode,
                    PhoneNumber=a.PhoneNumber,
                    Ward=a.Ward,
                    UserId=userId
                })
                .FirstOrDefaultAsync();
        }
        private async Task<MemberShipModel> GetCooperativeProfile(string userId)
        {
            return await _context.MemberShips
                .Where(c => c.UserId == userId)
                .Select(c => new MemberShipModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    MemberShipTypeId=c.MemberShipTypeId,
                    MembershipType=c.MemberShipType.Name,
                    BelongToBusinessCode=c.BelongToBusinessCode,
                    ContactPerson=c.ContactPerson,
                    MembershipLeaderFirstName=c.MembershipLeaderFirstName,
                    MembershipLeaderGender=c.MembershipLeaderGender,
                    MembershipLeaderLastName=c.MembershipLeaderLastName,
                    MembershipLeaderMiddleName=c.MembershipLeaderMiddleName,
                    OrganizationCode=c.OrganizationCode,
                    Lga=c.Lga,
                    Email=c.Email,
                    HasbeenAssignedAgent=c.HasbeenAssignedAgent,
                    IsAssignedToAgentId=c.IsAssignedToAgentId,
                    Location=c.Location,
                    OnboardedById=c.OnboardedById,
                    OnBoardedOn=c.OnBoardedOn,
                    PhoneNumber=c.PhoneNumber,
                    State=c.State,
                    Tier= EnumExtensions.GetDisplayName(c.Tier),
                    UserId=userId,
                    Ward=c.Ward
                })
                .FirstOrDefaultAsync();
        }


        //public async Task<UserProfileResponse> GetUserProfile(string userId)
        //{
        //    var user = await _context.Users
        //        .AsNoTracking()
        //        .FirstOrDefaultAsync(x => x.Id == userId);

        //    if (user == null)
        //        throw new BaseException(ExceptionType.NO_RECORD_FOUND, "Profile not found");

        //    var response = new UserProfileResponse
        //    {
        //        UserId = user.Id,
        //        UserType = user.UserType,
        //        Email = user.Email,
        //        Phone = user.PhoneNumber
        //    };

        //    switch (user.UserType)
        //    {
        //        case UserType.Farmer:
        //            response.Farmer = await GetFarmerProfile(userId);
        //            break;

        //        case UserType.Agent:
        //            response.Agent = await GetAgentProfile(userId);
        //            break;

        //        case UserType.MemberShip:
        //            response.Cooperative = await GetCooperativeProfile(userId);
        //            break;

        //        default:
        //            throw new BaseException(ExceptionType.BAD_REQUEST, "Invalid user type");
        //    }

        //    return response;
        //}
        //private async Task<FarmerProfileDto> GetFarmerProfile(string userId)
        //{
        //    var farmer = await _context.Farmers
        //        .Where(f => f.UserId == userId)
        //        .Select(f => new FarmerUserProfileDto
        //        {
        //            FarmerId = f.Id,
        //            FullName = f.FullName,

        //            Agents = f.FarmerAgents
        //                .Select(fa => new SimpleAgentDto
        //                {
        //                    AgentId = fa.Agent.Id,
        //                    Name = fa.Agent.Name
        //                })
        //                .ToList(),

        //            Cooperatives = f.FarmerCooperatives
        //                .Select(fc => new SimpleCooperativeDto
        //                {
        //                    CooperativeId = fc.Cooperative.Id,
        //                    Name = fc.Cooperative.Name
        //                })
        //                .ToList()
        //        })
        //        .FirstOrDefaultAsync();

        //    if (farmer == null)
        //        throw new BaseException(ExceptionType.NO_RECORD_FOUND, "Farmer profile not found");

        //    return farmer;
        //}
        //private async Task<AgentProfileDto> GetAgentProfile(string userId)
        //{
        //    return await _context.Agents
        //        .Where(a => a.UserId == userId)
        //        .Select(a => new AgentProfileDto
        //        {
        //            AgentId = a.Id,
        //            Name = a.Name
        //        })
        //        .FirstOrDefaultAsync();
        //}


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

            if (!string.IsNullOrWhiteSpace(resolvedName))
                user.Name = resolvedName;

            if (!string.IsNullOrWhiteSpace(model.PhoneNumber))
                user.PhoneNumber = model.PhoneNumber;

            if (!string.IsNullOrWhiteSpace(model.Email))
                user.Email = model.Email;


            if (!string.IsNullOrWhiteSpace(model.NIN))
                user.NIN = model.NIN;

            if (!string.IsNullOrWhiteSpace(model.Bvn))
                user.BVN = model.Bvn;

            user.ModifiedBy = userId;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                throw new BaseException(
                    ExceptionType.BAD_REQUEST,
                    result.Errors.First().Description);

            return user.Map();
        }

        public async Task<(OnboardingRequestModel model, bool status, string message)> RegisterUser(OnboardingRequestModel model, OnBoardingArgs args)
        {
            // --- 1. Generate salt for PIN encryption ---
            byte[] salt = EncryptionUtil.GetSalt();
            //Salt = Convert.ToBase64String(salt);

            // Use phone number as email if email is not provided
            if (string.IsNullOrWhiteSpace(model.Email) && string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    "Either email or phone number must be provided.");
            }

            User existingUser = null;

            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                existingUser = await _userManager.FindByNameAsync(model.Email);
            }
            else if (!string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == model.PhoneNumber);
            }

            // Get the role entity
            var roleEntity = await _roleManager.FindByNameAsync(args.userType);
            if (roleEntity == null)
                return (model, false, "Specified role does not exist.");

            if (existingUser != null)
            {
                // --- 3. Check if user already has this role + same user type ---
                bool duplicateRole = await _context.UserRoles
                    .Where(ur => ur.UserId == existingUser.Id && ur.RoleId == roleEntity.Id)
                    .AnyAsync() && existingUser.UserType == args.userType;

                if (duplicateRole)
                    return (model, false, "User already exists with the same role and user type.");

                // --- 4. Assign new role if not already assigned ---
                if (!await _userManager.IsInRoleAsync(existingUser, roleEntity.Name))
                {
                    var roleResult = await _userManager.AddToRoleAsync(existingUser, roleEntity.Name);
                    if (!roleResult.Succeeded)
                    {
                        var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                        return (model, false, $"Could not assign role: {errors}");
                    }
                }

                // --- 5. Link user to organization if applicable ---
                if (!string.IsNullOrEmpty(args.organizationCode))
                {
                    if (!_context.UserOrganizations.Local.Any(x => x.UserId == existingUser.Id && x.OrganizationCode == args.organizationCode))
                    {
                        _context.UserOrganizations.Add(new UserOrganization
                        {
                            UserId = existingUser.Id,
                            OrganizationCode = args.organizationCode
                        });
                        _log.LogInformation($"Existing user {model.PhoneNumber} added to organization {args.organizationCode}.");
                    }
                }
                else
                {
                    _log.LogInformation($"Existing user {model.PhoneNumber} is a direct business user (no organization).");
                }

                return ( new OnboardingRequestModel
                {
                    Email = existingUser.Email,
                    PhoneNumber = existingUser.PhoneNumber,
                    Pin = model.Pin,
                    Password = model.Password,
                    Id= existingUser.Id
                }, true, "User already exists; added to role and/or organization if applicable.");
            }

            // --- 6. New user registration ---
            var entity = new User
            {
                UserName = !string.IsNullOrWhiteSpace(model.Email) ? model.Email : model.PhoneNumber,
              
               
            };
            entity.Salt = Convert.ToBase64String(salt);
            entity.Email = model.Email;
            entity.PhoneNumber = model.PhoneNumber;
            entity.Pin = model.Pin;
            entity.CreatedOn = DateTimeOffset.Now;
           // entity.UserLevel = UserLevel.Level1;
            entity.Password = model.Password;
            //model.Map();
            entity.PinEncrypt = string.IsNullOrEmpty(model.Pin) ? null : model.Pin.EncryptTransactionPin(salt);
            entity.IsActive = true;

            // Assign user type and identifiers
            entity.UserType = args.userType;
            entity.BusinessCode = args.businessCode;
            // Generate custom user ID
            var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
            var randomDigits = new Random().Next(100, 999);
            entity.Id = !string.IsNullOrEmpty(args.businessCode)
                ? $"{args.businessCode}{datePart}{randomDigits}"
                : !string.IsNullOrEmpty(args.organizationCode)
                    ? $"{args.organizationCode}{datePart}{randomDigits}"
                    : Guid.NewGuid().ToString();

            // Quick onboarding: create user in Identity
            IdentityResult createUser = string.IsNullOrEmpty(model.Password)
                ? await _userManager.CreateAsync(entity)
                : await _userManager.CreateAsync(entity, model.Password);

            if (!createUser.Succeeded)
            {
                var errors = string.Join(", ", createUser.Errors.Select(e => e.Description));
                return (model, false, $"Could not create account: {errors}");
            }

            // Assign role
            var roleAddResult = await _userManager.AddToRoleAsync(entity, roleEntity.Name);
            if (!roleAddResult.Succeeded)
            {
                var errors = string.Join(", ", roleAddResult.Errors.Select(e => e.Description));
                return (model, false, $"Could not assign role: {errors}");
            }

            // Link user to organization if applicable
            if (!string.IsNullOrEmpty(args.organizationCode))
            {
                _context.UserOrganizations.Add(new UserOrganization
                {
                    UserId = entity.Id,
                    OrganizationCode = args.organizationCode
                });
                _log.LogInformation($"New user {model.PhoneNumber} added to organization {args.organizationCode}.");
            }
            else
            {
                _log.LogInformation($"New user {model.PhoneNumber} registered as a direct business user (no organization).");
            }

            return (new OnboardingRequestModel
            {
                Email = entity.Email,
                PhoneNumber = entity.PhoneNumber,
                Pin = model.Pin,
                Password = model.Password,
                Id= entity.Id
            }, true, "User successfully registered and linked to role/organization if applicable.");
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
