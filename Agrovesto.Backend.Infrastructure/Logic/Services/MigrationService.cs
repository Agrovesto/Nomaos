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
        //Task RunCommitmentPlanMigrationAsync();
        Task RunOrganizationTableMigrationAsync();
        //Task RunRepaymentPlanMigrationAsync();
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

    }
}
