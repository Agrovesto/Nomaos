using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses.Wallets.FCMBModels;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Agrovesto.Backend.Core.CoreUtils.Constants;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.ExternalServices.WalletServices;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers
{
    public interface ISettingsManager
    {
        Task<OrganizationConfigurationModel> GetByCodeAsync(BusinessRegistrationArgs args);
        //Task<IEnumerable<OrganizationConfigurationModel>> GetAllAsync();
        Task CreateOrUpdateOrgConfig(OrganizationConfigurationModel config, BusinessRegistrationArgs args);
        //Task UpdateAsync(OrganizationConfigurationModel config);
        //Task DeleteAsync(string orgId);
    }

    public class SettingsManager : ISettingsManager
    {
        private readonly IOrganizationConfigurationRepository _orgConfig;
        private readonly IBusinessRepository _businessRepository;
        private readonly ILogger<SettingsManager> _log;

        public SettingsManager(ILogger<SettingsManager> log,IOrganizationConfigurationRepository orgConfig, IBusinessRepository businessRepository)
        {
            _orgConfig = orgConfig;
            _businessRepository = businessRepository;
            _log = log;
        }

        public async Task CreateOrUpdateOrgConfig(OrganizationConfigurationModel config, BusinessRegistrationArgs args)
        {
            try
            {
                var org = await _businessRepository.GetOrganizationAsync(args.organizationCode);
                if (org == null) { throw new Exception($"Organization not found"); }

                        config.OrganizationCode = args.organizationCode;
                config.OrganizationName = org.OrganizationName;

                var hasDefault = false;
                // Retrieve organization configuration
                var orgConfig = await _orgConfig.GetByCodeAsync(args.organizationCode);
                if (config.WalletProviderConfiguration != null && config.WalletProviderConfiguration.Any())
                {
                  

                    foreach (var wallet in config.WalletProviderConfiguration)
                    {
                        if (!string.IsNullOrEmpty(wallet?.DefaultWalletProvider))
                        {
                            if (hasDefault)
                            {
                                // More than one default — disallow
                                throw new Exception("Only one wallet provider can be marked as the default wallet for this organization.");
                            }

                            hasDefault = true;
                        }
                    }

                    // If one is default, ensure all others have DefaultWalletProvider = null
                    if (hasDefault)
                    {
                        foreach (var wallet in config.WalletProviderConfiguration)
                        {
                            if (string.IsNullOrEmpty(wallet?.DefaultWalletProvider))
                            {
                                wallet.DefaultWalletProvider = null;
                            }
                        }
                    }
                }

                if (orgConfig == null) 
                {
                       await _orgConfig.CreateAsync(config);
                }
                else if(orgConfig != null &&  config.WalletProviderConfiguration.Count > 0)
                {

                    
                    await _orgConfig.UpdateWalletConfiguration(config);
                }
                else if(orgConfig !=null && config.InputFinancingConfig.Count > 0)
                {
                   
                    await _orgConfig.UpdateInputConfiguration(config);
                }
                
                   
              }
            catch (Exception ex)
            {
                 _log.LogError(ex, "Error Creating/Updating Organization configuration for Organization ", args.organizationCode);
                throw;
            }
        }

        public async Task<OrganizationConfigurationModel> GetByCodeAsync(BusinessRegistrationArgs args)
        {
            return await _orgConfig.GetByCodeAsync(args.organizationCode);
        }


    }
}
