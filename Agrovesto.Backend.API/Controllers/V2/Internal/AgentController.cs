using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Payment;
using Agrovesto.Backend.Infrastructure.Logic.Managers.V2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Agrovesto.Backend.API.Controllers.V2.Internal
{
    [Route("api/v2/agent")]
    [ApiController]
    [Authorize]
    public class AgentController : BaseController
    {
        private readonly IAgentManager _agentManager;

        public AgentController(IHttpContextAccessor httpContextAccessor, IAgentManager agentManager
            ) : base(httpContextAccessor)
        {
           _agentManager = agentManager;
        }
        [HttpGet("cooperatives")]
        public async Task<IActionResult> GetCooperativesByAgent([FromQuery] ListQueryParams query)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _agentManager.GetCooperativesByAgent(query.UserId, query);
                return Ok(new PagedListResponse<List<CooperativeModel>>()
                {
                    Data = result.Data,
                    Message = result.Message,
                    Succeeded = result.Succeeded,
                    Count = result.Count,
                    PageNumber = query.PageNumber,
                    PageSize = 10,
                    Code = (result.Succeeded && result.Data != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
                });
            }
            catch (BaseException ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.ExceptionMessage,
                    Succeeded = false,

                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.Message,
                    Succeeded = false,
                });
            }


        }


        [HttpGet("farmers")]
        public async Task<IActionResult> GetFarmersByAgent([FromQuery] ListQueryParams query)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _agentManager.GetFarmersByAgent(query.UserId, query);
                return Ok(new PagedListResponse<List<FarmerAgentDto>>()
                {
                    Data = result.Data,
                    Message = result.Message,
                    Succeeded = result.Succeeded,
                    Count = result.Count,
                    PageNumber = query.PageNumber,
                    PageSize = 10,
                    Code = (result.Succeeded && result.Data != null) ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
                });
            }
            catch (BaseException ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.ExceptionMessage,
                    Succeeded = false,

                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.Message,
                    Succeeded = false,
                });
            }


        }
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetAgentDashboardData([FromQuery] ListQueryParams query)
        {
            try
            {
                if (!string.IsNullOrEmpty(OrganizationCode) && string.IsNullOrEmpty(query.organizationCode))
                {
                    query.organizationCode = OrganizationCode;
                }
                query.UserId = !string.IsNullOrEmpty(query.UserId) ? query.UserId : CurrentUserId;
                var result = await _agentManager.GetAgentDashboardData(query);
                return Ok(new Response<AgentDashboardDto>()
                {
                    Data = result,

                   
                    Message = result !=null? "Successful": "Could not retrieve data",
                    Succeeded = result != null ? true : false,
              
                    Code = result != null ? Core.CoreUtils.Constants.ResponseCodes.Success : Core.CoreUtils.Constants.ResponseCodes.Failed
                });
            }
            catch (BaseException ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.ExceptionMessage,
                    Succeeded = false,

                });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new Response<string>
                {
                    Message = ex.Message,
                    Succeeded = false,
                });
            }


        }

    }
}
