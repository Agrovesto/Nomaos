using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Models.Businesses;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Admin
{
    public class FarmerWalletSummaryDto
    {
        public decimal TotalWalletBalance { get; set; }
        public decimal PendingSettlement { get; set; }
        public decimal TotalInflow { get; set; }
        public decimal TotalOutflow { get; set; }
    }

    public class FarmerWalletModule
    {
        public string UserId { get; set; }
        public long WalletId { get; set; }
        public string WalletName { get; set; }
        public decimal CurrentBalance { get; set; }
        public long WalletProviderId { get; set; }
        public DateTimeOffset DateAdded { get; set; }
    }

    public class FarmerListQuery
    {
        public long BusinessId { get; set; }
        public string? Search { get; set; }              // name, ID (FMR-1712 or 1712) or phone
        public FarmerStatus? Status { get; set; }
        public string? OrganizationCode { get; set; }
        public DateTimeOffset? From { get; set; }
        public DateTimeOffset? To { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? BusinessCode { get; set; }
        public string? UserId { get; set; }
    }

    public class FarmerSummaryDto
    {
        public int TotalFarmers { get; set; }
        public int VerifiedFarmers { get; set; }
        public double VerifiedPercent { get; set; }
        public int PendingVerification { get; set; }
        public double PendingPercent { get; set; }
        public int SuspendedFarmers { get; set; }
    }

    public class FarmerListItemDto
    {
        public long Id { get; set; }
        public string FarmerId { get; set; } = default!;   // FMR-1712
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTimeOffset RegDate { get; set; }
        public string Status { get; set; } = default!;
        public int Farms { get; set; }
        public string? AssignedAgent { get; set; }
        public string? Organization { get; set; }
        public string? OrganizationCode { get; set; }
        public string? AgentUserId { get; set; }
    }

    public class FarmerDetailsDto
    {
        // Header
        public long Id { get; set; }
        public string FarmerId { get; set; } = default!;      // FMR-17263
        public string? FullName { get; set; }
        public string Status { get; set; } = default!;
        public string? Organization { get; set; }
        public DateTimeOffset RegistrationDate { get; set; }
        public bool CanBeSuspended { get; set; }                  // false when already suspended

        // Overview tab
        public FarmerPersonalInfoDto Personal { get; set; } = default!;
        public FarmerRegistrationInfoDto Registration { get; set; } = default!;
    }

    public class FarmerPersonalInfoDto
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }              // +234 803 456 7821
        public string? Gender { get; set; }
        public string? DateOfBirth { get; set; }              // 1st December, 1998
        public string? NIN { get; set; }
        public string? Address { get; set; }
        public string? State { get; set; }
        public string? Lga { get; set; }
        public string? Ward { get; set; }
    }

    public class FarmerRegistrationInfoDto
    {
        public DateTimeOffset RegistrationDate { get; set; }
        public string RegistrationChannel { get; set; } = default!;   // Agent Assisted / Self Registration
        public string? RegistrationSource { get; set; }               // Musa Ibrahim
        public string Status { get; set; } = default!;
        public DateTimeOffset? LastUpdated { get; set; }
    }

    public class SuspendFarmerRequest
    {
        public long BusinessId { get; set; }
        public string? Reason { get; set; } 
    }
    public class FarmSummaryDto
    {
        public int TotalFarms { get; set; }
        public double TotalLandSizeHectares { get; set; }   // 27.6, and the UI adds "Hectares"
        public int VerifiedFarms { get; set; }
        public List<string> PrimaryValueChains { get; set; } = new();  // ["Rice", "Maize", "Soybean"]
    }

    public class FarmListItemDto
    {
        public long FarmId { get; set; } = default!;       // FM001204
        public string? FarmName { get; set; }
        public string? State { get; set; }
        public string? Lga { get; set; }
        public double SizeInHectares { get; set; }          // 8.5, and the UI adds "Ha"
        public string? PrimaryCrop { get; set; }
        public string Status { get; set; } = default!;       // Verified
    }

    public class FarmerOrgMembershipDto
    {
        public List<LinkedOrganizationDto> Organizations { get; set; } = new();
        public List<AssignedAgentDto> Agents { get; set; } = new();
        public List<MembershipBodyDto> Memberships { get; set; } = new();
    }

    public class LinkedOrganizationDto
    {
        public string Organization { get; set; } = default!;     // Kaduna Rice Farmers Cooperative
        public string OrganizationCode { get; set; } = default!;   // ORG000143
        public DateTimeOffset? JoinedOn { get; set; }
        public string Status { get; set; } = default!;           // Active / Inactive
    }

    public class AssignedAgentDto
    {
        public string AgentName { get; set; } = default!;
        public string AgentId { get; set; } = default!;          // AGT00142
        public string? PhoneNumber { get; set; }                       // +234 802 543 2198
        public string? Organization { get; set; }                // which org this assignment is under
        public string Status { get; set; } = default!;
    }

    public class MembershipBodyDto
    {
        public long MembershipId { get; set; }
        public string MembershipUserId { get; set; }
        public string Membership { get; set; } = default!;
       // public string? Role { get; set; }                        // Member
        public DateTimeOffset? Joined { get; set; }
        public string Status { get; set; } = default!;           // Verified
    }

    public class InputFinanceSummaryDto
    {
        public int TotalPackages { get; set; }
        public int ActivePackages { get; set; }
        public int TotalDisbursed { get; set; }
        public int PendingDisbursement { get; set; }
        public decimal TotalFinancingValue { get; set; }  
        // ₦, formatted on the frontend
        public decimal OutstandingBalance { get; set; }  // ₦
    }

    public class InputFinancePackageDto
    {
        //public int SerialNo { get; set; }
       // public long Id { get; set; }
        public long PackageId { get; set; } = default!;
        public string PackageName { get; set; } = default!;
        public string SubscriptionType { get; set; } = default!;
        public string? Season { get; set; }
        public decimal TotalFinancingValue { get; set; }
        public decimal TotalDisbursedValue { get; set; }
        public decimal TotalOutstandingValue { get; set; }
        public string Status { get; set; } = default!;   // Active / Completed / Pending
        public DateTimeOffset? ApprovedDate { get; set; }
    }

    public class AdminOnBoardingArgs
    {

        public string? Env { get; set; }
        public string userType { get; set; }
        public string? OnboardedByUserId { get; set; }
        public string? businessCode { get; set; }

    }

    public class AdminCreateFarmerRequest
    {
        public string? Pin { get; set; }
        public string? Password { get; set; }
        public string? Bvn { get; set; }
        public string? NIN { get; set; }
        [MaxLength(15, ErrorMessage = "FirstName cannot exceed 15 characters.")]
        public string? FirstName { get; set; }
        [MaxLength(15, ErrorMessage = "MiddleName cannot exceed 15 characters.")]
        public string? MiddleName { get; set; }
        [MaxLength(15, ErrorMessage = "LastName cannot exceed 15 characters.")]
        public string? LastName { get; set; }
        public string? Gender { get; set; }
        public string? DateOfBirth { get; set; }
        public SignupMethod SignupMethod { get; set; } = SignupMethod.PhoneNumber;
        [MaxLength(11, ErrorMessage = "Phone number cannot exceed 11 characters.")]
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Location { get; set; }
        public string? State { get; set; }
        public string? Lga { get; set; }
        public string? Ward { get; set; }
        public string? FarmerCategory { get; set; }
        public List<ValueChainSelectionDto>? ValueChain { get; set; }

        public long YearsInAgric { get; set; }

        public string? HighestLevelOfEducation { get; set; }
        public List<long>? AgentIds { get; set; }
        public List<string>? OrganizationCode { get; set; }

        public List<long>? CooperativeIds { get; set; }
    }
}
