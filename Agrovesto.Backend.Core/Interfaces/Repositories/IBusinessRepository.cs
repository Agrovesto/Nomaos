using Agrovesto.Backend.Core.Models.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Repositories
{
    public interface IBusinessRepository
    {
        Task<int> BusinessCount();
        Task<BusinessModel> CreateBusiness(BusinessModel businessModel, string userId);
        Task<BusinessWorkerModel> CreateBusinessWorker(BusinessWorkerModel bWorker);
        Task<BusinessModel> GetBusinessAsync(string businessName);
        Task<BusinessModel> GetBusinessByCodeAsync(string businessCode);
        Task<BusinessModel> GetBusinessByIdAsync(long businessId);
        Task<BusinessModel> GetBusinessBySinageSaltAsync(string signagesalt);
        Task<BusinessModel> GetBusinessByUserIdAsync(string userId);
        Task<List<BusinessModel>> GetBusinessesAsync();
        Task<List<BusinessWorkerModel>> GetStaffByBusinessCode(long businessId);
    }
}
