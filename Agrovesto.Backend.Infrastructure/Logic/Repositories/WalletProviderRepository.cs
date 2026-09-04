using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Payment;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Microsoft.EntityFrameworkCore;
using SendGrid.Helpers.Errors.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories
{
    public class WalletProviderRepository : IWalletProviderRepository
    {
        private readonly DataContext _context;
        public WalletProviderRepository(DataContext context)
        {
            _context = context;
        }

        // ============================= Global provider catalog =============================

        public async Task<WalletProviderModel> ChangeStatus(long id, ProviderStatus providerStatus)
        {
            WalletProvider provider = await _context.Set<WalletProvider>().FindAsync(id);
            if (provider == null)
            {
                throw new NotFoundException("Provider can not be found");
            }
            provider.Status = providerStatus.ToString();
            int count = await _context.SaveChangesAsync();
            if (count < 1)
            {
                return null;
            }
            return provider.Map();
        }

        public async Task<WalletProviderModel> CreateProvider(WalletProviderModel model)
        {
            WalletProvider entity = model.Map();
            _context.Set<WalletProvider>().Add(entity);

            int count = await _context.SaveChangesAsync();
            if (count < 1)
            {
                return null;
            }
            return entity.Map();
        }

        public async Task<WalletProviderModel[]> GetAll()
        {
            WalletProvider[] entities = await _context.Set<WalletProvider>()
                //.Where(x => x.Status == ProviderStatus.Active.ToString()
                //)
                .OrderBy(x => x.Name).ToArrayAsync();

            return entities.Select(x => x.Map()).ToArray();
        }

        public async Task<WalletProviderModel[]> GetActiveProvider()
        {
            WalletProvider[] entities = await _context.Set<WalletProvider>()
                .Where(x => x.Status == ProviderStatus.Active.ToString())
                .OrderBy(x => x.Name).ToArrayAsync();

            return entities.Select(x => x.Map()).ToArray();
        }

        public async Task<WalletProviderModel> Get(long providerId)
        {
            WalletProvider entity = await _context.Set<WalletProvider>()
                .Include(x => x.WalletProviderParameters)
                .FirstOrDefaultAsync(x => x.ProviderId == providerId);

            return entity?.Map();
        }

        public async Task<WalletProviderModel> Get(string providerCode)
        {
            WalletProvider entity = await _context.Set<WalletProvider>()
                .Include(x => x.WalletProviderParameters)
                .FirstOrDefaultAsync(x => x.Code.ToLower() == providerCode.ToLower());

            return entity?.Map();
        }

        public async Task<WalletProviderModel> GetByName(string name)
        {
            WalletProvider entity = await _context.Set<WalletProvider>()
             .Include(x => x.WalletProviderParameters)
             .FirstOrDefaultAsync(x => x.Code.ToLower() == name.ToLower());

            return entity?.Map();
        }

        public async Task<WalletProviderParameterModel> GetParametersById(long parameterId)
        {
            WalletProviderParameter entity = await _context.Set<WalletProviderParameter>()
                                   .FirstOrDefaultAsync(x => x.ParameterId == parameterId);

            return entity?.Map();
        }

        public async Task<WalletProviderModel> UpdateProvider(WalletProviderModel model)
        {
            WalletProvider entity = await _context.Set<WalletProvider>().FindAsync(model.ProviderId);
            if (entity == null)
            {
                throw new Exception("Provider is invalid");
            }

            entity.Name = model.Name;
            entity.Code = model.Code;
            int count = await _context.SaveChangesAsync();
            if (count < 1)
            {
                return null;
            }
            return entity.Map();
        }

        public async Task<WalletProviderParameterModel> EditParameterValue(WalletProviderParameterModel model)
        {
            WalletProviderParameter entity = await _context.Set<WalletProviderParameter>().FindAsync(model.ParameterId);
            if (entity == null)
            {
                throw new Exception("Provider is invalid");
            }

            entity.Value = model.Value;
            int count = await _context.SaveChangesAsync();
            if (count < 1)
            {
                return null;
            }
            return entity.Map();
        }

        public async Task<WalletProviderModel> UpdateProviderParameter(WalletProviderModel model)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.Execute(async () =>
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var entity = await _context.Set<WalletProvider>()
                                                               .Include(x => x.WalletProviderParameters).AsNoTracking()
                                                               .FirstOrDefaultAsync(y => y.ProviderId == model.ProviderId);

                        if (entity == null)
                        {
                            transaction.Rollback();
                            return null;
                        }

                        WalletProviderParameter[] creates = model.WalletProviderParameters
                                                   .Where(x => (x.Key != null || x.Value != null || x.Description != null))
                                                   .Select(x => x.Map())
                                                   .ToArray();

                        var createList = creates.Select(y => new WalletProviderParameter()
                        {
                            Key = y.Key,
                            Description = y.Description,
                            Value = y.Value,
                            Env = y.Env,
                            WalletProviderProviderId = entity.ProviderId
                        });
                        _context.AddRange(createList);
                        await _context.SaveChangesAsync();

                        transaction.Commit();

                        return entity.Map();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                    }
                    return null;
                }
            });
        }

        // Kept for backward compatibility — GLOBAL defaults only, no tenant override applied.
        public async Task<WalletProviderParameterModel[]> GetParametersByProvider(long providerId, string env)
        {
            WalletProviderParameter[] entities = await _context.Set<WalletProviderParameter>()
                                   .Where(x => x.WalletProviderProviderId == providerId && x.Env == env)
                                   .ToArrayAsync();

            return entities.Select(c => c.Map()).ToArray();
        }

        // ============================= Business-scoped (multi-tenant) layer =============================

        public async Task<BusinessWalletProviderModel[]> GetProvidersForBusiness(string businessCode)
        {
            var normalizedCode = businessCode.ToLower();
            var activeStatus = ProviderStatus.Active.ToString().ToLower();

            BusinessWalletProvider[] entities = await _context.Set<BusinessWalletProvider>()
                .Include(x => x.WalletProvider)
                .Where(x => x.BusinessCode.ToLower() == normalizedCode
                         && x.Status.ToLower() == activeStatus)
                .OrderBy(x => x.WalletProvider.Name)
                .ToArrayAsync();

            return entities.Select(x => x.Map()).ToArray();
        }

        public async Task<BusinessWalletProviderModel> EnableProviderForBusiness(
            string businessCode,
            long providerId,
            WalletProviderParameterModel[] overrideParameters,
            bool isDefault = false)
        {
            bool providerExists = await _context.Set<WalletProvider>().AnyAsync(x => x.ProviderId == providerId);
            if (!providerExists)
            {
                throw new NotFoundException("Provider can not be found");
            }

            bool alreadyEnabled = await _context.Set<BusinessWalletProvider>()
                .AnyAsync(x => x.BusinessCode.ToLower() == businessCode.ToLower() && x.ProviderId == providerId);
            if (alreadyEnabled)
            {
                throw new Exception("Provider is already enabled for this business");
            }

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.Execute(async () =>
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var junction = new BusinessWalletProvider
                        {
                            BusinessCode = businessCode,
                            ProviderId = providerId,
                            Status = ProviderStatus.Active.ToString(),
                            IsDefault = isDefault
                        };
                        _context.Set<BusinessWalletProvider>().Add(junction);
                        await _context.SaveChangesAsync();

                        if (overrideParameters != null && overrideParameters.Length > 0)
                        {
                            var parameterEntities = overrideParameters
                                .Where(x => x.Key != null || x.Value != null)
                                .Select(x => new BusinessWalletProviderParameter
                                {
                                    BusinessWalletProviderId = junction.BusinessWalletProviderId,
                                    Key = x.Key,
                                    Value = x.Value,
                                    Description = x.Description,
                                    Env = x.Env.ToString()
                                });
                            _context.Set<BusinessWalletProviderParameter>().AddRange(parameterEntities);
                            await _context.SaveChangesAsync();
                        }

                        transaction.Commit();

                        var result = await _context.Set<BusinessWalletProvider>()
                            .Include(x => x.WalletProvider)
                            .Include(x => x.Parameters)
                            .FirstOrDefaultAsync(x => x.BusinessWalletProviderId == junction.BusinessWalletProviderId);

                        return result.Map();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            });
        }

        public async Task<bool> DisableProviderForBusiness(string businessCode, long providerId)
        {
            var junction = await _context.Set<BusinessWalletProvider>()
                .FirstOrDefaultAsync(x => x.BusinessCode.ToLower() == businessCode.ToLower()
                                        && x.ProviderId == providerId);
            if (junction == null)
            {
                return false;
            }

            junction.Status = ProviderStatus.Disabled.ToString();
            int count = await _context.SaveChangesAsync();
            return count > 0;
        }

        /// <summary>
        /// Effective parameters for a business calling a provider: global defaults merged with
        /// business-level overrides — business value wins on a matching Key. Use this at runtime.
        /// </summary>
        public async Task<WalletProviderParameterModel[]> GetParametersByProvider(
            long providerId, string businessCode, string env)
        {
            var normalisedEnv = (env ?? string.Empty).Trim().ToLower();

            WalletProviderParameter[] defaults = await _context.Set<WalletProviderParameter>()
                .Where(x => x.WalletProviderProviderId == providerId && x.Env.ToLower() == normalisedEnv)
                .ToArrayAsync();

            BusinessWalletProviderParameter[] overrides = await _context.Set<BusinessWalletProviderParameter>()
                .Include(x => x.BusinessWalletProvider)
                .Where(x => x.BusinessWalletProvider.ProviderId == providerId
                         && x.BusinessWalletProvider.BusinessCode.ToLower() == businessCode.ToLower()
                         && x.Env.ToLower() == normalisedEnv)
                .ToArrayAsync();

            var merged = new Dictionary<string, WalletProviderParameterModel>(StringComparer.OrdinalIgnoreCase);

            foreach (var d in defaults)
            {
                merged[d.Key] = d.Map();
            }

            foreach (var o in overrides)
            {
                merged[o.Key] = new WalletProviderParameterModel
                {
                    ParameterId = o.ParameterId,
                    Key = o.Key,
                    Value = o.Value,
                    Description = o.Description,
                    ProviderId = providerId,
                    Env = Enum.Parse<Env>(o.Env)
                };
            }

            return merged.Values.ToArray();
        }

        public async Task<BusinessWalletProviderParameterModel> EditBusinessParameterValue(
            BusinessWalletProviderParameterModel model)
        {
            BusinessWalletProviderParameter entity = await _context.Set<BusinessWalletProviderParameter>()
                .FindAsync(model.ParameterId);
            if (entity == null)
            {
                throw new NotFoundException("Business parameter override can not be found");
            }

            entity.Value = model.Value;
            int count = await _context.SaveChangesAsync();
            if (count < 1)
            {
                return null;
            }
            return entity.Map();
        }
    }
}


