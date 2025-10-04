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

namespace Agrovesto.Backend.Infrastructure.Logic.Managers
{
    public interface ISettingsManager
    {
        Task<OrganizationConfigurationModel> GetByCodeAsync(BusinessRegistrationArgs args);
        //Task<IEnumerable<OrganizationConfigurationModel>> GetAllAsync();
        Task CreateOrgConfig(OrganizationConfigurationModel config, BusinessRegistrationArgs args);
        //Task UpdateAsync(OrganizationConfigurationModel config);
        //Task DeleteAsync(string orgId);
    }

    public class SettingsManager : ISettingsManager
    {
        private readonly IOrganizationConfigurationRepository _orgConfig;

        public SettingsManager(IOrganizationConfigurationRepository orgConfig)
        {
            _orgConfig = orgConfig;
        }

        public async Task CreateOrgConfig(OrganizationConfigurationModel config, BusinessRegistrationArgs args)
        {
            try
            {
                //var user = await _userRepo.GetByIdAsync(userId);
                //if (user == null) throw new Exception($"User {userId} not found");

                // Retrieve organization configuration
                config.OrganizationCode = args.organizationCode;
                config.OrganizationName = "OrgSIgnup";
                    await _orgConfig.CreateAsync(config);
                   
              }
            catch (Exception ex)
            {
                // _log.LogError(ex, "Error creating wallet for user {Email}", user.Email);
                throw;
            }
        }

        public async Task<OrganizationConfigurationModel> GetByCodeAsync(BusinessRegistrationArgs args)
        {
            return await _orgConfig.GetByCodeAsync(args.organizationCode);
        }
    }
}
