using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories.V2
{
    public interface IModuleRepository
    {
        Task<List<Module>> GetAllAsync();
        Task<Module> GetByIdAsync(long id);
        Task AddAsync(Module module);
        Task UpdateAsync(Module module);
    }

    public class ModuleRepository : IModuleRepository
    {
        private readonly DataContext _context;
        public ModuleRepository(DataContext context)
        {
            _context = context;
        }
        public async Task<List<Module>> GetAllAsync()
        {
            return await _context.Modules.ToListAsync();
        }
        public async Task<Module> GetByIdAsync(long id)
        {
            return await _context.Modules.FindAsync(id);
        }
        public async Task AddAsync(Module module)
        {
            _context.Modules.Add(module);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync(Module module)
        {
            _context.Modules.Update(module);
            await _context.SaveChangesAsync();
        }
    }
}
