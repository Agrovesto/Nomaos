using Agrovesto.Backend.Infrastructure.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses
{
    public class SubAccount
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long SubAccountId { get; set; }
        public string UserId { get; set; }
        public string? BankAccount { get; set; }
        public string? BankName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? SubAccountName { get; set; }
        public string? WalletCode { get; set; }
        public long BusinessId { get; set; }
        public string Status { get; set; }
        public bool IsMainAccount { get; set; } //is main account is true for business, where as users account under the business is false
        public DateTimeOffset CreatedOn { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
        public string? Bvn { get; set; }
        public string? DOB { get; set; }
        public string? NIN { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string SubAccountType { get; set; }
        public string? OrganizationCode { get; set; }
        public bool IsNinVerified { get; set; } = false;
    }

    //when user signsup
    //create the user and get the userid
    //use the user id to create the user as subaccount under business
    //automatically assign isBusinessUser role to the user
}
