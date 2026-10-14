using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Admin
{
    public class UpdateAdminUserRequest
    {
        [Required(ErrorMessage = "First name is required."), StringLength(100)]
        public string FirstName { get; set; } = default!;

        [StringLength(100)]
        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "Last name is required."), StringLength(100)]
        public string LastName { get; set; } = default!;

        [Required(ErrorMessage = "Phone number is required.")]
        public string PhoneNumber { get; set; } = default!;

        [StringLength(100)]
        public string? Department { get; set; }

        [Required(ErrorMessage = "Please select a role.")]
        public string RoleId { get; set; } = default!;

        //you can't edit email
    }
}
