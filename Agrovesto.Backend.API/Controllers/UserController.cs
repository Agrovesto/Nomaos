using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.Logic.Managers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Agrovesto.Backend.API.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class UserController : BaseController
    {
        private readonly IWalletManager _walletManager; 
        public UserController(IHttpContextAccessor httpContextAccessor, IWalletManager walletManager) : base(httpContextAccessor)
        {
            _walletManager = walletManager;
        }
        [HttpPost("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] SubAccountModel model, [FromQuery] ParameterArgs args)
        {
             args.UserId = CurrentUserId;          

            var data = await _walletManager.UpdateUserProfile(model, args);
            return Ok(new AgrovestoResponse<SubAccountModel>()
            {
                Message = "",
                Result = data,

            });
        }
    }
}
