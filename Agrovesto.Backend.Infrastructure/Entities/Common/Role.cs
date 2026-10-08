using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Common
{
    public class Role : IdentityRole<string>
    {
        public Role()
        {

        }
        public Role(string name, string descriptions, bool isAvailableToBusiness) : base(name)
        {
            Descriptions = descriptions;
            IsAvailableToBusiness = isAvailableToBusiness;
        }
        public string Descriptions { get; set; }
        public bool IsAvailableToBusiness { get; set; }
        public bool IsAvailableToAdmin { get; set; } = false;
        //add permission

        /// <summary>   
        /// Navigation property for the users in this role.
        /// Uses the application's custom join entity so the model doesn't include
        /// the base IdentityUserRole<string> type.
        /// </summary>
        public virtual ICollection<ApplicationUserRole> Users { get; set; } = new List<ApplicationUserRole>();

        /// <summary>
        /// Navigation property for claims in this role.
        /// </summary>
        public virtual ICollection<IdentityRoleClaim<string>> Claims { get; set; } = new List<IdentityRoleClaim<string>>();

        public long? BusinessId { get; set; }
        public Business Business { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

        public ICollection<ApplicationUserRole> UserRoles { get; set; } = new List<ApplicationUserRole>();
    }

    public class ApplicationUserRole : IdentityUserRole<string>
    {
        public long BusinessId { get; set; }

        // Initialised to true because `new ApplicationUserRole()` is what creates rows.
        public bool IsActive { get; set; } = true;

        public User User { get; set; } = default!;
        public Role Role { get; set; } = default!;
        public Business Business { get; set; } = default!;
    }
}
