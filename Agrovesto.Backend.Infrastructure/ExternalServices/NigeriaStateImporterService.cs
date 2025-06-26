using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Agrovesto.Backend.Core.Models.Common;
using States = Agrovesto.Backend.Infrastructure.Entities.Common.States;

namespace Agrovesto.Backend.Infrastructure.ExternalServices
{
    public class NigeriaStateImporterService : INigeriaStateImporterService
    {
        private readonly DataContext _context;
        private readonly string _connectionString;
        private readonly string _jsonFilePath;
        private readonly ILogger<NigeriaStateImporterService> _logger;

        public NigeriaStateImporterService(DataContext context,
            IHostEnvironment environment,IConfiguration configuration, ILogger<NigeriaStateImporterService> logger)
        {
            _context = context; 
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            //_jsonFilePath = configuration["statedata.json"];
            string relativePath = configuration["JsonFilePath"];
            _jsonFilePath = Path.Combine(environment.ContentRootPath, relativePath);

            _logger = logger;
        }

        public async Task<List<StateModel>> GetStatesAsync()
        {
            var states = await _context.States.AsNoTracking().ToListAsync();
            return states.Select(x=>x.Map()).ToList();
        }
        public async Task<List<LGAModel>> GetLgasByState(long stateId)
        {
            var stateExists = await _context.States.AnyAsync(s => s.StateId == stateId);
            if (!stateExists)
                return null;

            var lgas = await _context.LGAs.Where(l => l.StatesStateId == stateId)
                .AsNoTracking()
                .ToListAsync();
            return lgas.Select(x=>x.Map()).ToList();
        }

        public async Task<List<WardModel>> GetWardsByLga(long lgaId)
        {
            var lgaExists = await _context.LGAs.AnyAsync(s => s.LgaId == lgaId);
            if (!lgaExists)
                return null;

            var wards = await _context.Wards.Where(w => w.LgaId == lgaId)
                .AsNoTracking()
                .ToListAsync();
            return wards.Select(x=>x.Map()).ToList();
        }
        public async Task ImportLocationsFromJsonAsync()
        {
            if (!File.Exists(_jsonFilePath))
            {
                _logger.LogError("JSON file not found at {Path}", _jsonFilePath);
                throw new FileNotFoundException("JSON file not found.", _jsonFilePath);
            }

            string jsonData = await File.ReadAllTextAsync(_jsonFilePath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            List<States> states = JsonSerializer.Deserialize<List<States>>(jsonData, options);

            // Optional: Clear existing data (for full refresh)
            await ClearExistingData();

            // Add full tree
            await _context.States.AddRangeAsync(states);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Data successfully imported.");
        }

        private async Task ClearExistingData()
        {
            _logger.LogInformation("Clearing existing data...");
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM Wards");
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM LGAs");
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM States");
        }
    }
}


