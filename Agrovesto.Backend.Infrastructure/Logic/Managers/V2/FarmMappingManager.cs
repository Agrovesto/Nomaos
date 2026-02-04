using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses.Agrovesto.Application.DTOs.Farms;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2
{
    public interface IFarmMappingManager
    {
        //  Task MapFarmAsync(
        //long farmId,
        //long farmerId,
        //List<FarmCoordinateRequest> coordinates,
        //string performedByUserId,
        //string performedByName);
        Task<long> SubmitFarmAsync(MapFarmRequest request, FarmMappingArgs args);
    }
    public class FarmMappingManager : IFarmMappingManager

    {
        private readonly ILogger<FarmMappingManager> _log;
        private readonly IFarmMappingRepository _farmRepo;
        private readonly IGoogleDriveService _googleDriveService;
        private readonly IBusinessRepository _businessRepository;
        private readonly IFarmerRepository _farmerRepository;
        public FarmMappingManager(IFarmMappingRepository farmMappingRepository,
            IGoogleDriveService googleDriveService, 
            IBusinessRepository businessRepository, 
            IFarmerRepository farmerRepository,
            ILogger<FarmMappingManager> log)
        {
            _farmRepo = farmMappingRepository;
            _googleDriveService = googleDriveService;
            _businessRepository = businessRepository;
            _farmerRepository = farmerRepository;
            _log = log;
        }
        public async Task<long> SubmitFarmAsync(MapFarmRequest request, FarmMappingArgs args)
        {
            try { //get business code
            if (string.IsNullOrEmpty(args.businessCode))
                throw new BaseException(
                    Core.Enums.ExceptionType.BAD_REQUEST,
                    "Business code is required.");

            // ------------------ Fetch business ------------------
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new BaseException(
                    Core.Enums.ExceptionType.NOTFOUND,
                    "Business does not exist.");
            //get loginuser details
            //get farmers details
            var farmerDetails = await _farmerRepository.GetFarmerById(args.farmerId)
                ?? throw new BaseException(
                    Core.Enums.ExceptionType.NOTFOUND,
                    "Farmer does not exist.");
 

            // 1️⃣ Create or update farm
            var farm = await _farmRepo.GetOrCreateAsync(
                request, args);

            // Map if coordinates provided
            if (request.Coordinates?.Any() == true)
            {
                await _farmRepo.MapFarmAsync(
                    request.Coordinates,args);
            }

            // Upload documents if provided
            if (request.Documents?.Any() == true)
            {
                // get google drive service first
                foreach (var doc in request.Documents)
                {
                    // Upload file to Google Drive
                    var driveUrl = await _googleDriveService.UploadFileAsync(
                        doc.Document,
                        folderName: $"farm-{farm.Id}");
                    args.googleDriveDocumentId = driveUrl;
                    doc.DocumentUrl = driveUrl;
                    args.farmId= farm.Id;
                    // Attach document metadata to DB
                    await _farmRepo.AttachSingleDocumentsAsync(
                        doc,
                        args);
                }
                //var uploadToGooDriveAndGetId = await _googleDriveService
                //    .UploadFileAsync(request.Documents, args);
                //await _farmRepo.AttachDocumentsAsync(
                //  request.Documents, args);
            }

            return farm.Id;
            }
            catch (BaseException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Create Farm failed unexpectedly");
                throw new BaseException(
                    ExceptionType.INTERNAL_SERVER_ERROR,
                    "Unable to submit farm data");
            }
        }

        //public async Task MapFarmAsync(
        // long farmId,
        // long farmerId,
        // List<FarmCoordinateRequest> coordinates,
        // string performedByUserId,
        // string performedByName)
        //{
        //    if (coordinates.Count < 3)
        //        throw new DomainException("A farm polygon requires at least 3 coordinates.");

        //    var farm = await _farmRepo.GetWithCoordinatesAsync(farmId)
        //        ?? throw new DomainException("Farm not found.");

        //    // 1️⃣ Reset existing mapping (idempotent)
        //    await _farmRepo.RemoveByFarmIdAsync(farmId);

        //    // 2️⃣ Save coordinates
        //    var farmCoords = coordinates.Select(c => new FarmCoordinate
        //    {
        //        FarmId = farmId,
        //        Latitude = c.Latitude,
        //        Longitude = c.Longitude,
        //        Order = c.Order,
        //        CapturedOn = DateTimeOffset.UtcNow
        //    }).ToList();

        //    await _farmRepo.AddRangeAsync(farmCoords);

        //    // 3️⃣ Build polygon
        //    var polygon = BuildPolygon(farmCoords);

        //    farm.FarmPolygon = polygon;
        //    farm.MappingStatus = MappingStatus.InProgress;

        //    // 4️⃣ Overlap detection
        //    var overlaps = await _farmRepo.GetOverlappingFarmsAsync(
        //        farmId,
        //        farmerId,
        //        polygon);

        //    if (overlaps.Any())
        //    {
        //        farm.IsMappingFlagged = true;
        //        farm.MappingFlagReason =
        //            $"Overlaps with FarmerIds: {string.Join(",", overlaps.Select(x => x.FarmerId))}";

        //        await _farmRepo.AddAsync(new FarmMappingAuditTrail
        //        {
        //            FarmId = farmId,
        //            Action = "MappingFlagged",
        //            PerformedByUserId = performedByUserId,
        //            PerformedByName = performedByName,
        //            Remarks = farm.MappingFlagReason
        //        });
        //    }
        //    else
        //    {
        //        farm.IsMappingFlagged = false;
        //        farm.MappingFlagReason = null;
        //    }

        //    // 5️⃣ Audit mapping
        //    await _farmRepo.AddAsync(new FarmMappingAuditTrail
        //    {
        //        FarmId = farmId,
        //        Action = "MappingUpdated",
        //        PerformedByUserId = performedByUserId,
        //        PerformedByName = performedByName,
        //        Remarks = "Farm coordinates updated"
        //    });

        //    await _farmRepo.UpdateAsync(farm);
        //}

        //private Polygon BuildPolygon(List<FarmCoordinate> coords)
        //{
        //    var ordered = coords
        //        .OrderBy(c => c.Order)
        //        .Select(c => new Coordinate(c.Longitude, c.Latitude))
        //        .ToList();

        //    ordered.Add(ordered.First()); // close polygon

        //    return _geometryFactory.CreatePolygon(ordered.ToArray());
        //}
    }
}
