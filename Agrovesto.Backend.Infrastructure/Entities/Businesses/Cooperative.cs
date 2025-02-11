using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses
{
    public class Cooperative
    {
        public string Name { get; set; }
        public string Location { get; set; }
        public string State { get; set; }
        public string Lga { get; set; }
        public string ContactPerson { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string UserId { get; set; }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long Id { get; set; }
        public string BelongToBusinessCode { get; set; }  //which business

        public DateTimeOffset OnBoardedOn { get; set; } = DateTimeOffset.Now;

    }
}
