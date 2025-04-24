using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
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
    public class PaymentProviderRepository : IPaymentProviderRepository
    {
        private readonly DataContext _context;
        public PaymentProviderRepository(DataContext context)
        {
            _context = context;
        }
        public async Task<PaymentProviderModel> ChangeStatus(long id, ProviderStatus providerStatus)
        {
            PaymentProvider provider = await _context.Set<PaymentProvider>().FindAsync(id);
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

        public async Task<PaymentProviderModel> CreateProvider(PaymentProviderModel model)
        {
            PaymentProvider entity = model.Map();
            _context.Set<PaymentProvider>().Add(entity);

            int count = await _context.SaveChangesAsync();
            if (count < 1)
            {
                return null;
            }
            return entity.Map();
        }

        public async Task<PaymentProviderModel[]> Get()
        {
            PaymentProvider[] entities = await _context.Set<PaymentProvider>()

                .OrderBy(x => x.Name).ToArrayAsync();

            return entities.Select(x => x.Map()).ToArray();
        }

        public async Task<PaymentProviderModel[]> GetActiveProvider()
        {
            PaymentProvider[] entities = await _context.Set<PaymentProvider>()
                .Where(x => x.Status == ProviderStatus.Active.ToString())
                .OrderBy(x => x.Name).ToArrayAsync();

            return entities.Select(x => x.Map()).ToArray();
        }

        public async Task<PaymentProviderModel> Get(long providerId)
        {
            PaymentProvider entity = await _context.Set<PaymentProvider>()

                .Include(x => x.PaymentProviderParameters)
                .FirstOrDefaultAsync(x => x.ProviderId == providerId);
            if (entity == null)
            {
                return null;
            }
            return entity.Map();
        }

        public async Task<PaymentProviderModel> Get(string providerCode)
        {
            var entities = await _context.Set<PaymentProvider>()
                .Include(x => x.PaymentProviderParameters)
                .Where(x => x.Code.ToLower() == providerCode.ToLower()).FirstOrDefaultAsync();

            return entities.Map();
        }

        public async Task<PaymentProviderModel> GetByName(string name)
        {
            PaymentProvider entity = await _context.Set<PaymentProvider>()
             .Include(x => x.PaymentProviderParameters)
             .FirstOrDefaultAsync(x => x.Code.ToLower() == name.ToLower());
            if (entity == null)
            {
                return null;
            }
            return entity.Map();
        }

        public async Task<ProviderParameterModel[]> GetParametersByProvider(long providerId, string Env)
        {
            PaymentProviderParameter[] entities = await _context.Set<PaymentProviderParameter>()
                                   .Where(x => x.ProviderId == providerId && x.Env == Env)
                                   .ToArrayAsync();

            return entities.Select(c => c.Map()).ToArray();
        }

        public async Task<PaymentProviderModel> UpdateProvider(PaymentProviderModel model)
        {
            PaymentProvider entity = await _context.Set<PaymentProvider>().FindAsync(model.ProviderId);
            if (entity == null)
            {
                throw new Core.Exceptions.ValidationException("Provider is invalid");
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

        public async Task<PaymentProviderModel> UpdateProviderParameter(PaymentProviderModel model)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.Execute(async () =>
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var entity = await _context.Set<PaymentProvider>()
                                                               .Include(x => x.PaymentProviderParameters).AsNoTracking()
                                                               .FirstOrDefaultAsync(y => y.ProviderId == model.ProviderId);

                        PaymentProviderParameter[] creates = model.PaymentProviderParameters
                                                   .Where(x => (x.ParamStatus == ParameterStatus.Create) && (x.Key != null || x.Value != null || x.Description != null))
                                                   .Select(x => x.Map())
                                                   .ToArray();

                        var createList = creates.Select(y => new PaymentProviderParameter() { Key = y.Key, Description = y.Description, Value = y.Value, Env = y.Env, ProviderId = entity.ProviderId });
                        _context.AddRange(createList);
                        await _context.SaveChangesAsync();

                        PaymentProviderParameter[] removeList = model.PaymentProviderParameters.Where(x => (x.ParamStatus == ParameterStatus.Remove) && (x.ParameterId != 0))
                        .Select(x => x.Map()).ToArray();
                        _context.RemoveRange(removeList);
                        await _context.SaveChangesAsync();

                        PaymentProviderParameter[] updates = model.PaymentProviderParameters.Where(x => x.ParamStatus == ParameterStatus.Update).Select(x => x.Map()).ToArray();
                        _context.UpdateRange(updates);
                        await _context.SaveChangesAsync();

                        transaction.Commit();

                        return entity.Map();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                    }
                    return null;
                }

            });
        }



    }
}


