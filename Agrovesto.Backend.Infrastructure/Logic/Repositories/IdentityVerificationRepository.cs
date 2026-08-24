using Agrovesto.Backend.Core.Exceptions;
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
    public interface IIdentityVerificationRepository
    {
        Task<IdentityVerificationProviderModel> CreateProvider(IdentityVerificationProviderModel model);
        Task<IdentityVerificationProviderModel> UpdateProvider(IdentityVerificationProviderModel model);
        Task<IdentityVerificationProviderModel> ChangeStatus(long id, ProviderStatus providerStatus);
        Task<IdentityVerificationProviderModel[]> GetAll();
        Task<IdentityVerificationProviderModel> Get(long providerId);
        Task<IdentityVerificationProviderParameterModel> GetParametersById(long parameterId);
        Task<IdentityVerificationProviderParameterModel[]> GetParametersByProvider(long providerId, string env);
        Task<IdentityVerificationProviderModel> UpdateProviderParameter(IdentityVerificationProviderModel model);
    }
    public class IdentityVerificationRepository : IIdentityVerificationRepository
    {
        private readonly DataContext _context;
        public IdentityVerificationRepository(DataContext context)
        {
            _context = context;
        }

        public async Task<IdentityVerificationProviderModel> ChangeStatus(long id, ProviderStatus providerStatus)
        {
            IdentityVerificationProvider provider = await _context.Set<IdentityVerificationProvider>().FindAsync(id);
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

        public async Task<IdentityVerificationProviderModel> CreateProvider(IdentityVerificationProviderModel model)
        {

            IdentityVerificationProvider entity = model.Map();
            _context.Set<IdentityVerificationProvider>().Add(entity);

            int count = await _context.SaveChangesAsync();
            if (count < 1)
            {
                return null;
            }
            return entity.Map();
        }

        public async Task<IdentityVerificationProviderModel> Get(long providerId)
        {
            IdentityVerificationProvider entity = await _context.Set<IdentityVerificationProvider>()
                 .Include(x => x.Parameters)
                 .FirstOrDefaultAsync(x => x.ProviderId == providerId);

            return entity?.Map();
        }

        public async Task<IdentityVerificationProviderModel[]> GetAll()
        {
            IdentityVerificationProvider[] entities = await _context.Set<IdentityVerificationProvider>()
                            .Where(x => x.Status == ProviderStatus.Active.ToString())
                            .OrderBy(x => x.Name).ToArrayAsync();

            return entities.Select(x => x.Map()).ToArray();
        }

        public async Task<IdentityVerificationProviderParameterModel> GetParametersById(long parameterId)
        {
            IdentityVerificationProviderParameter entity = await _context.Set<IdentityVerificationProviderParameter>()
                                   .FirstOrDefaultAsync(x => x.ParameterId == parameterId);

            return entity?.Map();
        }

        public async Task<IdentityVerificationProviderParameterModel[]> GetParametersByProvider(long providerId, string env)
        {
            IdentityVerificationProviderParameter[] entities = await _context.Set<IdentityVerificationProviderParameter>()
                                    .Where(x => x.ProviderId == providerId && x.Env == env)
                                    .ToArrayAsync();

            return entities.Select(c => c.Map()).ToArray();
        }

        public async Task<IdentityVerificationProviderModel> UpdateProvider(IdentityVerificationProviderModel model)
        {
            IdentityVerificationProvider entity = await _context.Set<IdentityVerificationProvider>().FindAsync(model.ProviderId);
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

        public async Task<IdentityVerificationProviderModel> UpdateProviderParameter(IdentityVerificationProviderModel model)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.Execute(async () =>
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var entity = await _context.Set<IdentityVerificationProvider>()
                                                               .Include(x => x.Parameters).AsNoTracking()
                                                               .FirstOrDefaultAsync(y => y.ProviderId == model.ProviderId);

                        if (entity == null)
                        {
                            transaction.Rollback();
                            return null;
                        }

                        IdentityVerificationProviderParameter[] creates = model.Parameters
                                                   .Where(x => (x.Key != null || x.Value != null || x.Description != null))
                                                   .Select(x => x.Map())
                                                   .ToArray();

                        var createList = creates.Select(y => new IdentityVerificationProviderParameter()
                        {
                            Key = y.Key,
                            Description = y.Description,
                            Value = y.Value,
                            Env = y.Env,
                            ProviderId = entity.ProviderId
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

    }
}
