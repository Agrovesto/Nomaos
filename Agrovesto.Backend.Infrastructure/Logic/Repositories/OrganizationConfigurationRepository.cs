using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
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
        Task<OrganizationConfigurationModel> UpdateWalletConfiguration(OrganizationConfigurationModel config);
        Task DeleteAsync(string orgId);
        Task<OrganizationConfigurationModel> UpdateInputConfiguration(OrganizationConfigurationModel config);
        Task<InputFinancingConfigurationModel> GetInputConfigByCodeAsync(string configCode);
    }

    public class OrganizationConfigurationRepository : IOrganizationConfigurationRepository
    {
        private readonly DataContext _context;

        public OrganizationConfigurationRepository(DataContext context)
        {
            _context = context;
           
        }
        public async Task<OrganizationConfigurationModel> GetByCodeAsync(string orgCode)
        {
            var config = await _context.OrganizationConfig
                .Include(o => o.WalletProviderConfigurations)
               // .ThenInclude(w => w.w)
                .Include(o => o.InputFinancingConfigurations)
                    .ThenInclude(i => i.SubscriptionConfigs)
                .Include(o => o.InputFinancingConfigurations)
                    .ThenInclude(i => i.DisbursementConfigs)
                .Include(o => o.InputFinancingConfigurations)
                    .ThenInclude(i => i.RepaymentConfigs)
                .FirstOrDefaultAsync(o => o.OrganizationCode == orgCode);

            if (config == null)
                return null;

            return config.MapToModel();
        }


        public async Task<OrganizationConfigurationModel> CreateAsync(OrganizationConfigurationModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            // 🟢 Handle WalletProviderConfiguration
            var walletConfigs = model.WalletProviderConfiguration?
                .Select(w => w?.ToEntity())
                .Where(w => w != null)
                .ToList() ?? new List<WalletProviderConfiguration>();

            // If multiple default wallets exist, keep the first one only
            var defaultWallets = walletConfigs
                .Where(w => !string.IsNullOrEmpty(w.DefaultWalletProvider))
                .ToList();

            if (defaultWallets.Count > 1)
            {
                var firstDefault = defaultWallets.First().DefaultWalletProvider;
                foreach (var w in walletConfigs)
                {
                    if (w.DefaultWalletProvider != firstDefault)
                        w.DefaultWalletProvider = null;
                }
            }

            // 🟢 Handle InputFinancingConfiguration
            var inputConfigs = model.InputFinancingConfig?
                .Select(i => i?.ToEntity())
                .Where(i => i != null)
                .ToList() ?? new List<InputFinancingConfiguration>();

            // 🟢 Build Organization Configuration entity
            var entity = new OrganizationConfiguration
            {
                OrganizationCode = model.OrganizationCode,
                OrganizationName = model.OrganizationName,
                WalletProviderConfigurations = walletConfigs,
                InputFinancingConfigurations = inputConfigs
            };

            _context.OrganizationConfig.Add(entity);
            await _context.SaveChangesAsync();

            return entity.MapToModel();
        }


        //public async Task<OrganizationConfigurationModel> CreateAsync(OrganizationConfigurationModel config)
        //{
        //    var entity = config.MapToEntity();

        //    _context.OrganizationConfig.Add(entity);
        //await _context.SaveChangesAsync();
        //return entity.MapToModel();       
        //}
        public async Task<OrganizationConfigurationModel> UpdateWalletConfiguration(OrganizationConfigurationModel config)
        {
            // Retrieve existing org + wallet configurations
            var existing = await _context.OrganizationConfig
                .Include(c => c.WalletProviderConfigurations)
                .FirstOrDefaultAsync(c => c.OrganizationCode == config.OrganizationCode);

            if (existing == null)
                throw new KeyNotFoundException($"Organization {config.OrganizationCode} not found.");

            // Ensure WalletProviderConfigurations exists
            existing.WalletProviderConfigurations ??= new List<WalletProviderConfiguration>();

            // Map incoming wallet configurations
            var newWalletConfigs = config.WalletProviderConfiguration?
                .Select(w => w?.ToEntity())
                .Where(w => w != null)
                .ToList() ?? new List<WalletProviderConfiguration>();

            // Handle default wallet assignment
            var newDefaultWallet = newWalletConfigs.FirstOrDefault(w => w.IsDefault == true);

            if (newDefaultWallet != null)
            {
                // Clear any previously default wallet in DB
                foreach (var w in existing.WalletProviderConfigurations)
                {
                    w.IsDefault = false;
                    if (w.DefaultWalletProvider != null && w.DefaultWalletProvider != newDefaultWallet.DefaultWalletProvider)
                        w.DefaultWalletProvider = w.IsDefault ? w.DefaultWalletProvider : null;
                }

                // Ensure the new default wallet has correct DefaultWalletProvider code
                newDefaultWallet.IsDefault = true;
                newDefaultWallet.DefaultWalletProvider ??= newDefaultWallet.DefaultWalletProvider;
            }

            // Sync configurations (update existing or add new)
            foreach (var newWallet in newWalletConfigs)
            {
                var existingWallet = existing.WalletProviderConfigurations
                    .FirstOrDefault(x => x.DefaultWalletProvider == newWallet.DefaultWalletProvider);

                if (existingWallet != null)
                {
                    // Update fields
                    existingWallet.Currency = newWallet.Currency ?? existingWallet.Currency;
                    if (newWallet.IsDefault)
                    {
                        existingWallet.IsDefault = true;
                    }

                    existingWallet.DefaultWalletProvider = newWallet.DefaultWalletProvider ?? existingWallet.DefaultWalletProvider;
                }
                else
                {
                    // Add new wallet config
                    existing.WalletProviderConfigurations.Add(newWallet);
                }
            }

            await _context.SaveChangesAsync();
            return existing.MapToModel();
        }

        public async Task<OrganizationConfigurationModel> UpdateInputConfiguration(OrganizationConfigurationModel config)
        {
            var existing = await _context.OrganizationConfig
                .Include(c => c.InputFinancingConfigurations)
                    .ThenInclude(i => i.SubscriptionConfigs)
                .Include(c => c.InputFinancingConfigurations)
                    .ThenInclude(i => i.DisbursementConfigs)
                .Include(c => c.InputFinancingConfigurations)
                    .ThenInclude(i => i.RepaymentConfigs)
                .FirstOrDefaultAsync(c => c.OrganizationCode == config.OrganizationCode);

            if (existing == null)
                throw new KeyNotFoundException($"Organization {config.OrganizationCode} not found.");

            // ensure collections are initialized
            existing.InputFinancingConfigurations ??= new List<InputFinancingConfiguration>();

            if (config.InputFinancingConfig != null && config.InputFinancingConfig.Any())
            {
                foreach (var inputConfigModel in config.InputFinancingConfig)
                {
                    // attempt to match existing InputFinancingConfiguration by Id (or by Name if Id is not provided)
                    InputFinancingConfiguration existingInput = null;
                    if (inputConfigModel.Id != 0)
                        existingInput = existing.InputFinancingConfigurations.FirstOrDefault(i => i.Id == inputConfigModel.Id);


                    if (existingInput != null)
                    {
                        // update scalar props
                        existingInput.IsEnabled = inputConfigModel.IsEnabled;
                        existingInput.BundleEnabled = inputConfigModel.BundleEnabled;
                        existingInput.Name = inputConfigModel.ConfigurationName;
                        existingInput.ConfigurationCode = inputConfigModel.ConfigurationCode;
                        // ensure sub-collections initialized
                        existingInput.SubscriptionConfigs ??= new SubscriptionConfig();
                        existingInput.DisbursementConfigs ??= new DisbursementConfig();
                        existingInput.RepaymentConfigs ??= new RepaymentConfig();

                        // === replace SubscriptionConfigs if provided ===
                        if (inputConfigModel.SubscriptionConfig != null)
                        {
                            existingInput.SubscriptionConfigs = inputConfigModel.SubscriptionConfig.ToEntity();
                           
                        }

                        // === replace DisbursementConfigs if provided ===
                        if (inputConfigModel.DisbursementConfig != null)
                        {
                           
                                existingInput.DisbursementConfigs = inputConfigModel.DisbursementConfig.ToEntity();
                              
                        }

                        // === replace RepaymentConfigs if provided ===
                        if (inputConfigModel.RepaymentConfig != null)
                        {
                            existingInput.RepaymentConfigs = inputConfigModel.RepaymentConfig.ToEntity();                           
                        }
                    }
                    else
                    {
                        // add new InputFinancingConfiguration entity (including mapped sub-collections)
                        var newEntity = inputConfigModel.ToEntity();
                        existing.InputFinancingConfigurations.Add(newEntity);
                    }
                }

                await _context.SaveChangesAsync();
            }

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

        public async Task<InputFinancingConfigurationModel> GetInputConfigByCodeAsync(string configCode)
        {
            var configEntity = await _context.Set<InputFinancingConfiguration>()
                .Include(x=>x.SubscriptionConfigs)
                .Include(x=>x.RepaymentConfigs)
                .Include(x=>x.DisbursementConfigs)  
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ConfigurationCode == configCode);

            if (configEntity == null)
                return null;

            //SubscriptionConfigModel subscriptionConfig = null;

            //if (!string.IsNullOrWhiteSpace(configEntity.SubscriptionConfigs.ContributionSplitJson))
            //{
            //    try
            //    {
            //        subscriptionConfig = JsonConvert.DeserializeObject<SubscriptionConfigModel>(configEntity.SubscriptionConfigs.ContributionSplitJson);
            //    }
            //    catch (Exception ex)
            //    {
            //        throw new InvalidOperationException("Invalid subscription configuration format.", ex);
            //    }
            //}

            return configEntity.ToModel();

           
        }
    }
}


