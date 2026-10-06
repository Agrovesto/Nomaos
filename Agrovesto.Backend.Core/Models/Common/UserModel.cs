using Agrovesto.Backend.Core.Models.Businesses;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Common
{
    public class FarmerUploadModel
    {
        [Required(ErrorMessage = "The file field is required.")]
        public IFormFile? file { get; set; }

       
    }
    public class  BulkFarmerUpoad
    {
        public Stream? FileContent { get; set; }
        public IFormFile file { get; set; }
    }
    public class BulkUploadFarmersResponse
    {
        public int TotalRows { get; set; }
        public int Successful { get; set; }
        public int Failed { get; set; }
        public string Message { get; set; }
        public List<BulkUploadError> Errors { get; set; } = new();

        public List<(UserModel user, bool status, string message)> Results { get; set; }
    }

    public class BulkUploadProgress
    {
        public int TotalRows { get; set; }
        public int ProcressedRows { get; set; }
        public int SuccessfulRows { get; set; }
        public int FailedRows { get; set; }
        public string Status { get; set; }
    }
    public class BulkUploadError
    {
        public int RowNumber { get; set; }
        public string Message { get; set; }
    }
    public class ExtractedFarmerRow
    {
        public int RowNumber { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string Gender { get; set; }
        public string NIN { get; set; }
        public DateTime? DOB { get; set; }

        public string CooperativeCode { get; set; }
        public string AgentCode { get; set; }

        public string BVN { get; set; }
        public string Nationality { get; set; }
        public string MaritalStatus { get; set; }

        public decimal? FarmSize { get; set; }
        public int? NoOfPlots { get; set; }
        public string CropType { get; set; }

        public string ValueChainCategory { get; set; }
        public string ValueChainType { get; set; }

        public string Address { get; set; }
        public string LGA { get; set; }
        public string State { get; set; }
    }


    public class SupportingDOcument
    {
        public IFormFile File { get; set; }
    }
    public enum CredentialType
    {
        Password =1,
        Pin =2
    }
    public class ForgotCredentialModel
    {
        public CredentialType Type { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? BusinessCode { get; set; }
        public string? Env { get; set; } = default!;
    }

    public class ResetArtifact
    {
        public string Value { get; set; } = default!; // OTP code, or the reset callback URL
        public DateTime ExpiresAt { get; set; }
    }
    public class ConfirmCredentialResetRequest
    {
        public CredentialType Type { get; set; }

        // Pin path
        public string? PhoneNumber { get; set; }
        public string? Code { get; set; }

        // Password path
        public string? UserId { get; set; }
        public string? Token { get; set; }

        public string NewValue { get; set; } = default!;
        public string ConfirmValue { get; set; } = default!;
    }
    public class ForgotCredentialRequest
    {
        public CredentialType Type { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }
        public class ChangeCredentialRequest
    {
        public CredentialType Type { get; set; }
        public string OldValue { get; set; } = default!;
        public string NewValue { get; set; } = default!;
        public string ConfirmValue { get; set; } = default!;
    }
    public class ChangeCredentialModel
    {
        public CredentialType Type { get; set; }
        public string? Email { get; set; }        // required when Type == Password
        public string? PhoneNumber { get; set; }  // required when Type == Pin
        public string OldValue { get; set; } = default!;
        public string NewValue { get; set; } = default!;
        public string ConfirmValue { get; set; } = default!;
    }

    public class ConfirmCredentialResetModel
    {
        public CredentialType Type { get; set; }

        // Pin path
        public string? PhoneNumber { get; set; }
        public string? Code { get; set; }

        // Password path
        public string? UserId { get; set; }
        public string? Token { get; set; }

        public string NewValue { get; set; } = default!;
        public string ConfirmValue { get; set; } = default!;
    }
    public class ChangePinModel
    {
        public string? PhoneNumber { get; set; }
        public string? UserId { get; set; }
        public string OldPin { get; set; }
        public string NewPin { get; set; }
        [Compare(nameof(NewPin), ErrorMessage = "Confirm PIN must match New PIN.")]
        public string ConfirmPin { get; set; }
    }
        public class ChnagePasswordModel
        {
            public string Email { get; set; }
            public string OldPassword { get; set; }
            public string NewPassword { get; set; }
            [Compare(nameof(NewPassword), ErrorMessage = "Confirm Password must match New Password.")]
            public string ConfirmPassword { get; set; }
        }
      public class ForgotPasswordModel
    {
        public string? Username { get; set; }
        public string? PhoneNumber { get; set; }
    }
    public class PasswordResetModel
    {
        public string Token { get; set; }
        public string? Username { get; set; }
        public string? UserId { get; set; }
        public string Password { get; set; }
    }
    public class UserViewModel
    {
        public DateTimeOffset? LastLoginDate { get; set; }
        public string? Id { get; set; }
        public string PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string UserType { get; set; }
        public DateTimeOffset CreatedOn { get; set; } 
        public string? CreatedBy { get; set; }
        public bool IsActive { get; set; }
        public string? Name { get; set; }
        //public string Location { get; set; }
        //public string State { get; set; }
        //public string Lga { get; set; }
        //public string? Gender { get; set; }
        //public string? DateOfBirth { get; set; }
        public string BusinessCode { get; set; }
        public string? OrganizationCode { get; set; }
    }
    public class ImageComparisonResponse
    {
        public bool IsSimilarFace { get; set; }
        public double SimilarityPercentage { get; set; }
        public string ImageKey { get; set; }
    }

    public class ImageUploadResponse
    {
        public string Message { get; set; }
        public string ImageKey { get; set; }
    }
    public class ImageVerificationArgs
    {
        public string FarmerUserId { get; set; }
        public string BusinessCode { get; set; }
        public string? OrganizationCode { get; set; }
    }
    public class ImageModel
    {
        public IFormFile File { get; set; }
        public string? FarmerUserId { get; set; }
    }
    public class UserModel
    {
      //  public IFormFile? FarmerCapturedImage { get; set; }
        public string? Id { get; set; }
        public string? Salt { get; set; }
        public string PhoneNumber { get; set; }
        public string? Pin { get; set; }
        public string? Password { get; set; }
        public string? Email { get; set; }
        public string? Env { get; set; }
        //public UserType UserType { get; set; }
        public string UserType { get; set; }
        public string? OTP { get; set; }
        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now;
        public string? CreatedBy { get; set; }
        public DateTimeOffset? DateModified { get; set; } = DateTimeOffset.Now;
        public string? ModifiedBy { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset? LastLoginDate { get; set; } = DateTimeOffset.Now;
        public string? FirstName { get; set; }
        public string? Name { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? Location { get; set; }
        public string? State { get; set; }
        public string? Lga { get; set; }
        public string? Ward { get; set; }
        public string? Gender { get; set; }
        public string? DateOfBirth { get; set; }

        public string? Bvn { get; set; }
        public string? NIN { get; set; }
        public string? BankAccount { get; set; }
        public string? BankName { get; set; }
        public string? BusinessCode { get; set; }
        public string? Alias { get; set; }
        public string? OrganizationCode { get; set; }
        public string? FarmerCategory { get; set; }
        public long YearsInAgric { get; set; }
        public string? HighestLevelOfEducation { get; set; }
        public bool shouldVerifyNin { get; set; } = false;

        public List<ValueChainSelectionDto>? ValueChainDto { get; set; }
    }
    public enum UserType
    {
        [Description("User")]
        User,
        [Description("Business")]
        Business,
        [Description("Farmer")]
        Farmer,
        [Description("MemberShip")]
        MemberShip,
        [Description("Agent")]
        Agent,
        [Description("Suppliers")]
        Suppliers,
        [Description("Processors")]
        Processors,
        [Description("Agrovesto")]
        Agrovesto,
        [Description("Buyer")]
        Buyer,
        [Description("Trader")]
        Trader,

        [Description("Organization")]
        Organization,
        [Description("ServiceProvider")]
        ServiceProvider
    }
    public class LoginModel
    {
        public string? Email { get; set; }
        public string? BusinessCode { get; set; }
        public string? Password { get; set; }
        public string? Phonenumber { get; set; }
        public string? Pin { get; set; }
        public LoginType LoginMethod { get; set; }
    }
  
    public class UserObject
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone_number
        { get; set; }
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; }
    }
    public class TokenModel
    {
        public string RefreshToken { get; set; }
        public DateTime RefreshTokenExpiresOn { get; set; }
        public string Token { get; set; }
        public TokenType TokenType { get; set; }
        public string Authmethod { get; set; }
        public DateTimeOffset ExpiresOn { get; set; }
        public string Id { get; set; }
        public string phone_number { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public List<OrganizationDto> Organizations { get; set; }
      //  public List<BusinessClaimModel> Businesses { get; set; }
        public bool TwoFactorEnabled { get; set; }
       // public double ExpiresIn {  get; set; }
        public List<string> Roles { get; set; }
        public string BusinessCode { get; set; }
        public bool RequiresCredentialChange { get; set; }
        public string SecurityNotice { get; set; }
        //  public UserObject User { get; set; }
    }
    public class OrganizationDto
    {
        public string OrganizationCode { get; set; }
        public string OrganizationName { get; set; }
    }

    public enum TokenType
    {
        AccessToken,
        IdToken,
        RefreshToken
            
    }
    public enum LoginType
    {
        Password,
        Pin,
        Otp
    }

    public class PreparedUser
    {
        public UserModel User { get; set; }
        public bool IsValid => string.IsNullOrEmpty(ValidationError);
        public string ValidationError { get; set; }
    }

}
