using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.V2.Onboarding
{
   


    public class UsersProfileResponse
    {
        public string UserId { get; set; }
        public string UserType { get; set; }

        // Common
        public string Email { get; set; }
        public string Phone { get; set; }


        // Farmer
        public FarmerProfileDto Farmer { get; set; }

        // Agent
        public AgentModel Agent { get; set; }

        // Cooperative
      public MemberShipModel Membership { get; set; }
    }
  
    public class SimpleAgentDto
    {
        public long AgentId { get; set; }
        public string Name { get; set; }
    }
    public class SimpleCooperativeDto
    {
        public long CooperativeId { get; set; }
        public string Name { get; set; }
    }
    public class UserProfileResponse
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string FullName { get; set; }
        public string FirstName { get; set; }   
        public string LastName { get; set; }
        public string Level { get; set; }
    }
    public class BulkOnboardingExcelRow
    {
        // Columns A..R
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string StateOfResidence { get; set; }
        public string LGA { get; set; }
        public string Ward { get; set; }
        public string ResidentialAddress { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string CategoryOfFarmer { get; set; }
        public string PartOfAgriculture { get; set; }
        public string ProductTypes { get; set; }
        public string YearsInAgric { get; set; }
        public string HighestLevelOfEducation { get; set; }
        public string BVN { get; set; }
        public string NIN { get; set; }
    }
    public class BulkOnboardingResult
    {
        public int Total { get; set; }
        public int Successful { get; set; }
        public int Failed { get; set; }

        public List<BulkOnboardingError> Errors { get; set; } = new();
    }

    public class BulkOnboardingError
    {
        public int RowNumber { get; set; }
        public string Email { get; set; }
        public string Error { get; set; }
    }

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
    public class CreateAgentResponseModel
    {
        public string UserId { get; set; }
        public string AgentUserId { get; set; }
        public long AgentId { get; set; }
        public bool RequiresProfileCompletion { get; set; }
    }
    public class CreateMembershipResponseModel
    {
        public string UserId { get; set; }
        public long MembershipId { get; set; }
        public bool RequiresProfileCompletion { get; set; }
    }
    public class OnboardingResponseModel
    {
        public string? Id { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Message { get; set; }
    }
    public class OnboardingRequestModel
    {
        public string? LogDescription { get; set; }
        public string? ActivityName { get; set; }
        public string? Id { get; set; }
        public string? PhoneNumber { get; set; }
        public string? LastName { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? Pin { get; set; }
        public string? Password { get; set; }
        public string? Email { get; set; }
    }
    public enum OnboardingStatus
    {
        Pending,
        ProfileCompleted,
        SecuritySetup,
        Completed
    }
    public class RefreshToken
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; }
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; }
        public string DeviceId { get; set; }
    }

    public class LoginRequestModel
    {
        public string Identifier { get; set; } // username | email | phone
        public string Password { get; set; }
        public string Pin { get; set; }
        public LoginType LoginMethod { get; set; }
    }
    public class ProfileImageModel
    {
        public IFormFile File { get; set; }
    }
    public class ProfileRequestModel 
    {
        public bool IsLeaderAFarmer { get; set; } = false;
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
        public string? CooperativeName { get; set; }
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
        public string? Password { get; set; }
        public string? Pin { get; set; }
        public string? FarmerCategory { get; set; }
        public long YearsInAgric { get; set; }
        public long? MembershipTypeId { get; set; }
        public string? HighestLevelOfEducation { get; set; }
        public long? OrganizationId { get; set; }
        public List<ValueChainSelectionDto>? ValueChainDto { get; set; }
        public string? organizationCode { get; set; }
    }
    public class BulkOnboardingJobArgs
    {
        public long Id { get; set; }
        public string userType { get; set; }
        public string? UserId { get; set; }
        public string? OnboardedByUserId { get; set; }
        public string businessCode { get; set; }
        public string? organizationCode { get; set; }
        public List<long?>? CooperativeIds { get; set; }
        public List<long?>? AgentIds { get; set; }
        public List<BulkOnboardingExcelRow> Rows { get; set; }
        public bool DisableOtp { get; set; }
        public bool IsDryRun { get; set; }
    }
    public enum OnboardingTypes
    {
        Farmer,
        MemberShip,
        Agent,
        Organization,
        ServiceProvider
    }
    public class KYCVerificationDto
    {
        public string Number { get; set; }
      
    }
    public class VerificationArgs
    {
        public string? UserId { get; set; }
        [Required]
        public string Env { get; set; }
        public string businessCode { get; set; }
    }
    public class OnBoardingArgs
    {
        public long Id { get; set; }
    public string? Env { get; set; }
        public string userType { get; set; }
        public string? UserId { get; set; }
        public string? OnboardedByUserId { get; set; }
        public string businessCode { get; set; }
        public string? organizationCode { get; set; }
        public List<long?>? CooperativeIds { get; set; }
        public List<long?>? AgentIds { get; set; }

        //public string? env { get; set; }
    }
    public class Params
    {
        public long Id { get; set; }
        public string? UserId { get; set; }

        public string businessCode { get; set; }
        public string? organizationCode { get; set; }
    }
    public class AssignmentArgs
    {
        public long Id { get; set; }

        public string? farmerUserId { get; set; }
        public string? agentUserId { get; set; }
        public string? UserId { get; set; }
        public string? OnboardedByUserId { get; set; }
        public string businessCode { get; set; }
        public string? organizationCode { get; set; }
        public List<long?>? CooperativeIds { get; set; }
        public List<long?>? AgentIds { get; set; }

        //public string? env { get; set; }
    }

}
