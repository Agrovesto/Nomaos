using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Payment;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2
{
    public interface IAgentManager
    {
        Task<AgentDashboardDto> GetAgentDashboardData(ListQueryParams query);
        Task<PagedListResponse<List<MemberShipResponseModel>>> GetMembershipsByAgent(string userId, ListQueryParams query);
        Task<PagedListResponse<List<FarmerAgentDto>>> GetFarmersByAgent(string userId, ListQueryParams query);
        Task<PagedListResponse<List<FarmerModel>>> GetFarmersByCooperativeId(ListQueryParams query);
    }
    public class AgentManager : IAgentManager
    {

        private readonly IFarmerRepository _farmerRepository;
        private readonly IMembershipRepository _cooperativeRepository;
        private readonly IAgentRepository _agentRepository;
        private readonly ILogger<AgentManager> _log;
        private readonly IAWSRekognitionManager _faceManager;
        public AgentManager(IFarmerRepository farmerRepository, IAgentRepository agentRepository,
            IAWSRekognitionManager faceManager,
            IMembershipRepository cooperativeRepository,
            ILogger<AgentManager> log)
        {
            _agentRepository = agentRepository;
            _farmerRepository = farmerRepository;
            _faceManager = faceManager;
            _log = log;
            _cooperativeRepository = cooperativeRepository;
        }
        public async Task<PagedListResponse<List<MemberShipResponseModel>>> GetMembershipsByAgent(string userId,
       ListQueryParams query)
        {
            try
            {
                query.PageNumber = (query.PageNumber - 1) * query.PageSize;
                var agent = await _agentRepository.GetAgentDetailByUserId(userId);

                if (agent == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                var cooperative = await _cooperativeRepository.GetCooperativeByAgent(userId, query);
                if (cooperative == null) return new PagedListResponse<List<MemberShipResponseModel>>
                {
                    Succeeded = true,
                    Data = null,
                    Count = 0,
                    Message = "No Record Found"
                };
                return new PagedListResponse<List<MemberShipResponseModel>>
                {
                    Succeeded = true,
                    Data = cooperative,
                    Count = cooperative.Count,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Agents: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }
        public async Task<PagedListResponse<List<FarmerAgentDto>>> GetFarmersByAgent(string userId,

    ListQueryParams query)
        {
            try
            {
                query.PageNumber = (query.PageNumber - 1) * query.PageSize;
               
                var farmers = await _farmerRepository.GetFarmersByAgent(userId, query);
                if (farmers == null) return new PagedListResponse<List<FarmerAgentDto>>
                {
                    Succeeded = true,
                    Data = null,
                    Count = 0,
                    Message = "No Record Found"
                };

                //foreach (var farmer in farmers)
                //{
                //    if (!string.IsNullOrEmpty(farmer.FarmerImage))
                //    {
                //        farmer.FarmerImage = await _faceManager.GetImageUrlAsync(farmer.FarmerImage);
                //    }
                //}

                return new PagedListResponse<List<FarmerAgentDto>>
                {
                    Succeeded = true,
                    Data = farmers.Data,
                    Count = farmers.Count,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Agents: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }
        public async Task<AgentDashboardDto> GetAgentDashboardData(ListQueryParams query)
        {

            var request = await _farmerRepository.GetAgentDashboardData(query.UserId);
            
            return request;
        }

        public async Task<PagedListResponse<List<FarmerModel>>> GetFarmersByCooperativeId(

  ListQueryParams query)
        {
            try
            {
                query.PageNumber = (query.PageNumber - 1) * query.PageSize;

                var farmers = await _farmerRepository.GetFarmersByCooperativeId(query.CooperativeId, query);
                if (farmers == null) return new PagedListResponse<List<FarmerModel>>
                {
                    Succeeded = true,
                    Data = null,
                    Count = 0,
                    Message = "No Record Found"
                };
                foreach (var farmer in farmers)
                {
                    if (!string.IsNullOrEmpty(farmer.FarmerImage))
                    {
                        farmer.FarmerImage = await _faceManager.GetImageUrlAsync(farmer.FarmerImage);
                    }
                }

                return new PagedListResponse<List<FarmerModel>>
                {
                    Succeeded = true,
                    Data = farmers,
                    Count = farmers.Count,
                    Message = "Retrieved Successfully"
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Agents: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }

    }
}
