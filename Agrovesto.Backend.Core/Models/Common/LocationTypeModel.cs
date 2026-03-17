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
}
