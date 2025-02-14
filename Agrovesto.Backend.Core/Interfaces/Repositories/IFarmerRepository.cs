using Agrovesto.Backend.Core.Models.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Repositories
{
    public interface IFarmerRepository
    {
        Task<(bool status, FarmerModel data)> CreateFarmer(FarmerModel data);
        Task<FarmerModel> GetFarmerById(long Id);
        Task<FarmerModel> GetFarmerByUserId(string userId);
        Task<List<FarmerModel>> GetFarmers();
        Task<List<FarmerModel>> GetFarmersByBusinessCode(string businessCode);
        Task<List<FarmerModel>> GetFarmersByOnboardedUserId(string userId);
    }
}
