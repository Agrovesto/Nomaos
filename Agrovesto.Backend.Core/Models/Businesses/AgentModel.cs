using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses
{
    public class AgentModel
    {
        public string Name { get; set; }
        public string Location { get; set; }
        public string State { get; set; }
        public string Lga { get; set; }
        public string? Ward { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string UserId { get; set; }
        public long Id { get; set; }
        public string BelongToBusinessCode { get; set; }  //which business
        public DateTimeOffset OnBoardedOn { get; set; } = DateTimeOffset.Now;
        public string? SupervisorUserId { get; set; }
      //  public string? OrganizationCode { get; set; }
        public string? OnboardedById { get; set; }
        public long? OrganizationId { get; set; }
 
    }
    public class OrganizationAgentAssignmArgs
    {
        public long LocationId { get; set; }
        public long Id { get; set; }
        public string? BusinessCode { get; set; }
        public long RoleId { get; set; }
        public string? OrganizationCode { get; set; }
        public string? UserId { get; set; }
        public string? OnboardedById { get; set; }
        public long? OrganizationId { get; set; }

    }
    public class OrganizationAgentArgs
    {

        public string? OrganizationCode { get; set; }
        public string BusinessCode { get; set; }
        public long LocationId { get; set; }
        public long RoleId { get; set; }
        public string? OnboardedById { get; set; }
        public string UserId { get; set; }
        public long? OrganizationId { get; set; }

    }
    public class LocationAgentDetailResponse
    {
        public long AgentId { get; set; }
        public string AgentName { get; set; }
        public string AgentRole { get; set; }
        public int TotalSubscribers { get; set; }
        public double TotalInputDisbursed { get; set; }
        public bool IsActive { get; set; }
    }

    public class LocationInventoryBreakdownResponse
    {
        public long InputId { get; set; }
        public string InputName { get; set; }
        public double TotalStock { get; set; }
        public double QuantityAllocated { get; set; }
        public double AvailableBalance { get; set; }
    }
    public class LocationActivityLogResponse
    {
        public long Id { get; set; }
        public string ActivityType { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
    }

    public class OrganizationDashboardResponse
    {
        public int TotalAgents { get; set; }
        public int TotalActiveLocations { get; set; }
        public decimal TotalEquityPaid { get; set; }
        public decimal TotalProduceSold { get; set; }
    }
}
