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
        Task<BusinessModel> CreateBusiness(BusinessModel businessModel, string userId);
        Task<BusinessModel> GetBusinessAsync(string businessName);
    }
}
