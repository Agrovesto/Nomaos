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
    }
}
