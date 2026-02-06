using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses.Agrovesto.Application.DTOs.Farms;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.V2;
using Agrovesto.Backend.Infrastructure.Utilities;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2
{
    public interface IFarmMappingManager
    {
        Task<FarmDetailsResponse> FetchFarmDetails(FarmMappingArgs args);
        Task<List<FarmListRespopnseObject>> FetchFarmsByFarmer(FarmMappingArgs args);

        //  Task MapFarmAsync(
        //long farmId,
        //long farmerId,
        //List<FarmCoordinateRequest> coordinates,
        //string performedByUserId,
        //string performedByName);
        //Task<long> SubmitFarmAsync(MapFarmRequest request, FarmMappingArgs args);
        Task<long> SubmitFarmAsync(SubmitFarmRequest request, FarmMappingArgs args);
    }
    public class FarmMappingManager : IFarmMappingManager

    {
        private readonly ILogger<FarmMappingManager> _log;
        private readonly IFarmMappingRepository _farmRepo;
        private readonly IAWSRekognitionManager _aWSRekognitionManager;
        private readonly IBusinessRepository _businessRepository;
        private readonly IFarmerRepository _farmerRepository;
        private readonly IOnboardingRepository _onboardingRepository;
        public FarmMappingManager(IFarmMappingRepository farmMappingRepository,
            IAWSRekognitionManager aWSRekognitionManager, 
            IBusinessRepository businessRepository, 
            IFarmerRepository farmerRepository,
            IOnboardingRepository onboardingRepository,
            ILogger<FarmMappingManager> log)
        {
            _farmRepo = farmMappingRepository;
            _aWSRekognitionManager = aWSRekognitionManager;
            _businessRepository = businessRepository;
            _farmerRepository = farmerRepository;
            _onboardingRepository = onboardingRepository;
            _log = log;
        }
        public async Task<List<FarmListRespopnseObject>> FetchFarmsByFarmer(FarmMappingArgs args)
        {
            try
            { //get business code
                if (string.IsNullOrEmpty(args.businessCode))
                    throw new BaseException(
                        Core.Enums.ExceptionType.BAD_REQUEST,
                        "Business code is required.");

                // ------------------ Fetch business ------------------
                var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                    ?? throw new BaseException(
                        Core.Enums.ExceptionType.NOTFOUND,
                        "Business does not exist.");
                var fetchFarms = await _farmRepo.FetchAllFarmsByFarmer(args);
                if(fetchFarms == null)
                    throw new BaseException(
                        Core.Enums.ExceptionType.NOTFOUND,
                        "Farm does not exist.");
                var farms = fetchFarms.Select(fetchFarm =>
                {
                    string verificationStatus;

                    if (fetchFarm.FarmLandDocuments == null || !fetchFarm.FarmLandDocuments.Any())
                    {
                        verificationStatus = "Not Uploaded";
                    }
                    else if (!fetchFarm.IsDocumentVerified)
                    {
                        verificationStatus = "Partial";
                    }
                    else
                    {
                        verificationStatus = "Completed";
                    }

                    return new FarmListRespopnseObject
                    {
                        DateAdded = fetchFarm.CreatedAt,
                        MappingStatus = EnumExtensions.GetDisplayName(fetchFarm.MappingStatus),
                        DocumentStatus = verificationStatus,
                        LandSize = fetchFarm.AreaSize,
                        FarmId = fetchFarm.Id,
                    };
                }).ToList();



                return farms;
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
                   !string.IsNullOrEmpty(ex.Message) ? ex.Message : "Unable to submit farm data");
            }
        }

        public async Task<FarmDetailsResponse> FetchFarmDetails(FarmMappingArgs args)
        {
            try
            { //get business code
                if (string.IsNullOrEmpty(args.businessCode))
                    throw new BaseException(
                        Core.Enums.ExceptionType.BAD_REQUEST,
                        "Business code is required.");

                // ------------------ Fetch business ------------------
                var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                    ?? throw new BaseException(
                        Core.Enums.ExceptionType.NOTFOUND,
                        "Business does not exist.");
                var fetchFarms = await _farmRepo.FetchFarmsAsync(args);
                if (fetchFarms == null) return null;
                var farms = new FarmDetailsResponse
                {
                    DateAdded = fetchFarms.CreatedAt,
                    MappingStatus = EnumExtensions.GetDisplayName(fetchFarms.MappingStatus),
                    AddedBy = fetchFarms.CreatedBy,
                    Documents = fetchFarms.FarmLandDocuments.Count > 0 ? fetchFarms.FarmLandDocuments.Select(x => new FarmLandDocumentDto
                    {
                        DocumentTitle = x.DocumentTitle,
                        DocumentType = EnumExtensions.GetDisplayName(x.DocumentType),
                        DocumentUrl = x.DocumentUrl,
                        Status = fetchFarms.IsDocumentVerified ? "Verified" : "Not Verified",
                        UploadedBy = fetchFarms.CreatedBy,
                        UploadedOn = x.UploadedOn
                    }).ToList() : null,
                    FarmId = fetchFarms.Id,
                    MeasurementDetails = new LandMeasurementDetails
                    {
                        AreaUnit = fetchFarms.AreaSize,
                        DistanceCovered = fetchFarms.DistanceCovered,
                        MeasuredArea = fetchFarms.AreaSize,
                        MeasurementDate = fetchFarms.MeasurementDate.Value,
                        MeasurementMode = fetchFarms.MeasurementMode,
                        MappingAccuracy = fetchFarms.MappingAccuracy,   
                        PinnedLatitude = fetchFarms.PinnedLatitude,
                        PinnedLongitude = fetchFarms.PinnedLongitude,
                        
                    },
                    Coordinates = fetchFarms.Coordinates.Count > 0 ? fetchFarms.Coordinates.Select(x => new FarmCoordinateDto
                    {
                    Latitude=x.Latitude,
                    Longitude=x.Longitude,
                    Order=x.Order

                    }).ToList() : null,
                    Accuracy = fetchFarms.MappingAccuracy,
                    CapturedBy = fetchFarms.CapturedBy,
                    CapturedOn = fetchFarms.CapturedOn.Value,
                };
                if (farms != null && farms.Documents !=null &&  farms.Documents.Count > 0)
                {
                    foreach (var doc in farms.Documents)
                    {

                        doc.DocumentUrl =  await _aWSRekognitionManager.GetImageUrlAsync(doc.DocumentUrl);

                    }
                }
               
                return farms;
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
                   !string.IsNullOrEmpty(ex.Message)? ex.Message:  "Unable to submit farm data");
}
            }
       // public async Task<long> SubmitFarmAsync(SubmitFarmRequest request, FarmMappingArgs args)
       // {
       //     try
       //     { //get business code
       //         if (string.IsNullOrEmpty(args.businessCode))
       //             throw new BaseException(
       //                 Core.Enums.ExceptionType.BAD_REQUEST,
       //                 "Business code is required.");

       //         // ------------------ Fetch business ------------------
       //         var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
       //             ?? throw new BaseException(
       //                 Core.Enums.ExceptionType.NOTFOUND,
       //                 "Business does not exist.");
       //         //get loginuser details
       //         //get farmers details
       //         var farmerDetails = await _farmerRepository.GetFarmerById(args.farmerId)
       //             ?? throw new BaseException(
       //                 Core.Enums.ExceptionType.NOTFOUND,
       //                 "Farmer does not exist.");

       //         var user = await _onboardingRepository.GetUserById(args.performedByUserId);
       //             if(user.model == null)throw new BaseException(
       //                 Core.Enums.ExceptionType.NOTFOUND,
       //                 "User does not exist.");

       //             args.performedByName = $"{user.model.FirstName} {user.model.LastName}";
       //         // Map if coordinates provided
       //         var coordinates = string.IsNullOrWhiteSpace(request.CoordinatesJson)
       //? null
       //: JsonSerializer.Deserialize<List<FarmCoordinateDto>>(
       //    request.CoordinatesJson);


       //         var mapFarmRequest = new MapFarmRequest
       //         {
       //             AreaSize = request.SizeInHectares,
       //             OwnershipType = request.OwnershipType,
       //             FarmName = request.FarmName,
       //             FarmLocation = request.FarmLocation,
       //             FarmState = request.FarmState,
       //             FarmLga = request.FarmLga,
       //             FarmWard = request.FarmWard,
       //             BelongToBusinessCode = args.businessCode,
       //             SizeInHectares=request.SizeInHectares,
       //             SizeInKm=request.SizeInKm,
       //             MeasurementMode=request.MeasurementMode,
       //             OrganizationCode = args.organizationCode,
       //             Coordinates = coordinates ?? new List<FarmCoordinateDto>()
       //         };

       //         //  Create or update farm
       //         var farm = await _farmRepo.GetOrCreateDraftAsync(mapFarmRequest, args);

       //         if (coordinates?.Any() == true)
       //         {
       //             mapFarmRequest.Coordinates = coordinates;
       //             await _farmRepo.MapFarmAsync(
       //                 mapFarmRequest, args);
       //         }

       //         if (request.Documents?.Any() == true)
       //         {
       //             if (request.DocumentTypes == null ||
       //                 request.Documents.Count != request.DocumentTypes.Count)
       //             {
       //                 throw new BaseException(
       //                     ExceptionType.BAD_REQUEST,
       //                     "Each document must have a corresponding document type.");
       //             }

       //             for (int i = 0; i < request.Documents.Count; i++)
       //             {
       //                 var file = request.Documents[i];
       //                 var documentType = request.DocumentTypes[i];

       //                 // Upload file
       //                 var documentUrl = await _aWSRekognitionManager
       //                     .UploadFileAsync(file, $"farm-{farm.Id}");

       //                 var documentDto = new FarmLandDocumentDto
       //                 {
       //                     DocumentType = documentType,
       //                     DocumentUrl = documentUrl,
       //                     DocumentTitle = file.FileName
       //                 };

       //                 args.farmId = farm.Id;
       //                 args.googleDriveDocumentId = documentUrl;

       //                 await _farmRepo.AttachSingleDocumentsAsync(
       //                     documentDto,
       //                     args);
       //             }
       //         }

       //         return farm.Id;
       //     }
       //     catch (BaseException)
       //     {
       //         throw;
       //     }
       //     catch (Exception ex)
       //     {
       //         _log.LogError(ex, "Create Farm failed unexpectedly");
       //         throw new BaseException(
       //             ExceptionType.INTERNAL_SERVER_ERROR,
       //            !string.IsNullOrEmpty(ex.Message)? ex.Message:  "Unable to submit farm data");
       //     }
       // }

        public async Task<long> SubmitFarmAsync(SubmitFarmRequest request, FarmMappingArgs args)
        {
            try
            {
                if (string.IsNullOrEmpty(args.businessCode))
                    throw new BaseException(Core.Enums.ExceptionType.BAD_REQUEST, "Business code is required.");

                // Fetch business
                var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                    ?? throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "Business does not exist.");

                // Fetch farmer
                var farmer = await _farmerRepository.GetFarmerById(args.farmerId)
                    ?? throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "Farmer does not exist.");

                // Fetch user
                var user = await _onboardingRepository.GetUserById(args.performedByUserId);
                if (user.model == null)
                    throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "User does not exist.");
                args.performedByName = $"{user.model.FirstName} {user.model.LastName}";

                // Parse coordinates (JSON or raw text)
                var coordinates = ParseCoordinates(request.CoordinatesJson);

                // MapFarmRequest for repo
                var mapFarmRequest = new MapFarmRequest
                {
                    PinnedLatitude = request.PinnedLatitude,    
                    PinnedLongitude= request.PinnedLongitude,
                    DistanceCovered=request.DistanceCovered,
                    MappingAccuracy= request.MappingAccuracy,
                    AreaSize = request.SizeInHectares,
                    OwnershipType = request.OwnershipType,
                    FarmName = request.FarmName,
                    FarmLocation = request.FarmLocation,
                    FarmState = request.FarmState,
                    FarmLga = request.FarmLga,
                    FarmWard = request.FarmWard,
                    BelongToBusinessCode = args.businessCode,
                    SizeInHectares = request.SizeInHectares,
                    SizeInKm = request.SizeInKm,
                    MeasurementMode = request.MeasurementMode,
                    OrganizationCode = args.organizationCode,
                    Coordinates = coordinates
                };

                // Create or update farm
                var farm = await _farmRepo.GetOrCreateDraftAsync(mapFarmRequest, args);
                if (args.farmId <= 0)
                    args.farmId = farm.Id;
                // Map coordinates if provided
                if (coordinates?.Any() == true)
                {
                    mapFarmRequest.Coordinates = coordinates;
              
                    await _farmRepo.MapFarmAsync(mapFarmRequest, args);
                }

                // Handle documents
                if (request.Documents?.Any() == true)
                {
                    if (request.DocumentTypes == null || request.Documents.Count != request.DocumentTypes.Count)
                        throw new BaseException(ExceptionType.BAD_REQUEST, "Each document must have a corresponding document type.");

                    for (int i = 0; i < request.Documents.Count; i++)
                    {
                        var file = request.Documents[i];
                        var documentType = request.DocumentTypes[i];

                        // Upload to S3
                        var documentUrl = await _aWSRekognitionManager.UploadFileAsync(file, $"farm-{farm.Id}");

                        var documentDto = new FarmLandDocumentDto
                        {
                            DocumentType = documentType,
                            DocumentUrl = documentUrl,
                            DocumentTitle = file.FileName
                        };

                        args.farmId = farm.Id;
                        args.googleDriveDocumentId = documentUrl; // can rename to DocumentId if using S3

                        await _farmRepo.AttachSingleDocumentsAsync(documentDto, args);
                    }
                }

                return farm.Id;
            }
            catch (BaseException) { throw; }
            catch (Exception ex)
            {
                _log.LogError(ex, "Create Farm failed unexpectedly");
                throw new BaseException(ExceptionType.INTERNAL_SERVER_ERROR,
                    !string.IsNullOrEmpty(ex.Message) ? ex.Message : "Unable to submit farm data");
            }
        }

        /// <summary>
        /// Parse coordinates from JSON array or raw text with optional order
        /// </summary>
        private List<FarmCoordinateDto> ParseCoordinates(string coordinatesInput)
        {
            if (string.IsNullOrWhiteSpace(coordinatesInput))
                return new List<FarmCoordinateDto>();

            // Try JSON first
            try
            {
                var jsonCoords = JsonSerializer.Deserialize<List<FarmCoordinateDto>>(coordinatesInput);
                if (jsonCoords != null && jsonCoords.Any())
                    return jsonCoords.Select((c, i) =>
                    {
                        if (c.Order == 0) c.Order = i + 1; // assign default order if missing
                        return c;
                    }).ToList();
            }
            catch { /* ignore, might not be JSON */ }

            // Otherwise parse raw text like "8.680801/6.93005\n8\n..."
            var lines = coordinatesInput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var coords = new List<FarmCoordinateDto>();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();

                if (line.Contains('/'))
                {
                    var parts = line.Split('/');
                    if (parts.Length != 2) continue;

                    if (double.TryParse(parts[0], out double latitude) &&
                        double.TryParse(parts[1], out double longitude))
                    {
                        long order = i + 1; // default order

                        // Check if next line is a number (order)
                        if (i + 1 < lines.Length && long.TryParse(lines[i + 1], out long parsedOrder))
                        {
                            order = parsedOrder;
                            i++; // skip the order line
                        }

                        coords.Add(new FarmCoordinateDto
                        {
                            Latitude = latitude,
                            Longitude = longitude,
                            Order = order
                        });
                    }
                }
            }

            return coords;
        }

    }
}
