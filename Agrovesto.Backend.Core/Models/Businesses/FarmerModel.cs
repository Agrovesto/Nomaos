using Agrovesto.Backend.Core.Models.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses
{
    public class FarmerCooperativeDto
    {
        public string FarmerUserId { get; set; }
        public string FullName { get; set; }
        public string State { get; set; }
        public string Lga { get; set; }
        public string MappingStatus { get; set; }
        public DateTimeOffset DateAdded { get; set; }
        public int TotalMappedFarms { get; set; }
    }
    public class FarmerMembershipDto
    {
        public string FarmerUserId { get; set; }
        public string FullName { get; set; }
        public string Level { get; set; }
        public DateTimeOffset DateAdded { get; set; }
        public long TotalFarms { get; set; }
        public double LandSize { get; set; }
    }

    public enum FarmerLevel
    {
        [Display(Name = "Level 0")] //not verified
        Level0,

        [Display(Name = "Level 1")] //phone or email verified 
        Level1,

        [Display(Name = "Level 2")]//farmer:picture
        Level2,

        [Display(Name = "Level 3")] //farmer:nin/bvn,  cooperatuve: cooperative registration document
        Level3,

        [Display(Name = "Level 4")] //farmer: land mapping, cooperative: cac
        Level4,

        [Display(Name = "Level 5")] //repaid credit
        Level5
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
    public class FarmerProfileDto
    {
       public FarmerProfileLocationDto Location { get; set; }
       public FarmerPersonalInfoDto PersonalInformation { get; set; }    

        // Cooperatives
       public List<FarmerMembershipModel> CooperativeInformation { get; set; } = new List<FarmerMembershipModel>();
    }
    public class FarmerMembershipModel
    {
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
    public class FarmerUpdateModel
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
        public string? FarmerImage { get; set; }
        public string? OrganizationCode { get; set; }
        public string? NIN { get; set; }
        public string? BVN { get; set; }
        public string? FarmerCategory { get; set; }
        public long? YearsInAgric { get; set; }
        public string? HighestLevelOfEducation { get; set; }
        public bool? IsNinVerified { get; set; }
        public List<ValueChainSelectionDto>? SelectedItems { get; set; }
        public List<long>? CooperativeIds { get; set; }
        public List<long>? AgentIds { get; set; }
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
        public string ValueChainType { get; set; }
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
