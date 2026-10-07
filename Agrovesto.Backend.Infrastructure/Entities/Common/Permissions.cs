using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Entities.Common
{

    // =====================================================================================
    // 1) THE CATALOG: every permission the system knows about, defined once in code.
    //    Naming convention: Resource.Action
    // =====================================================================================
    public static class Permissions
    {
        public static class AdminPanel { public const string Access = "AdminPanel.Access"; }
        public static class Dashboard { public const string View = "Dashboard.View"; }
        public static class Farmers { public const string View = "Farmers.View"; public const string Manage = "Farmers.Manage"; }
        public static class Agents { public const string View = "Agents.View"; public const string Manage = "Agents.Manage"; }
        public static class Organizations { public const string View = "Organizations.View"; public const string Manage = "Organizations.Manage"; }
        public static class Memberships { public const string View = "Memberships.View"; public const string Manage = "Memberships.Manage"; }
        public static class ServiceProviders { public const string View = "ServiceProviders.View"; public const string Manage = "ServiceProviders.Manage"; }
        public static class Users { public const string View = "Users.View"; public const string Manage = "Users.Manage"; }
        public static class Roles { public const string Manage = "Roles.Manage"; }

        /// <summary>Every constant above, discovered by reflection so you never maintain a second list.</summary>
        public static IReadOnlyList<string> All { get; } = typeof(Permissions)
            .GetNestedTypes(BindingFlags.Public)
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();
    }


    // =====================================================================================
    // 2) ENTITIES (two new tables). ASSUMPTION: Role.Id is int. Change to Guid if needed.
    // =====================================================================================
    public class Permission
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;        // "Farmers.View"
        public string? Group { get; set; }                   // "Farmers"  (handy for a role-editing UI)
        public string? Description { get; set; }
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }

    public class RolePermission
    {
        public string RoleId { get; set; }
        public Role Role { get; set; } = default!;
        public int PermissionId { get; set; }
        public Permission Permission { get; set; } = default!;
    }


}
