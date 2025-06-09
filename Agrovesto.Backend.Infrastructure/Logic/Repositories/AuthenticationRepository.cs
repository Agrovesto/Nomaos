using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.Logic.Managers;
using Agrovesto.Backend.Infrastructure.Utilities;
using Amazon.Rekognition.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
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
            User user = new User();
            if (string.IsNullOrEmpty(header)) { throw new BaseException(Core.Enums.ExceptionType.UNAUTHORIZED, $"Forbidden"); }

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
                new Claim(JwtRegisteredClaimNames.Exp, DateTime.Now.AddDays(1).ToString()),
             //   new Claim("WalletId", userWallet.WalletId.ToString()),
                new Claim("Role", string.Join(',' ,userRole)),
                new Claim("UserType", user.UserType),
                new Claim("User", user.Name),
                new Claim("Email", user.Email),
                new Claim("LastLogin", user.LastLoginDate.ToString()),
                new Claim("MemberSince", user.CreatedOn.ToString()),
                //new Claim("BelongToBusiness", user.BusinessCode),



            };

                var signinKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Key"]));

                var securityToken = new JwtSecurityToken(
                            issuer: _configuration["JWT:ValidIssuer"],
                            audience: _configuration["JWT:ValidAudience"],
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
                };
                return (true, "Successful", response);
            }
            catch (Exception ex)
            {
                // await _userManager.AccessFailedAsync(user);
                return (false, ex.Message, null);
            }


        }
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
            IdentityResult createUser = new IdentityResult(); model.Email = !string.IsNullOrEmpty(model.Email) ? model.Email : model.PhoneNumber;
            var fetchuser = await _userManager.FindByNameAsync(model.Email);
            byte[] salt = EncryptionUtil.GetSalt();
            model.Salt = Convert.ToBase64String(salt);
            User entity = model.Map();

            //var strategy = _context.Database.CreateExecutionStrategy();
            //await strategy.ExecuteAsync(async () =>
            //{

            //    using (var dbTransaction = _context.Database.BeginTransaction()) 
            //    {
            try
            {

                entity.PinEncrypt = string.IsNullOrEmpty(model.Pin) ? null : model.Pin.EncryptTransactionPin(salt);
                entity.IsActive = true;
                if (fetchuser == null)
                {
                    var getuserPhone = await _context.Users.Where(x => x.PhoneNumber == model.PhoneNumber).FirstOrDefaultAsync();
                    if (getuserPhone != null)
                    {
                        return (model, false, "A user with the same phone number exists on the system");
                    }
                    if (!string.IsNullOrEmpty(model.BusinessCode))
                    {
                        var datePart = DateTime.UtcNow.ToString("yyyyMMdd"); // or any desired date format
                        var randomDigits = new Random().Next(100, 999); // 3 random digits
                        entity.Id = $"{model.BusinessCode}{datePart}{randomDigits}";
                    }
                    else
                    {
                        entity.Id = Guid.NewGuid().ToString();
                    }

                    entity.UserType = model.UserType;//Enum.GetName(typeof(UserType), model.UserType);

                    if (string.IsNullOrEmpty(model.Password))
                    {
                        createUser = await _userManager.CreateAsync(entity);
                    }
                    else
                    {
                        createUser = await _userManager.CreateAsync(entity, model.Password); //create passwordless
                    }

                    if (!createUser.Succeeded) throw new Exception($"Could not create account for {model.FirstName} {entity.UserType}");
                    entity = await _userManager.FindByNameAsync(entity.Email);

                    var uniqueRole = await _roleManager.FindByNameAsync(roleName);
                    if (uniqueRole == null) throw new Exception($"Could not find specified role");

                    var addUserToRole = await _userManager.AddToRoleAsync(entity, uniqueRole.Name);

                    if (!addUserToRole.Succeeded) throw new Exception($"Could not add user to specified role; {addUserToRole.Errors.Select(x => x.Description)}");


                    //var token = await _userManager.GenerateEmailConfirmationTokenAsync(entity);

                    //var mailUtil = new MailUtilities(_configuration);
                    //mailUtil.SendWelcomeEmail(model);

                    return (entity.Map(), true, "Success");

                }
                else
                {
                    return (model, false, "Business User already Exist, Kindly contact administrator");
                }
            }
            catch (Exception ex)
            {
                //await dbTransaction.RollbackAsync();
                return (model, false, ex.Message);

            }
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
