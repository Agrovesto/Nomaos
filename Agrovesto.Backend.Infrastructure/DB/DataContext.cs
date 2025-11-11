using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.DB
{
    public class DataContext :IdentityDbContext<User,Role, string>
    {
        public DataContext(DbContextOptions<DataContext> options): base(options)
        {
            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<UserOrganization>()
                .HasKey(uo => new
                {
                    uo.UserId, uo.OrganizationCode
                });
            modelBuilder.Entity<UserOrganization>()
                .HasOne(uo => uo.User)
                .WithMany(u => u.UserOrganizations)
                .HasForeignKey(uo => uo.UserId);


            modelBuilder.Entity<MandEActivityType>().HasKey(x => x.Id);
            modelBuilder.Entity<MandEActivityType>().HasIndex(x => x.Name).IsUnique();

            modelBuilder.Entity<MandESubActivity>().HasKey(x => x.Id);
            modelBuilder.Entity<MandESubActivity>().HasIndex(x => new { x.MandEActivityTypeId, x.Name }).IsUnique();

            modelBuilder.Entity<MandEFieldDefinition>().HasKey(x => x.Id);
            modelBuilder.Entity<MandEFieldDefinition>().HasIndex(x => new { x.MandESubActivityId, x.Key }).IsUnique();

            modelBuilder.Entity<MandEFarmActivity>().HasKey(x => x.Id);

            modelBuilder.Entity<Invoice>()
            .HasIndex(i => i.ReferenceCode)
            .IsUnique();

            modelBuilder.Entity<Distributions>()
                .HasIndex(d => d.ReferenceNumber)
                .IsUnique();


            modelBuilder.Entity<Subscriptions>()
            .HasOne(s => s.Invoice)
            .WithOne(i => i.Subscriptions)
            .HasForeignKey<Invoice>(i => i.SubscriptionsId)
            .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrganizationBundlePlans>()
                .HasMany(b => b.OrganizationBundleItems)
                .WithOne(i => i.OrganizationBundlePlans)
                .HasForeignKey(i => i.OrganizationBundlePlansId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Subscriptions>()
                .HasMany(s => s.Distributions)
                .WithOne(d => d.Subscriptions)
                .HasForeignKey(d => d.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Distributions>()
                .HasMany(d => d.SubscriptionRepayments)
                .WithOne(r => r.Distributions)
                .HasForeignKey(r => r.DistributionsId)
                .OnDelete(DeleteBehavior.Cascade);

            // Enum conversions (store as string)
            modelBuilder.Entity<Subscriptions>()
                .Property(s => s.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Invoice>()
                .Property(i => i.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Distributions>()
                .Property(d => d.Mode)
                .HasConversion<string>();

            modelBuilder.Entity<SubscriptionRepayment>()
                .Property(r => r.Mode)
                .HasConversion<string>();

            // Additional configuration as needed (max lengths, required fields)
            modelBuilder.Entity<Invoice>()
                .Property(i => i.ReferenceCode)
                .HasMaxLength(64)
                .IsRequired();

            modelBuilder.Entity<Invoice>()
                .Property(i => i.InvoiceNumber)
                .HasMaxLength(64)
                .IsRequired();
        }

        public DbSet<MandEActivityType> MandEActivityTypes { get; set; }
        public DbSet<MandESubActivity> MandESubActivities { get; set; }
        public DbSet<MandEFieldDefinition> MandEFieldDefinitions { get; set; }
        public DbSet<MandEFarmActivity> MandEFarmActivities { get; set; }

        public DbSet<Business> Businesses { get; set; }
        public DbSet<UserOrganization> UserOrganizations { get; set; }
        public DbSet<Organization> BusinessOrganizations { get; set; }
        public DbSet<BusinessWorker> BusinessWorkers { get; set; }  
        public DbSet<SubAccount> SubAccounts { get; set; }
        public DbSet<FeeSplitConfiguration> FeeSplitConfigurations { get; set; }

        public DbSet<PaymentProvider> PaymentProviders { get; set; }
        public DbSet<PaymentProviderParameter> PaymentProviderParameters { get; set; }
        public DbSet<WalletProvider> WalletProviders { get; set; }
        public DbSet<WalletProviderParameter> WalletProviderParameters { get; set; }
        public DbSet<SterlingBankWallet> SterlingBankWallets { get; set; }
        public DbSet<Agent> Agents { get; set; }
        public DbSet<Cooperative> Cooperatives { get; set; }
        public DbSet<Farmer> Farmers { get; set; }
        public DbSet<FarmPlotDetails> FarmPlotDetails { get; set; }
        public DbSet<FarmFlagAuditTrail> FarmFlagAuditTrail { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<WarehouseInputInventory> WarehouseInputInventories { get; set; }
        public DbSet<WarehouseUser> WarehouseUsers { get; set; }
        public DbSet<Bundle> Bundles { get; set; }
        public DbSet<BundleItems> BundleItems { get; set; }
        public DbSet<OtherBundlePaymentCost> OtherBundlePaymentCosts { get; set; }
        public DbSet<BundleSubscriptionInterest> BundleSubscriptionInterests { get; set; }
        public DbSet<FarmerImages> FarmerImages { get; set; }
        public DbSet<BundleSubscriptionDisbursement> BundleSubscriptionDisbursements { get; set; }
        public DbSet<FarmActivityType> FarmActivityTypes { get; set; }
        public DbSet<FarmVisitationActivity> FarmVisitationActivities { get; set; }
        public DbSet<InputDeliveryTracker> InputDeliveryTrackers { get; set; }
        public DbSet<FarmerSalesTracker> FarmerSalesTracker { get; set; }
        public DbSet<Trader> Traders { get; set; }
        public DbSet<Payout> Payouts { get; set; }
        public DbSet<States> States { get; set; }
        public DbSet<LGA> LGAs { get; set; }
        public DbSet<Ward> Wards { get; set; }
        public DbSet<FarmerCreditScore> FarmerCreditScores { get; set; }
        public DbSet<CreditScoreWeights> CreditScoreWeights { get; set; }
        public DbSet<Repayment> Repayments { get; set; }
        public DbSet<RepaymentBreakdown> RepaymentBreakdown { get; set; }

        public DbSet<FarmerValueChainItem> FarmerValueChainItems { get; set; }
        public DbSet<OrganizationConfiguration> OrganizationConfig { get; set; }
       public DbSet<Wallet> Wallets { get; set; }
       public DbSet<WalletTransaction> WalletTransactions { get; set; }


        public DbSet<OrganizationBundlePlans> OrganizationBundlePlans { get; set; }
        public DbSet<OrganizationBundleItem> OrganizationBundleItem { get; set; }
        public DbSet<Subscriptions> Subscriptions { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Distributions> Distributions { get; set; }
        public DbSet<SubscriptionRepayment> SubscriptionRepayments { get; set; }
        public DbSet<Receipt> Receipts { get; set; }
        public DbSet<CooperativeCreditScore> CooperativeCreditScores { get; set; }
        public DbSet<InputFinancingScheme> InputFinancingSchemes { get; set; }
        public DbSet<InputSubscription> InputSubscriptions { get; set; }
        public DbSet<FarmAllocation> FarmAllocations { get; set; }
        public DbSet<Farm> Farms { get; set; }
        public DbSet<FarmMappingAuditTrail> FarmMappingAuditTrails { get; set; }

    }
    //public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
    //{
    //    public DataContext CreateDbContext(string[] args)
    //    {
    //        var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
    //   optionsBuilder.UseSqlServer("Data Source=SQL5113.site4now.net;Initial Catalog=db_a971fc_agrovestopassdev;User Id=db_a971fc_agrovestopassdev_admin;Password=Agro144828@", //staging
    //    // optionsBuilder.UseSqlServer("Data Source=SQL8006.site4now.net;Initial Catalog=db_a971fc_farmermanagementpl;User Id=db_a971fc_farmermanagementpl_admin;Password=Agro144828@",
    //     // optionsBuilder.UseSqlServer("Data Source=agrovestodbinstance.cxgc0aq4mmfo.eu-west-1.rds.amazonaws.com;Initial Catalog=agrovestodbinstance;User Id=agrovesto;Password=sphRm,5WSQcwXlGZvxbkib,R7K8D52",
          


    //            options =>
    //        options.MigrationsAssembly("Agrovesto.Backend.API"));

    //        return new DataContext(optionsBuilder.Options);
    //    }
    //}

}
