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
                .Where(x=>x.Status == ProviderStatus.Active.ToString())
               // .Include(x=>x.WalletProviderParameters)
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
            if (entity == null)
            {
                return null;
            }
            return entity.Map();
        }

        public async Task<WalletProviderModel> Get(string providerCode)
        {
            var entities = await _context.Set<WalletProvider>()
                .Include(x => x.WalletProviderParameters)
                .Where(x => x.Code.ToLower() == providerCode.ToLower()).FirstOrDefaultAsync();

            return entities.Map();
        }

        public async Task<WalletProviderModel> GetByName(string name)
        {
            WalletProvider entity = await _context.Set<WalletProvider>()
             .Include(x => x.WalletProviderParameters)
             .FirstOrDefaultAsync(x => x.Code.ToLower() == name.ToLower());
            if (entity == null)
            {
                return null;
            }
            return entity.Map();
        }

        public async Task<WalletProviderParameterModel[]> GetParametersByProvider(long providerId, string Env)
        {
            WalletProviderParameter[] entities = await _context.Set<WalletProviderParameter>()
                                   .Where(x => x.WalletProviderProviderId == providerId && x.Env == Env)
                                   .ToArrayAsync();

            return entities.Select(c => c.Map()).ToArray();
        }

        public async Task<WalletProviderParameterModel> GetParametersById(long parameterId)
        {
            WalletProviderParameter entities = await _context.Set<WalletProviderParameter>()
                                   .Where(x => x.ParameterId == parameterId)
                                   .FirstOrDefaultAsync();

            return entities.Map();
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

          entity.Value= model.Value;
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

                        WalletProviderParameter[] creates = model.WalletProviderParameters
                                                   .Where(x =>  (x.Key != null || x.Value != null || x.Description != null))
                                                   .Select(x => x.Map())
                                                   .ToArray();

                        var createList = creates.Select(y => new WalletProviderParameter() { Key = y.Key, Description = y.Description, Value = y.Value, Env = y.Env, WalletProviderProviderId = entity.ProviderId });
                        _context.AddRange(createList);
                        await _context.SaveChangesAsync();

                        //WalletProviderParameter[] removeList = model.WalletProviderParameters.Where(x =>
                        //(x.ParamStatus == ParameterStatus.Remove) && (x.ParameterId != 0))
                        //.Select(x => x.Map()).ToArray();
                        //_context.RemoveRange(removeList);
                        //await _context.SaveChangesAsync();

                        //WalletProviderParameter[] updates = model.WalletProviderParameters.Where(x => x.ParamStatus == ParameterStatus.Update).Select(x => x.Map()).ToArray();
                        //_context.UpdateRange(updates);
                        //await _context.SaveChangesAsync();

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
