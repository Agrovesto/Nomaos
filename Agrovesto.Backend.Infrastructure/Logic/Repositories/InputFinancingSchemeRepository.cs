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
        Task DeleteAsync(long id);
        Task<bool> ExistsByNameAsync(string schemeName);
        Task<IEnumerable<InputFinancingScheme>> GetAllAsync();
        Task<InputFinancingScheme> GetByIdAsync(long id);
        Task UpdateAsync(InputFinancingScheme scheme);
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

        public async Task<IEnumerable<InputFinancingScheme>> GetAllAsync()
        {
            return await _context.InputFinancingSchemes
               // .Include(s => s.Targets)
                .ToListAsync();
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
    }
}
