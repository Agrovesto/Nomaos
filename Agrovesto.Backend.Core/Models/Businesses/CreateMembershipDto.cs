using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Agrovesto.Backend.Core.Models.Businesses
{
    // Phase 1 — everything needed to create the membership shell
    public class CreateMembershipDto
    {
        [Required]
        [MaxLength(50, ErrorMessage = "Name cannot exceed 50 characters.")]
        public string Name { get; set; }

        [Required]
        public long MemberShipTypeId { get; set; }

        public string? Location { get; set; }
        public string? State { get; set; }
        public string? Lga { get; set; }
        public string? Ward { get; set; }
        public string? ContactPerson { get; set; }
        public string? Email { get; set; }
        [MaxLength(11, ErrorMessage = "Phone number cannot exceed 11 characters.")]
        public string? PhoneNumber { get; set; }
        public string? DateOfBirth  { get; set; }
        public SignupMethod SignupMethod = SignupMethod.PhoneNumber;
       // public Tier TierLevel { get; set; } = Tier.None;

        // At least one business code required
        //[Required, MinLength(1)]
        //public List<string> BusinessCodes { get; set; }

        //// All optional — membership can exist without these
        //public string? OrganizationCode { get; set; }
        //public long? AgentId { get; set; }
    }

    public class AssignLeaderDto
    {
        public long MembershipId { get; set; }

        [Required]
        public string FirstName { get; set; }

        public string? MiddleName { get; set; }

        [Required]
        public string LastName { get; set; }

        public string? Gender { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        public string? Email { get; set; }
        public bool IsLeaderAFarmer { get; set; }
        public string? ExistingUserId { get; set; }
    }

    public class MembershipResponse
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string UserId { get; set; }
        public bool IsLeaderProfileComplete { get; set; }
        public string State { get; set; }
        public string Location { get; set; }
        public string BusinessCode { get; set; }
        public string? OrganizationCode { get; set; }
        public bool HasbeenAssignedAgent { get; set; }
    }
}
