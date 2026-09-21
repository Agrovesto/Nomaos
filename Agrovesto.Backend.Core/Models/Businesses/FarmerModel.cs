using Agrovesto.Backend.Core.Models.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses
{
    public enum SignupMethod
    {
        PhoneNumber = 1,
        Email = 2
    }
    public enum FarmerOnboardingStatus
    {
        Pending = 0,
        Completed = 1,
        Failed = 2
    }
    public class CreateFarmerRequest
    {
        public long Id { get; set; }
        public string? OnboardedByUserId { get; set; }
        public SignupMethod SignupMethod { get; set; } = SignupMethod.PhoneNumber;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? Location { get; set; }
        public string? State { get; set; }
        public string? Lga { get; set; }
        public string? Ward { get; set; }
        public string? Gender { get; set; }
        public string? DateOfBirth { get; set; }
        public string? FullName { get; set; }
        public long OrganizationId { get; set; }
        public string? Bvn { get; set; }
        public string? NIN { get; set; }
        public string? Password { get; set; }
        public string? Pin { get; set; }
        public string? FarmerCategory { get; set; }
        public long YearsInAgric { get; set; }
        public long? MembershipTypeId { get; set; }
        public string? HighestLevelOfEducation { get; set; }
        public List<long?>? AgentIds { get; set; }
        public List<ValueChainSelectionDto>? ValueChainDto { get; set; }
        public List<long?>? CooperativeIds { get; set; }
    }

    public class FarmerCooperativeDto
    {
        public string FarmerUserId { get; set; }
        public string FullName { get; set; }
        public string State { get; set; }
        public string Lga { get; set; }
        public string MappingStatus { get; set; }
        public DateTimeOffset DateAdded { get; set; }
        public int TotalMappedFarms { get; set; }
        public long CooperativeId { get; set; }        // Id of the cooperative
        public string CooperativeName { get; set; }    // Name of the cooperative
        public DateTimeOffset DateJoined { get; set; } // Date the farmer joined this cooperative
        public string OnboardedBy { get; set; }
    }
    public class FarmerMembershipDto
    {
        public string State { get; set; }
        public string Lga { get; set; }
        public string MappingStatus { get; set; }
        public long Id { get; set; }
        public string FarmerUserId { get; set; }
        public string FullName { get; set; }
        public string Level { get; set; }
        public DateTimeOffset DateAdded { get; set; }
        public long TotalFarms { get; set; }
        public double LandSize { get; set; }
        public long CooperativeId { get; set; }        // Id of the cooperative
        public string CooperativeName { get; set; }    // Name of the cooperative
        public DateTimeOffset DateJoined { get; set; } // Date the farmer joined this cooperative
        public string OnboardedBy { get; set; }

        public bool IsLeaderAFarmer { get; set; }
        public long MemberShipTypeId { get; set; }
        public string Name { get; set; }
        public string? MembershipType { get; set; }
        public string Location { get; set; }
        public string? Ward { get; set; }
        public string ContactPerson { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string UserId { get; set; }

        public string Tier { get; set; }
    }
    public class AgentOverviewDto
    {
        public long AgentId { get; set; }
        public string Status { get; set; }
        public DateTimeOffset DateRegistered { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string AgentUserId { get; set; }
        public string CommunityName { get; set; }
        public string State { get; set; }
        public long TotalMemberships { get; set; }
        public long TotalFarmers { get; set; }
        public double TotalFarmSize { get; set; }
        public long Farms { get; set; }
     //   public List<AgentFarmDto> Farms { get; set; } = new();
    }
    public class AgentFarmDto
    {
        public long FarmId { get; set; }
        public string FarmName { get; set; }
        public double FarmSize { get; set; }
        public string Location { get; set; }
    }
    public class AgentMembershipDto
    {   public long Id { get; set; }                    // Agent ID
        public string AgentUserId { get; set; }           // Linked User ID
        public string AgentName { get; set; }           // Agent Name
        public string PhoneNumber { get; set; }         // Phone Number
        public int TotalAssignedFarmers { get; set; }   // Number of farmers assigned
        public string Status { get; set; }              // Active / Inactive
        public DateTimeOffset DateAdded { get; set; }         // OnBoardedOn
        public string State { get; set; }               // Agent State
        public string Lga { get; set; }                 // Agent LGA
    }

    public class FarmerMembershipSummaryDto
    {
        public long MembershipId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
    }
    public class FarmerMembershipOverviewDto
    {
        public long MembershipId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string Location { get; set; }
        public DateTimeOffset DateCreated { get; set; }
        public long NumberOfFarmers { get; set; }
        public long TotalFarmCount { get; set; }
        public long TotalMappedFarms { get; set; }
    }
    public class FarmerFinancialSummaryDto
    {
        public decimal TotalWalletBalance { get; set; }
        public decimal OutstandingInputDebt { get; set; }
        public decimal NextRepaymentAmount { get; set; }  // e.g., next scheduled repayment
        public DateTimeOffset? NextRepaymentDate { get; set; }  // optional
    }
    public class FarmerMembershipProfileDto
    {
        public string FullName { get; set; }
        public DateTimeOffset DateAdded { get; set; }
        public long CooperativeId { get; set; }        // Id of the cooperative
        public string CooperativeName { get; set; }    // Name of the cooperative
        public string OnboardedBy { get; set; }

    }

    public enum FarmerLevel
    {
        [Display(Name = "Level 1")] //not verified
        Level1,

        [Display(Name = "Level 2")] //phone or email verified 
        Level2,

        [Display(Name = "Level 3")] //farmer:picture
        Level3,

        [Display(Name = "Level 4")]//farmer:nin/bvn,  c
        Level4,

        [Display(Name = "Level 5")] //farmer: land mapping, document
        Level5,

        [Display(Name = "Level 6")] //repaid credit
        Level6
    }
    public class FarmerDetailDto
    {
        public FarmerModel Farmer { get; set; }
        public List<MemberShipModel>? Cooperatives { get; set; }
        public List<AgentModel>? Agents { get; set; }
        public FarmerImagesModel? FarmerImage { get; set; }
        public List<FarmPlotDetailsModel>? PlotDetails { get; set; }
        public List<FarmVisitationActivityModel>? Visitations { get; set; }
        public List<RepaymentModel>? Repayments { get; set; }
        public List<FarmerValueChainSelectionDto>? ValueChainItems { get; set; }

    }

    public class FarmerAgentDto
    {
        public long Id { get; set; }
        public string FarmerUserId { get; set; }
        public string FullName { get; set; }
        public string State { get; set; }
        public string Lga { get; set; }
        public string CooperativeStatus { get; set; } // "Linked" | "Not Linked"
        public string MappingStatus { get; set; } // "Mapped" | "Unmapped"
        public DateTimeOffset DateAdded { get; set; }
        // Total number of farmers (in this agent's set) that are linked to any cooperative
        public int TotalLinkedFarmers { get; set; }
        public int TotalMappedFarms { get; set; }
        public int TotalFarms { get; set; }
        public double TotalFarmSizeHectares { get; set; }
    }

    public class FarmerUpdateResultDto
    {
        public bool Success { get; set; }
        public List<string> UpdatedFields { get; set; } = new List<string>();
        public FarmerModel UpdatedFarmer { get; set; }
        public string Message { get; set; }
    }
    public class FarmerProfileLocationDto
    {
        // Location
        public string State { get; set; }
        public string Lga { get; set; }
        public string Ward { get; set; }
        public string Location { get; set; }
    }
    public class FarmerPersonalInfoDto
    {
        public long Id { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public string Gender { get; set; }
        public string DateOfBirth { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string BVN { get; set; }
        public string NIN { get; set; }
        // Image (image key or url)
        public string FarmerImage { get; set; }
        // User level and active status
        public string FarmerLevel { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset DateRegistered { get; set; }
    }
    public class FarmerTransactionDto
    {
        public long TransactionId { get; set; }
        public string TransactionReference { get; set; }
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public string TransactionType { get; set; } // e.g., Deposit, Withdrawal
        public string Status { get; set; } // e.g., Completed, DebitPending, Failed
        public DateTimeOffset Timestamp { get; set; }
        public string Description { get; set; }
    }
    public class FarmerProfileDto
    {
        public FarmerProfileLocationDto Location { get; set; }
        public FarmerPersonalInfoDto PersonalInformation { get; set; }
        public List<FarmerFarmDto> Farms { get; set; }
        public FarmerWalletDto Wallet { get; set; }
        public List<FarmerActivityLogDto> ActivityLogs { get; set; }
        public List<OrganizationFarmerAgentDto> Agents { get; set; } = new();

        // Memberships (Cooperatives)
        public List<OrganizationFarmerCooperativeDto> Memberships { get; set; } = new();


        // Cooperatives
        public List<FarmerMembershipProfileDto> CooperativeInformation { get; set; } = new List<FarmerMembershipProfileDto>();
    }
    public class FarmerWalletDto
    {
        public decimal OutStandingInputDebt { get; set; }
        public decimal NextDuePaymentAmount { get; set; }
        public DateTimeOffset NextDuePaymentDate { get; set; }
        public decimal TotalWalletBalance { get; set; }
        public List<FarmerTransactionDto> Transactions { get; set; } = new List<FarmerTransactionDto>();
        public List<FarmerInputObligationDto> InputObligations { get; set; } = new List<FarmerInputObligationDto>();
    }
    public class FarmerInputObligationDto
    {
        public long Id { get; set; }
        public string InputReference { get; set; }
        public string Name { get; set; }
        public decimal TotalCost { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal RemainigBalance { get; set; }
        public DateTimeOffset Date {  get; set; }
        public decimal RepaymentProgress { get; set; } // e.g., Deposit, Withdrawal


    }
    public class FarmerActivityLogDto
    {
        public DateTimeOffset Timestamp { get; set; }
        public string Description { get; set; }
        public string ActivityName { get; set; }
        public string ActivityType { get; set; } // e.g., FarmUpdate, WalletTransaction, ProfileUpdate
    }
    public class FarmerFarmDto
    {
        public long FarmId { get; set; }
        public string FarmName { get; set; }
        public string Location { get; set; }
        public string State { get; set; }
        public string Lga { get; set; }
        public double LandSize { get; set; } // in hectares or acres
        public string MappingStatus { get; set; }
        public string DocumentStatus { get; set; }
    }
    public class FarmerMembershipModel
    {
        public long TotalFarms { get; set; }
        public double LandSize { get; set; }
        public long CooperativeId { get; set; }        // Id of the cooperative
        public string CooperativeName { get; set; }    // Name of the cooperative
        public DateTimeOffset DateJoined { get; set; } // Date the farmer joined this cooperative
        public string OnboardedBy { get; set; }
        public bool IsLeaderAFarmer { get; set; }
        public long MemberShipTypeId { get; set; }
        public string Name { get; set; }
        public string? MembershipType { get; set; }
        public string Location { get; set; }
        public string State { get; set; }
        public string Lga { get; set; }
        public string? Ward { get; set; }
        public string ContactPerson { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string UserId { get; set; }
        public long Id { get; set; }

        public string Tier { get; set; }
    }

    public class AgentUpdateModel
    {
        public string? Name { get; set; }
        public string? Location { get; set; }
        public string? State { get; set; }
        public string? Lga { get; set; }
        public string? Ward { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Gender { get; set; }
        public string? DateOfBirth { get; set; }
        public string? OrganizationCode { get; set; }
        public string? NIN { get; set; }
        public string? BVN { get; set; }
        public bool? IsNinVerified { get; set; }
    }


    public class FarmerModel
    {
        public string BelongToBusinessCode { get; set; }  //which business
        public string? Name { get; set; }
        public string? Location { get; set; }
        public string? State { get; set; }
        public string? Lga { get; set; }
        public string? Ward { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string UserId { get; set; }
        public long Id { get; set; }
        public DateTimeOffset OnBoardedOn { get; set; } = DateTimeOffset.Now;
        public string? Gender { get; set; }
        public string? DateOfBirth { get; set; }
        public string? OnboardedByUserId { get; set; } //if individual farmers register, automatically assign them to Agrovesto
                                                       // public long BelongToCooperativeId { get; set; }
                                                       // public string? CooperativeName { get; set; }
        public string? FarmerImage { get; set; }
        public string? OrganizationCode { get; set; }
        public string? NIN { get; set; }
        public string? BVN { get; set; }
        public string? FarmerCategory { get; set; }
        public long YearsInAgric { get; set; }
        public string? HighestLevelOfEducation { get; set; }
        public bool IsNinVerified { get; set; } = false;
        public string? OnboardedById { get; set; }
        public List<ValueChainSelectionDto> SelectedItems { get; set; }
        public List<FarmerValueChainsSelectionDto>? ValueChainItems { get; set; }
        public List<long>? CooperativeIds { get; set; }
        public List<long>? AgentIds { get; set; }
        public List<string>? CooperativeNames { get; set; }
        public string? CooperativeName { get; set; }
        public string? Level { get; set; }

    }
    public class FarmerValueChainSelectionDto
    {
        public long CategoryId { get; set; }
        public string CategoryName { get; set; }
        public long TypeId { get; set; }
        public string TypeName { get; set; }
        public long ItemId { get; set; }
        public string ItemName { get; set; }
    }

    public class ValueChainSelectionDto
    {
        [Required(ErrorMessage = "Value chain type is required.")]
        public string ValueChainType { get; set; }
      [Required(ErrorMessage = "Value chain item(s) is required")]
        public List<string> Items { get; set; }
    }
    public class FarmerValueChainsSelectionDto
    {
        public string TypeName { get; set; } = string.Empty;

        // List of items under this value chain type
        public List<ValueChainItemDto> Items { get; set; } = new List<ValueChainItemDto>();
    }

    public class ValueChainItemDto
    {
        public long ItemId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

}
