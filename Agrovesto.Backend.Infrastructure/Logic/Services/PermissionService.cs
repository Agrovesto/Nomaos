using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Logic.Services.RolePermissionService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
        public PermissionService(DataContext db, IMemoryCache cache, IPermissionCacheInvalidator cacheInvalidator) { _db = db; _cache = cache; _cacheInvalidator = cacheInvalidator; }

        public async Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken ct = default)
        {
            var perms = await _cache.GetOrCreateAsync($"perms:{userId}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);   // short, so role or status changes take effect quickly
                entry.AddExpirationToken(_cacheInvalidator.GetToken());

                var names = await (from u in _db.Users
                                   join ur in _db.UserRoles on u.Id equals ur.UserId
                                   join rp in _db.RolePermissions on ur.RoleId equals rp.RoleId
                                   where u.Id == userId && u.IsActive && ur.IsActive
                                   select rp.Permission.Name)
                                  .Distinct().ToListAsync(ct);

                return names.ToHashSet(StringComparer.OrdinalIgnoreCase);
            });

            return perms!.Contains(permission);
        }
    }
}
