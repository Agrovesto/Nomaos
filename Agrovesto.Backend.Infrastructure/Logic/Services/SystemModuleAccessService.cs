using Agrovesto.Backend.Infrastructure.DB;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Services
{
    public interface IModuleAccessService
    {
        Task<bool> HasAccess(
            long businessId,
            string moduleCode);
    }
    public class SystemModuleAccessService : IModuleAccessService
    {
        private readonly DataContext _context;
        public SystemModuleAccessService(DataContext context)
        {
            _context = context;
        }
        public async Task<bool> HasAccess(
     long businessId,
     string moduleCode)
        {
            var now = DateTimeOffset.UtcNow;

            return await _context.BusinessLicenseModules
                .AnyAsync(x =>
                    x.IsEnabled &&
                    x.SystemModule.Code == moduleCode &&
                    x.BusinessLicense.BusinessId == businessId &&
                    x.BusinessLicense.IsActive &&
                    x.BusinessLicense.StartDate <= now &&
                    x.BusinessLicense.EndDate >= now);
        }
    }
}
