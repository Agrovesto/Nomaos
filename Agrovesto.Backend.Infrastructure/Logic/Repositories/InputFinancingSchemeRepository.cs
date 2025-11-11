using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories
{
    public interface IInputFinancingSchemeRepository
    {
        Task<InputFinancingScheme> AddAsync(InputFinancingScheme scheme);
        Task AddInputSubscription(InputSubscription subscription);
        Task DeleteAsync(long id);
        Task<bool> ExistsByNameAsync(string schemeName);
        Task<IEnumerable<InputFinancingScheme>> GetAllAsync(BusinessRegistrationArgs query);
        Task<IEnumerable<InputSubscription>> GetAllInputSubscriptionAsync(BusinessRegistrationArgs query);
        Task<InputFinancingScheme> GetByIdAsync(long id);
        Task<InputSubscription?> GetInputSubscriptionByIdAsync(long id);
        Task<IEnumerable<InputSubscription>> GetInputSubscriptionByUserIdAsync(string userId);
        Task<IEnumerable<InputSubscription>> GetSubscriptionByStatusAsync(string status);
        Task UpdateAsync(InputFinancingScheme scheme);
        Task UpdateSubscriptionStatusAsync(long id, string status, string verifiedBy);
    }
    public class InputFinancingSchemeRepository : IInputFinancingSchemeRepository
    {
        private readonly DataContext _context;

        public InputFinancingSchemeRepository(DataContext context)
        {
            _context = context;
        }

        public async Task<InputFinancingScheme> AddAsync(InputFinancingScheme scheme)
        {
            _context.InputFinancingSchemes.Add(scheme);
            await _context.SaveChangesAsync();
            return scheme;
        }

        public async Task<InputFinancingScheme> GetByIdAsync(long id)
        {
            return await _context.InputFinancingSchemes
                //.Include(s => s.Targets)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<InputFinancingScheme>> GetAllAsync(BusinessRegistrationArgs query)
        {

            var inputSchemes = _context.InputFinancingSchemes
              .Where(x => x.BusinessCode == query.businessCode)
              .AsQueryable();
            if (!string.IsNullOrEmpty(query.organizationCode))
            {
                inputSchemes = inputSchemes.Where(x=> x.OrganizationCode == query.organizationCode);
            }

                     var result = await inputSchemes
                .OrderByDescending(s => s.CreatedOn)
                .Skip(query.PageNumber)
                .Take(query.PageSize)
                 .Select(s => s).ToListAsync();


            if (result.Count > 0)
            {
                return result;
            }
            return null;

        }
        

        public async Task UpdateAsync(InputFinancingScheme scheme)
        {
            _context.InputFinancingSchemes.Update(scheme);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var scheme = await _context.InputFinancingSchemes.FindAsync(id);
            if (scheme != null)
            {
                _context.InputFinancingSchemes.Remove(scheme);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<bool> ExistsByNameAsync(string schemeName)
        {
            return await _context.InputFinancingSchemes.AnyAsync(s => s.SchemeName == schemeName);
        }


        public async Task AddInputSubscription(InputSubscription subscription)
        {
            // Basic validation and tracking
            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            await _context.InputSubscriptions.AddAsync(subscription);
            await _context.SaveChangesAsync();
        }

        public async Task<InputSubscription?> GetInputSubscriptionByIdAsync(long id)
        {
            return await _context.InputSubscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<IEnumerable<InputSubscription>> GetInputSubscriptionByUserIdAsync(string userId)
        {
          

            return await _context.InputSubscriptions
                .AsNoTracking()
                .Where(x => x.SubscribedByUserId == userId)
                .ToListAsync();
        }

        public async Task<IEnumerable<InputSubscription>> GetAllInputSubscriptionAsync(BusinessRegistrationArgs query)
        {
            var subscriptions = _context.InputSubscriptions
          .Where(x => x.BusinessCode == query.businessCode)
          .AsQueryable();
            if (!string.IsNullOrEmpty(query.organizationCode))
            {
                subscriptions = subscriptions.Where(x => x.OrganizationCode == query.organizationCode);
            }

            var result = await subscriptions
              .OrderByDescending(s => s.CreatedOn)
             // .Skip(query.PageNumber)
              //.Take(query.PageSize)
               .Select(s => s).ToListAsync();


            if (result.Count > 0)
            {
                return result;
            }
            return null;

        }
        public async Task<IEnumerable<InputSubscription>> GetSubscriptionByStatusAsync(string status)
        {
            return await _context.InputSubscriptions
                .Where(s => s.Status == status)
                .AsNoTracking()
                .ToListAsync();
        }
        public async Task UpdateSubscriptionStatusAsync(long id, string status, string verifiedBy)
        {
            var subscription = await _context.InputSubscriptions.FindAsync(id);
            if (subscription == null) return;

            subscription.Status = status;
            subscription.VerifiedBy = verifiedBy;
            subscription.VerifiedOn = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
    }
}

