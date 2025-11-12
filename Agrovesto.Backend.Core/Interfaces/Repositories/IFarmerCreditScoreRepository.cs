using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Models.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Repositories
{
    public interface IFarmerCreditScoreRepository
    {
        Task<PagedListResponse<List<FarmerCreditScoreModel>>> GetCreditScorebybusiness(string businessCode, int pageNumber = 1, int pageSize = 10, string organizationCode = null);
        Task<CreditScoreWeightsModel> GetCreditScoreWeightByBusiness(string businessCode, string organizationCode = null);
        Task SaveCreditScoreWeightAsync(CreditScoreWeightsModel score);
    }
}
