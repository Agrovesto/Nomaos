using Agrovesto.Backend.Core.Models.Businesses;
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
        Task<List<StorageCategory>> GetAllAsync(ListQueryParams args);
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
        public async Task<List<StorageCategory>> GetAllAsync(ListQueryParams args)
        {
            return await _context.StorageCategories.Where(x=>x.BusinessCode == args.businessCode).ToListAsync();
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
