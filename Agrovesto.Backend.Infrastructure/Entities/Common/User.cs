
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Common
{
    public class User : IdentityUser
    {
        public string? Salt { get; set; }
        public string PhoneNumber { get; set; }
        public string? Pin { get; set; } 
        public string? PinEncrypt { get; set; }
        public string? Password { get; set; }         
        public string UserType { get; set; }
        public string? OTP { get; set; }
        public DateTimeOffset CreatedOn { get; set; } //= DateTimeOffset.Now;
        public string? CreatedBy { get; set; }
        public DateTimeOffset? DateModified { get; set; } = DateTimeOffset.Now;
        public string? ModifiedBy { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset? LastLoginDate { get; set; } = DateTimeOffset.Now;
        public string Name { get; set; }
        public string? BusinessCode { get; set; }
        public string? NIN { get; set; }
        public string? BVN { get; set; }
        public ICollection<UserOrganization> UserOrganizations { get; set; }
    }

    public class UserOrganization
    {
        public string UserId { get; set; }
        public User User { get; set; }

        public string OrganizationCode { get; set; }
    }

}
