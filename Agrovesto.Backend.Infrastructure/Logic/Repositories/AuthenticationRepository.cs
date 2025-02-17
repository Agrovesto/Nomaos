using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories
{
    public class AuthenticationRepository : IAuthenticationRepository
    {
        private readonly DataContext _context;
        private readonly RoleManager<Role> _roleManager;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;

        private readonly SignInManager<User> _signInManager;
        public AuthenticationRepository(IConfiguration config, RoleManager<Role> roleManager, UserManager<User> userManager,
           SignInManager<User> signInManager, DataContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context; _signInManager = signInManager;
            _configuration = config;
        }
        public async Task<(bool Status, string Message, TokenModel response)> Login(LoginModel model )
        {
            User user = new User();
            try
            {
                IList<string> userRole = new List<string>();
                if (model.LoginMethod == LoginType.Password)
                {
                    user = await _userManager.FindByNameAsync(model.Username);
                    if (user == null)
                    {
                        await _userManager.AccessFailedAsync(user);
                        return (false, "Invalid username or password", null); }
                    var checkPassword = await _userManager.CheckPasswordAsync(user, model.password);
                    if (!checkPassword) { await _userManager.AccessFailedAsync(user); 
                        return (false, "Invalid username or password", null); }

                }
                else
                {
                    user = await FindByPhoneNumberAsync(model.phonenumber);
                    if (string.IsNullOrEmpty(user.Salt))
                    {
                        return (false, "Could not login user. Contact administrator", null);

                    }
                    byte[] salt = Convert.FromBase64String(user.Salt);
                    string transPin = model.pin.HashSecret(salt);
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
                new Claim("Email", user.Email),
                new Claim("LastLogin", user.LastLoginDate.ToString()),
                new Claim("MemberSince", user.CreatedOn.ToString()),



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
                    ExpiresOn = DateTime.Now.AddDays(1),
                    Roles = userRole.ToList(),
                    Token= token,
                    TokenType = TokenType.AccessToken,
                };
                return (true, "Successful", response);
            }
            catch(Exception ex)
            {
                await _userManager.AccessFailedAsync(user);
                return (false, ex.Message, null);
            }
         
        
        }
        public async Task<bool> ValidateTransactionPin(User user, string transactionPin)
        {

            byte[] salt = Convert.FromBase64String(user.Salt);
            string pin = transactionPin.HashSecret(salt);

            if (!user.PinEncrypt.Equals(transactionPin))
            {
                return false;
            }
            else
            {
                return true;
            }

        }
        private async Task<User> FindByPhoneNumberAsync(string phoneNumber)
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
            try
            {
                var fetchuser = await _userManager.FindByNameAsync(model.Email);
                byte[] salt = EncryptionUtil.GetSalt();
                model.Salt = Convert.ToBase64String(salt);
                User entity = model.Map();
                entity.PinEncrypt = model.Pin.EncryptTransactionPin(salt);
                entity.IsActive = true;
                if (fetchuser == null)
                {
                    var getuserPhone = await _context.Users.Where(x => x.PhoneNumber == model.PhoneNumber).FirstOrDefaultAsync();
                    if (getuserPhone != null)
                    {
                        return (model, false, "A user with the same phone number exists on the system");
                    }
                    entity.Id = Guid.NewGuid().ToString();
                    entity.UserType = Enum.GetName(typeof(UserType), model.UserType);
                    var createUser = await _userManager.CreateAsync(entity, model.Password);
                    if (!createUser.Succeeded) throw new Exception("Could not create user");
                    entity = await _userManager.FindByNameAsync(entity.Email);

                    var uniqueRole = await _roleManager.FindByNameAsync(roleName);
                    createUser = await _userManager.AddToRoleAsync(entity, uniqueRole.Name);

                    // var token = await _userManager.GenerateEmailConfirmationTokenAsync(entity);

                    //var mailUtil = new MailUtilities(_configuration);
                    //mailUtil.SendWelcomeEmail(model);

                    return (entity.Map(), true, "Success");

                }
                else
                {
                    return (model, false, "Admin User already Exist, Kindly contact administrator");
                }
            }
            catch (Exception ex)
            {
                return (model, false, ex.Message);

            }
        }

        public async Task<(UserModel model, bool status, string message)> RegisterBusinessUser(UserModel model, string roleName)
        {
            IdentityResult createUser = new IdentityResult();
            try
            {
                model.Email = !string.IsNullOrEmpty(model.Email) ? model.Email : model.PhoneNumber;
                var fetchuser = await _userManager.FindByNameAsync(model.Email);
                byte[] salt = EncryptionUtil.GetSalt();
                model.Salt = Convert.ToBase64String(salt);
                User entity = model.Map();
                
                entity.PinEncrypt = string.IsNullOrEmpty(model.Pin) ? null : model.Pin.EncryptTransactionPin(salt);
                entity.IsActive = true;
                if (fetchuser == null)
                {
                    var getuserPhone = await _context.Users.Where(x=>x.PhoneNumber== model.PhoneNumber).FirstOrDefaultAsync();
                    if (getuserPhone != null)
                    {
                        return (model, false, "A user with the same phone number exists on the system");
                    }
                    entity.Id = Guid.NewGuid().ToString();
                    entity.UserType = Enum.GetName(typeof(UserType), model.UserType);
                    
                    if (string.IsNullOrEmpty(model.Password))
                    {
                        createUser = await _userManager.CreateAsync(entity);
                    }
                    else
                    {
                        createUser = await _userManager.CreateAsync(entity, model.Password); //create passwordless
                    }
                    
                    if (!createUser.Succeeded) throw new Exception($"Could not create account for {model.Name} {entity.UserType}");
                    entity = await _userManager.FindByNameAsync(entity.Email);

                    var uniqueRole = await _roleManager.FindByNameAsync(roleName);
                    createUser = await _userManager.AddToRoleAsync(entity, uniqueRole.Name);

                    // var token = await _userManager.GenerateEmailConfirmationTokenAsync(entity);

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
                return (model, false, ex.Message);

            }
        }
   
        public async Task<(UserModel model, bool status, string message)> GetUserById(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if(user == null) return (null, false, "No record found");
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

                var mailUtil =  SendGridService.SendDynamicForgotPasswordMail(mailRequest);
                
                return (true, encodedToken);
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


    }
}
