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
    public class Business
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long BusinessId { get; set; }
        [Required]
        public string BusinessCode { get; set; }
        public string UserId { get; set; }
        public string BusinessName { get; set; }
        public string ContactEmail { get; set; }
        public string SupportEmail { get; set; }
        public string DisputeEmail { get; set; }
        public string BusinessEmail { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string Website { get; set; }
        public string BVN { get; set; }
        public string NIN { get; set; }
        public string BusinessStatus { get; set; }
        public string Category { get; set; }
        public string KycCategory { get; set; }
        public string BusinessLogo { get; set; }
        /*
         This map to business categories in the UI
         */
        public ICollection<BusinessWorker> BusinessWorkers { get; set; }

        public DateTimeOffset CreatedOn { get; set; } = DateTime.Now;
        public ICollection<SubAccount> SubAccounts { get; set; } = new HashSet<SubAccount>(); //subaccounts are the business users
                                                                                              //  public List<Product> Products { get; set; } // product maps agrovesto offerings to the business configuration of selected products
        public ICollection<User> users { get; set; } = new List<User>();
    }

}
