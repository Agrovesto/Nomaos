using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Payment;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Agrovesto.Backend.API.Controllers.V2.Internal
{
    [Route("api/[controller]")]
    [ApiController]
    [Route("api/v2agent/dashboard")]
    public class AgentDashboardController : ControllerBase
    {
        private readonly IFarmRepository _farmRepository;
        private readonly ICooperativeRepository _cooperativeRepository;

        public AgentDashboardController(
            IFarmRepository farmRepository,
            ICooperativeRepository cooperativeRepository)
        {
            _farmRepository = farmRepository;
            _cooperativeRepository = cooperativeRepository;
        }

        }

}
