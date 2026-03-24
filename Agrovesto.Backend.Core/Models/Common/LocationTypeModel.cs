using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Common
{
    public class LocationTypeModel
    {
        public long Id { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }

        public string? BusinessCode { get; set; }
    }
    public class OrganizationAgentLocationResponseModel
    {

        public long Id { get; set; }
        public long OrganizationId { get; set; }
        public string LocationName { get; set; }
        public long LocationTypeId { get; set; }
        public string LocationType { get; set; }
        public string State { get; set; }
        public string Lga { get; set; }
        public long StateId { get; set; }
        public long LgaId { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; }
        public double Capacity { get; set; }
        public double Allocation { get; set; }
        public double AvailableCapacity { get; set; }
      
        public string? CapacityType { get; set; }
        public string? BusinessCode { get; set; }

    }

    public class OrganizationLocationResponseModel
    {

        public long Id { get; set; }
        public long OrganizationId { get; set; }
        public string LocationName { get; set; }
        public long LocationTypeId { get; set; }
        public string LocationType { get; set; }
        public string State { get; set; }
        public string Lga { get; set; }
        public long StateId { get; set; }
        public long LgaId { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; }
        public double Capacity { get; set; }
        public double TotalStock { get; set; }
        public double CurrentUsage { get; set; }
        public string AssignedAgentCount { get
                ;set;
        }
        public string? CapacityType { get; set; } 
        public string? BusinessCode { get; set; }

    }

    public class OrganizationLocationModel
    {
        public string? LocationType { get; set; }
        public string? State { get; set; }
        public string? Lga { get; set; }
        public long Id { get; set; }
        public long OrganizationId { get; set; }
        public string LocationName { get; set; }
        public long LocationTypeId { get; set; }
        public long StateId { get; set; }
        public long LgaId { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; }
        public double Capacity { get; set; }
        public double StockLevel { get; set; }
        public string? CapacityType { get; set; } = "Bag";
        public string? BusinessCode { get; set; }
        public double Used { get; set; }
        public double Available { get; set; }

    }
    public class OrganizationRoleModel
    {

        public long Id { get; set; }
        public long OrganizationId { get; set; }
        [Required]
        public string RoleName { get; set; }
        [Required]
        public string Description { get; set; }
        public string? BusinessCode { get; set; }
    }

    public class CreateFarmerResponseModel
    {
        public string UserId { get; set; }
        public string Message { get; set; }
        public bool RequiresProfileCompletion { get; set; }
    }
    public class OrganizationFarmerResponse
    {
        public long FarmerId { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Gender { get; set; }
        public string State { get; set; }
        public string Lga { get; set; }
        public string Ward { get; set; }
        public string Location { get; set; }
        public string FarmerCategory { get; set; }
        public string Level { get; set; }
        public int AgentCount { get; set; }
        public int MembershipCount { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
    }

    public class OrganizationFarmerDetailResponse
    {
        // Farmer Details
        public long FarmerId { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Gender { get; set; }
        public string State { get; set; }
        public string Lga { get; set; }
        public string Ward { get; set; }
        public string Location { get; set; }
        public string FarmerCategory { get; set; }
        public string Level { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public List<FarmerValueChainItemDto> ValueChainItems { get; set; } = new();
        public double FarmSize { get; set; }
        public long YearsInAgric { get; set; }
        // Agents
        public List<OrganizationFarmerAgentDto> Agents { get; set; } = new();

        // Memberships (Cooperatives)
        public List<OrganizationFarmerCooperativeDto> Memberships { get; set; } = new();
    }
    public class FarmerValueChainItemDto
    {
        public string ValueChainType { get; set; }
        public string ItemName { get; set; }
    }
    public class OrganizationFarmerAgentDto
    {
        public long AgentId { get; set; }
        public string AgentName { get; set; }
        public string AgentUserId { get; set; }
        public DateTimeOffset AssignedOn { get; set; }
        public bool Active { get; set; }
    }

    public class OrganizationFarmerCooperativeDto
    {
        public long MembershipId { get; set; }
        public string MembershipName { get; set; }
        public DateTimeOffset JoinedOn { get; set; }
        public bool IsActive { get; set; }
    }
   
}
