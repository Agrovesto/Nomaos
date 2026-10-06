using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Payment;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories
{
    public interface ISMSProviderRepository
    {
        // ===================== Global provider catalog (unchanged) =====================
        Task<SMSProviderModel> ChangeStatus(long id, ProviderStatus providerStatus);
        Task<SMSProviderModel> CreateProvider(SMSProviderModel model);
        Task<SMSProviderModel> Get(string providerCode);
        Task<SMSProviderModel> Get(long providerId);
        Task<SMSProviderModel[]> GetAll();
        Task<SMSProviderModel[]> GetActiveProvider();
        Task<SMSProviderModel> GetByName(string name);
        Task<SMSProviderModel> UpdateProvider(SMSProviderModel model);
        Task<SMSProviderModel> UpdateProviderParameter(SMSProviderModel model);
        Task<SMSProviderParameterModel> GetParametersById(long parameterId);
        Task<SMSProviderParameterModel> EditParameterValue(SMSProviderParameterModel model);

        Task<SMSProviderParameterModel[]> GetParametersByProvider(long providerId, string env);

        Task<BusinessSMSProviderModel[]> GetProvidersForBusiness(string businessCode);

              Task<BusinessSMSProviderModel> EnableProviderForBusiness(
            string businessCode, long providerId, SMSProviderParameterModel[] overrideParameters, bool isDefault = false);

           Task<bool> DisableProviderForBusiness(string businessCode, long providerId);

         Task<SMSProviderParameterModel[]> GetParametersByProvider(long providerId, string businessCode, string env);

        Task<BusinessSMSProviderParameterModel> EditBusinessParameterValue(BusinessSMSProviderParameterModel model);
    }

    public class SMSProviderRepository : ISMSProviderRepository
    {
        private readonly DataContext _context;
        private readonly IOutboxWriter _outboxWriter;

        public SMSProviderRepository(DataContext context, IOutboxWriter outboxWriter)
        {
            _context = context;
            _outboxWriter = outboxWriter;
        }

           public async Task<SMSProviderModel> ChangeStatus(long id, ProviderStatus providerStatus)
        {
            SMSProvider provider = await _context.Set<SMSProvider>().FindAsync(id);
            if (provider == null)
            {
                throw new NotFoundException("Provider can not be found");
            }
            provider.Status = providerStatus.ToString();
            EnqueueProviderStatusChanged("sms", provider.ProviderId, providerStatus);
            int count = await _context.SaveChangesAsync();
            if (count < 1)
            {
                return null;
            }
            return provider.Map();
        }

        public async Task<SMSProviderModel> CreateProvider(SMSProviderModel model)
        {
            SMSProvider entity = model.Map();
            _context.Set<SMSProvider>().Add(entity);
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await _context.SaveChangesAsync();
            EnqueueProviderCreated("sms", entity.ProviderId, entity.Code);
            int count = await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            if (count < 1)
            {
                return null;
            }
            return entity.Map();
        }

        public async Task<SMSProviderModel[]> GetAll()
        {
            SMSProvider[] entities = await _context.Set<SMSProvider>()
                .Where(x => x.Status == ProviderStatus.Active.ToString())
                .OrderBy(x => x.Name).ToArrayAsync();

            return entities.Select(x => x.Map()).ToArray();
        }

        public async Task<SMSProviderModel[]> GetActiveProvider()
        {
            SMSProvider[] entities = await _context.Set<SMSProvider>()
                .Where(x => x.Status == ProviderStatus.Active.ToString())
                .OrderBy(x => x.Name).ToArrayAsync();

            return entities.Select(x => x.Map()).ToArray();
        }

        public async Task<SMSProviderModel> Get(long providerId)
        {
            SMSProvider entity = await _context.Set<SMSProvider>()
                .Include(x => x.SMSProviderParameters)
                .FirstOrDefaultAsync(x => x.ProviderId == providerId);

            return entity?.Map();
        }

        public async Task<SMSProviderModel> Get(string providerCode)
        {
            SMSProvider entity = await _context.Set<SMSProvider>()
                .Include(x => x.SMSProviderParameters)
                .FirstOrDefaultAsync(x => x.Code.ToLower() == providerCode.ToLower());

            return entity?.Map();
        }

        public async Task<SMSProviderModel> GetByName(string name)
        {
            SMSProvider entity = await _context.Set<SMSProvider>()
             .Include(x => x.SMSProviderParameters)
             .FirstOrDefaultAsync(x => x.Code.ToLower() == name.ToLower());

            return entity?.Map();
        }

        public async Task<SMSProviderParameterModel> GetParametersById(long parameterId)
        {
            SMSProviderParameter entity = await _context.Set<SMSProviderParameter>()
                                   .FirstOrDefaultAsync(x => x.ParameterId == parameterId);

            return entity?.Map();
        }

        public async Task<SMSProviderModel> UpdateProvider(SMSProviderModel model)
        {
            SMSProvider entity = await _context.Set<SMSProvider>().FindAsync(model.ProviderId);
            if (entity == null)
            {
                throw new Exception("Provider is invalid");
            }

            entity.Name = model.Name;
            entity.Code = model.Code;
            EnqueueProviderConfigurationChanged("sms", entity.ProviderId);
            int count = await _context.SaveChangesAsync();
            if (count < 1)
            {
                return null;
            }
            return entity.Map();
        }

        public async Task<SMSProviderParameterModel> EditParameterValue(SMSProviderParameterModel model)
        {
            SMSProviderParameter entity = await _context.Set<SMSProviderParameter>().FindAsync(model.ParameterId);
            if (entity == null)
            {
                throw new Exception("Provider is invalid");
            }

            entity.Value = model.Value;
            EnqueueProviderConfigurationChanged("sms", entity.SMSProviderProviderId);
            int count = await _context.SaveChangesAsync();
            if (count < 1)
            {
                return null;
            }
            return entity.Map();
        }

        public async Task<SMSProviderModel> UpdateProviderParameter(SMSProviderModel model)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.Execute(async () =>
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var entity = await _context.Set<SMSProvider>()
                                                               .Include(x => x.SMSProviderParameters).AsNoTracking()
                                                               .FirstOrDefaultAsync(y => y.ProviderId == model.ProviderId);

                        if (entity == null)
                        {
                            transaction.Rollback();
                            return null;
                        }

                        SMSProviderParameter[] creates = model.SMSProviderParameters
                                                   .Where(x => (x.Key != null || x.Value != null || x.Description != null))
                                                   .Select(x => x.Map())
                                                   .ToArray();

                        var createList = creates.Select(y => new SMSProviderParameter()
                        {
                            Key = y.Key,
                            Description = y.Description,
                            Value = y.Value,
                            Env = y.Env,
                            SMSProviderProviderId = entity.ProviderId
                        });
                        _context.AddRange(createList);
                        await _context.SaveChangesAsync();
                        EnqueueProviderConfigurationChanged("sms", entity.ProviderId);
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
  public async Task<SMSProviderParameterModel[]> GetParametersByProvider(long providerId, string env)
        {
            SMSProviderParameter[] entities = await _context.Set<SMSProviderParameter>()
                                   .Where(x => x.SMSProviderProviderId == providerId && x.Env == env)
                                   .ToArrayAsync();

            return entities.Select(c => c.Map()).ToArray();
        }

       
        public async Task<BusinessSMSProviderModel[]> GetProvidersForBusiness(string businessCode)
        {
            if (string.IsNullOrWhiteSpace(businessCode))
               throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST, "businessCode is required.");
            BusinessSMSProvider[] entities = await _context.Set<BusinessSMSProvider>()
                .Include(x => x.SMSProvider)
                .Where(x => x.BusinessCode.ToLower() == businessCode.ToLower()
                         && x.Status == ProviderStatus.Active.ToString()
                         && x.SMSProvider != null)
                .OrderBy(x => x.SMSProvider.Name)
                .ToArrayAsync();

            return entities.Select(x => x.Map()).ToArray();
        }
        public async Task<BusinessSMSProviderModel> EnableProviderForBusiness(
            string businessCode,
            long providerId,
            SMSProviderParameterModel[] overrideParameters,
            bool isDefault = false)
        {
            bool providerExists = await _context.Set<SMSProvider>().AnyAsync(x => x.ProviderId == providerId);
            if (!providerExists)
            {
                throw new NotFoundException("Provider can not be found");
            }

            bool alreadyEnabled = await _context.Set<BusinessSMSProvider>()
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
                        var junction = new BusinessSMSProvider
                        {
                            BusinessCode = businessCode,
                            ProviderId = providerId,
                            Status = ProviderStatus.Active.ToString(),
                            IsDefault = isDefault
                        };
                        _context.Set<BusinessSMSProvider>().Add(junction);
                        await _context.SaveChangesAsync();

                        if (overrideParameters != null && overrideParameters.Length > 0)
                        {
                            var parameterEntities = overrideParameters
                                .Where(x => x.Key != null || x.Value != null)
                                .Select(x => new BusinessSMSProviderParameter
                                {
                                    BusinessSMSProviderId = junction.BusinessSMSProviderId,
                                    Key = x.Key,
                                    Value = x.Value,
                                    Description = x.Description,
                                    Env = x.Env.ToString()
                                });
                            _context.Set<BusinessSMSProviderParameter>().AddRange(parameterEntities);
                            await _context.SaveChangesAsync();
                        }

                        EnqueueProviderConfigurationChanged("sms", providerId, businessCode);
                        await _context.SaveChangesAsync();
                        transaction.Commit();

                        var result = await _context.Set<BusinessSMSProvider>()
                            .Include(x => x.SMSProvider)
                            .Include(x => x.Parameters)
                            .FirstOrDefaultAsync(x => x.BusinessSMSProviderId == junction.BusinessSMSProviderId);

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
            var junction = await _context.Set<BusinessSMSProvider>()
                .FirstOrDefaultAsync(x => x.BusinessCode.ToLower() == businessCode.ToLower()
                                        && x.ProviderId == providerId);
            if (junction == null)
            {
                return false;
            }

            junction.Status = ProviderStatus.Disabled.ToString();
            EnqueueProviderStatusChanged("sms", providerId, ProviderStatus.Disabled, businessCode);
            int count = await _context.SaveChangesAsync();
            return count > 0;
        }

        /// <summary>
        /// Effective parameters for a business calling a provider: global defaults merged with
        /// business-level overrides — business value wins on a matching Key. Use this at runtime.
        /// </summary>
        public async Task<SMSProviderParameterModel[]> GetParametersByProvider(
            long providerId, string businessCode, string env)
        {
            SMSProviderParameter[] defaults = await _context.Set<SMSProviderParameter>()
                .Where(x => x.SMSProviderProviderId == providerId && x.Env == env)
                .ToArrayAsync();

            BusinessSMSProviderParameter[] overrides = await _context.Set<BusinessSMSProviderParameter>()
                .Include(x => x.BusinessSMSProvider)
                .Where(x => x.BusinessSMSProvider.ProviderId == providerId
                         && x.BusinessSMSProvider.BusinessCode.ToLower() == businessCode.ToLower()
                         && x.Env == env)
                .ToArrayAsync();

            var merged = new Dictionary<string, SMSProviderParameterModel>(StringComparer.OrdinalIgnoreCase);

            foreach (var d in defaults)
            {
                merged[d.Key] = d.Map();
            }

            foreach (var o in overrides)
            {
                merged[o.Key] = new SMSProviderParameterModel
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

        public async Task<BusinessSMSProviderParameterModel> EditBusinessParameterValue(
            BusinessSMSProviderParameterModel model)
        {
            BusinessSMSProviderParameter entity = await _context.Set<BusinessSMSProviderParameter>()
                .Include(x => x.BusinessSMSProvider)
                .FirstOrDefaultAsync(x => x.ParameterId == model.ParameterId);
            if (entity == null)
            {
                throw new NotFoundException("Business parameter override can not be found");
            }

            entity.Value = model.Value;
            EnqueueProviderConfigurationChanged("sms", entity.BusinessSMSProvider.ProviderId, entity.BusinessSMSProvider.BusinessCode);
            int count = await _context.SaveChangesAsync();
            if (count < 1)
            {
                return null;
            }
            return entity.Map();
        }

        private void EnqueueProviderCreated(string providerType, long providerId, string code) =>
            _outboxWriter.Enqueue("provider.created", new { providerType, providerId, code });

        private void EnqueueProviderStatusChanged(string providerType, long providerId, ProviderStatus status, string businessCode = null) =>
            _outboxWriter.Enqueue(
                status == ProviderStatus.Active ? "provider.enabled" : "provider.disabled",
                new { providerType, providerId, status = status.ToString(), businessCode });

        private void EnqueueProviderConfigurationChanged(string providerType, long providerId, string businessCode = null) =>
            _outboxWriter.Enqueue("provider.configuration-changed", new { providerType, providerId, businessCode });
    }

}
