using Agrovesto.Backend.Core.Models.Market;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Repositories
{
    public interface ICommodityRepository
    {
        Task<CommodityDto?> GetByIdAsync(long commodityId);
        Task<List<CommodityDto>> GetByCategoryAsync(long categoryId);
        Task<List<CommodityDto>> GetAllAsync();

        Task AddAsync(CommodityDto commodity);
        Task SaveChangesAsync();
    }


}
