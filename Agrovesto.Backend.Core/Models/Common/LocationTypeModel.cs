using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Common
{
    public class LocationUtilizationDto
    {
        public long LocationId { get; set; }
        public string LocationName { get; set; }
        public double Capacity { get; set; }
        public double StockLevel { get; set; }
        public double UtilizationRate { get; set; }
    }
    public class LocationDashboardFullDto
    {
        // LOCATION
        public int TotalWarehouses { get; set; }
        public int TotalSubWarehouses { get; set; }

        // CAPACITY
        public double TotalCapacity { get; set; }
        public double TotalStockLevel { get; set; }
        public double OccupancyRate { get; set; }
        public double AvailableCapacity { get; set; }

        // AGENTS
        public int ActiveAgents { get; set; }
        public int InactiveAgents { get; set; }
        public int PrimaryAgents { get; set; }
        public int SupportingAgents { get; set; }

        // INVENTORY HEALTH
        public int ExpiringSoonCount { get; set; }
        public int ExpiredCount { get; set; }

        // ALERTS
        public int OverCapacityLocations { get; set; }

        // TOP LOCATIONS
        public List<LocationUtilizationDto> TopUtilizedLocations { get; set; } = new();
    }
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

    public class CreateSubLocationDto
    {
        public string LocationName { get; set; }
        public long ParentLocationId { get; set; }
        public string LocationStatus { get; set; }
        public long LocationTypeId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Capacity { get; set; }
        public string? CapacityType { get; set; } = "Bag";

        public long StateId { get; set; }
        public long LgaId { get; set; }
        public string? Address { get; set; }

        public DateTime? DateOfCommissioning { get; set; }

        public bool HasTemperatureControl { get; set; }
        public long? TemperatureControlTypeId { get; set; }

        public List<long> ModuleIds { get; set; } = new();
        public List<long> StorageCategoryIds { get; set; } = new();
        public string? CreatedByUserId { get; set; }
    }
    public class OrganizationLocationModel
    {
        public long Id { get; set; }

        public long OrganizationId { get; set; }
        public string BusinessCode { get; set; }

        public string LocationName { get; set; }

        public double Capacity { get; set; }
        public string? CapacityType { get; set; } = "Bag";

        public double StockLevel { get; set; }

        public long LocationTypeId { get; set; }
        public string? LocationTypeName { get; set; } // for response only

        public long StateId { get; set; }
        public string? StateName { get; set; }

        public long LgaId { get; set; }
        public string? LgaName { get; set; }

        public string? Address { get; set; }

        public bool IsActive { get; set; }

        public long? ParentLocationId { get; set; } // 👈 important

        public DateTimeOffset CreatedOn { get; set; }

        public string? CreatedByUserId { get; set; }
        public double Used { get; set; }
        public double Available { get; set; }
    }
    public class LocationCreateResponse
    {
        public OrganizationLocationModel Location { get; set; }

        public bool AgentAssigned { get; set; }
        public string? AgentWarning { get; set; }
    }
    public class CreateLocationDto
    {
        public string LocationName { get; set; }
        public long? OrganizationId { get; set; }
        public string? BusinessCode { get; set; }
        public string LocationStatus { get; set; }
        public long LocationTypeId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Capacity { get; set; }
        public string? CapacityType { get; set; } = "Bag";

        public long StateId { get; set; }
        public long LgaId { get; set; }
        public string? Address { get; set; }

        public DateTime? DateOfCommissioning { get; set; }

        public bool HasTemperatureControl { get; set; }
        public long? TemperatureControlTypeId { get; set; }

        public List<long> ModuleIds { get; set; } = new();
        public List<long> StorageCategoryIds { get; set; } = new();

        public string? CreatedByUserId { get; set; }

        public string? MainAgentUserId { get; set; }
        public string? SupportinAgentUserId { get; set; }
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
    public class FarmerFlatRow
    {
        public string UserId { get; set; }
        public long? FarmerId { get; set; }
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
        public string Status { get; set; }
        public bool IsPending { get; set; }
        public int AgentCount { get; set; }
        public int MembershipCount { get; set; }
    }
    public class BaseUserDto
    {
        public string UserId { get; set; } = default!;
        public DateTimeOffset CreatedOn { get; set; }
    }
    public class OrganizationFarmerResponse
    {
        public string FarmerOnboardingStatus { get; set; }
        public bool IsPendingOnboarding { get; set; }
        public long? FarmerId { get; set; }
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
        public double RepaymentPercentage { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public bool IsActive { get; set; }
    }
    public class OrganizationFarmerDashboardResponse
    {
        public int TotalFarmers { get; set; }
        public int ActiveFarmers { get; set; }
        public int PendingFarmers { get; set; }
        public int FarmersWithLoans { get; set; }
        public decimal RecoveryRate { get; set; }
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
        public string Dob { get; set; }
        public string NIN { get; set; }
        public string BVN { get; set; }
        public string FarmerImage { get; set; }
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

    public class EditLocationDto
    {
        public long Id { get; set; }

        public string LocationName { get; set; }

        public double Capacity { get; set; }
        public string? CapacityType { get; set; }

        public long StateId { get; set; }
        public long LgaId { get; set; }
        public string? Address { get; set; }

        public DateTime? DateOfCommissioning { get; set; }

        public bool HasTemperatureControl { get; set; }
        public long? TemperatureControlTypeId { get; set; }

        public List<long> ModuleIds { get; set; } = new();
        public List<long> StorageCategoryIds { get; set; } = new();
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
    public class EditSubLocationDto : EditLocationDto
    {
        public long? NewParentLocationId { get; set; } // optional move
    }
    public class DeleteLocationDto
    {
        public long Id { get; set; }
        public bool ForceDelete { get; set; } = false;
        public string? Reason { get; set; }
        public string? UserId { get; set; }
    }

    public class LocationResponseDto
    {
        public long Id { get; set; }
        public string LocationName { get; set; }

        public long LocationTypeId { get; set; }
        public string LocationTypeName { get; set; }

        public double Capacity { get; set; }
        public string CapacityType { get; set; }
        public double StockLevel { get; set; }

        public string? Address { get; set; }

        public bool HasTemperatureControl { get; set; }
        public string? TemperatureControlType { get; set; }

        public DateTime? DateOfCommissioning { get; set; }

        public List<string> Modules { get; set; }
        public List<string> StorageCategories { get; set; }
        public List<AgentDto> Agents { get; set; } = new();
        public AgentDto PrimaryAgent { get; set; }
        public string LocationStatus { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string OwnerShip { get; set; }
        public long? ParentLocationId { get; set; }
        public List<LocationResponseDto> Children { get; set; } = new();
    }
    public class AgentDto
    {
        public long AgentId { get; set; }
        public string UserId { get; set; }
        public string? Name { get; set; }
        public bool IsPrimary { get; set; }
    }

    public class SubLocationListDto
    {
        public long Id { get; set; }
        public string LocationName { get; set; }
        public string LocationType { get; set; }

        public string? AssignedAgentName { get; set; } // Primary agent
        public string? AssignedAgentUserId { get; set; }

        public double Capacity { get; set; }
        public string? CapacityType { get; set; }

        public bool IsActive { get; set; }
    }

    public class LocationAgentsResponseDto
    {
        public PrimaryAgentDto? PrimaryAgent { get; set; }
        public List<OtherAgentDto> OtherAgents { get; set; } = new();
    }

    public class PrimaryAgentDto
    {
        public long AgentId { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }

        public DateTimeOffset AssignedOn { get; set; }

        public string? AgentLevel { get; set; } // e.g. Senior, Junior
        public string Status { get; set; }      // Active / Inactive in location
    }

    public class OtherAgentDto
    {
        public long AgentId { get; set; }
        public string Name { get; set; }

        public DateTimeOffset AssignedOn { get; set; }
        public string Status { get; set; }
    }

    public class ReassignPrimaryAgentDto
    {
        public long LocationId { get; set; }
        public string NewPrimaryUserId { get; set; }
        public string? PerformedByUserId { get; set; }
    }

    public class RemoveAgentFromLocationDto
    {
        public long LocationId { get; set; }
        public string UserId { get; set; }
        public string? RemovedByUserId { get; set; }
    }

    public class CreateInventoryDto
    {
        public long LocationId { get; set; }

        public string? InventoryCode { get; set; }
        public string? BatchNumber { get; set; }

        public string InventoryOwner { get; set; }

        public double Quantity { get; set; }
        public string? QuantityType { get; set; }

        public string? Description { get; set; }

        public DateTimeOffset? StorageDate { get; set; }
        public DateTimeOffset? ExpiryDate { get; set; }

        public string? QualityGrade { get; set; }
        public string? BusinessCode { get; set; }

        public string? RecordedByUserId { get; set; }
    }

    public class EditInventoryDto
    {
        public long Id { get; set; }

        public double Quantity { get; set; }
        public string? QuantityType { get; set; }

        public string InventoryOwner { get; set; }

        public string? Description { get; set; }

        public DateTimeOffset? StorageDate { get; set; }
        public DateTimeOffset? ExpiryDate { get; set; }

        public string? QualityGrade { get; set; }
        public string? BusinessCode { get; set; }
    }
    public class InventoryListDto
    {
        public long Id { get; set; }

        public string? InventoryCode { get; set; }
        public string? BatchNumber { get; set; }

        public string InventoryOwner { get; set; }

        public double Quantity { get; set; }
        public string? QuantityType { get; set; }

        public DateTimeOffset? StorageDate { get; set; }
        public DateTimeOffset? ExpiryDate { get; set; }

        public string? QualityGrade { get; set; }
    }
}
