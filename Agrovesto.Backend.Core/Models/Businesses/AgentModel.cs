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
        public string? OrganizationCode { get; set; }
        public string? OnboardedById { get; set; }
    }
}
