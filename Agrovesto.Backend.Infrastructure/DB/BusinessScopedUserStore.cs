using Agrovesto.Backend.Infrastructure.Entities.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.DB
{
    /// <summary>
    /// Identity's default EF user store creates an ApplicationUserRole with only UserId and RoleId.
    /// The admin-panel schema also requires a BusinessId and permits one role assignment per business,
    /// so role membership writes must resolve and use the user's business context explicitly.
    /// </summary>
    public sealed class BusinessScopedUserStore : UserStore<User, Role, DataContext, string,
        IdentityUserClaim<string>, ApplicationUserRole, IdentityUserLogin<string>,
        IdentityUserToken<string>, IdentityRoleClaim<string>>
    {
        public BusinessScopedUserStore(DataContext context, IdentityErrorDescriber describer)
            : base(context, describer)
        {
        }

        public override async Task AddToRoleAsync(
            User user, string normalizedRoleName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(user);

            if (string.IsNullOrWhiteSpace(normalizedRoleName))
                throw new ArgumentException("Role name is required.", nameof(normalizedRoleName));

            var role = await Context.Roles
                .FirstOrDefaultAsync(r => r.NormalizedName == normalizedRoleName, cancellationToken);
            if (role is null)
                throw new InvalidOperationException($"Role '{normalizedRoleName}' does not exist.");

            var businessId = await ResolveBusinessIdAsync(user, role, cancellationToken);
            if (businessId is null)
            {
                throw new InvalidOperationException(
                    $"Cannot assign role '{role.Name}' to user '{user.Id}' without an active business context.");
            }

            var assignments = Context.UserRoles;
            var trackedAssignment = assignments.Local.FirstOrDefault(x =>
                x.UserId == user.Id && x.RoleId == role.Id && x.BusinessId == businessId.Value);
            var existingAssignment = trackedAssignment ?? await assignments.FirstOrDefaultAsync(x =>
                x.UserId == user.Id && x.RoleId == role.Id && x.BusinessId == businessId.Value,
                cancellationToken);

            if (existingAssignment is not null)
            {
                existingAssignment.IsActive = true;
                return;
            }

            assignments.Add(new ApplicationUserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                BusinessId = businessId.Value,
                IsActive = true
            });
        }

        public override async Task<IList<string>> GetRolesAsync(
            User user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(user);

            var businessId = await ResolveUserBusinessIdAsync(user, cancellationToken);
            if (businessId is null)
                return new List<string>();

            return await Context.UserRoles
                .Where(assignment => assignment.UserId == user.Id &&
                                     assignment.BusinessId == businessId.Value &&
                                     assignment.IsActive)
                .Select(assignment => assignment.Role.Name!)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        public override async Task<bool> IsInRoleAsync(
            User user, string normalizedRoleName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(user);

            if (string.IsNullOrWhiteSpace(normalizedRoleName))
                throw new ArgumentException("Role name is required.", nameof(normalizedRoleName));

            var role = await Context.Roles
                .FirstOrDefaultAsync(r => r.NormalizedName == normalizedRoleName, cancellationToken);
            if (role is null)
                return false;

            var businessId = await ResolveBusinessIdAsync(user, role, cancellationToken);
            if (businessId is null)
                return false;

            return await Context.UserRoles.AnyAsync(x =>
                x.UserId == user.Id && x.RoleId == role.Id &&
                x.BusinessId == businessId.Value && x.IsActive,
                cancellationToken);
        }

        public override async Task<IList<User>> GetUsersInRoleAsync(
            string normalizedRoleName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(normalizedRoleName))
                throw new ArgumentException("Role name is required.", nameof(normalizedRoleName));

            var role = await Context.Roles
                .FirstOrDefaultAsync(r => r.NormalizedName == normalizedRoleName, cancellationToken);
            if (role is null)
                return new List<User>();

            return await Context.Users
                .Where(user => Context.UserRoles.Any(assignment =>
                    assignment.UserId == user.Id &&
                    assignment.RoleId == role.Id &&
                    assignment.IsActive))
                .ToListAsync(cancellationToken);
        }

        public override async Task RemoveFromRoleAsync(
            User user, string normalizedRoleName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(user);

            if (string.IsNullOrWhiteSpace(normalizedRoleName))
                throw new ArgumentException("Role name is required.", nameof(normalizedRoleName));

            var role = await Context.Roles
                .FirstOrDefaultAsync(r => r.NormalizedName == normalizedRoleName, cancellationToken);
            if (role is null)
                return;

            var businessId = await ResolveBusinessIdAsync(user, role, cancellationToken);
            if (businessId is null)
                return;

            var matches = await Context.UserRoles
                .Where(x => x.UserId == user.Id && x.RoleId == role.Id && x.BusinessId == businessId.Value)
                .ToListAsync(cancellationToken);

            // Also update rows created earlier in this unit of work but not yet persisted.
            matches.AddRange(Context.UserRoles.Local
                .Where(x => x.UserId == user.Id && x.RoleId == role.Id &&
                            x.BusinessId == businessId.Value && !matches.Contains(x)));
            foreach (var assignment in matches)
                assignment.IsActive = false;
        }

        protected override Task<ApplicationUserRole?> FindUserRoleAsync(
            string userId, string roleId, CancellationToken cancellationToken)
        {
            // Identity's default implementation uses FindAsync(userId, roleId), which is incompatible
            // with this entity's three-part (UserId, RoleId, BusinessId) key.
            return Context.UserRoles.FirstOrDefaultAsync(
                x => x.UserId == userId && x.RoleId == roleId,
                cancellationToken);
        }

        private async Task<long?> ResolveBusinessIdAsync(
            User user, Role role, CancellationToken cancellationToken)
        {
            if (role.BusinessId is > 0)
                return role.BusinessId.Value;

            return await ResolveUserBusinessIdAsync(user, cancellationToken);
        }

        private async Task<long?> ResolveUserBusinessIdAsync(
            User user, CancellationToken cancellationToken)
        {
            var membershipBusinessId = await Context.UserBusinesses
                .Where(x => x.UserId == user.Id && x.IsActive)
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.BusinessId)
                .Select(x => (long?)x.BusinessId)
                .FirstOrDefaultAsync(cancellationToken);
            if (membershipBusinessId is > 0)
                return membershipBusinessId.Value;

            if (string.IsNullOrWhiteSpace(user.BusinessCode))
                return null;

            return await Context.Businesses
                .Where(x => x.BusinessCode == user.BusinessCode.Trim())
                .Select(x => (long?)x.BusinessId)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
