using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Models.Businesses;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Admin
{
    public class AdminCreateAgentRequest
    {
        public string? Pin { get; set; }
        public string? Password { get; set; }
        [MaxLength(15, ErrorMessage = "FirstName cannot exceed 15 characters.")]
        public string? FirstName { get; set; }
        [MaxLength(15, ErrorMessage = "MiddleName cannot exceed 15 characters.")]
        public string? MiddleName { get; set; }
        [MaxLength(15, ErrorMessage = "LastName cannot exceed 15 characters.")]
        public string? LastName { get; set; }
        public string? Gender { get; set; }
        public string? DateOfBirth { get; set; }
        public SignupMethod SignupMethod { get; set; } = SignupMethod.PhoneNumber;
        [MaxLength(11, ErrorMessage = "Phone number cannot exceed 11 characters.")]
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Location { get; set; }
        public string? State { get; set; }
        public string? Lga { get; set; }
        public string? Ward { get; set; }
      
    }

    public class AgentSummaryDto
    {
        public int TotalAgents { get; set; }
        public int TotalActiveAgents { get; set; }
        public int TotalPendingVerificationAgents { get; set; }
        public int TotalSuspendedAgents { get; set; }
    }
    public class AgentListItemDto
    {
        public long Id { get; set; }
        public string AgentId { get; set; } = default!;   // FMR-1712
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTimeOffset RegDate { get; set; }
        public string Status { get; set; } = default!;
   public int Organizations { get; set; }
        public int Farmers { get; set; }
    }

    public class AgentListQuery
    {
        public long BusinessId { get; set; }
        public string? Search { get; set; }              // name, ID (FMR-1712 or 1712) or phone
        public AgentStatus? Status { get; set; }
        public string? OrganizationCode { get; set; }
        public DateTimeOffset? From { get; set; }
        public DateTimeOffset? To { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
