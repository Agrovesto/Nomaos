using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Agrovesto.Backend.Infrastructure.Entities.Common;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses
{
    public class Agent
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string UserId { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Location { get; set; }
        public string? State { get; set; }
        public string? Lga { get; set; }
        public string? Ward { get; set; }
       
        public string? BelongToBusinessCode { get; set; }  //which business
        public DateTimeOffset OnBoardedOn { get; set; } = DateTimeOffset.Now;
        public string? SupervisorUserId { get; set; }
        public string? OnboardedById { get; set; }
        public bool IsActive { get; set; } = true;

    }

    public class OrganizationAgent //Agents that belong to organization
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }

        public long OrganizationId { get; set; }
        public Organization Organization { get; set; }
        public long AgentId { get; set; }
        public ICollection<OrganizationAgentRole> Roles { get; set; }

        public DateTimeOffset AssignedOn { get; set; } = DateTimeOffset.Now;
        public string? AssignedByUserId { get; set; }
        public long LocationId { get; set; } //warehouse/aggregration centre, distribution centre etc
        public bool IsActive { get; set; } = true;

        public string? DeactivationReason { get; set; }
        public string? DeactivatedByUserId { get; set; }
    }
    public class OrganizationAgentRole
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long OrganizationAgentId { get; set; }

        public long RoleId { get; set; }
        public OrganizationRole OrganizationRole { get; set; }

        public DateTimeOffset AssignedOn { get; set; } = DateTimeOffset.Now;
    }
    public class OrganizationRole
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }

        public long OrganizationId { get; set; }
        public Organization Organization { get; set; }

        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now;
        public string RoleName { get; set; }
        public string Description { get; set; }

        public string BusinessCode { get; set; }
    }
    public class OrganizationLocation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        public long OrganizationId { get; set; }
        public Organization Organization { get; set; }
        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now;
        public string LocationName { get; set; }
        public double Capacity { get; set; }
        public string? CapacityType { get; set; } = "Bag";
        public double StockLevel { get; set; }
        public long LocationTypeId { get; set; }
        public LocationType LocationType { get; set; }
        public long StateId { get; set; }
        public States State { get; set; }
        public long LgaId   { get; set; }
        public LGA LGA { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; }
        public string BusinessCode { get; set; }
        public string? CreatedByUserId { get; set; }
        public string? DeactivationReason { get; set; }
        public string? DeactivatedByUserId { get; set; }
    }
    public class LocationType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string? BusinessCode { get; set; }
    }
    public class OrganizationAgentPermission
    {

    }


    public class AgentActivity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }

        public long AgentId { get; set; }

        public string OrganizationCode { get; set; }

        public long? LocationId { get; set; }

        public string ActivityType { get; set; }

        public string? Description { get; set; }

        public DateTimeOffset CreatedOn { get; set; }
    }
}
