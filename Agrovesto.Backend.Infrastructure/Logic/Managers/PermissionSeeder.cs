using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers
{

    // =====================================================================================
    // 3) SEEDER: run once at startup (idempotent, safe to run on every deploy).
    // =====================================================================================
    public static class PermissionSeeder
    {
        // ASSUMPTION: these must match the Role.Name values already in your Roles table.
        public const string SuperAdminRole = "Super Admin";

        /// <summary>
        /// Starting permissions for the other roles. Applied ONLY while a role has no permissions at all,
        /// so changes you later make to a role (via SQL or a role-editing screen) are never overwritten.
        /// Treat this as a starting point; decide the real matrix with your team.
        /// </summary>
        private static readonly Dictionary<string, string[]> DefaultRoleMap = new()
        {
            ["SystemAdmin"] =
            [
                Permissions.AdminPanel.Access, Permissions.Dashboard.View,
                
            Permissions.Farmers.View, Permissions.Farmers.Manage,
            Permissions.Agents.View, Permissions.Agents.Manage,
            Permissions.Organizations.View, Permissions.Organizations.Manage,
            Permissions.Memberships.View, Permissions.Memberships.Manage,
            Permissions.ServiceProviders.View, Permissions.ServiceProviders.Manage,
            Permissions.Users.View
            ],
            ["Super Admin"] =
            [
                Permissions.AdminPanel.Access, Permissions.Dashboard.View,
                
            Permissions.Farmers.View, Permissions.Farmers.Manage,
            Permissions.Agents.View, Permissions.Agents.Manage,
            Permissions.Organizations.View, Permissions.Organizations.Manage,
            Permissions.Memberships.View, Permissions.Memberships.Manage,
            Permissions.ServiceProviders.View, Permissions.ServiceProviders.Manage,
            Permissions.Users.View
            ],
            ["Admin"] =
            [
                Permissions.AdminPanel.Access, Permissions.Dashboard.View,
            Permissions.Farmers.View, Permissions.Farmers.Manage,
            Permissions.Agents.View, Permissions.Agents.Manage,
            Permissions.Organizations.View, Permissions.Organizations.Manage,
            Permissions.Memberships.View, Permissions.Memberships.Manage,
            Permissions.ServiceProviders.View, Permissions.ServiceProviders.Manage,
            Permissions.Users.View
            ],
            ["Operations"] =
            [
                Permissions.AdminPanel.Access, Permissions.Dashboard.View,
            Permissions.Farmers.View, Permissions.Agents.View, Permissions.Organizations.View,
            Permissions.Memberships.View, Permissions.ServiceProviders.View
            ]
        };
         
        public static async Task SeedAsync(DataContext db, CancellationToken ct = default)
        {
            // a) insert any catalog permission that isn't in the table yet
            var existing = await db.Permissions.Select(p => p.Name).ToListAsync(ct);
            var missing = Permissions.All.Where(n => !existing.Contains(n, StringComparer.OrdinalIgnoreCase));

            db.Permissions.AddRange(missing.Select(n => new Permission
            {
                Name = n,
                Group = n.Split('.')[0],
                Description = n
            }));
            await db.SaveChangesAsync(ct);

            var permissionIds = await db.Permissions.ToDictionaryAsync(p => p.Name, p => p.Id, ct);

            // b) Super Admin always holds every permission (so new permissions reach them automatically)
            var superAdmin = await db.Roles.FirstOrDefaultAsync(r => r.Name == SuperAdminRole, ct);
            if (superAdmin != null)
            {
                var has = await db.RolePermissions
                    .Where(rp => rp.RoleId == superAdmin.Id)
                    .Select(rp => rp.PermissionId)
                    .ToListAsync(ct);

                db.RolePermissions.AddRange(permissionIds.Values
                    .Except(has)
                    .Select(id => new RolePermission { RoleId = superAdmin.Id, PermissionId = id }));
            }

            // c) starting permissions for other roles, only if the role has none yet
            foreach (var (roleName, names) in DefaultRoleMap)
            {
                var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName, ct);
                if (role == null) continue;

                var alreadyHasAny = await db.RolePermissions.AnyAsync(rp => rp.RoleId == role.Id, ct);
                if (alreadyHasAny) continue;

                db.RolePermissions.AddRange(names
                    .Where(permissionIds.ContainsKey)
                    .Select(n => new RolePermission { RoleId = role.Id, PermissionId = permissionIds[n] }));
            }

            await db.SaveChangesAsync(ct);
        }
    }
}
