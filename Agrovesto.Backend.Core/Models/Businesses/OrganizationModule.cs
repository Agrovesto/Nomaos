using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses
{
    internal class OrganizationModule
    {
    }
    public class OrganizationAgentDetailResponse
    {
        public long AgentId { get; set; }
        public string UserId { get; set; }

        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }

        public string? State { get; set; }
        public string? Lga { get; set; }
        public string? Ward { get; set; }

        public DateTimeOffset OnBoardedOn { get; set; }
        public List<OrganizationLocationDto> Locations { get; set; }

        public List<OrganiationRoleDto> Roles { get; set; }
    }
    
    public class OrganizationAgentPerformanceSummary
    {
        public long Subscribers { get; set; }
        public long InputDisbursed { get; set; }
        public long TotalEquityPaid { get; set; }
        public long ProductSold { get; set; }
    }
    public class OrganizationAgentInputAllocationModel
    {
        public long Id { get; set; }
        public string InputName { get; set; }
        public long Allocated { get; set; }
        public long Distributed { get; set; }
        public long Remaining { get; set;  }

    }
    public class OrganizationAgentSubscriberModel
    {
        public long Id { get; set; }

        public string FarmerName { get; set; }
        public string FarmerUserId { get; set; }
        public string InputPackage { get; set; }
        public long InputPackageId { get; set; }
        public decimal EquityPaid { get; set; }
        public bool Status { get; set; }

    }

    public class OrganizationAgentTransactionHistory
    {
        public long Id { get; set; }
    public string TransactionType { get; set; }
        public string FarmerName { get; set; }
        public string FarmerUserId { get; set; }
        public string InputType { get; set; }
        public double Quantity { get; set; }
        public string QuantityType { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
    }
    public class OrganizationAgentActivityLog
    {
        public long Id { get; set; }
        public string ActivityType { get; set; }
        public string ActivityName { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
    }
    public class OrganizationAgentResponse
    {
        public long AgentId { get; set; }
        public string UserId { get; set; }

        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }

        public string? State { get; set; }
        public string? Lga { get; set; }
        public string? Ward { get; set; }
        public bool Status { get; set; }
        public DateTimeOffset OnBoardedOn { get; set; }
        public long LocationCount { get; set; }
        public long RoleCount { get; set; } 
        public long Subscribers { get; set; }
        public double InputDisbursed { get; set; }
        public decimal EquityPaid { get; set; }
        public decimal ProductSold { get; set; }
        //public List<OrganizationLocationDto> Locations { get; set; }

        //public List<OrganiationRoleDto> Roles { get; set; }
    }

    public class OrganiationRoleDto
    {
        public long RoleId { get; set; }
        public string RoleName { get; set; }
    }
    public class DeliveryInformation
    {
        public string Warehouse { get; set; }
        public string AgentName { get; set; }
        public decimal? AmountDisbursed { get; set; }
        public string Location { get; set; }
    }
    public class OrganizationLocationDto
    {
        public long LocationId { get; set; }
        public string LocationName { get; set; }
        public string LocationType { get; set; }
        public string State { get; set; }
        public string Lga { get; set; }
    }

    public class OrganizationFarmerSalesSummary
    {
        public decimal TotalVolumeSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageSalesValue { get; set; }
    }

    public class OrganizationFarmerSalesHistor
    {

        public DateTimeOffset Date { get; set; }
        public string ProductSold { get; set; }
        public double Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalValue { get; set; }
        public string PaymentStatus { get; set; } //Pending, Paid, Cancelled
    }

    public class OrganizationFarmerProjection
    {
         public string UserId { get; set; }
        public FarmerLevel LevelRaw { get; set; }
        public long FarmerId { get; set; }
        public string Name { get; set; }
        public string? Gender { get; set; }
        public string State { get; set; }
        public string Lga { get; set; }
        public string Ward { get; set; }
        public string Location { get; set; }
        public string? FarmerCategory { get; set; }
        public string Level { get; set; }

        public string PhoneNumber { get; set; }
        public string Email { get; set; }

           public string OrganizationCode { get; set; }
        public bool OrganizationIsActive { get; set; }

        public DateTimeOffset CreatedOn { get; set; }
        public FarmerOnboardingStatus FarmerOnboardingStatus { get; set; }
    }
}
