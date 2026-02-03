using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Payment;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2
{
    public interface IMembershipManager
    {
        //Task<MembershipDashboardDto> GetMembershipDashboardData(ListQueryParams query);
        Task<MembershipDashboardDto> GetMembershipDashboardAsync(ListQueryParams query);
        Task<PagedListResponse<List<FarmerCooperativeDto>>> GetMembershipFarmers(ListQueryParams query);
    }
    public class MembershipManager : IMembershipManager
    {
        private readonly IFarmerRepository _farmerRepository;
        private readonly IMembershipRepository _membershipRepository;
        private readonly ILogger<AgentManager> _log;
        private readonly IFaceManager _faceManager;
        private readonly IOnboardingRepository _onboardingRepository;
        public MembershipManager(IFarmerRepository farmerRepository,
            IMembershipRepository membershipRepository,
            IFaceManager faceManager,
            ILogger<AgentManager> log, IOnboardingRepository onboardingRepository)
        {
            _farmerRepository = farmerRepository;
            _faceManager = faceManager;
            _log = log;
            _onboardingRepository = onboardingRepository;
            _membershipRepository = membershipRepository;
        }
        public async Task<MembershipDashboardDto> GetMembershipDashboardAsync(ListQueryParams query)
        {
           
            var profile = await _onboardingRepository.GetUserProfile(query.UserId);
            if (profile == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Membership Record not found"); }

            if (profile.Membership == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Membership Record not found"); }

            return await _membershipRepository.GetMembershipDashboardAsync(profile.Membership.Id);
        }
        public async Task<PagedListResponse<List<FarmerCooperativeDto>>> GetMembershipFarmers(ListQueryParams query)
        {
            var profile = await _onboardingRepository.GetUserProfile(query.UserId);
            if (profile == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Membership Record not found"); }

            if (profile.Membership == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Membership Record not found"); }
            // Farmers linked to this agent
            return await _farmerRepository.GetFarmersByMembership(profile.Membership.Id, query);

        }
        //public async Task<MembershipDashboardDto> GetMembershipDashboardData(ListQueryParams query)
        //{

        //    //  Total cooperatives linked to this agent
        //    var profile = await _onboardingRepository.GetUserProfile(query.UserId);
        //    if (profile == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Membership Record not found"); }

        //    if(profile.Membership == null) { throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Membership Record not found"); }
        //    // Farmers linked to this agent
        //    var farmers = await _farmerRepository.GetFarmersByMembership(profile.Membership.Id, query);

        //    //  Farms
        //    //var farms = await _farmRepository.GetFarmsByAgentId(currentAgentId);
        //    //var mappedFarms = farms.Count(f => f.IsMapped);
        //    //var unmappedFarms = farms.Count(f => !f.IsMapped);

        //    //  Total mapped / unmapped farms (based on whether farmer has any farm plots)
        //    //var totalMappedFarms = farmers.Count(f => string.Equals(f.MappingStatus, "Mapped", StringComparison.OrdinalIgnoreCase));
        //    //var totalUnmappedFarms = farmers.Count - totalMappedFarms;

        //    //  Build DTO
        //    var dto = new MembershipDashboardDto
        //    {

        //        TotalFarmers = farmers.Count,
        //        IsActive=true,
        //        Location=profile.Membership.Location,
        //        MembershipType=profile.Membership.MembershipType,
        //        Name=profile.Membership.Name,
        //        OnboardedOn=profile.Membership.OnBoardedOn,

        //        // leave TotalFarmSizeHectares and TotalFarms unchanged unless farm repo is available

        //    };

        //    return dto;
        //}

    }
}
