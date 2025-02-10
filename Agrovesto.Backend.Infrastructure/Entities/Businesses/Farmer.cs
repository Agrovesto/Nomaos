using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses
{
    public class Farmer
    {
        public string BelongToBusinessCode { get; set; }  //which business
        public string Name { get; set; }
        public string Location { get; set; }
        public string State { get; set; }
        public string Lga { get; set; }
        public string PhoneNumber { get; set; }
        public string? Email { get; set; }       
        public string UserId { get; set; }       
        public long Id { get; set; }       
        public DateTimeOffset OnBoardedOn { get; set; } = DateTimeOffset.Now;
        public string? Gender { get; set; }
        public string? DateOfBirth { get; set; }
        public string OnboardedByUserId { get; set; } //if individual farmers register, automatically assign them to Agrovesto
    }
}
