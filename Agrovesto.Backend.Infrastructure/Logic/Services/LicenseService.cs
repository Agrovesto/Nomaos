using Agrovesto.Backend.Infrastructure.DB;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Services
{
    public interface ILicenseService
    {
        Task<bool> IsBusinessLicenseValid(long businessId);
    }
    public class LicenseService : ILicenseService
    {
        private readonly DataContext _context;
        public LicenseService(DataContext context)
        {
            _context = context;
        }
        public async Task<bool> IsBusinessLicenseValid(long businessId)
        {
            var now = DateTimeOffset.UtcNow;

            return await _context.BusinessLicenses
                .AnyAsync(x =>
                    x.BusinessId == businessId &&
                    x.IsActive &&
                    x.StartDate <= now &&
                    x.EndDate >= now);
        }
    }
}
