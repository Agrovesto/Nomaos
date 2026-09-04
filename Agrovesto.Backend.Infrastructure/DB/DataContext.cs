using Agrovesto.Backend.Core.Models.Businesses.Input;
using Agrovesto.Backend.Core.Models.Common;
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
using SubscriptionDocument = Agrovesto.Backend.Infrastructure.Entities.Businesses.SubscriptionDocument;

namespace Agrovesto.Backend.Infrastructure.DB
{
    public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
    {
        public DataContext CreateDbContext(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
               // .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
            optionsBuilder.UseSqlServer(
            config.GetConnectionString("DefaultConnection"),
            sqlOptions =>
            {
                sqlOptions.MigrationsAssembly("Agrovesto.Backend.API");

                sqlOptions.UseNetTopologySuite();

                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);

                sqlOptions.CommandTimeout(300);
            });


            return new DataContext(optionsBuilder.Options);
        }
    }
    public class FarmConfiguration : IEntityTypeConfiguration<Farm>
    {
        public void Configure(EntityTypeBuilder<Farm> builder)
        {
            builder.HasIndex(f => f.FarmerId)
                .HasDatabaseName("IX_Farms_FarmerId");
        }
    }

    public class FarmerCooperativeConfiguration : IEntityTypeConfiguration<FarmerCooperative>
    {
        public void Configure(EntityTypeBuilder<FarmerCooperative> builder)
        {
            builder.HasIndex(fc => fc.FarmerId)
                .HasDatabaseName("IX_FarmerCooperatives_FarmerId");
        }
    }

    public class MemberShipAgentConfiguration : IEntityTypeConfiguration<MemberShipAgent>
    {
        public void Configure(EntityTypeBuilder<MemberShipAgent> builder)
        {
            builder.HasIndex(ma => ma.AgentId)
                .HasDatabaseName("IX_MemberShipAgents_AgentId");
        }
    }

    public class FarmerAgentConfiguration : IEntityTypeConfiguration<FarmerAgent>
    {
        public void Configure(EntityTypeBuilder<FarmerAgent> builder)
        {
            builder.HasIndex(fa => new { fa.AgentId, fa.FarmerId })
                .HasDatabaseName("IX_FarmerAgents_AgentId_FarmerId");
        }
    }

    public class FarmerConfiguration : IEntityTypeConfiguration<Farmer>
    {
        public void Configure(EntityTypeBuilder<Farmer> builder)
        {
            builder.HasIndex(f => f.OnBoardedOn)
                .HasDatabaseName("IX_Farmers_OnBoardedOn");
        }
    }

    public class DataContext :IdentityDbContext<User,Role, string>
    {
        public DataContext(DbContextOptions<DataContext> options): base(options)
        {
            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new FarmConfiguration());
            modelBuilder.ApplyConfiguration(new FarmerCooperativeConfiguration());
            modelBuilder.ApplyConfiguration(new MemberShipAgentConfiguration());
            modelBuilder.ApplyConfiguration(new FarmerAgentConfiguration());
            modelBuilder.ApplyConfiguration(new FarmerConfiguration());


            modelBuilder.Entity<BusinessWalletProvider>()
     .HasIndex(x => new { x.BusinessCode, x.ProviderId })
     .IsUnique();

            modelBuilder.Entity<BusinessWalletProvider>()
                .HasOne(x => x.WalletProvider)
                .WithMany(x => x.BusinessWalletProviders)   // add this ICollection to WalletProvider entity
                .HasForeignKey(x => x.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);          // don't let deleting a provider cascade-wipe tenants

            modelBuilder.Entity<BusinessWalletProviderParameter>()
                .HasOne(x => x.BusinessWalletProvider)
                .WithMany(x => x.Parameters)
                .HasForeignKey(x => x.BusinessWalletProviderId)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<BusinessSMSProvider>()
    .HasIndex(x => new { x.BusinessCode, x.ProviderId })
    .IsUnique();

            modelBuilder.Entity<BusinessSMSProvider>()
                .HasOne(x => x.SMSProvider)
                .WithMany(x => x.BusinessSMSProviders)   // add this ICollection to WalletProvider entity
                .HasForeignKey(x => x.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);          // don't let deleting a provider cascade-wipe tenants

            modelBuilder.Entity<BusinessSMSProviderParameter>()
                .HasOne(x => x.BusinessSMSProvider)
                .WithMany(x => x.Parameters)
                .HasForeignKey(x => x.BusinessSMSProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserNotificationPreference>()
                .HasIndex(x => new { x.UserId, x.UserRole })
                .IsUnique();

            modelBuilder.Entity<User>()
       .HasIndex(u => u.PhoneNumber)
       .IsUnique()
       .HasFilter("[PhoneNumber] IS NOT NULL");

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique()
                .HasFilter("[Email] IS NOT NULL");

            modelBuilder.Entity<User>()
                .HasIndex(u => u.BVN)
                .IsUnique()
                .HasFilter("[BVN] IS NOT NULL");

            modelBuilder.Entity<User>()
                .HasIndex(u => u.NIN)
                .IsUnique()
                .HasFilter("[NIN] IS NOT NULL");

       //     modelBuilder.Entity<User>()
       //.HasIndex(u => u.PhoneNumber)       
       //.IsUnique();

            modelBuilder.Entity<UserOrganization>()
                .HasKey(uo => new { uo.UserId, uo.OrganizationCode });

            modelBuilder.Entity<FarmerOrganization>()
                .HasIndex(fo => new { fo.FarmerId, fo.OrganizationCode });

            modelBuilder.Entity<FarmerCooperative>()
                .HasIndex(fc => new { fc.FarmerId, fc.CooperativeId });

            modelBuilder.Entity<FarmerAgent>()
                .HasIndex(fa => new { fa.FarmerId, fa.AgentId });



            modelBuilder.Entity<OrganizationLocation>()
       .HasOne(l => l.ParentLocation)
       .WithMany(l => l.SubLocations)
       .HasForeignKey(l => l.ParentLocationId)
       .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserWallet>(entity =>
            {
                entity.HasIndex(x => new { x.UserId, x.WalletProviderId })
                      .IsUnique();

                entity.Property(x => x.Currency)
                      .HasMaxLength(3)
                      .IsRequired();

                entity.Property(x => x.WalletNumber)
                      .HasMaxLength(50)
                      .IsRequired();

                entity.Property(x => x.BelongToBusinessCode)
                      .HasMaxLength(50);

                entity.HasOne(x => x.WalletProvider)
                      .WithMany()
                      .HasForeignKey(x => x.WalletProviderId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<UserWalletContext>(entity =>
            {
                entity.HasIndex(x => new { x.UserId, x.ContextType, x.ContextReferenceId })
                      .IsUnique();

                entity.Property(x => x.ContextReferenceId)
                      .HasMaxLength(100);

                entity.HasOne(x => x.UserWallet)
                      .WithMany(x => x.WalletContexts)
                      .HasForeignKey(x => x.UserWalletId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(x => x.ContextType)
                      .HasConversion<string>();
            });
            modelBuilder.Entity<UserWalletTransactions>(entity =>
            {
                entity.HasIndex(x => x.TransactionReference)
                      .IsUnique();

                entity.Property(x => x.TransactionReference)
                      .HasMaxLength(50)
                      .IsRequired();

                entity.Property(x => x.Currency)
                      .HasMaxLength(3)
                      .IsRequired();

                entity.Property(x => x.ProviderReference)
                      .HasMaxLength(100);

                entity.Property(x => x.ProviderStatus)
                      .HasMaxLength(50);

                entity.Property(x => x.TransactionDescription)
                      .HasMaxLength(500);

                entity.Property(x => x.ChargedTo)
                      .HasMaxLength(100);

                entity.Property(x => x.ChargedToId)
                      .HasMaxLength(100);

                entity.HasOne(x => x.UserWallet)
                      .WithMany(x => x.Transactions)
                      .HasForeignKey(x => x.UserWalletId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(x => x.WalletTransactionType)
                      .HasConversion<string>();

                entity.Property(x => x.WalletTransactionStatus)
                      .HasConversion<string>();
            });
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

            modelBuilder.Entity<Farm>()
                .HasKey(f => f.Id);

            modelBuilder.Entity<Farm>()
                .Property(f => f.FarmPolygon)
                .HasColumnType("geography");
            modelBuilder.Entity<OrganizationLocation>()
              .HasOne(o => o.State)
              .WithMany()
              .HasForeignKey(o => o.StateId)
              .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrganizationLocation>()
                .HasOne(o => o.LGA)
                .WithMany()
                .HasForeignKey(o => o.LgaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrganizationLocation>()
                .HasOne(o => o.LocationType)
                .WithMany()
                .HasForeignKey(o => o.LocationTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<OrganizationAgentRole>()
                .HasOne(o => o.OrganizationRole)
                .WithMany()
                .HasForeignKey(o => o.RoleId)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<LocationModule>()
    .HasKey(x => new { x.LocationId, x.ModuleId });

            modelBuilder.Entity<LocationModule>()
                .HasOne(x => x.Location)
                .WithMany(x => x.LocationModules)
                .HasForeignKey(x => x.LocationId);

            modelBuilder.Entity<LocationModule>()
                .HasOne(x => x.Module)
                .WithMany(x => x.LocationModules)
                .HasForeignKey(x => x.ModuleId);



            modelBuilder.Entity<LocationStorageCategory>()
    .HasKey(x => new { x.LocationId, x.StorageCategoryId });

            modelBuilder.Entity<LocationStorageCategory>()
                .HasOne(x => x.Location)
                .WithMany(x => x.LocationStorageCategories)
                .HasForeignKey(x => x.LocationId);

            modelBuilder.Entity<LocationStorageCategory>()
                .HasOne(x => x.StorageCategory)
                .WithMany(x => x.LocationStorageCategories)
                .HasForeignKey(x => x.StorageCategoryId);

            modelBuilder.Entity<TemperatureControlType>()
    .HasKey(x => x.Id);

            modelBuilder.Entity<OrganizationLocation>()
    .HasOne(x => x.TemperatureControlType)
    .WithMany()
    .HasForeignKey(x => x.TemperatureControlTypeId)
    .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<LocationModule>()
    .HasIndex(x => x.ModuleId);

            modelBuilder.Entity<LocationStorageCategory>()
                .HasIndex(x => x.StorageCategoryId);

            modelBuilder.Entity<OrganizationAgent>()
    .HasIndex(x => new { x.LocationId, x.IsPrimary })
    .HasFilter("[IsPrimary] = 1")
    .IsUnique();

            modelBuilder.Entity<InputPackageItem>()
    .HasOne(x => x.OrganizationProduct)
    .WithMany()
    .HasForeignKey(x => x.OrganizationProductId)
    .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Organization>()
    .ToTable("Organizations");

            modelBuilder.Entity<RepaymentSchedule>()
        .HasOne(x => x.InputPackageSubscription)
        .WithMany(x => x.RepaymentSchedules)
        .HasForeignKey(x => x.InputPackageSubscriptionId);

            modelBuilder.Entity<InputPackage>()
    .HasOne(p => p.CommitmentPlan)
    .WithOne(c => c.InputPackage)
    .HasForeignKey<CommitmentPlan>(c => c.InputPackageId);

            modelBuilder.Entity<InputPackage>()
    .HasOne(p => p.RepaymentPlan)
    .WithOne(c => c.InputPackage)
    .HasForeignKey<RepaymentPlan>(c => c.InputPackageId);

            modelBuilder.Entity<RepaymentSchedule>()
    .HasQueryFilter(x => !x.IsDeleted);
        }

     
        public DbSet<OneTimePassword> OneTimePasswords { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<MemberShipAgent> MemberShipAgents { get; set; }
        public DbSet<UserDevice> UserDevices { get; set; }
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
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<BusinessWorker> BusinessWorkers { get; set; }  
        public DbSet<SubAccount> SubAccounts { get; set; }
        public DbSet<FeeSplitConfiguration> FeeSplitConfigurations { get; set; }

        public DbSet<PaymentProvider> PaymentProviders { get; set; }
        public DbSet<PaymentProviderParameter> PaymentProviderParameters { get; set; }
        public DbSet<WalletProvider> WalletProviders { get; set; }
        public DbSet<WalletProviderParameter> WalletProviderParameters { get; set; }
        public DbSet<SterlingBankWallet> SterlingBankWallets { get; set; }
        public DbSet<Agent> Agents { get; set; }
        public DbSet<LocationType> LocationTypes { get; set; }
        public DbSet<OrganizationLocation> OrganizationLocations { get; set; }
        public DbSet<OrganizationRole> OrganizationRoles { get; set; }
        public DbSet<OrganizationAgentRole> OrganizationAgentRoles { get; set; }

        public DbSet<OrganizationAgent> OrganizationAgents { get; set; }
        public DbSet<MemberShip> MemberShips { get; set; }
        public DbSet<MemberShipType> MemberShipTypes { get; set; }
        public DbSet<OrganizationMembership> OrganizationMemberships { get; set; }
        public DbSet<Farmer> Farmers { get; set; }
        public DbSet<FarmerOrganization> FarmerOrganizations { get; set; }
        public DbSet<FarmerCooperative> FarmerCooperatives { get; set; }
        public DbSet<FarmerAgent> FarmerAgents { get; set; }
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
        public DbSet<Entities.Common.States> States { get; set; }
        public DbSet<LGA> LGAs { get; set; }
        public DbSet<Ward> Wards { get; set; }
        public DbSet<FarmerCreditScore> FarmerCreditScores { get; set; }
        public DbSet<CreditScoreWeights> CreditScoreWeights { get; set; }
        public DbSet<Repayment> Repayments { get; set; }
        public DbSet<RepaymentBreakdown> RepaymentBreakdown { get; set; }

        public DbSet<FarmerValueChainItem> FarmerValueChainItems { get; set; }
        public DbSet<OrganizationConfiguration> OrganizationConfig { get; set; }
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
        public DbSet<Agrovesto.Backend.Infrastructure.Entities.Common.BulkUploadJob> BulkUploadJobs { get; set; }



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
   
        public DbSet<Farm> Farms { get; set; }
        public DbSet<FarmMappingAuditTrail> FarmMappingAuditTrails { get; set; }
        public DbSet<FarmLandDocument> FarmLandDocuments { get; set; }
        public DbSet<FarmAllocation> FarmAllocations { get; set; }
        public DbSet<FarmCoordinate> FarmCoordinates { get; set; }
        public DbSet<KYCDetails> KYCDetails { get; set; }
        public DbSet<UserWallet> UserWallets { get; set; }
        public DbSet<WalletSecurity> WalletSecurities { get; set; }
        public DbSet<UserWalletTransactions> UserWalletTransactions { get; set; }
        public DbSet<WalletReconciliationLog> WalletReconciliationLogs { get; set; }
        public DbSet<UserWalletContext> UserWalletContexts { get; set; }
        public DbSet<ProviderWalletTransaction> ProviderWalletTransactions { get; set; }
        public DbSet<ProviderWalletTransactionLog> ProviderWalletTransactionLogs { get; set; }
        public DbSet<InputPackage> InputPackages { get; set; }
        public DbSet<CommitmentPlan> CommitmentPlans { get; set; }
        public DbSet<RepaymentPlan> RepaymentPlans { get; set; }
        public DbSet<InputPackageSubscription> InputPackageSubscriptions { get; set; }
        public DbSet<SubscriptionDocument> SubscriptionDocuments { get; set; }
        public DbSet<RepaymentSchedule> RepaymentSchedules { get; set; }
        public DbSet<RepaymentPenalty> RepaymentPenalties { get; set; }
        public DbSet<PenaltyPolicy> PenaltyPolicies { get; set; }
        public DbSet<SubscriptionPaymentAllocation> SubscriptionPaymentAllocations { get; set; }
        public DbSet<InputPackageSubscriptionPayment> InputPackageSubscriptionPayments { get; set; }
        public DbSet<SubscriptionCreditBalance> SubscriptionCreditBalance { get; set; }
        public DbSet<PaymentReconciliation> PaymentReconciliation { get; set; }
        public DbSet<InputSubscriptionDisbursement> InputSubscriptionDisbursements { get; set; }
        public DbSet<OutboxMessage> OutboxMessages { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<StorageCategory> StorageCategories { get; set; }
        public DbSet<TemperatureControlType> TemperatureControlTypes { get; set; }

        public DbSet<LocationModule> LocationModules { get; set; }
        public DbSet<LocationStorageCategory> LocationStorageCategories { get; set; }
        public DbSet<LocationInventory> LocationInventories { get; set; }
        public DbSet<OrganizationProduct> OrganizationProducts { get; set; }
        public DbSet<OrganizationProductCategory> OrganizationProductCategories { get; set; }
        public DbSet<AdditionalInputPackageCost> AdditionalInputPackageCosts { get; set; }
        public DbSet<EconomicsOfProduction> EconomicsOfProductions { get; set; }
        public DbSet<EopCostItem> EopCostItems { get; set; }
        public DbSet<InputFinancingCategory> InputFinancingCategories { get; set; }
        public DbSet<InputPackageItem> InputPackageItems { get; set; }
        public DbSet<BusinessLicenseModule> BusinessLicenseModules { get; set; }
        public DbSet<SystemModule> SystemModules { get; set; }
        public DbSet<BusinessLicense> BusinessLicenses { get; set; }
        public DbSet<BusinessBillingInvoice> BusinessBillingInvoices { get; set; }
        public DbSet<BusinessPayment> BusinessPayments { get; set; }
        public DbSet<UserBusiness> UserBusinesses { get; set; }
        public DbSet<MigrationTracker> MigrationTrackers { get; set; }
        public DbSet<OrganizationBusiness> OrganizationBusinesses { get; set; }
        public DbSet<DisbursementLineItem> DisbursementLineItems { get; set; }
        public DbSet<MonetaryDisbursementTransaction> MonetaryDisbursementTransactions { get; set; }
        public DbSet<BusinessWalletProvider> BusinessWalletProviders { get; set; }
        public DbSet<BusinessWalletProviderParameter> BusinessWalletProviderParameters { get; set; }
        public DbSet<BusinessSMSProvider> BusinessSMSProviders { get; set; }
        public DbSet<BusinessSMSProviderParameter> BusinessSMSProviderParameters { get; set; }
        public DbSet<SMSProviderParameter> SMSProviderParameters { get; set; }
        public DbSet<SMSProvider> SMSProvider { get; set; }
        public DbSet<RevokedToken> RevokedTokens { get; set; }
        public DbSet<UserNotificationPreference> UserNotificationPreferences { get; set; }
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
