using Agrovesto.Backend.Core.Models.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Repositories
{
  
    public interface ITraderRepository
    {
        Task<(bool status, TraderModel data)> CreateTrader(TraderModel data);
        Task<TraderModel> GetTraderDetailById(long Id);
        Task<TraderModel> GetTraderDetailByUserId(string userId);
        Task<List<TraderModel>> GetTraders();
        Task<List<TraderModel>> GetTradersByBusinessCode(string businessCode, ListQueryParams query);
    }
}
