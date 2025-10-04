using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Agrovesto.Backend.Core.CoreUtils.Constants;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories
{
    public interface IOrganizationConfigurationRepository
    {
        Task<OrganizationConfigurationModel> GetByCodeAsync(string orgId);
        //Task<IEnumerable<OrganizationConfigurationModel>> GetAllAsync();
        Task<OrganizationConfigurationModel> CreateAsync(OrganizationConfigurationModel config);
        Task<OrganizationConfigurationModel> UpdateAsync(OrganizationConfigurationModel config);
        Task DeleteAsync(string orgId);
    }

    public class OrganizationConfigurationRepository : IOrganizationConfigurationRepository
    {
        private readonly DataContext _context;

        public OrganizationConfigurationRepository(DataContext context)
        {
            _context = context;
            // Optional: Initialize with mock data
           /** _configs.Add(new OrganizationConfigurationModel
            {
                OrganizationCode = "ORG94",
                OrganizationName = "Agrovesto Farms Ltd",
                WalletProviderConfiguration = new WalletProviderConfigurationModel
                {
                    DefaultWalletProvider = Providers.FCMBWallet,
                    Currency = "NGN",
                    EnableMultiWallet = true,
                    WalletProviderSettings = new Dictionary<string, string>
            {
                { "BankAWalletApiKey", "abc123" },
                { "BankAWalletSecret", "secretKeyHere" }
            },
                }


                //InputFinancingConfig = new InputFinancingConfiguration
                //{
                //    IsEnabled = true,
                //    DefaultRepaymentMethod = "PercentageOfHarvest",
                //    MaxFinancingAmount = 50000,
                //    BundleOptions = new List<InputBundleOption>
                //{
                //    new InputBundleOption { BundleName = "Starter Pack", InputType = "Seed", Units = 10, Price = 10000 },
                //    new InputBundleOption { BundleName = "Fertilizer Pack", InputType = "Fertilizer", Units = 5, Price = 15000 }
                //}
                //}
            });

            **/
        }

        public async Task<OrganizationConfigurationModel> GetByCodeAsync(string orgCode)
        {
            var config = await (from o in _context.OrganizationConfig
                                join w in _context.Set<WalletProviderConfiguration>()
                                on o.Id equals w.OrganizationConfigurationId
                                where o.OrganizationCode == orgCode
                                select new OrganizationConfiguration
                                {
                                    OrganizationCode = orgCode,
                                    OrganizationName=o.OrganizationName,
                                    WalletProviderConfiguration = new WalletProviderConfiguration
                                    {
                                        Currency = w.Currency,
                                        DefaultWalletProvider=w.DefaultWalletProvider,
                                        EnableMultiWallet=w.EnableMultiWallet,
                                        Id=w.Id,
                                        

                                    }
                                })
                .FirstOrDefaultAsync();

            if (config == null)
                return null;

            return config.MapToModel();
        }

        //public Task<IEnumerable<OrganizationConfigurationModel>> GetAllAsync()
        //{
        //    return Task.FromResult(_configs.AsEnumerable());
        //}

        public async Task<OrganizationConfigurationModel> CreateAsync(OrganizationConfigurationModel config)
        {
            var entity = config.MapToEntity();
        _context.OrganizationConfig.Add(entity);
        await _context.SaveChangesAsync();
        return entity.MapToModel();       
        }

        public async Task<OrganizationConfigurationModel> UpdateAsync(OrganizationConfigurationModel config)
        {
            var existing = await _context.OrganizationConfig.FirstOrDefaultAsync(c => c.OrganizationCode == config.OrganizationCode);
            if (existing == null)
                throw new KeyNotFoundException($"Organization {config.OrganizationCode} not found.");

            existing.OrganizationName = config.OrganizationName;
            existing.WalletProviderConfiguration.DefaultWalletProvider = config.WalletProviderConfiguration.DefaultWalletProvider;
            existing.WalletProviderConfiguration.Currency = config.WalletProviderConfiguration.Currency;
            existing.WalletProviderConfiguration.EnableMultiWallet = config.WalletProviderConfiguration.EnableMultiWallet;
          //  existing.WalletProviderConfiguration.WalletProviderSettings = config.WalletProviderConfiguration.WalletProviderSettings;
            //existing.WalletProviderConfiguration.InputFinancingConfig = config.WalletProviderConfiguration.InputFinancingConfig;
            await _context.SaveChangesAsync();
            return existing.MapToModel();
        }

        public async Task DeleteAsync(string orgId)
        {
            var existing = await _context.OrganizationConfig.FirstOrDefaultAsync(c => c.OrganizationCode == orgId);
            if (existing == null)
                throw new KeyNotFoundException($"Organization {orgId} not found.");
            _context.OrganizationConfig.Remove(existing);
            await _context.SaveChangesAsync();
            //return Task.CompletedTask;
        }
    }

}
