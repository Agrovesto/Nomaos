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
    public interface ITemperatureControlType
    {
        Task<List<TemperatureControlType>> GetAllAsync();
        Task<TemperatureControlType> GetByIdAsync(long id);
        Task AddAsync(TemperatureControlType temperatureControlType);
        Task UpdateAsync(TemperatureControlType temperatureControlType);
    }

    public class TemperatureControlTypeRepository : ITemperatureControlType
    {
        private readonly DataContext _context;
        public TemperatureControlTypeRepository(DataContext context)
        {
            _context = context;
        }
        public async Task<List<TemperatureControlType>> GetAllAsync()
        {
            return await _context.TemperatureControlTypes.ToListAsync();
        }
        public async Task<TemperatureControlType> GetByIdAsync(long id)
        {
            return await _context.TemperatureControlTypes.FindAsync(id);
        }
        public async Task AddAsync(TemperatureControlType temperatureControlType)
        {
            _context.TemperatureControlTypes.Add(temperatureControlType);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync(TemperatureControlType temperatureControlType)
        {
            _context.TemperatureControlTypes.Update(temperatureControlType);
            await _context.SaveChangesAsync();
        }
    }
}
