using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Admin
{
    public class AdminCreateAgentRequest
    {
        public string? Pin { get; set; }
        public string? Password { get; set; }
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
        [Required(ErrorMessage = "Location is required.")]
        public string Location { get; set; }
        [Required(ErrorMessage = "State is required.")]
        public string State { get; set; }
        [Required(ErrorMessage = "Lga is required.")]
        public string Lga { get; set; }
        [Required(ErrorMessage = "Ward is required.")]  
        public string Ward { get; set; }
      
    }

    public class AgentSummaryDto
    {
        public int TotalAgents { get; set; }
        public int TotalActiveAgents { get; set; }
        public int TotalPendingVerificationAgents { get; set; }
        public int TotalSuspendedAgents { get; set; }
    }
    public class AgentListItemDto
    {
        public long Id { get; set; }
        public string AgentId { get; set; } = default!;   // FMR-1712
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTimeOffset RegDate { get; set; }
        public string Status { get; set; } = default!;
   public int Organizations { get; set; }
        public int Farmers { get; set; }
    }

    public class AgentListQuery
    {
        public long BusinessId { get; set; }
        public string? Search { get; set; }              // name, ID (FMR-1712 or 1712) or phone
        public AgentStatus? Status { get; set; }
        public string? OrganizationCode { get; set; }
        public DateTimeOffset? From { get; set; }
        public DateTimeOffset? To { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class AgentFarmersSummaryDto
    {
        public int TotalFarmers { get; set; }
        public int VerifiedFarmers { get; set; }
        public int PendingVerification { get; set; }
        public int SuspendedFarmers { get; set; }
    }

    public class AgentFarmerItemDto
    {
        public string FarmerId { get; set; }      // e.g. FRM001245 (the display code)
        public string FarmerName { get; set; }
        public string Phone { get; set; }
        public int Farms { get; set; }
        public DateTimeOffset RegDate { get; set; }
        public string State { get; set; }
        public string Status { get; set; }
    }

    public class AgentFarmersResponse
    {
        public AgentFarmersSummaryDto Summary { get; set; }
        public PagedResult<AgentFarmerItemDto> Farmers { get; set; }
    }


    public class AgentMembershipItemDto
    {
        public long MembershipId { get; set; }        // for the "View Details" link
        public string RepresentativeId { get; set; }
        public string Name { get; set; }
        //public string MembershipCode { get; set; }   // e.g. MRM00078
        public string Type { get; set; }             // Cooperation / Association / Group
        public string Level { get; set; }            // Tier 1 / Tier 2
        public string Status { get; set; }
        public int Farmers { get; set; }
        public DateTimeOffset DateRegistered { get; set; }
    }

    public class AgentMembershipsResponse
    {
        public int TotalLinked { get; set; }         // "Linked Membership (4)"
        public List<AgentMembershipItemDto> Memberships { get; set; }
    }

    public class AgentOrganizationPerformanceDto
    {
        public int FarmersOnboarded { get; set; }
        public decimal VerificationSuccessRate { get; set; }   // %
        public decimal? AverageApprovalTimeHours { get; set; } // null if nothing approved yet
        public decimal DataQualityScore { get; set; }          // %
        public decimal GpsAccuracy { get; set; }               // %
        public int DuplicateRecords { get; set; }
    }

    public class AgentOrganizationItemDto
    {
        public long OrganizationId { get; set; }          // for the View Details link
        public string OrganizationCode { get; set; }     // ORG000143
        public string Name { get; set; }
        public DateTimeOffset AssignmentDate { get; set; }
        public string Region { get; set; }
        public int CoverageAreaCount { get; set; }
        //public string CoverageArea { get; set; }         // "4 LGAs"
        public AgentOrganizationPerformanceDto Performance { get; set; }
    }

    public class AgentOrganizationsResponse
    {
        public int TotalLinked { get; set; }             // "Linked Organization (4)"
      //  public string Label { get; set; }                // "Linked Organization (4)"
       // public bool IsEmpty { get; set; }                // drives the empty state
        public List<AgentOrganizationItemDto> Organizations { get; set; }
    }

    public class InputFinancePackage
    {
        public int Id { get; set; }
        public string PackageName { get; set; }          // "Rice Input Package"
        public string AgentUserId { get; set; }          // the id the client already sends
        public string OrganizationName { get; set; }     // denormalised copy, or look up via an API
        public decimal Value { get; set; }               // package value in naira
        public SubscriptionStatus Status { get; set; }        // Pending, Active, Completed
        public DateTimeOffset? ApprovedOn { get; set; }
    }

    public class PackageFarmer                            // farmers who received a package
    {
        public int PackageId { get; set; }
        public string FarmerUserId { get; set; }
    }

    public class AgentInputFinanceSummaryDto
    {
        public int PackagesFacilitated { get; set; }
        public int FarmersFinanced { get; set; }
        public decimal TotalValueAllocated { get; set; }
        public string TotalValueFormatted { get; set; }    // "₦186M"
        public int ActivePackages { get; set; }
    }

    public class AgentInputFinancePackageDto
    {
        public string Package { get; set; }
        public int Farmers { get; set; }
        public string Organization { get; set; }
        public decimal PackageValue { get; set; }
        public string PackageValueFormatted { get; set; }  // "₦450K"
        public string Status { get; set; }                 // Active / Completed / Pending
        public DateTimeOffset? ApprovedDate { get; set; }
    }

    public class AgentInputFinanceResponse
    {
        public AgentInputFinanceSummaryDto Summary { get; set; }
        public PagedResult<AgentInputFinancePackageDto> Packages { get; set; }
       // public bool IsEmpty { get; set; }                   // drives the empty state
    }


    public class AgentWalletSummaryDto
    {
        public decimal TotalWalletBalance { get; set; }
        public decimal PendingSettlement { get; set; }
        public decimal TotalInflow { get; set; }
        public decimal TotalOutflows { get; set; }
    }

    public class AgentWalletItemDto
    {
        public long WalletId { get; set; }
        public string WalletName { get; set; }
        public string LogoUrl { get; set; }
        public decimal CurrentBalance { get; set; }
        public DateTimeOffset DateAdded { get; set; }
    }

    public class AgentWalletResponse
    {
        public AgentWalletSummaryDto Summary { get; set; }
        public List<AgentWalletItemDto> Wallets { get; set; }
     //   public bool IsEmpty { get; set; }
    }

    public class AgentHeaderDto
    {
        public string AgentUserId { get; set; }              // AGT00142
        public string FullName { get; set; }
        public string Status { get; set; }                 // Active / Suspended / Pending
        public bool CanSuspend { get; set; }               // drives the Suspend Agent button
        public string Organization { get; set; }
        public DateTimeOffset RegistrationDate { get; set; }
    }

    public class AgentPersonalInfoDto
    {
        public string AgentUserId { get; set; }              // AGT00142
        public string FullName { get; set; }
        public string Status { get; set; }                 // Active / Suspended / Pending
        public bool CanSuspend { get; set; }               // drives the Suspend Agent button
       // public string Organization { get; set; }
        public DateTimeOffset RegistrationDate { get; set; }

        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Gender { get; set; }
        public string DateOfBirth { get; set; }
        public string NIN { get; set; }
        public string Address { get; set; }
        public string State { get; set; }
        public string Lga { get; set; }
        public string Ward { get; set; }
    }

    public class AgentRegistrationInfoDto
    {
        public DateTimeOffset RegistrationDate { get; set; }
        public string RegistrationChannel { get; set; }    // "Organization Assisted"
        public string RegistrationSource { get; set; }     // "Kaduna Rice Farmers Cooperative"
        public string Status { get; set; }
        public DateTimeOffset LastUpdated { get; set; }
    }

    public class AgentOverviewResponse
    {
        public AgentPersonalInfoDto PersonalInformation { get; set; }
        public AgentRegistrationInfoDto RegistrationInformation { get; set; }
    }
}
