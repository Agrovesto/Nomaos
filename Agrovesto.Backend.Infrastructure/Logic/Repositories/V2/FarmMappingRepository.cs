using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Agrovesto.Application.DTOs.Farms;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories.V2
{
    public interface IFarmMappingRepository
    {
        Task<Farm?> GetFarmByIdAsync(long farmId);

        Task<Farm?> GetFarmWithCoordinatesAsync(long farmId);

        Task AddFarmAsync(Farm farm);
        Task UpdateFarmAsync(Farm farm);

        Task<List<FarmOverlapInfo>> GetOverlappingFarmsAsync(
            long farmId,
            long currentFarmerId,
            Polygon newPolygon);
        Task AddCoordinateRangeAsync(IEnumerable<FarmCoordinate> coordinates);
        Task RemoveByFarmIdAsync(long farmId);
        Task AddFarmMappingAsync(FarmMappingAuditTrail audit);
        Task MapFarmAsync(MapFarmRequest request, FarmMappingArgs args);
        Task<Farm> GetOrCreateAsync(MapFarmRequest request, FarmMappingArgs args);
        Task AttachDocumentsAsync(List<FarmLandDocumentDto> documents, FarmMappingArgs args);
        Task AttachSingleDocumentsAsync(FarmLandDocumentDto document, FarmMappingArgs args);
        Task<Farm> GetOrCreateDraftAsync(MapFarmRequest request, FarmMappingArgs args);
        Task<Farm> FetchFarmsAsync(FarmMappingArgs args);
        Task<List<Farm>> FetchAllFarmsByFarmer(FarmMappingArgs args);
        Task<List<Farm>> FetchAllFarmsByFarmersBelongingToAnAgent(FarmMappingArgs args);
    }
    public class FarmMappingRepository : IFarmMappingRepository
    {
        private readonly DataContext _context;
        private readonly GeometryFactory _geometryFactory;
        public FarmMappingRepository(DataContext context)
        {
            _context = context;
            _geometryFactory = NtsGeometryServices.Instance
            .CreateGeometryFactory(srid: 4326);
        }
        public async Task<List<Farm>> FetchAllFarmsByFarmersBelongingToAnAgent(FarmMappingArgs args)
        {
            var farms = await _context.Farms
                .Where(f => _context.FarmerAgents
                    .Any(fa => fa.FarmerId == f.FarmerId && fa.AgentId == args.agentId))
                .Include(f => f.Coordinates)
                .Include(f => f.FarmLandDocuments)
                //.Include(f => f.AuditTrails)
                //.Include(f => f.Allocations)
                .ToListAsync();

            if (farms == null || farms.Count == 0)
                throw new BaseException(
                    Core.Enums.ExceptionType.NO_RECORD_FOUND,
                    "No farms found for this agent."
                );

            return farms;
        }


        public async Task<List<Farm>> FetchAllFarmsByFarmer(FarmMappingArgs args)
        {
            var farms = await _context.Farms
                .Where(f => f.FarmerId == args.farmerId)
                .Include(f => f.Coordinates)
                .Include(f => f.FarmLandDocuments)
              //  .Include(f=>f.AuditTrails)
               // .Include(f=>f.Allocations)
                .ToListAsync(); 
            if (farms.Count < 0 )
                throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Farm not found.");
            return farms;
        }
        public async Task<Farm> FetchFarmsAsync(FarmMappingArgs args)
        {
            var farm = await _context.Farms
                .Include(f => f.Coordinates)
                .Include(f => f.FarmLandDocuments)
              //  .Include(f=>f.AuditTrails)
               // .Include(f=>f.Allocations)
                .FirstOrDefaultAsync(f => f.Id == args.farmId);
            if (farm == null)
                throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Farm not found.");
            return farm;
        }
        public async Task AttachSingleDocumentsAsync(
    FarmLandDocumentDto document,
    FarmMappingArgs args)
        {
            var farmExists = await _context.Farms
                .AnyAsync(f => f.Id == args.farmId);

            if (!farmExists)
                throw new BaseException(
                    Core.Enums.ExceptionType.NO_RECORD_FOUND,
                    "Farm not found.");

            var documentTypeEnum = document.DocumentType.ToEnum<FarmLandDocumentType>();

            var existingDocument = await _context.FarmLandDocuments
                .FirstOrDefaultAsync(d =>
                    d.FarmId == args.farmId &&
                    d.DocumentType == documentTypeEnum);

            if (existingDocument != null)
            {
                existingDocument.DocumentId = document.DocumentUrl;
                existingDocument.DocumentUrl = document.DocumentUrl;
                existingDocument.DocumentTitle = document.DocumentTitle;
                existingDocument.UploadedOn = DateTimeOffset.UtcNow;
                existingDocument.IsVerified = false;

                _context.FarmLandDocuments.Update(existingDocument);
            }
            else
            {
                var entity = new FarmLandDocument
                {
                    FarmId = args.farmId,
                    DocumentType = documentTypeEnum,
                    DocumentId = document.DocumentUrl,
                    DocumentUrl = document.DocumentUrl,
                    DocumentTitle = document.DocumentTitle,
                    UploadedOn = DateTimeOffset.UtcNow,
                    IsVerified = false
                };

                await _context.FarmLandDocuments.AddAsync(entity);
            }

            _context.FarmMappingAuditTrails.Add(
                new FarmMappingAuditTrail
                {
                    FarmId = args.farmId,
                    Action = existingDocument != null ? "DocumentUpdated" : "DocumentUploaded",
                    PerformedByUserId = args.performedByUserId,
                    PerformedByName = args.performedByName,
                    Remarks = $"{document.DocumentType} {(existingDocument != null ? "updated" : "uploaded")}"
                });

            await _context.SaveChangesAsync();
        }
        //  public async Task AttachSingleDocumentsAsync(

        //FarmLandDocumentDto document,
        // FarmMappingArgs args)
        //  {
        //      var farmExists = await _context.Farms
        //          .AnyAsync(f => f.Id == args.farmId);

        //      if (!farmExists)
        //          throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Farm not found.");

        //      var entities = new FarmLandDocument
        //      {
        //          FarmId = args.farmId,
        //          DocumentType = document.DocumentType.ToEnum<FarmLandDocumentType>(),
        //          DocumentId = document.DocumentUrl,
        //          DocumentUrl = document.DocumentUrl,
        //          DocumentTitle = document.DocumentTitle,
        //          UploadedOn = DateTimeOffset.UtcNow,
        //          IsVerified = false,

        //      };

        //      await _context.FarmLandDocuments.AddAsync(entities);
        //      _context.FarmMappingAuditTrails.Add(
        // new FarmMappingAuditTrail
        // {
        //     FarmId = args.farmId,
        //     Action = "DocumentUploaded",
        //     PerformedByUserId = args.performedByUserId,
        //     PerformedByName = args.performedByName,
        //     Remarks = $"{document.DocumentType} uploaded"
        // });

        //      await _context.SaveChangesAsync();
        //  }

        public async Task AttachDocumentsAsync(
      
        List<FarmLandDocumentDto> documents,
        FarmMappingArgs args)
        {
            var farmExists = await _context.Farms
                .AnyAsync(f => f.Id == args.farmId);

            if (!farmExists)
                throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND,"Farm not found.");
            
            var entities = documents.Select(d => new FarmLandDocument
            {
                FarmId = args.farmId,
                DocumentType =d.DocumentType.ToEnum<FarmLandDocumentType>(),
                DocumentId =args.googleDriveDocumentId,
                DocumentUrl = d.DocumentUrl,
                DocumentTitle = d.DocumentTitle,
                UploadedOn = DateTimeOffset.UtcNow,
                IsVerified = false,
                
            });

            await _context.FarmLandDocuments.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }

        public async Task<Farm> CreateAsync(
    MapFarmRequest request,
    FarmMappingArgs args)
        {
            var farm = new Farm
            {
                Name = request.FarmName,
                FarmerId = args.farmerId,
                OwnershipType = request.OwnershipType,
                FarmState = request.FarmState,
                FarmLga = request.FarmLga,
                FarmWard = request.FarmWard,
                FarmLocation = request.FarmLocation,
                BelongToBusinessCode = args.businessCode,
                OrganizationCode = args.organizationCode,
                LocationDescription = request.FarmLocation,
                FarmSize = request.AreaSize,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = args.performedByUserId,
                MappingStatus = MappingStatus.NotStarted
            };

            _context.Farms.Add(farm);
            await _context.SaveChangesAsync();

            return farm;
        }
        public async Task<Farm> GetOrCreateDraftAsync(
            MapFarmRequest request,
            FarmMappingArgs args)
        {
            if (args.farmId > 0)
            {
                var existingFarm = await _context.Farms
                    .FirstOrDefaultAsync(f =>
                        f.Id == args.farmId &&
                        f.FarmerId == args.farmerId);

                if (existingFarm == null)
                    throw new BaseException(
                        ExceptionType.NO_RECORD_FOUND,
                        "Farm not found or access denied.");

                UpdateFarmFields(existingFarm, request);

                await _context.SaveChangesAsync();
                return existingFarm;
            }

            if (args.ContinueDraft)
            {
                var draftFarm = await _context.Farms
                    .Where(f =>
                        f.FarmerId == args.farmerId &&
                        f.MappingStatus == MappingStatus.NotStarted)
                    .OrderByDescending(f => f.CreatedAt)
                    .FirstOrDefaultAsync();

                if (draftFarm != null)
                    return draftFarm;
            }

            var newFarm = new Farm
            {
                Name = request.FarmName,
                FarmerId = args.farmerId,
                OwnershipType = request.OwnershipType,
                FarmState = request.FarmState,
                FarmLga = request.FarmLga,
                FarmWard = request.FarmWard,
                FarmLocation = request.FarmLocation,
                LocationDescription = request.FarmLocation,
                FarmSize = request.AreaSize,
                BelongToBusinessCode = args.businessCode,
                OrganizationCode = args.organizationCode,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = args.performedByUserId,
                CapturedBy = args.performedByUserId,
                MappingStatus = MappingStatus.NotStarted,
                MeasurementMode = request.MeasurementMode,
                PinnedLatitude = request.PinnedLatitude,
                PinnedLongitude = request.PinnedLongitude,
                MappingAccuracy = request.MappingAccuracy,
                DistanceCovered = request.DistanceCovered
            };

            _context.Farms.Add(newFarm);
            await _context.SaveChangesAsync();

            return newFarm;
        }
        private void UpdateFarmFields(Farm farm, MapFarmRequest request)
        {
            farm.Name = request.FarmName;
            farm.OwnershipType = request.OwnershipType;
            farm.FarmState = request.FarmState;
            farm.FarmLga = request.FarmLga;
            farm.FarmWard = request.FarmWard;
            farm.FarmLocation = request.FarmLocation;
            farm.LocationDescription = request.FarmLocation;
            farm.FarmSize = request.AreaSize;
            farm.PinnedLongitude = request.PinnedLongitude;
            farm.PinnedLatitude = request.PinnedLatitude;
            farm.DistanceCovered = request.DistanceCovered;
            farm.MappingAccuracy = request.MappingAccuracy;
            farm.MeasurementMode = request.MeasurementMode;
        }
        public async Task<Farm> GetOrCreateAsync(
        MapFarmRequest request,
        FarmMappingArgs args)
        {
            //  find an existing draft farm
            var existing = await _context.Farms.Where(x =>
            x.FarmerId == args.farmerId).FirstOrDefaultAsync(); 

            if (existing != null)
                return existing;

            //  Create new farm
            var farm = new Farm
            {
                Name = request.FarmName,
                FarmerId = args.farmerId,
                OwnershipType = request.OwnershipType,
                FarmState = request.FarmState,
                FarmLga = request.FarmLga,
                FarmWard = request.FarmWard,
                FarmLocation= request.FarmLocation,
                BelongToBusinessCode=args.businessCode,
                OrganizationCode=args.organizationCode,
              //  MembershipId= args.membershipId,
                LocationDescription = request.FarmLocation,
               // AgentId=args.agentId,
                CreatedAt= DateTimeOffset.UtcNow,
                FarmSize=request.AreaSize,
                CreatedBy = args.performedByUserId,
                MappingStatus = MappingStatus.NotStarted
            };

            await _context.AddAsync(farm);
            await _context.SaveChangesAsync();

            return farm;
        }
        public async Task MapFarmAsync(
       MapFarmRequest request,
       FarmMappingArgs args)
        {
            if (args.farmId <= 0)
                throw new BaseException(ExceptionType.BAD_REQUEST, "FarmId is required for mapping.");

            if (request.Coordinates == null || request.Coordinates.Count < 3)
                throw new BaseException(ExceptionType.BAD_REQUEST, "At least 3 coordinates are required.");

            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();

                // 1️⃣ Load farm with existing coordinates
                var farm = await _context.Farms
                    .Include(f => f.Coordinates)
                    .FirstOrDefaultAsync(f => f.Id == args.farmId);

                if (farm == null)
                    throw new BaseException(ExceptionType.NO_RECORD_FOUND, "Farm not found.");

                // 2️⃣ Clear old coordinates (re-mapping allowed)
                if (farm.Coordinates.Any())
                    _context.FarmCoordinates.RemoveRange(farm.Coordinates);

                // 3️⃣ Normalize & save new coordinates
                var newCoords = request.Coordinates
                    .Select((c, index) => new FarmCoordinate
                    {
                        FarmId = farm.Id,
                        Latitude = c.Latitude,
                        Longitude = c.Longitude,
                        Order = c.Order > 0 ? c.Order : index + 1
                    })
                    .ToList();

                await _context.FarmCoordinates.AddRangeAsync(newCoords);

                // 4️⃣ Build SAFE polygon (valid, CCW, non-self-intersecting)
                var polygon = BuildPolygon(newCoords);

                // 5️⃣ Update farm mapping info
                farm.FarmPolygon = polygon;
                farm.MappingStatus = MappingStatus.InProgress;
                farm.CapturedBy = args.performedByUserId;
                farm.CapturedOn = DateTimeOffset.UtcNow;
                farm.MappedBy = args.performedByUserId;
                farm.MeasurementDate = DateTimeOffset.UtcNow;
                farm.MeasurementMode = request.MeasurementMode;

                // Area calculations (geography uses meters²)
                farm.FarmSize = polygon.Area;
                farm.SizeInHectares = polygon.Area / 10_000d;
                farm.SizeInKm = polygon.Area / 1_000_000d;
                farm.PinnedLongitude= request.PinnedLongitude;
                farm.PinnedLatitude= request.PinnedLatitude;
                farm.DistanceCovered= request.DistanceCovered;
                farm.MappingAccuracy= request.MappingAccuracy;
                farm.IsMappingVerified = false;
                farm.IsSizeVerified = false;

                // 6️⃣ SAFE overlap detection (MakeValid on BOTH sides)
                var candidateFarms = await _context.Farms
      .Where(f =>
          f.Id != args.farmId &&
          f.FarmPolygon != null &&
          f.FarmerId != args.farmerId)
      .Select(f => new { f.FarmerId, f.FarmPolygon })
      .ToListAsync(); // SQL-safe part

                var overlappingFarmers = candidateFarms
                    .Where(f =>
                        f.FarmPolygon.IsValid &&
                        f.FarmPolygon.Intersects(polygon))
                    .Select(f => f.FarmerId)
                    .Distinct()
                    .ToList();





                if (overlappingFarmers.Any())
                {
                    farm.IsMappingFlagged = true;
                    farm.MappingFlagReason =
                        $"Overlaps with FarmerIds: {string.Join(",", overlappingFarmers)}";

                    foreach (var coord in newCoords)
                    {
                        coord.IsSystemOverlap = true;
                        coord.FlaggedOn = DateTimeOffset.UtcNow;
                        coord.FlaggedReason = farm.MappingFlagReason;
                        coord.CoordinateOverlapWithFarmerIds =
                            string.Join(",", overlappingFarmers);
                    }

                    await _context.FarmMappingAuditTrails.AddAsync(
                        new FarmMappingAuditTrail
                        {
                            FarmId = farm.Id,
                            Action = "MappingFlagged",
                            PerformedByUserId = args.performedByUserId,
                            PerformedByName =
                                string.IsNullOrWhiteSpace(args.performedByName)
                                    ? "NA"
                                    : args.performedByName,
                            Remarks = farm.MappingFlagReason
                        });
                }
                else
                {
                    farm.IsMappingFlagged = false;
                    farm.MappingFlagReason = null;
                }

                // 7️⃣ Audit mapping submission
                await _context.FarmMappingAuditTrails.AddAsync(
                    new FarmMappingAuditTrail
                    {
                        FarmId = farm.Id,
                        Action = "MappingSubmitted",
                        PerformedByUserId = args.performedByUserId,
                        PerformedByName =
                            string.IsNullOrWhiteSpace(args.performedByName)
                                ? "NA"
                                : args.performedByName,
                        Remarks = "Farm mapping submitted"
                    });

                // 8️⃣ Persist everything
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            });
        }

        private Polygon BuildPolygon(List<FarmCoordinate> coords)
        {
            var geometryFactory = NtsGeometryServices.Instance
                .CreateGeometryFactory(srid: 4326);

            // 1️⃣ Order + remove duplicates
            var coordinates = coords
                .OrderBy(c => c.Order)
                .Select(c => new Coordinate(c.Longitude, c.Latitude))
                .Distinct(new CoordinateEqualityComparer())
                .ToList();

            if (coordinates.Count < 3)
                throw new BaseException(
                    ExceptionType.BAD_REQUEST,
                    "A valid polygon requires at least 3 unique points.");

            // 2️⃣ Close ring
            if (!coordinates.First().Equals2D(coordinates.Last()))
                coordinates.Add(coordinates.First());

            // 3️⃣ Create polygon
            var polygon = geometryFactory.CreatePolygon(coordinates.ToArray());

            // 4️⃣ Fix self-intersections (THIS replaces SQL MakeValid)
            if (!polygon.IsValid)
                polygon = (Polygon)polygon.Buffer(0);

            // 5️⃣ Force CCW orientation (SQL Server requirement)
            var shellCoords = polygon.ExteriorRing.Coordinates.ToList();
            if (!IsCounterClockwise(shellCoords))
            {
                shellCoords.Reverse();
                polygon = geometryFactory.CreatePolygon(
                    geometryFactory.CreateLinearRing(shellCoords.ToArray()));
            }

            // 6️⃣ Final validation (fail fast)
            if (!polygon.IsValid)
                throw new BaseException(
                    ExceptionType.BAD_REQUEST,
                    "Invalid farm boundary. Please redraw the farm.");

            return polygon;
        }
        private static bool IsCounterClockwise(IReadOnlyList<Coordinate> coords)
        {
            double sum = 0;
            for (int i = 0; i < coords.Count - 1; i++)
            {
                sum += (coords[i + 1].X - coords[i].X)
                     * (coords[i + 1].Y + coords[i].Y);
            }
            return sum < 0;
        }
        private sealed class CoordinateEqualityComparer : IEqualityComparer<Coordinate>
        {
            public bool Equals(Coordinate a, Coordinate b) =>
                a.X == b.X && a.Y == b.Y;

            public int GetHashCode(Coordinate c) =>
                HashCode.Combine(c.X, c.Y);
        }


        //private Polygon BuildPolygon(List<FarmCoordinate> coords)
        //{
        //    var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        //    // 1️⃣ Order by 'Order' or sequence
        //    var ordered = coords
        //        .OrderBy(c => c.Order)
        //        .Select(c => new Coordinate(c.Longitude, c.Latitude))
        //        .ToList();

        //    // 2️⃣ Close polygon
        //    if (!ordered.First().Equals2D(ordered.Last()))
        //        ordered.Add(ordered.First());

        //    // 3️⃣ Reverse if clockwise
        //    if (IsClockwise(ordered))
        //        ordered.Reverse();

        //    // 4️⃣ Build polygon
        //    return geometryFactory.CreatePolygon(ordered.ToArray());
        //}

        private bool IsClockwise(List<Coordinate> coords)
        {
            double sum = 0;
            for (int i = 0; i < coords.Count - 1; i++)
            {
                var current = coords[i];
                var next = coords[i + 1];
                sum += (next.X - current.X) * (next.Y + current.Y);
            }
            // If sum > 0, the polygon is clockwise
            return sum > 0;
        }

        public Task AddFarmAsync(Farm farm)
        {
            throw new NotImplementedException();
        }

        public Task AddFarmMappingAsync(FarmMappingAuditTrail audit)
        {
            throw new NotImplementedException();
        }

        public Task AddCoordinateRangeAsync(IEnumerable<FarmCoordinate> coordinates)
        {
            throw new NotImplementedException();
        }

        public Task<Farm?> GetFarmByIdAsync(long farmId)
        {
            throw new NotImplementedException();
        }

        public async Task<List<FarmOverlapInfo>> GetOverlappingFarmsAsync(
    long farmId,
    long currentFarmerId,
    Polygon newPolygon)
        {
            return await _context.Farms
                .Where(f => f.Id != farmId)
                .Where(f => f.FarmPolygon != null)
                .Where(f => f.FarmPolygon.Intersects(newPolygon))
                .Where(f => f.FarmerId != currentFarmerId)
                .Select(f => new FarmOverlapInfo
                {
                    FarmId = f.Id,
                    FarmerId = f.FarmerId
                })
                .ToListAsync();
        }

        public Task<Farm?> GetFarmWithCoordinatesAsync(long farmId)
        {
            throw new NotImplementedException();
        }

        public Task RemoveByFarmIdAsync(long farmId)
        {
            throw new NotImplementedException();
        }

        public Task UpdateFarmAsync(Farm farm)
        {
            throw new NotImplementedException();
        }
    }
}
