using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Common
{
    public class User : IdentityUser<string>
    {
        public string? Salt { get; set; }
        public string PhoneNumber { get; set; }
        public string? Pin { get; set; } 
        public string? Password { get; set; }         
        public UserType UserType { get; set; }
        public string? WalletId { get; set; }
        public string? OTP { get; set; }
        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now;
        public string? CreatedBy { get; set; }
        public DateTimeOffset? DateModified { get; set; }
        public string? ModifiedBy { get; set; }
        public bool IsActive { get; set; }
        public string? Bvn { get; set; }
        public string? DOB { get; set; }
        public string? NIN { get; set; }
        public DateTimeOffset? LastLoginDate { get; set; }
    }
    public enum UserType
    {
        Individual,
        Business,
        Farmer,
        Cooperative,
        Agent,
        Suppliers,
        Agrovesto,
    }
}
