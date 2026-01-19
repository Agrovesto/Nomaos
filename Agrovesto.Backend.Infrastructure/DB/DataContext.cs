using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Businesses.MandE;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.DB
{
    //public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
    //{
    //    public DataContext CreateDbContext(string[] args)
    //    {
    //        var config = new ConfigurationBuilder()
    //            .SetBasePath(Directory.GetCurrentDirectory())
    //            .AddJsonFile("appsettings.Development.json")
    //            .Build();

    //        var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
    //        optionsBuilder.UseSqlServer(
    //          config.GetConnectionString("DefaultConnection"),
    //    sqlOptions =>
    //    {
    //        sqlOptions.MigrationsAssembly("Agrovesto.Backend.API");
    //        sqlOptions.EnableRetryOnFailure(
    //            maxRetryCount: 5,
    //            maxRetryDelay: TimeSpan.FromSeconds(10),
    //            errorNumbersToAdd: null);
    //        sqlOptions.CommandTimeout(60); // Optional: Increases command timeout
    //    });

    //        return new DataContext(optionsBuilder.Options);
    //    }
    //}
   
    
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
            modelBuilder.Entity<OneTimePassword>()
    .HasIndex(x => new { x.UserId, x.Purpose, x.Target });

            modelBuilder.Entity<OneTimePassword>()
                .HasIndex(x => x.ExpiresAt);

            modelBuilder.Entity<OneTimePassword>()
                .HasIndex(x => x.Destination);


            modelBuilder.Entity<MandEActivityType>().HasKey(x => x.Id);
            //modelBuilder.Entity<MandEActivityType>().HasIndex(x => x.Name).IsUnique();

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

            modelBuilder.Entity<MandEDataset>()
       .HasMany(d => d.Fields)
       .WithOne(f => f.MandEDataset)
       .HasForeignKey(f => f.MandEDatasetId)
       .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MandEDataset>()
                .HasMany(d => d.Records)
                .WithOne(r => r.MandEDataset)
                .HasForeignKey(r => r.MandEDatasetId)
                .OnDelete(DeleteBehavior.Cascade);
            //for market transaction
            //  modelBuilder.ApplyConfiguration(new MarketTransactionConfiguration());
        }

        //public DbSet<MarketTransaction> MarketTransactions { get; set; }
        //public DbSet<Commodity> Commodities { get; set; }
        //public DbSet<CommodityVariety> CommodityVarieties { get; set; }
        //public DbSet<CommodityCategory> CommodityCategories { get; set; }
        //public DbSet<TransactionCost> TransactionCosts { get; set; }
        //public DbSet<Market> Markets { get; set; }
        //public DbSet<MarketCommodity> MarketCommodities { get; set; }
        //public DbSet<MarketDay> MarketDays { get; set; }


        public DbSet<OneTimePassword> OneTimePasswords { get; set; }
        public DbSet<MandEDataset> MandEDatasets { get; set; }
        public DbSet<MandEDataFieldDefinition> MandEDataFieldDefinitions { get; set; }
        public DbSet<MandERecord> MandERecords { get; set; }
        public DbSet<MandEChartDefinition> MandEChartDefinitions { get; set; }
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
        public DbSet<FarmerOrganization> FarmerOrganizations { get; set; }
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
    public class MarketTransactionConfiguration
    : IEntityTypeConfiguration<MarketTransaction>
    {
        public void Configure(EntityTypeBuilder<MarketTransaction> builder)
        {
           

            builder.ToTable("MarketTransactions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.QuantityKg)
                .HasPrecision(18, 4)
                .IsRequired();

            builder.Property(x => x.PricePerKg)
                .HasPrecision(18, 4)
                .IsRequired();

            builder.Property(x => x.MoistureContent)
                .HasPrecision(5, 2);

            builder.Property(x => x.CreatedOn)
                .IsRequired();

            // Relationships
            builder.HasOne(x => x.Commodity)
                .WithMany()
                .HasForeignKey(x => x.CommodityId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.CommodityVariety)
                .WithMany()
                .HasForeignKey(x => x.CommodityVarietyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Market)
                .WithMany()
                .HasForeignKey(x => x.MarketId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.MarketDay)
                .WithMany()
                .HasForeignKey(x => x.MarketDayId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(x => new
            {
                x.CommodityId,
                x.MarketId,
                x.MarketDayId,
                x.SalesDate
            });

            builder.HasIndex(x => new
            {
                x.MarketId,
                x.SalesDate
            });

            builder.HasIndex(x => new
            {
                x.Status,
                x.Source
            });
        }
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
