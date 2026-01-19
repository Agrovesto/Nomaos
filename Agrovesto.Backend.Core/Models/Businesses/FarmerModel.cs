using Agrovesto.Backend.Core.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses
{
    public class FarmerDetailDto
    {
        public FarmerModel Farmer { get; set; }
        public CooperativeModel Cooperative { get; set; }
        public AgentModel? Agent { get; set; }
        public FarmerImagesModel? FarmerImage { get; set; }
        public List<FarmPlotDetailsModel> PlotDetails { get; set; }
        public List<FarmVisitationActivityModel> Visitations { get; set; }
        public List<RepaymentModel> Repayments { get; set; }
        public List<FarmerValueChainSelectionDto>? ValueChainItems { get; set; }

    }




    public class FarmerModel
    {
        public string BelongToBusinessCode { get; set; }  //which business
        public string Name { get; set; }
        public string Location { get; set; }
        public string State { get; set; }
        public string Lga { get; set; }
        public string? Ward { get; set; }
        public string PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string UserId { get; set; }
        public long Id { get; set; }
        public DateTimeOffset OnBoardedOn { get; set; } = DateTimeOffset.Now;
        public string? Gender { get; set; }
        public string? DateOfBirth { get; set; }
        public string OnboardedByUserId { get; set; } //if individual farmers register, automatically assign them to Agrovesto
        public long BelongToCooperativeId { get; set; }
        public string? CooperativeName { get; set; }
        public string? FarmerImage { get; set; }
        public string? OrganizationCode { get; set; }
        public string? NIN { get; set; }
        public string? BVN { get; set; }
        public string FarmerCategory { get; set; }
        public long YearsInAgric { get; set; }
        public string? HighestLevelOfEducation { get; set; }
        public bool IsNinVerified { get; set; } = false;
        public string? OnboardedById { get; set; }
        public List<ValueChainSelectionDto> SelectedItems { get; set; }
        public List<FarmerValueChainsSelectionDto>? ValueChainItems { get; set; }
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
