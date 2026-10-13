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
        private readonly TimeSpan _tokenLifespan;
        public PasswordChangeHandler(UserManager<User> userManager, DataContext context,
             IAWSService awsService, IConfiguration configuration,

        IOptions<IdentityOptions> identityOptions, IOptions<DataProtectionTokenProviderOptions> tokenOptions)
        {
            _tokenLifespan = tokenOptions.Value.TokenLifespan;
            _userManager = userManager;
            _context = context;
            _passwordOptions = identityOptions.Value.Password;

            _awsService = awsService;
            _configuration = configuration;
        }
        public CredentialType Type => CredentialType.Password;
        public string RequiredFieldMissingMessage => "Your current password is required.";

        public async Task<User> FindUserAsync(ChangeCredentialModel model)
        {
            var identifier = model.Email?.Trim();

            if (string.IsNullOrWhiteSpace(identifier))
                throw new BaseException(ExceptionType.BAD_REQUEST,
                    "Email or user ID is required.");

            User user = null;

            if (identifier.Contains("@"))
                user = await _userManager.FindByEmailAsync(identifier);

            if (user == null)
            {
                try
                {
                    user = await _userManager.FindByIdAsync(identifier);
                }
                catch (FormatException)
                {
                    // identifier isn't a valid key format (e.g. not a Guid/int), so treat as not found
                }
            }

            if (user == null)
                throw new BaseException(ExceptionType.BAD_REQUEST,
                    "No account was found for the details you entered.");

            if (string.IsNullOrWhiteSpace(user.Email))
                throw new BaseException(ExceptionType.OPERATION_FAILED,
                    "Cannot change password for an account with no email address, kindly update your profile");

            return user;
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

            return new ResetArtifact { Value = callbackUrl, ExpiresAt = DateTime.UtcNow.AddMinutes(_tokenLifespan.TotalMinutes) }; // adjust to your actual token lifetime
        }

        public async Task DeliverLiveAsync(User user, ResetArtifact artifact, ForgotCredentialModel model)
        {
            var message = $"Please have your password reset by <a href='{artifact.Value}'>clicking here</a>. Expires in {FormatDuration(_tokenLifespan)}.";

            await _awsService.SendEmailAsync(
                _configuration["Official:Email"],
                user.Email,
                "Password Reset Request",
                message,
                message);
        }
        public string BuildNonLiveMessage(ResetArtifact artifact)
            => $"Your reset link: {artifact.Value}. Expires in {FormatDuration(_tokenLifespan)}.";

        private static string FormatDuration(TimeSpan span)
        {
            if (span.TotalDays >= 1 && span.TotalDays % 1 == 0)
                return $"{(int)span.TotalDays} day{((int)span.TotalDays == 1 ? "" : "s")}";
            if (span.TotalHours >= 1 && span.TotalHours % 1 == 0)
                return $"{(int)span.TotalHours} hour{((int)span.TotalHours == 1 ? "" : "s")}";
            var minutes = (int)Math.Ceiling(span.TotalMinutes);
            return $"{minutes} minute{(minutes == 1 ? "" : "s")}";
        }
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
