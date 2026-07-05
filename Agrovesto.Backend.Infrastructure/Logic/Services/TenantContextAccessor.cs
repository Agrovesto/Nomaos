using Agrovesto.Backend.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Services
{
    public interface ITenantContextAccessor
    {
        TenantContext Current { get; set; }
    }
    public class TenantContextAccessor : ITenantContextAccessor
    {
        public TenantContext Current { get; set; }
    }
}
