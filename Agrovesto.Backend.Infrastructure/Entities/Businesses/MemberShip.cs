using Agrovesto.Backend.Infrastructure.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses
{
    public class MemberShipType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }

        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset? UpdatedOn { get;set; } 
        public string CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public string BusinessCode { get; set; }
        public ICollection<MemberShip> MemberShips { get; set; }
    }
    public class MemberShip
    {
        public bool IsLeaderAFarmer { get; set; }
        public string? Name { get; set; }
        public string? Location { get; set; }
        public string? State { get; set; }
        public string? Lga { get; set; }
        public string? Ward { get; set; }
        public string? ContactPerson { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? RegistrationNumber { get; set; } //CAC
        public string? MembershipLeaderFirstName { get; set; }
        public string? MembershipLeaderMiddleName { get; set; }
        public string? MembershipLeaderLastName { get; set; }
        public string? MembershipLeaderGender { get; set; }
        public string UserId { get; set; }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        public string BelongToBusinessCode { get; set; }  //which business

        public DateTimeOffset OnBoardedOn { get; set; } = DateTimeOffset.Now;
       // public string? IsAssignedToAgentId { get; set; }
        public bool HasbeenAssignedAgent { get; set; }
       // public string? OrganizationCode { get; set; }
        public string? OnboardedById { get; set; }
        public Tier TierLevel { get; set; }
        public long MemberShipTypeId { get; set; }
        public MemberShipType MemberShipType { get; set; }
        public bool IsActive { get; set; } = true;

    }
    public class MemberShipAgent
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }

        public long AgentId { get; set; }
        public Agent Agent { get; set; }
        public long MemberShipId { get; set; }
        public MemberShip MemberShip { get; set; }

        public DateTimeOffset AssignedOn { get; set; } = DateTimeOffset.Now;
        public string? OnboardedByUserId { get; set; }
        public bool IsActive { get; set; } = true;

        public string? DeactivationReason { get; set; }
        public string? DeactivatedByUserId { get; set; }
    }
    public enum Tier
    {
        None,
        Tier1,
        Tier2,
        Tier3,
        Tier4

    }

    public class OrganizationMembership //Memberships that belong to organization
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }

        public long OrganizationId { get; set; }
        public Organization Organization { get; set; }
        public long MembershipId { get; set; }
        public MemberShip MemberShip { get; set; }

        public DateTimeOffset AssignedOn { get; set; } = DateTimeOffset.Now;
        public string? AssignedByUserId { get; set; }
        public bool IsActive { get; set; } = true;
        public string? DeactivationReason { get; set; }
        public string? DeactivatedByUserId { get; set; }
    }
}
