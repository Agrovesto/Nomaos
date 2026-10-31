using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2
{
    public class PasswordChangeHandler : ICredentialChangeHandler
    {
        private readonly UserManager<User> _userManager;
        private readonly DataContext _context;
        private readonly IAWSService _awsService;
        private readonly IConfiguration _configuration;
        private readonly PasswordOptions _passwordOptions;

        public PasswordChangeHandler(UserManager<User> userManager, DataContext context,
             IAWSService awsService, IConfiguration configuration,

        IOptions<IdentityOptions> identityOptions)
        {
            _userManager = userManager;
            _context = context;
            _passwordOptions = identityOptions.Value.Password;

            _awsService = awsService;
            _configuration = configuration;
        }
        public CredentialType Type => CredentialType.Password;
        public string RequiredFieldMissingMessage => "Your current password is required.";

        public async Task<(User? user, string message)> FindUserAsync(ChangeCredentialModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
                throw new BaseException(ExceptionType.OPERATION_FAILED,
                    "Cannot change password for an account with no email address, kindly update your profile");

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                 user = await _userManager.FindByIdAsync(model.Email);
            if (user == null)
            {
                return (null, "Reset password link has been sent to your registered email");
            
            }
            return (user, "Reset password link has been sent to your registered email");
        }

        public void ValidateNewValueFormat(string newValue)
        {
            if (newValue.Length < _passwordOptions.RequiredLength)
                throw new BaseException(ExceptionType.BAD_REQUEST,
                    $"Password must be at least {_passwordOptions.RequiredLength} characters long.");

            if (_passwordOptions.RequireDigit && !newValue.Any(char.IsDigit))
                throw new BaseException(ExceptionType.BAD_REQUEST, "Password must contain at least one digit.");

            if (_passwordOptions.RequireLowercase && !newValue.Any(char.IsLower))
                throw new BaseException(ExceptionType.BAD_REQUEST, "Password must contain at least one lowercase letter.");

            if (_passwordOptions.RequireUppercase && !newValue.Any(char.IsUpper))
                throw new BaseException(ExceptionType.BAD_REQUEST, "Password must contain at least one uppercase letter.");

            if (_passwordOptions.RequireNonAlphanumeric && newValue.All(char.IsLetterOrDigit))
                throw new BaseException(ExceptionType.BAD_REQUEST, "Password must contain at least one special character.");

            if (_passwordOptions.RequiredUniqueChars > 1)
            {
                var uniqueCharCount = newValue.Distinct().Count();
                if (uniqueCharCount < _passwordOptions.RequiredUniqueChars)
                    throw new BaseException(ExceptionType.BAD_REQUEST,
                        $"Password must contain at least {_passwordOptions.RequiredUniqueChars} unique characters.");
            }
        }
        public async Task ChangeAsync(User user, string oldValue, string newValue)
        {
            var result = await _userManager.ChangePasswordAsync(user, oldValue, newValue);

            if (!result.Succeeded)
            {
                var reason = string.Join(" ", result.Errors.Select(x => x.Description));
                throw new BaseException(
                    ExceptionType.BAD_REQUEST,
                    string.IsNullOrWhiteSpace(reason) ? "The current password you entered is incorrect." : reason);
            }

            user.HasDefaultPassword = false;
            await _context.SaveChangesAsync();
        }

        public async Task<ResetArtifact> GenerateArtifactAsync(User user, ForgotCredentialModel model)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var callbackUrl = $"{_configuration["ResetPasswordLink"]}?t={encodedToken}&u={user.Id}";

            return new ResetArtifact { Value = callbackUrl, ExpiresAt = DateTime.UtcNow.AddHours(1) }; // adjust to your actual token lifetime
        }

        public async Task DeliverLiveAsync(User user, ResetArtifact artifact, ForgotCredentialModel model)
        {
            var message = $"Please have your password reset by <a href='{artifact.Value}'>clicking here</a>.";

            await _awsService.SendEmailAsync(
                _configuration["Official:Email"],
                user.Email,
                "Password Reset Request",
                message,
                message);
        }
        public string BuildNonLiveMessage(ResetArtifact artifact)
        => $"Your reset link: {artifact.Value}";
        public async Task ConfirmAsync(User user, ConfirmCredentialResetModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Token))
                throw new BaseException(ExceptionType.BAD_REQUEST, "Invalid or expired reset link.");

            string decodedToken;
            try
            {
                decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));
            }
            catch (FormatException)
            {
                throw new BaseException(ExceptionType.BAD_REQUEST, "Invalid or expired reset link.");
            }

            var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.NewValue);
            if (!result.Succeeded)
            {
                var reason = string.Join(" ", result.Errors.Select(e => e.Description));
                throw new BaseException(ExceptionType.BAD_REQUEST,
                    string.IsNullOrWhiteSpace(reason) ? "Invalid or expired reset link." : reason);
            }

            user.Password = model.NewValue;
            _context.SaveChangesAsync();
        }

    }
}
