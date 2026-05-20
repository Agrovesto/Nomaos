using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Services
{
    public interface IMigrationService
    {
        Task RunOrganizationTableMigrationAsync();
        Task RunUserTableMigrationAsync();
    }
    public class MigrationService : IMigrationService
    {
        private readonly DataContext _context;

        public MigrationService(DataContext context)
        {
            _context = context;
        }
        public async Task RunOrganizationTableMigrationAsync()
        {
            // 1. Prevent re-running migration
            if (await _context.MigrationTrackers
                .AnyAsync(x =>
                    x.Name == "OrganizationBusinessMigration"
                    && x.Completed))
            {
                return;
            }

            // 2. Load data
            var organizations = await _context.Organizations.ToListAsync();
            var businesses = await _context.Businesses.ToListAsync();

            var organizationBusinesses = new List<OrganizationBusiness>();

            foreach (var org in organizations)
            {
                if (!string.IsNullOrWhiteSpace(org.BusinessCode))
                {
                    var business = businesses
                        .FirstOrDefault(x =>
                            x.BusinessCode == org.BusinessCode);

                    if (business != null)
                    {
                        organizationBusinesses.Add(new OrganizationBusiness
                        {
                            CreatedOn = DateTimeOffset.Now,
                            OrganizationId=org.OrganizationId,
                            BusinessId = business.BusinessId,
                            IsActive = true,
                            
                        });
                    }
                }
            }

            _context.OrganizationBusinesses.AddRange(organizationBusinesses);
            await _context.SaveChangesAsync();

            // 3. Mark migration as completed
            var tracker = new MigrationTracker
            {
                Name = "OrganizationBusinessMigration",
                Completed = true,
                RunOn = DateTimeOffset.UtcNow
            };

            _context.MigrationTrackers.Add(tracker);
            await _context.SaveChangesAsync();
        }

        public async Task RunUserTableMigrationAsync()
        {
            // 1. Prevent re-running migration
            if (await _context.MigrationTrackers
                .AnyAsync(x =>
                    x.Name == "UserBusinessMigration"
                    && x.Completed))
            {
                return;
            }

            // 2. Load data
            var users = await _context.Users.ToListAsync();
            var businesses = await _context.Businesses.ToListAsync();

            var userBusinesses = new List<UserBusiness>();

            foreach (var user in users)
            {
                if (!string.IsNullOrWhiteSpace(user.BusinessCode))
                {
                    var business = businesses
                        .FirstOrDefault(x =>
                            x.BusinessCode == user.BusinessCode);

                    if (business != null)
                    {
                        userBusinesses.Add(new UserBusiness
                        {
                            UserId = user.Id,
                            BusinessId = business.BusinessId,
                            IsDefault = true,
                            IsActive = true,
                            Role = user.UserType
                        });
                    }
                }
            }

            _context.UserBusinesses.AddRange(userBusinesses);
            await _context.SaveChangesAsync();

            // 3. Mark migration as completed
            var tracker = new MigrationTracker
            {
                Name = "UserBusinessMigration",
                Completed = true,
                RunOn = DateTimeOffset.UtcNow
            };

            _context.MigrationTrackers.Add(tracker);
            await _context.SaveChangesAsync();
        }
    }
}
