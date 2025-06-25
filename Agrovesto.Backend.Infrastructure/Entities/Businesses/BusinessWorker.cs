using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses
{
    public class BusinessWorker
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long BusinessWorkerId { get; set; }
        public string UserId { get; set; }

        public long BusinessId { get; set; }
        [ForeignKey("BusinessId")]
        public Business Business { get; set; }
        public bool IsOwner { get; set; } = false;
        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now;
        public string? OrganizationCode { get; set; }

    }
    //when business creates a businessworker,
    //create as a user, then use the id to create as a businessworker
    //assign role to the user as isBusinessWorker. 
}
