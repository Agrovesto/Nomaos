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
    public interface IStorageCategoryRepository
    {
        Task<List<StorageCategory>> GetAllAsync();
        Task<StorageCategory> GetByIdAsync(long id);
        Task AddAsync(StorageCategory storageCategory);
        Task UpdateAsync(StorageCategory storageCategory);

    }
    public class StorageCategoryRepository : IStorageCategoryRepository
    {
        private readonly DataContext _context;
        public StorageCategoryRepository(DataContext context)
        {
            _context = context;
        }
        public async Task<List<StorageCategory>> GetAllAsync()
        {
            return await _context.StorageCategories.ToListAsync();
        }
        public async Task<StorageCategory> GetByIdAsync(long id)
        {
            return await _context.StorageCategories.FindAsync(id);
        }
        public async Task AddAsync(StorageCategory storageCategory)
        {
            _context.StorageCategories.Add(storageCategory);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync(StorageCategory storageCategory)
        {
            _context.StorageCategories.Update(storageCategory);
            await _context.SaveChangesAsync();
        }
    }
}
