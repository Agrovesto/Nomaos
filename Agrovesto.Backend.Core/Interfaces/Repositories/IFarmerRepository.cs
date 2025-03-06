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
        Task<BusinessEntitiesMetrics> GetBusinessEntitiesMetricsAsync(string businessCode);
        Task<FarmerModel> GetFarmerById(long Id);
        Task<FarmerModel> GetFarmerByUserId(string userId);
        Task<List<FarmerModel>> GetFarmers();
        Task<List<FarmerModel>> GetFarmersByAgent(long agentId, ListQueryParams query);
        Task<List<FarmerModel>> GetFarmersByBusinessCode(string businessCode, ListQueryParams query);
        Task<List<FarmerModel>> GetFarmersByCooperativeId(long Id, ListQueryParams query);
        Task<List<FarmerModel>> GetFarmersByOnboardedUserId(string userId, ListQueryParams query);
        Task<(bool status, FarmerImagesModel data)> SaveImage(FarmerImagesModel data);

        Task<FarmerImagesModel> GetFarmerImage(string userId);
        Task<List<FarmerImagesModel>> GetImagesByBusinessCode(string businessCode);
    }
}
