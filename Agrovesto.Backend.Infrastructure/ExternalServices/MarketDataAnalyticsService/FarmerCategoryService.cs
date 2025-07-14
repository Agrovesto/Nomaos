using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.MarketDataAnalyticsService
{
    public class FarmerCategoryService : IFarmerCategoryService
    {
        private readonly DataContext _context;
        private readonly string _connectionString;
        private readonly string _jsonFilePath;
        private readonly ILogger<NigeriaStateImporterService> _logger;

        public FarmerCategoryService(DataContext context,
            IHostEnvironment environment, IConfiguration configuration, ILogger<NigeriaStateImporterService> logger)
        {
            _context = context;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
          
            string relativePath = configuration["FarmerCategoryPath"];
            _jsonFilePath = Path.Combine(environment.ContentRootPath, relativePath);

            _logger = logger;
        }

        public async Task<List<FarmerCategoryModel>> GetFarmerCategoryAsync()
        {
            var categories = await _context.FarmerCategorys.AsNoTracking().ToListAsync();
            return categories.Select(x => x.Map()).ToList();
        }
        public async Task<List<ValueChainTypeModel>> GetValueChangeByCategory(long categoryId)
        {
            var categoryExists = await _context.FarmerCategorys.AnyAsync(s => s.Id == categoryId);
            if (!categoryExists)
                return null;

            var types = await _context.ValueChainTypes.Where(l => l.FarmerCategorysId == categoryId)
                .AsNoTracking()
                .ToListAsync();
            return types.Select(x => x.Map()).ToList();
        }

        public async Task<List<FarmerValueChainItemModel>> GetValueChainItemByType(long typeId)
        {
            var typeExists = await _context.ValueChainTypes.AnyAsync(s => s.Id == typeId);
            if (!typeExists)
                return null;

            var items = await _context.FarmerValueChainItems.Where(w => w.ValueChainTypeId == typeId)
                .AsNoTracking()
                .ToListAsync();
            return items.Select(x => x.Map()).ToList();
        }
        public async Task ImportFarmerValuesFromJsonAsync()
        {
            if (!File.Exists(_jsonFilePath))
            {
                _logger.LogError("JSON file not found at {Path}", _jsonFilePath);
                throw new FileNotFoundException("JSON file not found.", _jsonFilePath);
            }

            string jsonData = await File.ReadAllTextAsync(_jsonFilePath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            List<FarmerCategorys> incomingCategories = JsonSerializer.Deserialize<List<FarmerCategorys>>(jsonData, options);

            await ImportUniqueFarmerData(incomingCategories);

            await _context.SaveChangesAsync();
            _logger.LogInformation("Data successfully imported.");
        }

        private async Task ImportUniqueFarmerData(List<FarmerCategorys> incomingCategories)
        {
            var existingCategories = await _context.FarmerCategorys
                .Include(c => c.ValueChainType)
                    .ThenInclude(v => v.FarmerValueChainItem)
                .ToListAsync();

            foreach (var incomingCategory in incomingCategories)
            {
                if (existingCategories.Any(c => c.FarmerCategory == incomingCategory.FarmerCategory))
                    continue;

                foreach (var incomingType in incomingCategory.ValueChainType)
                {
                    if (existingCategories.SelectMany(c => c.ValueChainType).Any(v => v.Name == incomingType.Name))
                        continue;

                    foreach (var incomingItem in incomingType.FarmerValueChainItem)
                    {
                        if (existingCategories
                            .SelectMany(c => c.ValueChainType)
                            .SelectMany(v => v.FarmerValueChainItem)
                            .Any(i => i.Name == incomingItem.Name))
                            continue;
                    }
                }
                await _context.FarmerCategorys.AddAsync(incomingCategory);
            }
        }

    }
}
