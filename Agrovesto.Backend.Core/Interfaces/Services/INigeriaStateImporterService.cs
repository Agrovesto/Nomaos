using Agrovesto.Backend.Core.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Services
{
    public interface INigeriaStateImporterService
    {
        Task<List<LGAModel>> GetLgasByState(long stateId);
        Task<List<StateModel>> GetStatesAsync();
        Task<List<WardModel>> GetWardsByLga(long lgaId);
        Task ImportLocationsFromJsonAsync();
    }
}
