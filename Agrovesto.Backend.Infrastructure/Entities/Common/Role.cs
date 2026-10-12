using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Common
{
    public class Role : IdentityRole
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
        //add permission

        /// <summary>   
        /// Navigation property for the users in this role.
        /// </summary>
        public virtual ICollection<IdentityUserRole<string>> Users { get; set; } = new List<IdentityUserRole<string>>();

        /// <summary>
        /// Navigation property for claims in this role.
        /// </summary>
        public virtual ICollection<IdentityRoleClaim<string>> Claims { get; set; } = new List<IdentityRoleClaim<string>>();

        public long? BusinessId { get; set; }
        public Business Business { get; set; }
    }
}
