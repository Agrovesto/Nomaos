using Agrovesto.Backend.Core.Models.Admin;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.Logic.Services.RolePermissionService;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2.AdminManager
{
    public interface IRoleAccessManager
    {
        Task<AccessResult<IReadOnlyList<RoleListItemDto>>> ListRolesAsync(CancellationToken ct);
        Task<AccessResult<RoleDetailDto>> GetRoleAsync(string id, CancellationToken ct);
        Task<AccessResult<RoleDetailDto>> CreateRoleAsync(CreateRoleRequest req, CancellationToken ct);
        Task<AccessResult<RoleDetailDto>> UpdateRoleAsync(string id, SaveRoleRequest req, CancellationToken ct);
        Task<AccessResult<object>> DeleteRoleAsync(string id, CancellationToken ct);
        Task<AccessResult<RoleDetailDto>> SetRolePermissionsAsync(string id, SetRolePermissionsRequest req, CancellationToken ct);
        Task<AccessResult<IReadOnlyList<PermissionDto>>> ListPermissionsAsync(CancellationToken ct);
        Task<AccessResult<PermissionDto>> CreatePermissionAsync(CreatePermissionRequest req, CancellationToken ct);
        Task<AccessResult<PermissionDto>> UpdatePermissionAsync(int id, UpdatePermissionRequest req, CancellationToken ct);
        Task<AccessResult<object>> DeletePermissionAsync(int id, CancellationToken ct);
    }
    public class RoleAccessManager : IRoleAccessManager
    {
        private readonly DataContext _db;
        private readonly RoleManager<Role> _roles;
        private readonly AdminPanelOptions _opts;
        private readonly IPermissionCacheInvalidator _cache;

        public RoleAccessManager(DataContext db, RoleManager<Role> roles,
            IOptions<AdminPanelOptions> opts, IPermissionCacheInvalidator cache)
        { _db = db; _roles = roles; _opts = opts.Value; _cache = cache; }

        private IQueryable<Role> AdminRoles =>
            _db.Roles.Where(r => r.IsAvailableToAdmin && (r.BusinessId == null || r.BusinessId == _opts.BusinessId));

        private static bool IsProtectedName(string? name) =>
            string.Equals(name, PermissionSeeder.SuperAdminRole, StringComparison.OrdinalIgnoreCase);

        private static bool IsSystem(string name) =>
            Permissions.All.Contains(name, StringComparer.OrdinalIgnoreCase);

        private static string Errors(IdentityResult r) => string.Join(" ", r.Errors.Select(e => e.Description));

        // ---------- Roles ----------

        public async Task<AccessResult<IReadOnlyList<RoleListItemDto>>> ListRolesAsync(CancellationToken ct)
        {
            var rows = await AdminRoles.AsNoTracking().OrderBy(r => r.Name)
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.Descriptions,
                    Perms = r.RolePermissions.Count,
                    Users = r.UserRoles.Count(ur => ur.IsActive)
                }).ToListAsync(ct);

            IReadOnlyList<RoleListItemDto> list = rows
                .Select(r => new RoleListItemDto(r.Id, r.Name!, r.Descriptions, r.Perms, r.Users, IsProtectedName(r.Name)))
                .ToList();
            return AccessResult.Ok(list);
        }

        public async Task<AccessResult<RoleDetailDto>> GetRoleAsync(string id, CancellationToken ct)
        {
            var role = await AdminRoles.AsNoTracking().Where(r => r.Id == id)
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.Descriptions,
                    Perms = r.RolePermissions.Select(rp => new { rp.Permission.Id, rp.Permission.Name, rp.Permission.Group, rp.Permission.Description })
                }).FirstOrDefaultAsync(ct);

            if (role is null) return AccessResult.Fail<RoleDetailDto>(404, "Role not found.");

            var perms = role.Perms.OrderBy(p => p.Group).ThenBy(p => p.Name)
                .Select(p => new PermissionDto(p.Id, p.Name, p.Group, p.Description, IsSystem(p.Name))).ToList();

            return AccessResult.Ok(new RoleDetailDto(role.Id, role.Name!, role.Descriptions, IsProtectedName(role.Name), perms));
        }

        public async Task<AccessResult<RoleDetailDto>> CreateRoleAsync(CreateRoleRequest req, CancellationToken ct)
        {
            var name = req.Name.Trim();
            var ids = (req.PermissionIds ?? new()).Distinct().ToList();

            if (await _db.Permissions.CountAsync(p => ids.Contains(p.Id), ct) != ids.Count)
                return AccessResult.Fail<RoleDetailDto>(400, "One or more permissions do not exist.");
            if (await _roles.RoleExistsAsync(name))
                return AccessResult.Fail<RoleDetailDto>(409, "A role with this name already exists.");

            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var role = new Role(name, req.Description?.Trim() ?? "", false)
            {
                Id = Guid.NewGuid().ToString(),        // IdentityRole<string> does not generate its own Id
                IsAvailableToAdmin = true,
                BusinessId = _opts.BusinessId
            };

            var created = await _roles.CreateAsync(role);
            if (!created.Succeeded) return AccessResult.Fail<RoleDetailDto>(400, Errors(created));

            _db.RolePermissions.AddRange(ids.Select(pid => new RolePermission { RoleId = role.Id, PermissionId = pid }));
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            _cache.InvalidateAll();

            var detail = await GetRoleAsync(role.Id, ct);
            return detail with { StatusCode = 201 };
        }

        public async Task<AccessResult<RoleDetailDto>> UpdateRoleAsync(string id, SaveRoleRequest req, CancellationToken ct)
        {
            var role = await AdminRoles.FirstOrDefaultAsync(r => r.Id == id, ct);
            if (role is null) return AccessResult.Fail<RoleDetailDto>(404, "Role not found.");

            var name = req.Name.Trim();
            if (IsProtectedName(role.Name) && !string.Equals(role.Name, name, StringComparison.Ordinal))
                return AccessResult.Fail<RoleDetailDto>(403, "The Super Admin role cannot be renamed.");

            var clash = await _db.Roles.AnyAsync(r => r.Id != id && r.Name!.ToLower() == name.ToLower(), ct);
            if (clash) return AccessResult.Fail<RoleDetailDto>(409, "A role with this name already exists.");

            role.Name = name;
            role.Descriptions = req.Description?.Trim() ?? "";
            var updated = await _roles.UpdateAsync(role);    // also refreshes NormalizedName
            if (!updated.Succeeded) return AccessResult.Fail<RoleDetailDto>(400, Errors(updated));

            return await GetRoleAsync(id, ct);
        }

        public async Task<AccessResult<object>> DeleteRoleAsync(string id, CancellationToken ct)
        {
            var role = await AdminRoles.FirstOrDefaultAsync(r => r.Id == id, ct);
            if (role is null) return AccessResult.Fail<object>(404, "Role not found.");
            if (IsProtectedName(role.Name)) return AccessResult.Fail<object>(403, "The Super Admin role cannot be deleted.");

            var active = await _db.UserRoles.CountAsync(ur => ur.RoleId == id && ur.IsActive, ct);
            if (active > 0)
                return AccessResult.Fail<object>(409, $"This role is assigned to {active} user(s). Reassign them before deleting it.");

            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            _db.UserRoles.RemoveRange(_db.UserRoles.Where(ur => ur.RoleId == id));          // inactive leftovers
            _db.RolePermissions.RemoveRange(_db.RolePermissions.Where(rp => rp.RoleId == id));
            await _db.SaveChangesAsync(ct);

            var deleted = await _roles.DeleteAsync(role);
            if (!deleted.Succeeded) return AccessResult.Fail<object>(400, Errors(deleted));

            await tx.CommitAsync(ct);
            _cache.InvalidateAll();
            return AccessResult.Ok<object>(null!);
        }

        public async Task<AccessResult<RoleDetailDto>> SetRolePermissionsAsync(string id, SetRolePermissionsRequest req, CancellationToken ct)
        {
            var role = await AdminRoles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.Id == id, ct);
            if (role is null) return AccessResult.Fail<RoleDetailDto>(404, "Role not found.");
            if (IsProtectedName(role.Name))
                return AccessResult.Fail<RoleDetailDto>(403, "Super Admin always holds every permission and cannot be changed.");

            var ids = req.PermissionIds.Distinct().ToList();
            if (await _db.Permissions.CountAsync(p => ids.Contains(p.Id), ct) != ids.Count)
                return AccessResult.Fail<RoleDetailDto>(400, "One or more permissions do not exist.");

            var current = role.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();
            _db.RolePermissions.RemoveRange(role.RolePermissions.Where(rp => !ids.Contains(rp.PermissionId)));
            _db.RolePermissions.AddRange(ids.Where(pid => !current.Contains(pid))
                .Select(pid => new RolePermission { RoleId = id, PermissionId = pid }));

            await _db.SaveChangesAsync(ct);
            _cache.InvalidateAll();
            return await GetRoleAsync(id, ct);
        }

        // ---------- Permissions ----------

        public async Task<AccessResult<IReadOnlyList<PermissionDto>>> ListPermissionsAsync(CancellationToken ct)
        {
            var rows = await _db.Permissions.AsNoTracking().OrderBy(p => p.Group).ThenBy(p => p.Name).ToListAsync(ct);
            IReadOnlyList<PermissionDto> list = rows
                .Select(p => new PermissionDto(p.Id, p.Name, p.Group, p.Description, IsSystem(p.Name))).ToList();
            return AccessResult.Ok(list);
        }

        public async Task<AccessResult<PermissionDto>> CreatePermissionAsync(CreatePermissionRequest req, CancellationToken ct)
        {
            var name = req.Name.Trim();
            if (!Regex.IsMatch(name, @"^[A-Za-z]+(\.[A-Za-z]+)+$"))
                return AccessResult.Fail<PermissionDto>(400, "Permission names use the format Resource.Action, for example Reports.View.");
            if (await _db.Permissions.AnyAsync(p => p.Name.ToLower() == name.ToLower(), ct))
                return AccessResult.Fail<PermissionDto>(409, "A permission with this name already exists.");

            var permission = new Permission
            {
                Name = name,
                Group = string.IsNullOrWhiteSpace(req.Group) ? name.Split('.')[0] : req.Group.Trim(),
                Description = string.IsNullOrWhiteSpace(req.Description) ? name : req.Description.Trim()
            };
            _db.Permissions.Add(permission);

            // Super Admin always holds every permission
            var superAdmin = await _db.Roles.FirstOrDefaultAsync(r => r.Name == PermissionSeeder.SuperAdminRole, ct);
            if (superAdmin is not null)
                _db.RolePermissions.Add(new RolePermission { RoleId = superAdmin.Id, Permission = permission });

            await _db.SaveChangesAsync(ct);
            _cache.InvalidateAll();

            return new(new PermissionDto(permission.Id, permission.Name, permission.Group, permission.Description, IsSystem(permission.Name)), null, 201);
        }

        public async Task<AccessResult<PermissionDto>> UpdatePermissionAsync(int id, UpdatePermissionRequest req, CancellationToken ct)
        {
            var p = await _db.Permissions.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (p is null) return AccessResult.Fail<PermissionDto>(404, "Permission not found.");

            // Name is deliberately immutable: code and role mappings refer to it
            if (req.Group is not null) p.Group = req.Group.Trim();
            if (req.Description is not null) p.Description = req.Description.Trim();
            await _db.SaveChangesAsync(ct);

            return AccessResult.Ok(new PermissionDto(p.Id, p.Name, p.Group, p.Description, IsSystem(p.Name)));
        }

        public async Task<AccessResult<object>> DeletePermissionAsync(int id, CancellationToken ct)
        {
            var p = await _db.Permissions.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (p is null) return AccessResult.Fail<object>(404, "Permission not found.");
            if (IsSystem(p.Name))
                return AccessResult.Fail<object>(409, "Built-in permissions are defined in code and cannot be deleted.");

            var roles = await _db.RolePermissions.CountAsync(rp => rp.PermissionId == id, ct);
            if (roles > 0)
                return AccessResult.Fail<object>(409, $"This permission is assigned to {roles} role(s). Remove it from them first.");

            _db.Permissions.Remove(p);
            await _db.SaveChangesAsync(ct);
            return AccessResult.Ok<object>(null!);
        }
    }

}
