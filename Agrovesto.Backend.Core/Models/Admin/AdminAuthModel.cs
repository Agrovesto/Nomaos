using Agrovesto.Backend.Core.Models.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Admin
{
    public record AdminWorkInfoDto(string? Department, string? AdminLevel, IReadOnlyList<string> Roles, DateTimeOffset Joined);

    public record AdminProfileDto(
        string UserId, string AdminCode, string FullName, string? ProfileImageUrl,
        string Status, string? Department, string? AdminLevel,
        DateTimeOffset Joined, DateTimeOffset? LastLogin, string? Gender, string? DateOfBirth,
        AdminWorkInfoDto WorkInfo, AdminPersonalInfoDto PersonalInfo);

    public record ProfileImageDto(string ProfileImageUrl);


    public record AdminPersonalInfoDto(
    string? Gender, string? DateOfBirth,
    string? PhoneNumber, 
    //string? PhoneNumberDisplay,
    string Email, string? OfficeAddress, string? State, string? City);

    public class UpdatePersonalInfoRequest
    {
        [Required(ErrorMessage = "Phone number is required.")]
        public string PhoneNumber { get; set; } = default!;

        [Required(ErrorMessage = "Office address is required."), StringLength(300)]
        public string OfficeAddress { get; set; } = default!;

        [Required(ErrorMessage = "Please select a state."), StringLength(100)]
        public string State { get; set; } = default!;

        [Required(ErrorMessage = "Please select a city."), StringLength(100)]
        public string Lga { get; set; } = default!;

        public string? DateOfBirth { get; set; }

        public string? Gender { get; set; }
    }


    public static class PasswordPolicy
    {
        public const int MinLength = 8;

        public static List<string> Validate(string? password)
        {
            var p = password ?? "";
            var unmet = new List<string>();

            if (p.Length < MinLength)
                unmet.Add("Password must be at least 8 characters long.");
            if (!p.Any(char.IsUpper))
                unmet.Add("Password must contain at least one uppercase letter.");
            if (!p.Any(char.IsDigit))
                unmet.Add("Password must contain at least one number.");
            if (!p.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c)))
                unmet.Add("Password must contain at least one special character.");

            return unmet;
        }
    }

    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "Current password is required.")]
        public string CurrentPassword { get; set; } = default!;

        [Required(ErrorMessage = "New password is required.")]
        [StringLength(128, ErrorMessage = "Password must not exceed 128 characters.")]
        public string NewPassword { get; set; } = default!;

        [Required(ErrorMessage = "Please confirm your new password.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmNewPassword { get; set; } = default!;
    }
    internal class AdminAuthModel
    {
    }

    public class AdminLogoutModel
    {
        public bool AllDevices { get; set; }
    }
    public class AdminRefreshTokenModel
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
    public class AdminLoginModel
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(254, ErrorMessage = "Please enter a valid email address.")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(256, ErrorMessage = "Password is too long.")]
        public string? Password { get; set; }

        public LoginType LoginMethod { get; set; } = LoginType.Password;
    }

    // ------------------------------------------------------------------ messages / constants

    public static class AdminMessages
    {
        public const string InvalidCredentials = "Incorrect email or password. Please check your details and try again.";
        public const string AccountInactive = "Your account is inactive. Please contact your administrator.";
        public const string NoAdminAccess = "You do not have permission to access the Admin Panel.";
        public const string Deactivated = "Your account has been deactivated. Please contact your administrator.";
        public const string Suspended = "Your account has been suspended. Please contact your administrator.";
        public const string TooManyAttempts = "Too many failed login attempts. Please try again later.";
        public const string ServerError = "Something went wrong. Please try again.";
    }

    public static class AdminPermissions
    {
        /// <summary>The permission that grants entry to the Admin Panel. Rename to match your permission seed data.</summary>
        public const string AccessPanel = "AdminPanel.Access";

        /// <summary>Claim type the permissions are written to in the JWT.</summary>
        public const string ClaimType = "permission";

        public const string PolicyName = "AdminPanel";
    }

    // ------------------------------------------------------------------ result / response

    public enum AdminLoginStatus
    {
        Success,
        InvalidCredentials, // -> 401
        AccountInactive,    // -> 403
        NoAdminAccess,      // -> 403
        TooManyAttempts     // -> 429
    }

    public class AdminUserProfile
    {
        public string Id { get; set; } = default!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
    }

    public class AdminLoginResponseModel
    {
        //public List<string> Permissions { get; set; } = new();
        public string RefreshToken { get; set; }
        public DateTime RefreshTokenExpiresOn { get; set; }
        public string Token { get; set; }
        public TokenType TokenType { get; set; }
        //public string Authmethod { get; set; }
        public DateTimeOffset ExpiresOn { get; set; }
       // public string Id { get; set; }
       // public string phone_number { get; set; }
       // public string Name { get; set; }
       // public string Email { get; set; }
       // public List<OrganizationDto> Organizations { get; set; }
        //  public List<BusinessClaimModel> Businesses { get; set; }
        public bool TwoFactorEnabled { get; set; }
        // public double ExpiresIn {  get; set; }
       // public List<string> Roles { get; set; }
       // public string BusinessCode { get; set; }
        public bool RequiresCredentialChange { get; set; }
        public string SecurityNotice { get; set; }
        // public TokenModel Tokens { get; set; } = default!;      // your existing TokenModel
        //public AdminUserProfile User { get; set; } = default!;
        //public List<string> Roles { get; set; } = [];
        //public List<string> Permissions { get; set; } = [];
        //public bool RequiresCredentialChange { get; set; }
        //public string? CredentialMessage { get; set; }
    }

    public record AdminLoginResult(AdminLoginStatus Status, string Message, AdminLoginResponseModel? Data = null)
    {
        public static AdminLoginResult Ok(AdminLoginResponseModel data) =>
            new(AdminLoginStatus.Success, "Successful", data);

        public static AdminLoginResult Fail(AdminLoginStatus status, string message) =>
            new(status, message);
    }

    public record AdminAccess(bool HasAccess, List<string> Roles, List<string> Permissions);


    public class InviteAdminUserRequest
    {
        [Required(ErrorMessage = "First name is required."), StringLength(100)]
        public string FirstName { get; set; } = default!;

        [Required(ErrorMessage = "Last name is required."), StringLength(100)]
        public string LastName { get; set; } = default!;

        [Required(ErrorMessage = "Phone number is required.")]
        public string PhoneNumber { get; set; } = default!;

        [Required(ErrorMessage = "Email address is required.")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = default!;

        [Required(ErrorMessage = "Please select a role.")]
        public string RoleId { get; set; } = default!;
    }

    public record InviteAdminUserResponse(string UserId, string AdminCode, string Status, bool EmailSent, DateTimeOffset ExpiresAt);
    public record AdminRoleOptionDto(string Id, string Name, string? Description);
    public record InviteResult(InviteAdminUserResponse? Data, string? Error, int StatusCode);


    public class AcceptInviteRequest
    {
        [Required(ErrorMessage = "Invitation token is required.")]
        public string Token { get; set; } = default!;

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = default!;
    }

    public record AcceptResult(bool Success, string? Error, int StatusCode);

    public record AdminUserStatusDto(string UserId, string Status);
}
