using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Admin
{
    public record AdminUserDetailDto(
     string UserId, string AdminCode,
     string FullName, string FirstName, string? MiddleName, string LastName,
     string Status, string? Department, IReadOnlyList<string> Roles,
     // Personal information
     string Email, string? PhoneNumber,
     //string? PhoneNumberDisplay,
     // Account information
     DateTimeOffset AccountCreated, string? CreatedBy, DateTimeOffset? LastUpdated, DateTimeOffset? LastLogin,
     // Registration information
     string RegistrationChannel, string? VerifiedBy, DateTimeOffset? VerificationDate,
     // Which buttons the header should show
     IReadOnlyList<string> Actions
     , string? RoleId
        );

    public record UserPermissionDto(string Name, string? Group, string? Description);
    public record UserRoleAccessDto(string RoleId, string RoleName, string? Description, IReadOnlyList<UserPermissionDto> Permissions);
    public record PermissionGroupDto(string Group, IReadOnlyList<string> Permissions);
    public record AdminUserAccessDto(IReadOnlyList<UserRoleAccessDto> Roles
        // ,
        //IReadOnlyList<PermissionGroupDto> EffectivePermissions
        );

    public record AdminActivityDto(long Id, string Action, string Module, string? Details, string? ActorName, DateTimeOffset Date, string Record, string? IpAddress, string? Browser, string? OperatingSystem, string? DeviceType, string? DeviceName);
    public record AdminLoginHistoryDto(long Id, DateTimeOffset LoggedInAt, bool Succeeded, string? FailureReason, string? IpAddress, string? Browser, string? OperatingSystem, string? DeviceType, string? DeviceName);

    public static class AdminActiviLogModule
    {
        public static class AdminPanel {
            public const string Dashboard = "Dashboard";
            public const string UserMgt = "User Management";
            public const string Profile = "Profile";
            public const string Settings = "Settings";
            public const string Farmer = "Farmer Management";
            public const string Agent = "Agent Management";
            public const string Membership = "Membership Management";
            public const string Organization = "Organization Management";
        }

    }
    public static class UserActiviLogModule
    {
        public static class FarmerPanel
        {
            public const string Dashboard = "Dashboard";
            public const string UserMgt = "User Management";
            public const string Profile = "Profile";
            public const string Settings = "Settings";
        }
        public static class MembershipPanel
        {
            public const string Dashboard = "Dashboard";
            public const string UserMgt = "User Management";
            public const string Profile = "Profile";
            public const string Settings = "Settings";
        }
        public static class AgentPanel
        {
            public const string Dashboard = "Dashboard";
            public const string UserMgt = "User Management";
            public const string Profile = "Profile";
            public const string Settings = "Settings";
        }
        public static class OrganizationPanel
        {
            public const string Dashboard = "Dashboard";
            public const string UserMgt = "User Management";
            public const string Profile = "Profile";
            public const string Settings = "Settings";
        }

    }
}
