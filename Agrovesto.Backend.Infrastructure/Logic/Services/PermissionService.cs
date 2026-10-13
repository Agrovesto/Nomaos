using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Models.Admin;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.Logic.Services.RolePermissionService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Services
{
    public interface IPermissionService
    {
        Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken ct = default);
    }

    public class PermissionService : IPermissionService
    {
        private readonly IPermissionCacheInvalidator _cacheInvalidator;
        private readonly DataContext _db;
        private readonly IMemoryCache _cache;
        private readonly long _businessId;

        public PermissionService(DataContext db, IMemoryCache cache,
            IPermissionCacheInvalidator cacheInvalidator, IOptions<AdminPanelOptions> options)
        {
            _db = db;
            _cache = cache;
            _cacheInvalidator = cacheInvalidator;
            _businessId = options.Value.BusinessId;
        }

        public async Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken ct = default)
        {
            var perms = await _cache.GetOrCreateAsync($"perms:{_businessId}:{userId}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);   // short, so role or status changes take effect quickly
                entry.AddExpirationToken(_cacheInvalidator.GetToken());

                var names = await (from u in _db.Users
                                   join ur in _db.UserRoles on u.Id equals ur.UserId
                                   join rp in _db.RolePermissions on ur.RoleId equals rp.RoleId
                                   where u.Id == userId && u.IsAdminUser && u.IsActive &&
                                         (u.Status == null || u.Status == AdminUserStatus.Active) &&
                                         ur.BusinessId == _businessId && ur.IsActive &&
                                         ur.Role.IsAvailableToAdmin &&
                                         (ur.Role.BusinessId == null || ur.Role.BusinessId == _businessId)
                                   select rp.Permission.Name)
                                  .Distinct().ToListAsync(ct);

                return names.ToHashSet(StringComparer.OrdinalIgnoreCase);
            });

            return perms!.Contains(AdminPermissions.AccessPanel) && perms.Contains(permission);
        }
    }
}
