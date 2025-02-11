using Agrovesto.Backend.Core.Models.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Repositories
{
    public interface ICooperativeRepository
    {
        Task<(bool status, CooperativeModel data)> CreateCooperative(CooperativeModel data);
        Task<CooperativeModel> GetCooperativeDetailById(long Id);
        Task<CooperativeModel> GetCooperativeDetailByUserId(string userId);
        Task<List<CooperativeModel>> GetCooperatives();
        Task<List<CooperativeModel>> GetCooperativesByBusinessCode(string businessCode);
    }
}
