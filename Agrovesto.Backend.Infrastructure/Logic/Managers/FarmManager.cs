using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Agrovesto.Application.DTOs.Farms;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;
using Agrovesto.Backend.Infrastructure.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers
{
    public interface IFarmManager
    {
        Task<CreateFarmDto> CreateFarmAsync(CreateFarmDto dto, BusinessRegistrationArgs args);
        Task<List<Farm?>> GetAllWithCoordinatesAsync(BusinessRegistrationArgs args);
        Task<List<Farm?>> GetFarmByCooperativeIdAsync(long cooperativeId);
        Task<Farm?> GetFarmByIdAsync(long id);
    }
    public class FarmManager : IFarmManager
    {
        private readonly IFarmRepository _farmRepository;
        private readonly IBusinessRepository _businessRepository;

        private readonly IAuthenticationRepository _authenticationRepository;

        public FarmManager(IFarmRepository farmRepository, IBusinessRepository businessRepository, IAuthenticationRepository authenticationRepository)
        {
            _farmRepository = farmRepository;
            _businessRepository = businessRepository;
            _authenticationRepository = authenticationRepository;
        }
        

        public async Task<CreateFarmDto> CreateFarmAsync(CreateFarmDto dto, BusinessRegistrationArgs args)
        {
            try { 
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
           ?? throw new Exception("Business does not exist");

            var getUserCred = await _authenticationRepository.GetUserById(args.UserId);
            if (!getUserCred.status) throw new Exception("User taking coordinate not recognized");

            // 1️⃣ Compute area
            var points = dto.Coordinates.Select(c => (c.Latitude, c.Longitude)).ToList();
            var area = GeometryHelper.ComputePolygonArea(points);
                var areaInSqMetres = GeometryHelper.CalculatePolygonAreaSqMeters(points); // Convert hectares to square metres
           var areaInDegrees = GeometryHelper.CalculatePolygonAreaDegrees(points);
            // 2️⃣ Build farm entity
            var newFarm = new Farm
            {
                Name = dto.FarmName,
                OwnershipType = dto.OwnershipType,
                FarmerId = dto.FarmerId,
                LocationDescription = dto.FarmLocation,
                FarmLocation = dto.FarmLocation,
                FarmState = dto.FarmState,
                FarmLga = dto.FarmLga,
                FarmWard = dto.FarmWard,
                //CoordinateTakenBy = getUserCred.model.Name,
                //CoordinateTakenByUserId = getUserCred.model.Id,
                BelongToBusinessCode = args.businessCode,
                OrganizationCode = args.organizationCode,
                CreatedBy = getUserCred.model.Name,
                MappedBy = getUserCred.model.Name,
                AreaSize = area,
                TotalAreaInDegress = areaInDegrees,
                TotalAreaInSqMetre = areaInSqMetres,
                Coordinates = dto.Coordinates.Select(c => new FarmCoordinate
                {
                    Latitude = c.Latitude,
                    Longitude = c.Longitude,
                    Order = c.Order
                }).ToList()
            };

            // 3️⃣ Check overlaps
            var allFarms = await _farmRepository.GetAllWithCoordinatesAsync();
            var overlapping = new List<FarmCoordinate>();

            //foreach (var existing in allFarms)
            //{
            //    if (GeometryHelper.IsOverlapping(existing.Coordinates.ToList(), newFarm.Coordinates.ToList()))
            //        overlapping.Add(existing);
            //}

            //if (overlapping.Any())
            //{
            //    newFarm.IsFarmFlaggedBySystem = true;
            //    newFarm.FlaggedReason = "Overlapping coordinates with existing farms";
            //        newFarm.FlaggedOn= DateTimeOffset.UtcNow;
            //        newFarm.CoordinateOverlapWithFarmerIds = string.Join(",", overlapping.Select(x => x.FarmerUserId));

            //    // Add audit trail
            //    var audit = new FarmMappingAuditTrail
            //    {
            //        Action = "Flagged",
            //        Farm = newFarm,
            //        PerformedByName = getUserCred.model.Name,
            //        PerformedByUserId = getUserCred.model.Id,
            //        Remarks = $"Farm flagged due to overlap with Farmer IDs: {newFarm.CoordinateOverlapWithFarmerIds}"
            //    };
            //    newFarm.AuditTrails.Add(audit);
            //}

            // 4️⃣ Save
            var save= await _farmRepository.AddFarmAsync(newFarm);
                dto.Id = save.Id;
                return dto;
            }
            catch (Exception ex)
            {
                // Log exception (not implemented here)
                throw new Exception($"Error creating farm: {ex.Message}");
            }
        }

        public async Task<Farm?> GetFarmByIdAsync(long id) => await _farmRepository.GetByIdAsync(id);

        public async Task<List<Farm?>> GetFarmByCooperativeIdAsync(long cooperativeId) => await _farmRepository.CooperativeFarmMaps(cooperativeId);

        public async Task<List<Farm?>> GetAllWithCoordinatesAsync(BusinessRegistrationArgs args) => await _farmRepository.GetAllWithCoordinatesAsync(args);

    }
}
