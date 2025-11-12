using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.Logic.Managers;
using Agrovesto.Backend.Infrastructure.Utilities;
using Amazon.Rekognition.Model;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static Amazon.S3.Util.S3EventNotification;
using User = Agrovesto.Backend.Infrastructure.Entities.Common.User;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories
{
    public class AuthenticationRepository : IAuthenticationRepository
    {
        private readonly DataContext _context;
        private readonly RoleManager<Role> _roleManager;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthenticationRepository> _log;

        private readonly SignInManager<User> _signInManager;
        public AuthenticationRepository(IConfiguration config, RoleManager<Role> roleManager, UserManager<User> userManager,
           SignInManager<User> signInManager, DataContext context, ILogger<AuthenticationRepository> log)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _signInManager = signInManager;
            _configuration = config;
            _log = log;
        }
        public async Task<(bool Status, string Message, TokenModel response)> Login(LoginModel model, string businessCode, string header)
        {
            if (string.IsNullOrEmpty(header))
                throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Forbidden");

            var business = await _context.Businesses
                .FirstOrDefaultAsync(x => x.BusinessCode == businessCode);

            if (business == null || business.BusinessStatus == BusinessStatus.Deactivated.ToString())
                throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, "Forbidden");

            try
            {
                User user;
                IList<string> userRoles;

                // 🔹 Validate credentials based on login method
                if (model.LoginMethod == LoginType.Password)
                {
                    user = header.Contains("IsAgrovesto")
                        ? await _userManager.FindByNameAsync(model.Username)
                        : await _context.Users
                            .FirstOrDefaultAsync(x => x.UserName == model.Username && x.BusinessCode == businessCode)
                            ?? throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "User not found");

                    var validPassword = await _userManager.CheckPasswordAsync(user, model.password);
                    if (!validPassword)
                    {
                        await _userManager.AccessFailedAsync(user);
                        return (false, "Invalid username or password", null);
                    }
                }
                else
                {
                    user = header.Contains("IsAgrovesto")
                        ? await FindByPhoneNumberAsync(model.phonenumber)
                        : await _context.Users
                            .FirstOrDefaultAsync(x => x.PhoneNumber == model.phonenumber && x.BusinessCode == businessCode)
                            ?? throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "User not found");

                    if (string.IsNullOrEmpty(user.Salt))
                        return (false, "Could not login user. Contact administrator", null);

                    var salt = Convert.FromBase64String(user.Salt);
                    var hashedPin = model.pin.HashSecret(salt);

                    if (string.IsNullOrEmpty(user.Pin) || string.IsNullOrEmpty(user.PinEncrypt))
                        return (false, "Login with Email and Password, then enable login with Pin in settings", null);

                    var isValidPin = await ValidateTransactionPin(user, hashedPin);
                    if (!isValidPin)
                    {
                        await _userManager.AccessFailedAsync(user);
                        return (false, "Invalid pin", null);
                    }
                }

                // 🔹 Retrieve user roles
                userRoles = await _userManager.GetRolesAsync(user);
                if (userRoles == null) throw new Exception("Could not sign in user, kindly contact Administrator");

                // 🔹 Update last login
                user.LastLoginDate = DateTimeOffset.Now;
                await _context.SaveChangesAsync();

                // 🔹 Retrieve all organization codes the user belongs to
                var userOrganizations = await _context.UserOrganizations
                    .Where(uo => uo.UserId == user.Id)
                    .Select(uo => uo.OrganizationCode)
                    .Distinct()
                    .ToListAsync();

                // 🔹 Fetch organization details (name + code)
                var organizations = await _context.BusinessOrganizations
                    .Where(o => userOrganizations.Contains(o.OrganizationCode))
                    .Select(o => new OrganizationDto{ OrganizationCode= o.OrganizationCode, OrganizationName= o.OrganizationName })
                    .ToListAsync();

                // 🔹 Convert to claim-safe JSON
                var organizationsJson = JsonConvert.SerializeObject(organizations);

                // 🔹 Build user claims
                var userClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(JwtRegisteredClaimNames.Jti, user.Id),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new Claim("Role", string.Join(',', userRoles)),
            new Claim("UserType", user.UserType ?? ""),
            new Claim("User", user.Name ?? ""),
            new Claim("LastLogin", user.LastLoginDate.ToString()),
            new Claim("MemberSince", user.CreatedOn.ToString()),
            new Claim("Organizations", organizationsJson) // 👈 JSON claim containing code + name
        };

                var signinKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Key"]));
                var securityToken = new JwtSecurityToken(
                    issuer: _configuration["JWT:Issuer"],
                    audience: _configuration["JWT:Audience"],
                    expires: DateTime.Now.AddDays(1),
                    claims: userClaims,
                    signingCredentials: new SigningCredentials(signinKey, SecurityAlgorithms.HmacSha256)
                );

                var token = new JwtSecurityTokenHandler().WriteToken(securityToken);

                var response = new TokenModel
                {
                    Id = user.Id,
                    ExpiresOn = DateTime.Now.AddDays(1),
                    Roles = userRoles.ToList(),
                    Token = token,
                    TokenType = TokenType.AccessToken,
                    Organizations = organizations // 👈 return code + name in response object
                };

                return (true, "Successful", response);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Login failed with error");
                throw;
            }
        }

        /** public async Task<(bool Status, string Message, TokenModel response)> Login(LoginModel model, string businessCode, string header)
         {
             User user = new User();
             if (string.IsNullOrEmpty(header)) { throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, $"Forbidden"); }

             var business = await _context.Businesses.Where(x => x.BusinessCode == businessCode).FirstOrDefaultAsync();
             if(business == null || business.BusinessStatus == BusinessStatus.Deactivated.ToString()) { throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, $"Forbidden"); }
             try

             {
                 IList<string> userRole = new List<string>();
                 if (model.LoginMethod == LoginType.Password)
                 {
                     if (header.Contains("IsAgrovesto"))
                     {
                         user = await _userManager.FindByNameAsync(model.Username);
                     }
                     else if (header.Contains("IsBusiness"))
                     {
                         if (string.IsNullOrEmpty(businessCode))
                         {
                             throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"Business not found");
                         }
                         user = await _context.Users.Where(x => x.UserName == model.Username && x.BusinessCode == businessCode).FirstOrDefaultAsync();
                     }
                     else
                     {
                         throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, $"Unexpected Domain detected");
                     }

                     if (user == null)
                     {

                         //  await _userManager.AccessFailedAsync(user);
                         return (false, "Invalid username or password", null);
                     }
                     var checkPassword = await _userManager.CheckPasswordAsync(user, model.password);
                     if (!checkPassword)
                     {
                         await _userManager.AccessFailedAsync(user);
                         return (false, "Invalid username or password", null);
                     }

                 }
                 else
                 {

                     if (header.Contains("IsAgrovesto"))
                     {
                         user = await FindByPhoneNumberAsync(model.phonenumber);
                     }
                     else if (header.Contains("IsBusiness"))
                     {
                         if (string.IsNullOrEmpty(businessCode))
                         {
                             throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"Business not found");
                         }
                         user = await _context.Users.Where(x => x.PhoneNumber == model.phonenumber && x.BusinessCode == businessCode).FirstOrDefaultAsync();
                     }
                     else
                     {
                         throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, $"Unexpected Domain detected");
                     }

                     if (user == null)
                     {
                         return (false, "User not found", null);

                     }
                     if (string.IsNullOrEmpty(user.Salt))
                     {
                         return (false, "Could not login user. Contact administrator", null);

                     }
                     byte[] salt = Convert.FromBase64String(user.Salt);
                     string transPin = model.pin.HashSecret(salt);
                     if (string.IsNullOrEmpty(user.Pin) || string.IsNullOrEmpty(user.PinEncrypt))
                     {
                         return (false, "Login with Email and Password, then enable login with Pin in settings", null);
                     }
                     var userPin = await ValidateTransactionPin(user, transPin);
                     if (!userPin)
                     {
                         await _userManager.AccessFailedAsync(user);
                         return (false, "Invalid pin", null);
                     }
                     //var result = await _signInManager.CheckPasswordSignInAsync(user, transPin, lockoutOnFailure: true);
                     //if (!result.Succeeded) { return (false, "Invalid pin"); }
                 }
                 //get walletId

                 userRole = await _userManager.GetRolesAsync(user);
                 if (userRole == null) { throw new Exception("Could not sign in user, kindly contact Administrator"); }

                 user.LastLoginDate = DateTimeOffset.Now;
                 await _context.SaveChangesAsync();
                 var userOrganizations = await _context.UserOrganizations
     .Where(uo => uo.UserId == user.Id)
     .Select(uo => uo.OrganizationCode)
     .ToListAsync();

                 var organizationCodes = (userOrganizations != null && userOrganizations.Any())
                     ? string.Join(",", userOrganizations)
                     : null;


                 var userClaims = new List<Claim>
             {
                 new Claim(ClaimTypes.Name, $"{user.UserName}"),
                 new Claim(ClaimTypes.NameIdentifier, user.Id),
                 new Claim(JwtRegisteredClaimNames.Jti, user.Id),
                 new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                 new Claim(JwtRegisteredClaimNames.Email, user.Email),
                 new Claim(JwtRegisteredClaimNames.Typ, "JWT"),
                 new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
                 new Claim(JwtRegisteredClaimNames.Iss, _configuration["JWT:Issuer"]),
                 new Claim(JwtRegisteredClaimNames.Aud, _configuration["JWT:Audience"]),
              //   new Claim("WalletId", userWallet.WalletId.ToString()),
                 new Claim("Role", string.Join(',' ,userRole)),
                 new Claim("UserType", user.UserType),
                 new Claim("User", user.Name),
                 new Claim("Email", user.Email),
                 new Claim("LastLogin", user.LastLoginDate.ToString()),
                 new Claim("MemberSince", user.CreatedOn.ToString()),
             new Claim("OrganizationCodes", organizationCodes ?? string.Empty),


                 //new Claim("OrganizationCode", !string.IsNullOrEmpty(user.OrganizationCode) ? user.OrganizationCode : ""),
                 //new Claim("BelongToBusiness", user.BusinessCode),



             };

                 var signinKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Key"]));

                 var securityToken = new JwtSecurityToken(
                             issuer: _configuration["JWT:Issuer"],
                             audience: _configuration["JWT:Audience"],
                             expires: DateTime.Now.AddDays(1),
                             claims: userClaims,

                             signingCredentials: new SigningCredentials(signinKey, SecurityAlgorithms.HmacSha256));
                 var token = new JwtSecurityTokenHandler().WriteToken(securityToken);


                 var response = new TokenModel
                 {

                     Id = user.Id,
                     ExpiresOn = DateTime.Now.AddDays(1),
                     Roles = userRole.ToList(),
                     Token = token,
                     TokenType = TokenType.AccessToken,
                     OrganizationCodes = userOrganizations
                     //  organizationCode= !string.IsNullOrEmpty(user.OrganizationCode) ? user.OrganizationCode : ""
                 };
                 return (true, "Successful", response);
             }
             catch (Exception ex)
             {
                 _log.LogInformation("Login failed with error", ex.Message);
                 // await _userManager.AccessFailedAsync(user);
                 throw;
                 //return (false, "Oops, an error occured, not your fault!", null);
             }


         }
       **/
        public async Task<bool> ValidateTransactionPin(User user, string transactionPin)
        {

            byte[] salt = Convert.FromBase64String(user.Salt);
            string pin = transactionPin.HashSecret(salt);
            if (string.IsNullOrEmpty(user.PinEncrypt)) return false;
            if (!user.PinEncrypt.Equals(transactionPin))
            {
                return false;
            }
            else
            {
                return true;
            }

        }
        public async Task<User> FindByPhoneNumberAsync(string phoneNumber)
        {
            var user = new User();
            var users = await _context.Set<User>().Where(u => u.PhoneNumber == phoneNumber).ToListAsync();
            if (users.Count > 0)
            {
                if (users.Count > 1)
                {
                    user = users.Where(u => u.UserType == UserType.Cooperative.ToString()).FirstOrDefault();
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


            return user;
        }

        public async Task<(UserModel model, bool status, string message)> RegisterAdmin(UserModel model, string roleName)
        {
            (UserModel model, bool status, string message) result = (model, false, "Operation failed"); // Default return value

            User entity = new User();
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {

                using (var dbTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {

                        var fetchuser = await _userManager.FindByNameAsync(model.Email);
                        byte[] salt = EncryptionUtil.GetSalt();
                        model.Salt = Convert.ToBase64String(salt);
                        entity = model.Map();
                        entity.PinEncrypt = model.Pin.EncryptTransactionPin(salt);
                        entity.IsActive = true;
                        if (fetchuser == null)
                        {
                            var getuserPhone = await _context.Users.Where(x => x.PhoneNumber == model.PhoneNumber).FirstOrDefaultAsync();
                            if (getuserPhone != null)
                            {
                                result = (model, false, "A user with the same phone number exists on the system");
                                return result;
                            }

                            entity.Id = Guid.NewGuid().ToString();
                            entity.UserType = model.UserType;//Enum.GetName(typeof(UserType), model.UserType);
                            var createUser = await _userManager.CreateAsync(entity, model.Password);
                            if (!createUser.Succeeded) throw new Exception($"Could not create user; {createUser.Errors.Select(x => x.Description)}");
                            entity = await _userManager.FindByNameAsync(entity.Email);

                            var uniqueRole = await _roleManager.FindByNameAsync(roleName);
                            if (uniqueRole == null) throw new Exception($"Could not find specified role");
                            var addUserToRole = await _userManager.AddToRoleAsync(entity, uniqueRole.Name);

                            if (!addUserToRole.Succeeded) throw new Exception($"Could not add user to specified role; {addUserToRole.Errors.Select(x => x.Description)}");

                            await _context.SaveChangesAsync();
                            await dbTransaction.CommitAsync();
                            result = (entity.Map(), true, "Success");
                            return result;
                        }
                        else if (fetchuser != null && fetchuser.UserType != "Admin")
                        {
                            var uniqueRole = await _roleManager.FindByNameAsync(roleName);
                            
                            if (uniqueRole == null) throw new Exception($"Could not find specified role");
                            var addUserToRole = await _userManager.AddToRoleAsync(entity, uniqueRole.Name);

                            if (!addUserToRole.Succeeded) throw new Exception($"Could not add user to specified role; {addUserToRole.Errors.Select(x => x.Description)}");

                            await _context.SaveChangesAsync();
                            await dbTransaction.CommitAsync();
                            result = (entity.Map(), true, "User as been added as an Admin");
                            return result;
                        }
                        else
                        {
                            result = (model, false, "Admin User already exists, Kindly contact administrator");
                            return result;
                        }

                    }
                    catch (Exception ex)
                    {
                        await dbTransaction.RollbackAsync();
                        return (model, false, ex.Message);
                    }
                }

            });

            return result;
        }

        //TODO: Update Agent name to fullname
        public async Task<(UserModel model, bool status, string message)> UpdateUserProfile(string userId, UserModel user)
        {
            var model = await _userManager.FindByIdAsync(userId);
            if (model == null) return (null, false, "");
            model.Name = user.FirstName;
            var update = await _userManager.UpdateAsync(model);
            if (!update.Succeeded) throw new Exception($"Could not update user details");
            return (model.Map(), true, "Success");
        }
        public async Task<(UserModel model, bool status, string message)> ChangeUserRole(string userId, string roleName)
        {
            var model = await _userManager.FindByIdAsync(userId);
            var uniqueRole = await _roleManager.FindByNameAsync(roleName);
            if (uniqueRole == null) throw new Exception($"Could not find specified role");
            var userExistingRole = await _userManager.GetRolesAsync(model);
            if (userExistingRole.Any())
            {
                var removeFromRole = await _userManager.RemoveFromRoleAsync(model, userExistingRole[0]);
            }
            var addUserToRole = await _userManager.AddToRoleAsync(model, uniqueRole.Name);

            if (!addUserToRole.Succeeded) throw new Exception($"Could not add user to specified role; {addUserToRole.Errors.Select(x => x.Description)}");
            return (model.Map(), true, "Success");
        }
        public async Task<(UserModel model, bool status, string message)> RegisterBusinessUser(UserModel model, string roleName)
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

            // Optionally save changes here if needed
            // await _context.SaveChangesAsync();

            return (entity.Map(), true, "User successfully registered and linked to organization if applicable.");
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

        public async Task RemoveUserFromOrganization(string userId, string organizationCode)
        {
            var userOrg = await _context.UserOrganizations
                .FirstOrDefaultAsync(x => x.UserId == userId && x.OrganizationCode == organizationCode);

            if (userOrg == null)
            {
                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "User is not part of the specified organization.");
            }

            _context.UserOrganizations.Remove(userOrg);
            await _context.SaveChangesAsync();

            _log.LogInformation($"User {userId} removed from organization {organizationCode}.");
        }

        public async Task TrackUserRoleAssignment(UserModel user, string userType)
        {
            try
            {
                var model = await _userManager.FindByIdAsync(user.Id);
                if (!await _userManager.IsInRoleAsync(model, userType))
                {
                    await _userManager.AddToRoleAsync(model, userType);

                }

                _log.LogInformation($"Assigned role '{userType}' to user  under the business .");
            }
            catch (Exception ex)
            {
                _log.LogError(ex, $"Failed to track user role assignment for user {JsonConvert.SerializeObject(user)}");
            }
        }

        //});

        //return (entity.Map(), true, "Success");
        //}

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

        public async Task<(List<UserViewModel> model, bool status, string message)> GetUsersAsync()
        {
            var user = await _context.Users.ToListAsync();
            if (user == null) return (null, false, "No record found");
            var response = user.Select(x => x.Map()).ToList();
            return (response.Select(x => x.ViewMap()).ToList(), true, "Retrieved Successfully");
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

        //TODO: Rework delete user
        public async Task<bool> DeletUserAsync(string userId, string roleId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;
            var role = await _roleManager.FindByIdAsync(roleId);

            if (role == null) { }
            var removeFromRole = await _userManager.RemoveFromRoleAsync(user, role.Name);
            var delete = await _userManager.DeleteAsync(user);

            return true;

        }

    }
}
