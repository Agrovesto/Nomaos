using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses
{
    internal class OrganizationModule
    {
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

        public DateTimeOffset OnBoardedOn { get; set; }

        public OrganizationLocationDto Location { get; set; }

        public List<OrganiationRoleDto> Roles { get; set; }
    }

    public class OrganiationRoleDto
    {
        public long RoleId { get; set; }
        public string RoleName { get; set; }
    }

    public class OrganizationLocationDto
    {
        public long LocationId { get; set; }
        public string LocationName { get; set; }
        public string LocationType { get; set; }
        public string State { get; set; }
        public string Lga { get; set; }
    }
}
