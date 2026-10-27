using Agrovesto.Backend.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Admin
{
    internal class AdminOrganizationModule
    {
    }
    public class OrganizationListItemDto
    {
        public long Id { get; set; }
        public string OrganizationCode { get; set; } = default!;   // FMR-1712
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTimeOffset RegDate { get; set; }
        public string Status { get; set; } = default!;
        public string? OrganizationName { get; set; }
        public int Memberships { get; set; }
        public int Farmers { get; set; }
        public int Agents { get; set; }
    }

    public class OrganizationListQuery
    {
        public long BusinessId { get; set; }
        public string? Search { get; set; }              // name, ID (FMR-1712 or 1712) or phone
        public OrganizationStatus? Status { get; set; }
        public string? OrganizationCode { get; set; }
        public DateTimeOffset? From { get; set; }
        public DateTimeOffset? To { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
