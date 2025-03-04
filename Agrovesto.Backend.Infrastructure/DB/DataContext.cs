using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Linq;
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
        }
        public DbSet<Business> Businesses { get; set; }
        public DbSet<BusinessWorker> BusinessWorkers { get; set; }  
        public DbSet<SubAccount> SubAccounts { get; set; }
        public DbSet<FeeSplitConfiguration> FeeSplitConfigurations { get; set; }

        public DbSet<WalletProvider> WalletProviders { get; set; }
        public DbSet<WalletProviderParameter> WalletProviderParameters { get; set; }
        public DbSet<SterlingBankWallet> SterlingBankWallets { get; set; }
        public DbSet<Agent> Agents { get; set; }
        public DbSet<Cooperative> Cooperatives { get; set; }
        public DbSet<Farmer> Farmers { get; set; }
        public DbSet<FarmPlotDetails> FarmPlotDetails { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<WarehouseInputInventory> WarehouseInputInventories { get; set; }
        public DbSet<WarehouseUser> WarehouseUsers { get; set; }
        public DbSet<Bundle> Bundles { get; set; }
        public DbSet<BundleItems> BundleItems { get; set; }
        public DbSet<BundleSubscriptionInterest> BundleSubscriptionInterests { get; set; }
        public DbSet<FarmerImages> FarmerImages { get; set; }


    }
    //public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
    //{
    //    public DataContext CreateDbContext(string[] args)
    //    {
    //        var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
    //        optionsBuilder.UseSqlServer("Data Source=SQL5113.site4now.net;Initial Catalog=db_a971fc_paasdev;User Id=db_a971fc_paasdev_admin;Password=Agro144828@");

    //        return new DataContext(optionsBuilder.Options);
    //    }
    //}

}
