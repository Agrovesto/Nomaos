using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Models.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Admin
{
    public class SuspendMembershipRequest
    {
        public string Reason { get; set; }
    }

    public class MembershipStatusChangeResult
    {
        public long MembershipId { get; set; }
        public string Status { get; set; }          // Verified / Pending / Suspended
        public bool CanSuspend { get; set; }
        public bool CanReactivate { get; set; }
        public string Message { get; set; }         // toast text
    }
    public class MembershipInputFinancePackageDto
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

        public decimal PackageValue { get; set; }  // ₦, formatted on the frontend }
        public string PackageValueFormatted { get; set; }
        public string Organization { get; set; }
        public int Farmers { get; set; }
    }
    public class InputFinanceResponse
    {
        public MembersipInputFinanceSummaryDto Summary { get; set; }
        public PagedResult<MembershipInputFinancePackageDto> Packages { get; set; }
 
    }
    public class MembersipInputFinanceSummaryDto
    {
        public int TotalPackages { get; set; }
        public int ActivePackages { get; set; }
        public int FarmersFinanced { get; set; }
        public string TotalValueFormatted { get; set; }
        public decimal TotalFinancingValue { get; set; }
    }
    public class MembershipAgentsResponse
    {
        public int LinkedAgentCount { get; set; }                 // "Linked Agent (n)"
        public List<MembershipAgentItemDto> Agents { get; set; } = new();
    }

    public class MembershipAgentItemDto
    {
        public long AgentId { get; set; }
        public string AgentUserId { get; set; }                    // AGT00142
        public string Name { get; set; }
        public DateTimeOffset? AssignmentDate { get; set; }
        public MembershipAgentPerformanceDto Performance { get; set; }
    }

    public class MembershipAgentPerformanceDto
    {
        public int FarmersOnboarded { get; set; }                 // this membership's farmers assigned to the agent
        public decimal VerificationSuccessRate { get; set; }      // percent
        public int TotalFarms { get; set; }
        public decimal TotalFarmSizeHectares { get; set; }
    }
    public class MembershipOrganizationsResponse
    {
        public int LinkedOrganizationCount { get; set; }          // "Linked Organization (n)"
        public List<MembershipOrganizationItemDto> Organizations { get; set; } = new();
    }

    public class MembershipOrganizationItemDto
    {
        public long OrganizationId { get; set; }                  // for View Details navigation
        public string OrganizationCode { get; set; }              // display ID
        public string Name { get; set; }
        public DateTimeOffset? AssignmentDate { get; set; }
        public string Region { get; set; }
        public string CoverageArea { get; set; }
        public MembershipOrganizationPerformanceDto Performance { get; set; }
    }

    public class MembershipOrganizationPerformanceDto
    {
        public int MembersOnboarded { get; set; }                 // this membership's farmers linked to the org
        public decimal VerificationSuccessRate { get; set; }      // percent
        public int TotalFarms { get; set; }
        public decimal TotalFarmSizeHectares { get; set; }
    }
    public class MembershipMembersSummaryDto
    {
        public int TotalMembers { get; set; }
        public int VerifiedMembers { get; set; }
        public int PendingVerification { get; set; }
        public int SuspendedFarmers { get; set; }
        public int TotalFarms { get; set; }
        public decimal TotalFarmSizeHectares { get; set; }
    }

    public class MembershipMemberItemDto
    {
        public long FarmerId { get; set; }
        public string FarmerUserId { get; set; }      // for navigating to the farmer profile
        public string FullName { get; set; }
        public int Farms { get; set; }
        public string State { get; set; }
        public int LinkedAgents { get; set; }
        public string Status { get; set; }            // Verified / Pending / Suspended
    }

    public class MembershipMembersResponse
    {
        public MembershipMembersSummaryDto Summary { get; set; }
        public PagedResult<MembershipMemberItemDto> Members { get; set; }
    }


    public class MemberShipOverviewDto
    {
        public MembershipStatsDto Stats { get; set; }
        public MembershipGeneralInfoDto GeneralInformation { get; set; }
        public MembershipContactDto PrimaryContact { get; set; }
        public MembershipAddressDto Address { get; set; }
        public MembershipRegistrationInfoDto RegistrationInformation { get; set; }
    }

 
    public class MembershipStatsDto
    {
        public int Members { get; set; }
        public int Farms { get; set; }
        public decimal TotalLandSizeHectares { get; set; }
        public int LinkedOrganizations { get; set; }
        public int ActiveProgrammes { get; set; }
    }

    public class MembershipGeneralInfoDto
    {
        public long Id { get; set; }
        public string MembershipRepresentativeId { get; set; }        // MEM00042
        public string Name { get; set; }
        public string Status { get; set; }                // Verified / Pending / Suspended
        public string MembershipType { get; set; }
        public DateTimeOffset RegistrationDate { get; set; }
        public bool CanSuspend { get; set; }              // drives "Suspend Membership" button
        public bool CanReactivate { get; set; }
        public string MembershipRepresentaiveId { get; set; }
        public string RegistrationNumber { get; set; }
    }

    public class MembershipContactDto
    {
        public string ContactPerson { get; set; }
        public string Position { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
    }

    public class MembershipAddressDto
    {
        public string Country { get; set; }
        public string State { get; set; }
        public string Lga { get; set; }
        public string Address { get; set; }
        public string PostalCode { get; set; }
    }

    public class MembershipRegistrationInfoDto
    {
        public string RegistrationChannel { get; set; }
        public string RegistrationSource { get; set; }
        public string VerifiedBy { get; set; }
        public DateTimeOffset? VerificationDate { get; set; }
        public DateTimeOffset? LastUpdated { get; set; }
    }

    public class AdminCreateMembershipRequest
    {
        public string MembershipName { get; set; }
        public int MembershipTypeId { get; set; }
        public string FirstName { get; set; }       // leader
        public string LastName { get; set; }        // leader
        public string PhoneNumber { get; set; }     // leader
        public string Gender { get; set; }          // same type as FarmerModel.Gender
        public bool IsLeaderAFarmer { get; set; }
        public SignupMethod SignupMethod { get; set; }
        public string? Pin { get; set; }
    } 
    public class CreateMembershipRequest
    {
        public string MembershipName { get; set; }
        public int MembershipTypeId { get; set; }
        public string LeaderFirstName { get; set; }
        public string LeaderLastName { get; set; }
        public string PhoneNumber { get; set; }
        public string Gender { get; set; }
        public string State { get; set; }
        public string LGA { get; set; }
        public string Ward { get; set; }
        public string? Location { get; set; }
        public bool IsLeaderAFarmer { get; set; }
    }

    public class MembershipTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    public class MemberShipSummaryDto
    {
        public int TotalMemberShip { get; set; }
        public int TotalActiveMemberShip { get; set; }
        public int TotalPendingApproval { get; set; }
        public int TotalSuspendedMembers { get; set; }
        public int TotalMembers { get; set; } //total number of farmers in the entire membershipbodies identified
        public int TotalLinkedOrganizations { get; set; } //total number of organizations linked to the membershipbodies
    }
    public class MemberShipListItemDto
    {
        public long Id { get; set; }
        public string MemberShipLeaderId { get; set; } = default!;   // FMR-1712
        public string? MemberShipName { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTimeOffset RegDate { get; set; }
        public string Status { get; set; } = default!;
       public int LinkedOrganizations { get; set; }
        public int Members { get; set; }
        public string State { get; set; }
        public string MemberShipType { get; set; }
    }

    public class MemberShipListQuery
    {
        public long BusinessId { get; set; }
        public string? Search { get; set; }              // name, ID (FMR-1712 or 1712) or phone
        public MembershipStatus? Status { get; set; }
        public string? OrganizationCode { get; set; }
        public DateTimeOffset? From { get; set; }
        public DateTimeOffset? To { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
