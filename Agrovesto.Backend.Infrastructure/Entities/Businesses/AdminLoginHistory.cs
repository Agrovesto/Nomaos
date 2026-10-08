using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Businesses
{
    public class AdminLoginHistory
    {
        public long Id { get; set; }
        public string UserId { get; set; } = default!;
        public DateTimeOffset LoggedInAt { get; set; }
        public bool Succeeded { get; set; }
        public string? FailureReason { get; set; }     // "Incorrect password", "Account inactive"...
        public string? IpAddress { get; set; }
        [MaxLength(100)] public string? Browser { get; set; }          // "Chrome 129"
        [MaxLength(100)] public string? OperatingSystem { get; set; }  // "Windows 10", "Android 14"
        [MaxLength(20)] public string? DeviceType { get; set; }       // Desktop | Mobile | Tablet | Bot
        [MaxLength(150)] public string? DeviceName { get; set; }       // "Apple iPhone", "Samsung SM-S918B"
        [MaxLength(512)] public string? UserAgent { get; set; }        // AdminActivityLog only; raw string kept for forensics
    }

    public class AdminActivityLog
    {
        public long Id { get; set; }
        public string? Module { get; set; }
        public string? AffectedRecord { get; set; }
        public string? ActorUserId { get; set; }        // who did it
        public string? TargetUserId { get; set; }       // who it was done to (null if not user-specific)
        public string Action { get; set; } = default!;  // "UserInvited", "UserDeactivated", "RoleChanged"...
        public string? Details { get; set; }
        public DateTimeOffset OccurredAt { get; set; }
        public string? IpAddress { get; set; }
        [MaxLength(100)] public string? Browser { get; set; }          // "Chrome 129"
        [MaxLength(100)] public string? OperatingSystem { get; set; }  // "Windows 10", "Android 14"
        [MaxLength(20)] public string? DeviceType { get; set; }       // Desktop | Mobile | Tablet | Bot
        [MaxLength(150)] public string? DeviceName { get; set; }       // "Apple iPhone", "Samsung SM-S918B"
        [MaxLength(512)] public string? UserAgent { get; set; }        // AdminActivityLog only; raw string kept for forensics
    }
}
