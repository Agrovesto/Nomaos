using Agrovesto.Backend.Core.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Services
{
    public interface IFarmerCategoryService
    {
        Task<List<FarmerCategoryModel>> GetFarmerCategoryAsync();
        Task<List<ValueChainTypeModel>> GetValueChangeByCategory(long categoryId);
        Task<List<FarmerValueChainItemModel>> GetValueChainItemByType(long typeId);
        Task ImportFarmerValuesFromJsonAsync();
    }
}
