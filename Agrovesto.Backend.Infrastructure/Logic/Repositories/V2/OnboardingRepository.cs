using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public async Task ConfirmUserEmailOrPassword(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            user.IsActive = true;
            user.EmailConfirmed = true;

            await _userManager.UpdateAsync(user);
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
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Salt = Convert.ToBase64String(salt)
            };
            entity.Pin = model.Pin;
            entity.UserLevel = UserLevel.Level1;
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

    }
}
