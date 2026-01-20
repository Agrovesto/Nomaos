using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.V2.Onboarding
{
    public class GenerateOtpRequest
    {
        public string UserId { get; set; }
        public OtpTarget Target { get; set; }
        public string Destination { get; set; }
        public OtpPurpose Purpose { get; set; }
        public int ExpiryMinutes { get; set; }
    }

    public class OtpResult
    {
        public string Code { get; set; }
        public string Destination { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
    public class ConfirmOtpRequest
    {
        [Required]
        public string UserId { get; set; }
        [Required]
        public string Code { get; set; }
        public OtpPurpose Purpose { get; set; }
    }

    public class ResendOtpRequest
    {
        [Required]
        public string UserId { get; set; }
        [Required]
        public OtpTarget OtpTarget { get; set; }
        public OtpPurpose Purpose { get; set; }
    }
    public enum OtpTarget
    {
        Email,
        Phone
    }

    public enum OtpPurpose
    {
        Onboarding,
        Login,
        PasswordReset
    }

    public class OnboardingRequestModel
    {
        public string? Id { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Pin { get; set; }
        public string? Password { get; set; }
        public string? Email { get; set; }
    }

    public class ProfileRequestModel 
    {
        public string? OnboardedByUserId { get; set; }
        //  public IFormFile? FarmerCapturedImage { get; set; }
        public string? Id { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public DateTimeOffset CreatedOn { get; set; } 
        public string? CreatedBy { get; set; }
        public DateTimeOffset? DateModified { get; set; } = DateTimeOffset.Now;
        public string? ModifiedBy { get; set; }
        public string? FirstName { get; set; }
        public string? FullName { get; set; }
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
        public string? FarmerCategory { get; set; }
        public long YearsInAgric { get; set; }
        public string? HighestLevelOfEducation { get; set; }

        public List<ValueChainSelectionDto>? ValueChainDto { get; set; }
    }

    public class OnBoardingArgs
    {
        public long Id { get; set; }
        public string userType { get; set; }
        public string? UserId { get; set; }
        public string businessCode { get; set; }
        public string? organizationCode { get; set; }
        //public string? env { get; set; }
    }
  
}
