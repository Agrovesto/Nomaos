using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Common
{
    public class UserModel
    {
        public string? Id { get; set; }
        public string? Salt { get; set; }
        public string PhoneNumber { get; set; }
        public string? Pin { get; set; }
        public string? Password { get; set; }
        public string Email { get; set; }
        public UserType UserType { get; set; }
        public string? OTP { get; set; }
        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now;
        public string? CreatedBy { get; set; }
        public DateTimeOffset? DateModified { get; set; } = DateTimeOffset.Now;
        public string? ModifiedBy { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset? LastLoginDate { get; set; } = DateTimeOffset.Now;
        public string? Name { get; set; }

    }
    public enum UserType
    {
        Individual,
        Business,
        Farmer,
        Cooperative,
        Agent,
        Suppliers,
        Processors,
        Agrovesto,
        Buyer
    }
    public class LoginModel
    {
        public string? Username { get; set; }
        public string? password { get; set; }
        public string? phonenumber { get; set; }
        public string? pin { get; set; }
        public LoginType LoginMethod { get; set; }
    }

    public enum LoginType
    {
        Password,
        Pin
    }
}
