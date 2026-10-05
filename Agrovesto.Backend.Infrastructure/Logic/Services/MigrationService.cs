using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Agrovesto.Backend.Infrastructure.Logic.Services
{
    public record NamePreview(string UserId, string FullName, string First, string? Middle, string? Last);
    public record BackfillResult(int Candidates, int Updated, bool DryRun, List<NamePreview> Preview);

    public interface IMigrationService
    {
        Task RunInputFinancingCategoryTableMigrationAsync(string sourceBusinessCode, string newBusinessCode);
        Task RunLocationTypeTableMigrationAsync(string sourceBusinessCode, string newBusinessCode);
        Task RunMemberShipTypeTableMigrationAsync(string sourceBusinessCode, string newBusinessCode, string performedByUserId);
        Task RunModuleTableMigrationAsync(string sourceBusinessCode, string newBusinessCode, string performedByUserId);
        Task RunOrganizationProductCategoryTableMigrationAsync(string sourceBusinessCode, string newBusinessCode);

        //Task RunCommitmentPlanMigrationAsync();
        Task RunOrganizationTableMigrationAsync();
        Task RunStorageCategoryTableMigrationAsync(string sourceBusinessCode, string newBusinessCode, string performedByUserId);
        Task RunTemperatureControlTypeTableMigrationAsync(string sourceBusinessCode, string newBusinessCode, string performedByUserId);
        Task<BackfillResult?> RunUserNameBackfillAsync(bool dryRun, CancellationToken ct = default);

        //Task RunRepaymentPlanMigrationAsync();
        Task RunUserTableMigrationAsync();
    }
    public class MigrationService : IMigrationService
    {
        private readonly DataContext _context;
        private static readonly SemaphoreSlim BackfillLock = new(1, 1);
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
                            OrganizationId = org.OrganizationId,
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


        public async Task RunOrganizationProductCategoryTableMigrationAsync(
        string sourceBusinessCode,
        string newBusinessCode)
        {
            const string migrationName = "OrganizationProductCategories";

            if (await _context.MigrationTrackers
                .AnyAsync(x => x.Name == migrationName && x.Completed))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(sourceBusinessCode) || string.IsNullOrWhiteSpace(newBusinessCode))
            {
                throw new ArgumentException("Both sourceBusinessCode and newBusinessCode must be provided.");
            }

            var sourceBusiness = await _context.Businesses
                .FirstOrDefaultAsync(x => x.BusinessCode == sourceBusinessCode);

            var newBusiness = await _context.Businesses
                .FirstOrDefaultAsync(x => x.BusinessCode == newBusinessCode);

            if (sourceBusiness == null)
            {
                throw new InvalidOperationException($"Source business '{sourceBusinessCode}' was not found.");
            }

            if (newBusiness == null)
            {
                throw new InvalidOperationException($"Target business '{newBusinessCode}' was not found.");
            }

            var sourceCategories = await _context.OrganizationProductCategories
                .Where(x => x.BusinessCode == sourceBusinessCode)
                .ToListAsync();

            if (!sourceCategories.Any())
            {
                return; // nothing to copy
            }

            var existingTargetCategoryNames = await _context.OrganizationProductCategories
                .Where(x => x.BusinessCode == newBusinessCode)
                .Select(x => x.CategoryName)
                .ToListAsync();

            var newCategories = sourceCategories
                .Where(pd => !existingTargetCategoryNames.Contains(pd.CategoryName))
                .Select(pd => new OrganizationProductCategory
                {
                    CategoryName = pd.CategoryName,
                    BusinessCode = newBusinessCode,
                    IsActive = pd.IsActive,
                    CreatedOn = DateTimeOffset.UtcNow
                })
                .ToList();

            if (newCategories.Any())
            {
                _context.OrganizationProductCategories.AddRange(newCategories);
                await _context.SaveChangesAsync();
            }

            _context.MigrationTrackers.Add(new MigrationTracker
            {
                Name = migrationName,
                Completed = true,
                RunOn = DateTimeOffset.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        public async Task RunInputFinancingCategoryTableMigrationAsync(
        string sourceBusinessCode,
        string newBusinessCode)
        {
            const string migrationName = "InputFinancingCategories";
            if (await _context.MigrationTrackers
                .AnyAsync(x => x.Name == migrationName && x.Completed))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(sourceBusinessCode) || string.IsNullOrWhiteSpace(newBusinessCode))
            {
                throw new ArgumentException("Both sourceBusinessCode and newBusinessCode must be provided.");
            }

            var sourceBusiness = await _context.Businesses
                .FirstOrDefaultAsync(x => x.BusinessCode == sourceBusinessCode);

            var newBusiness = await _context.Businesses
                .FirstOrDefaultAsync(x => x.BusinessCode == newBusinessCode);

            if (sourceBusiness == null)
            {
                throw new InvalidOperationException($"Source business '{sourceBusinessCode}' was not found.");
            }

            if (newBusiness == null)
            {
                throw new InvalidOperationException($"Target business '{newBusinessCode}' was not found.");
            }

            var sourceCategories = await _context.InputFinancingCategories
                .Where(x => x.BusinessCode == sourceBusinessCode)
                .ToListAsync();

            if (!sourceCategories.Any())
            {
                return; // nothing to copy
            }

            var existingTargetCategoryNames = await _context.InputFinancingCategories
                .Where(x => x.BusinessCode == newBusinessCode)
                .Select(x => x.Name)
                .ToListAsync();

            var newCategories = sourceCategories
                .Where(fc => !existingTargetCategoryNames.Contains(fc.Name))
                .Select(fc => new InputFinancingCategory
                {
                    Name = fc.Name,
                    Description = fc.Description,
                    IsActive = fc.IsActive,
                    BusinessCode = newBusinessCode,
                    CreatedOn = DateTimeOffset.UtcNow
                })
                .ToList();

            if (newCategories.Any())
            {
                _context.InputFinancingCategories.AddRange(newCategories);
                await _context.SaveChangesAsync();
            }

            _context.MigrationTrackers.Add(new MigrationTracker
            {
                Name = migrationName,
                Completed = true,
                RunOn = DateTimeOffset.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        public async Task RunTemperatureControlTypeTableMigrationAsync(
        string sourceBusinessCode,
        string newBusinessCode,
        string performedByUserId)
        {
            const string migrationName = "TemperatureControlTypes";

            // 1. Prevent re-running migration
            if (await _context.MigrationTrackers
                .AnyAsync(x => x.Name == migrationName && x.Completed))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(sourceBusinessCode) || string.IsNullOrWhiteSpace(newBusinessCode))
            {
                throw new ArgumentException("Both sourceBusinessCode and newBusinessCode must be provided.");
            }

            // 2. Verify both businesses exist
            var sourceBusiness = await _context.Businesses
                .FirstOrDefaultAsync(x => x.BusinessCode == sourceBusinessCode);

            var newBusiness = await _context.Businesses
                .FirstOrDefaultAsync(x => x.BusinessCode == newBusinessCode);

            if (sourceBusiness == null)
            {
                throw new InvalidOperationException($"Source business '{sourceBusinessCode}' was not found.");
            }

            if (newBusiness == null)
            {
                throw new InvalidOperationException($"Target business '{newBusinessCode}' was not found.");
            }

            // 3. Load the source business's already-configured temperature control types
            var sourceTypes = await _context.TemperatureControlTypes
                .Where(x => x.BusinessCode == sourceBusinessCode)
                .ToListAsync();

            if (!sourceTypes.Any())
            {
                return; // nothing to copy
            }

            // 4. Avoid duplicating types the target business may already have
            var existingTargetNames = await _context.TemperatureControlTypes
                .Where(x => x.BusinessCode == newBusinessCode)
                .Select(x => x.Name)
                .ToListAsync();

            var newTypes = sourceTypes
                .Where(t => !existingTargetNames.Contains(t.Name))
                .Select(t => new TemperatureControlType
                {
                    Name = t.Name,
                    BusinessCode = newBusinessCode,
                    CreatedOn = DateTimeOffset.UtcNow,
                    CreatedByUserId = performedByUserId
                })
                .ToList();

            if (newTypes.Any())
            {
                _context.TemperatureControlTypes.AddRange(newTypes);
                await _context.SaveChangesAsync();
            }

            // 5. Mark migration as completed
            _context.MigrationTrackers.Add(new MigrationTracker
            {
                Name = migrationName,
                Completed = true,
                RunOn = DateTimeOffset.UtcNow
            });

            await _context.SaveChangesAsync();
        }
        public async Task RunStorageCategoryTableMigrationAsync(
        string sourceBusinessCode,
        string newBusinessCode,
        string performedByUserId)
        {
            const string migrationName = "StorageCategories";

            // 1. Prevent re-running migration
            if (await _context.MigrationTrackers
                .AnyAsync(x => x.Name == migrationName && x.Completed))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(sourceBusinessCode) || string.IsNullOrWhiteSpace(newBusinessCode))
            {
                throw new ArgumentException("Both sourceBusinessCode and newBusinessCode must be provided.");
            }

            // 2. Verify both businesses exist
            var sourceBusiness = await _context.Businesses
                .FirstOrDefaultAsync(x => x.BusinessCode == sourceBusinessCode);

            var newBusiness = await _context.Businesses
                .FirstOrDefaultAsync(x => x.BusinessCode == newBusinessCode);

            if (sourceBusiness == null)
            {
                throw new InvalidOperationException($"Source business '{sourceBusinessCode}' was not found.");
            }

            if (newBusiness == null)
            {
                throw new InvalidOperationException($"Target business '{newBusinessCode}' was not found.");
            }

            // 3. Load the source business's already-configured storage categories
            var sourceCategories = await _context.StorageCategories
                .Where(x => x.BusinessCode == sourceBusinessCode)
                .ToListAsync();

            if (!sourceCategories.Any())
            {
                return; // nothing to copy
            }

            // 4. Avoid duplicating categories the target business may already have
            var existingTargetNames = await _context.StorageCategories
                .Where(x => x.BusinessCode == newBusinessCode)
                .Select(x => x.Name)
                .ToListAsync();

            var newCategories = sourceCategories
                .Where(sc => !existingTargetNames.Contains(sc.Name))
                .Select(sc => new StorageCategory
                {
                    Name = sc.Name,
                    BusinessCode = newBusinessCode,
                    CreatedOn = DateTimeOffset.UtcNow,
                    CreatedByUserId = performedByUserId
                })
                .ToList();

            if (newCategories.Any())
            {
                _context.StorageCategories.AddRange(newCategories);
                await _context.SaveChangesAsync();
            }

            // 5. Mark migration as completed
            _context.MigrationTrackers.Add(new MigrationTracker
            {
                Name = migrationName,
                Completed = true,
                RunOn = DateTimeOffset.UtcNow
            });

            await _context.SaveChangesAsync();
        }
        public async Task RunLocationTypeTableMigrationAsync(
        string sourceBusinessCode,
        string newBusinessCode)
        {
            const string migrationName = "LocationTypes";

            // 1. Prevent re-running migration
            if (await _context.MigrationTrackers
                .AnyAsync(x => x.Name == migrationName && x.Completed))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(sourceBusinessCode) || string.IsNullOrWhiteSpace(newBusinessCode))
            {
                throw new ArgumentException("Both sourceBusinessCode and newBusinessCode must be provided.");
            }

            // 2. Verify both businesses exist
            var sourceBusiness = await _context.Businesses
                .FirstOrDefaultAsync(x => x.BusinessCode == sourceBusinessCode);

            var newBusiness = await _context.Businesses
                .FirstOrDefaultAsync(x => x.BusinessCode == newBusinessCode);

            if (sourceBusiness == null)
            {
                throw new InvalidOperationException($"Source business '{sourceBusinessCode}' was not found.");
            }

            if (newBusiness == null)
            {
                throw new InvalidOperationException($"Target business '{newBusinessCode}' was not found.");
            }

            // 3. Load the source business's already-configured location types
            var sourceTypes = await _context.LocationTypes
                .Where(x => x.BusinessCode == sourceBusinessCode)
                .ToListAsync();

            if (!sourceTypes.Any())
            {
                return; // nothing to copy
            }

            // 4. Avoid duplicating types the target business may already have
            var existingTargetTypes = await _context.LocationTypes
                .Where(x => x.BusinessCode == newBusinessCode)
                .Select(x => x.Type)
                .ToListAsync();

            var newTypes = sourceTypes
                .Where(lt => !existingTargetTypes.Contains(lt.Type))
                .Select(lt => new LocationType
                {
                    Type = lt.Type,
                    Description = lt.Description,
                    BusinessCode = newBusinessCode
                })
                .ToList();

            if (newTypes.Any())
            {
                _context.LocationTypes.AddRange(newTypes);
                await _context.SaveChangesAsync();
            }

            // 5. Mark migration as completed
            _context.MigrationTrackers.Add(new MigrationTracker
            {
                Name = migrationName,
                Completed = true,
                RunOn = DateTimeOffset.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        public async Task RunMemberShipTypeTableMigrationAsync(
        string sourceBusinessCode,
        string newBusinessCode,
        string performedByUserId)
        {
            const string migrationName = "MemberShipTypes";

            // 1. Prevent re-running migration
            if (await _context.MigrationTrackers
                .AnyAsync(x => x.Name == migrationName && x.Completed))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(sourceBusinessCode) || string.IsNullOrWhiteSpace(newBusinessCode))
            {
                throw new ArgumentException("Both sourceBusinessCode and newBusinessCode must be provided.");
            }

            // 2. Verify both businesses exist
            var sourceBusiness = await _context.Businesses
                .FirstOrDefaultAsync(x => x.BusinessCode == sourceBusinessCode);

            var newBusiness = await _context.Businesses
                .FirstOrDefaultAsync(x => x.BusinessCode == newBusinessCode);

            if (sourceBusiness == null)
            {
                throw new InvalidOperationException($"Source business '{sourceBusinessCode}' was not found.");
            }

            if (newBusiness == null)
            {
                throw new InvalidOperationException($"Target business '{newBusinessCode}' was not found.");
            }

            // 3. Load the source business's already-configured membership types
            var sourceTypes = await _context.MemberShipTypes
                .Where(x => x.BusinessCode == sourceBusinessCode)
                .ToListAsync();

            if (!sourceTypes.Any())
            {
                return; // nothing to copy
            }

            // 4. Avoid duplicating types the target business may already have
            var existingTargetNames = await _context.MemberShipTypes
                .Where(x => x.BusinessCode == newBusinessCode)
                .Select(x => x.Name)
                .ToListAsync();

            var now = DateTimeOffset.UtcNow;

            var newTypes = sourceTypes
                .Where(mt => !existingTargetNames.Contains(mt.Name))
                .Select(mt => new MemberShipType
                {
                    Name = mt.Name,
                    Description = mt.Description,
                    BusinessCode = newBusinessCode,
                    CreatedOn = now,
                    UpdatedOn = now,
                    CreatedBy = performedByUserId,
                    UpdatedBy = performedByUserId
                })
                .ToList();

            if (newTypes.Any())
            {
                _context.MemberShipTypes.AddRange(newTypes);
                await _context.SaveChangesAsync();
            }

            // 5. Mark migration as completed
            _context.MigrationTrackers.Add(new MigrationTracker
            {
                Name = migrationName,
                Completed = true,
                RunOn = DateTimeOffset.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        public async Task RunModuleTableMigrationAsync(
        string sourceBusinessCode,
        string newBusinessCode,
        string performedByUserId)
        {
            const string migrationName = "Modules";

            // 1. Prevent re-running migration
            if (await _context.MigrationTrackers
                .AnyAsync(x => x.Name == migrationName && x.Completed))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(sourceBusinessCode) || string.IsNullOrWhiteSpace(newBusinessCode))
            {
                throw new ArgumentException("Both sourceBusinessCode and newBusinessCode must be provided.");
            }

            // 2. Verify both businesses exist
            var sourceBusiness = await _context.Businesses
                .FirstOrDefaultAsync(x => x.BusinessCode == sourceBusinessCode);

            var newBusiness = await _context.Businesses
                .FirstOrDefaultAsync(x => x.BusinessCode == newBusinessCode);

            if (sourceBusiness == null)
            {
                throw new InvalidOperationException($"Source business '{sourceBusinessCode}' was not found.");
            }

            if (newBusiness == null)
            {
                throw new InvalidOperationException($"Target business '{newBusinessCode}' was not found.");
            }

            // 3. Load the source business's already-configured modules
            var sourceModules = await _context.Modules
                .Where(x => x.BusinessCode == sourceBusinessCode)
                .ToListAsync();

            if (!sourceModules.Any())
            {
                return; // nothing to copy
            }

            // 4. Avoid duplicating modules the target business may already have
            var existingTargetCodes = await _context.Modules
                .Where(x => x.BusinessCode == newBusinessCode)
                .Select(x => x.Code)
                .ToListAsync();

            var newModules = sourceModules
                .Where(m => !existingTargetCodes.Contains(m.Code))
                .Select(m => new Module
                {
                    Name = m.Name,
                    Code = m.Code,
                    BusinessCode = newBusinessCode,
                    CreatedOn = DateTimeOffset.UtcNow,
                    CreatedByUserId = performedByUserId
                })
                .ToList();

            if (newModules.Any())
            {
                _context.Modules.AddRange(newModules);
                await _context.SaveChangesAsync();
            }

            // 5. Mark migration as completed
            _context.MigrationTrackers.Add(new MigrationTracker
            {
                Name = migrationName,
                Completed = true,
                RunOn = DateTimeOffset.UtcNow
            });

            await _context.SaveChangesAsync();
        }
        //public async Task RunCommitmentPlanMigrationAsync()
        //{
        //    // 1. Prevent re-running migration
        //    if (await _context.MigrationTrackers
        //        .AnyAsync(x =>
        //            x.Name == "CommitmentPlan"
        //            && x.Completed))
        //    {
        //        return;
        //    }

        //    // 2. Load data
        //    var packages = await _context.InputPackages.ToListAsync();
        //    var cmtplans = await _context.CommitmentPlans.ToListAsync();

        //    var commitmentPlan = new List<CommitmentPlan>();

        //    foreach (var package in packages)
        //    {

        //        var cmtplan = cmtplans
        //            .FirstOrDefault(x =>
        //                x.InputPackageId == package.Id);


        //        commitmentPlan.Add(new CommitmentPlan
        //        {
        //            CommitmentPaymentEndDate = package.CommitmentPaymentEndDate,
        //            CommitmentPaymentStartDate = package.CommitmentPaymentStartDate,
        //            CommitmentValue = package.CommitmentValue,
        //            CommitmentType = package.CommitmentType,
        //            InputPackageId = package.Id
        //        });

        //    }
        //    _context.CommitmentPlans.AddRange(commitmentPlan);

        //    await _context.SaveChangesAsync();

        //    // 3. Mark migration as completed
        //    var tracker = new MigrationTracker
        //    {
        //        Name = "CommitmentPlan",
        //        Completed = true,
        //        RunOn = DateTimeOffset.UtcNow
        //    };

        //    _context.MigrationTrackers.Add(tracker);
        //    await _context.SaveChangesAsync();
        //}

        //public async Task RunRepaymentPlanMigrationAsync()
        //{
        //    // 1. Prevent re-running migration
        //    if (await _context.MigrationTrackers
        //        .AnyAsync(x =>
        //            x.Name == "RepaymentPlan"
        //            && x.Completed))
        //    {
        //        return;
        //    }

        //    // 2. Load data
        //    var packages = await _context.InputPackages.ToListAsync();
        //    var pytplans = await _context.RepaymentPlans.ToListAsync();

        //    var rpytPlan = new List<RepaymentPlan>();

        //    foreach (var package in packages)
        //    {

        //        var rmtplan = pytplans
        //            .FirstOrDefault(x =>
        //                x.InputPackageId == package.Id);


        //        rpytPlan.Add(new RepaymentPlan
        //        {
        //            RepaymentDuration= package.RepaymentDuration,
        //            RepaymentEndDate= package.RepaymentEndDate,
        //            Frequency= package.Frequency,
        //            Duration= package.Duration,
        //            RepaymentStartDate= package.RepaymentStartDate,
        //            InterestRate= package.InterestRate,
        //            InterestType= package.InterestType,
        //            RepaymentFrequency= package.Frequency,
        //            RepaymentGracePeriod=package.RepaymentGracePeriod,
        //            ScheduleType= package.ScheduleType,
        //            RepaymentMode=package.RepaymentMode,
        //            RepaymentValue= package.RepaymentValue,
        //            RiskCategory= package.RiskCategory,
        //            DefaultPenaltyRate= package.DefaultPenaltyRate,
        //            PatternType= package.PatternType,
        //            ApplyInterestOnDefault= package.ApplyInterestOnDefault,
        //            AlignToDisbursement=package.AlignToDisbursement,
        //            BusinessCode=package.BusinessCode,
        //            OrganizationId= package.OrganizationId,
        //            InputPackageId = package.Id,

        //        });

        //    }
        //    _context.RepaymentPlans.AddRange(rpytPlan);

        //    await _context.SaveChangesAsync();

        //    // 3. Mark migration as completed
        //    var tracker = new MigrationTracker
        //    {
        //        Name = "RepaymentPlan",
        //        Completed = true,
        //        RunOn = DateTimeOffset.UtcNow
        //    };

        //    _context.MigrationTrackers.Add(tracker);
        //    await _context.SaveChangesAsync();
        //}
        public async Task<BackfillResult?> RunUserNameBackfillAsync(bool dryRun, CancellationToken ct = default)
        {
            if (!await BackfillLock.WaitAsync(0, ct)) return null; // already running

            try
            {
                var candidates = new Dictionary<string, string>();

                void Collect(IEnumerable<(string UserId, string? Name)> rows)
                {
                    foreach (var (userId, name) in rows)
                        if (!string.IsNullOrWhiteSpace(name) && !candidates.ContainsKey(userId))
                            candidates[userId] = name;
                }

                Collect((await _context.Farmers.AsNoTracking().Where(x => x.Name != null)
                    .Select(x => new { x.UserId, x.Name }).ToListAsync(ct)).Select(x => (x.UserId, x.Name)));
                Collect((await _context.Agents.AsNoTracking().Where(x => x.Name != null)
                    .Select(x => new { x.UserId, x.Name }).ToListAsync(ct)).Select(x => (x.UserId, x.Name)));
                Collect((await _context.MemberShips.AsNoTracking().Where(x => x.Name != null)
                    .Select(x => new { x.UserId, x.Name }).ToListAsync(ct)).Select(x => (x.UserId, x.Name)));

                const int batchSize = 500;
                var userIds = candidates.Keys.ToList();
                var preview = new List<NamePreview>();
                int updated = 0;

                for (int i = 0; i < userIds.Count; i += batchSize)
                {
                    var batchIds = userIds.Skip(i).Take(batchSize).ToList();

                    var users = await _context.Users
                        .Where(u => batchIds.Contains(u.Id) && string.IsNullOrEmpty(u.FirstName))
                        .ToListAsync(ct);

                    foreach (var user in users)
                    {
                        var full = candidates[user.Id];
                        var (first, middle, last) = MigrationNameSplitter.Split(full);

                        if (dryRun)
                        {
                            if (preview.Count < 200)
                                preview.Add(new NamePreview(user.Id, full, first, middle, last));
                        }
                        else
                        {
                            user.FirstName = first;
                            user.MiddleName = middle;
                            user.LastName = last;
                        }
                        updated++;
                    }

                    if (!dryRun) await _context.SaveChangesAsync(ct);
                    _context.ChangeTracker.Clear();
                }

                return new BackfillResult(candidates.Count, updated, dryRun, preview);
            }
            finally
            {
                BackfillLock.Release();
            }
        }
    }


    public static class MigrationNameSplitter
    {
        public static (string First, string? Middle, string? Last) Split(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return (string.Empty, null, null);

            var parts = fullName
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries); // splits on any whitespace

            return parts.Length switch
            {
                1 => (parts[0], null, null),
                2 => (parts[0], null, parts[1]),
                _ => (parts[0],
                      string.Join(' ', parts[1..^1]),
                      parts[^1])
            };
        }
    }
}
