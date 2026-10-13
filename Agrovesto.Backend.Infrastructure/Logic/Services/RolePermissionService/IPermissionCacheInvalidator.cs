using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Services.RolePermissionService
{
    public interface IPermissionCacheInvalidator { IChangeToken GetToken(); void InvalidateAll(); }

    public class PermissionCacheInvalidator : IPermissionCacheInvalidator
    {
        private CancellationTokenSource _cts = new();
        public IChangeToken GetToken() => new CancellationChangeToken(Volatile.Read(ref _cts).Token);
        public void InvalidateAll() => Interlocked.Exchange(ref _cts, new CancellationTokenSource()).Cancel();
    }
}
