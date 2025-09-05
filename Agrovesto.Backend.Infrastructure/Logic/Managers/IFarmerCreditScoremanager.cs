using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Models.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers
{
    public interface IFarmerCreditScoremanager
    {
        Task<PagedListResponse<List<FarmerCreditScoreModel>>> GetFarmerScoreAsync(BusinessRegistrationArgs args);
        Task UpdateAllFarmerScores(BusinessRegistrationArgs args);
        Task UpdateFarmerScores(BusinessRegistrationArgs args);
    }
}
