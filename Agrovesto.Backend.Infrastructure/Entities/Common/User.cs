
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.V2.Onboarding;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Common
{
    public class User : IdentityUser
    {
        public FarmerOnboardingStatus FarmerOnboardingStatus { get; set; }
        public string? OnboardingError { get; set; }
        public string? Salt { get; set; }
        public string? PhoneNumber { get; set; }
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
        public string? Name { get; set; }
        public string? BusinessCode { get; set; }
        public string? NIN { get; set; }
        public string? BVN { get; set; }
        public ICollection<UserOrganization> UserOrganizations { get; set; }
       // public UserLevel UserLevel { get; set; }
        public bool IsPhoneVerified { get; set; } = false;
        public bool IsEmailVerified { get; set; } = false;

        public bool IsPinEnabled { get; set; } = false;
    }

    public class UserOrganization
    {
        public string UserId { get; set; }
        public User User { get; set; }

        public string OrganizationCode { get; set; }
    }
    public class OneTimePassword
    {
        public long Id { get; set; }

        public string UserId { get; set; }
        public User User { get; set; }

        public string CodeHash { get; set; } // NEVER store raw OTP
        public OtpTarget Target { get; set; } // Email | Phone
        public string Destination { get; set; }

        public OtpPurpose Purpose { get; set; }

        public DateTime ExpiresAt { get; set; }
        public DateTime? UsedAt { get; set; }

        public int AttemptCount { get; set; }
        public DateTime CreatedAt { get; set; }

        public bool IsUsed => UsedAt.HasValue;
    }

}
