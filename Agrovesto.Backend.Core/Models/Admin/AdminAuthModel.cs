using Agrovesto.Backend.Core.Models.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Admin
{
    internal class AdminAuthModel
    {
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
        NoAdminAccess       // -> 403
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

}
