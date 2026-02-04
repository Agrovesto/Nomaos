using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Agrovesto.Application.DTOs.Farms;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
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
        Task MapFarmAsync(List<FarmCoordinateDto> coordinates, FarmMappingArgs args);
        Task<Farm> GetOrCreateAsync(MapFarmRequest request, FarmMappingArgs args);
        Task AttachDocumentsAsync(List<FarmLandDocumentDto> documents, FarmMappingArgs args);
        Task AttachSingleDocumentsAsync(FarmLandDocumentDto document, FarmMappingArgs args);
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
        public async Task AttachSingleDocumentsAsync(

      FarmLandDocumentDto document,
       FarmMappingArgs args)
        {
            var farmExists = await _context.Farms
                .AnyAsync(f => f.Id == args.farmId);

            if (!farmExists)
                throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND, "Farm not found.");

            var entities = new FarmLandDocument
            {
                FarmId = args.farmId,
                DocumentType = document.DocumentType.ToEnum<FarmLandDocumentType>(),
                DocumentId = args.googleDriveDocumentId,
                DocumentUrl = document.DocumentUrl,
                DocumentTitle = document.DocumentTitle,
                UploadedOn = DateTimeOffset.UtcNow,
                IsVerified = false,

            };

            await _context.FarmLandDocuments.AddAsync(entities);
            _context.FarmMappingAuditTrails.Add(
       new FarmMappingAuditTrail
       {
           FarmId = args.farmerId,
           Action = "DocumentUploaded",
           PerformedByUserId = args.performedByUserId,
           PerformedByName = args.performedByName,
           Remarks = $"{document.DocumentType} uploaded"
       });

            await _context.SaveChangesAsync();
        }

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
        public async Task<Farm> GetOrCreateAsync(
        MapFarmRequest request,
        FarmMappingArgs args)
        {
            //  find an existing draft farm
            var existing = await _context.Farms.Where(x =>
            x.FarmerId == args.farmerId).FirstOrDefaultAsync(); ;

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
                
                CreatedBy = args.performedByUserId,
                MappingStatus = MappingStatus.NotStarted
            };

            await _context.AddAsync(farm);
            await _context.SaveChangesAsync();

            return farm;
        }
        public async Task MapFarmAsync(
   
       List<FarmCoordinateDto> coordinates,
      FarmMappingArgs args)
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            // Load farm
            var farm = await _context.Farms
                .Include(f => f.Coordinates)
                .FirstOrDefaultAsync(f => f.Id == args.farmId);

            if (farm == null)
                throw new BaseException(Core.Enums.ExceptionType.NO_RECORD_FOUND,"Farm not found.");

            // Reset old coordinates (re-mapping is allowed)
            _context.FarmCoordinates.RemoveRange(farm.Coordinates);

            // Save new coordinates
            var newCoords = coordinates.Select(c => new FarmCoordinate
            {
                FarmId = args.farmId,
                Latitude = c.Latitude,
                Longitude = c.Longitude,
                Order = c.Order,
                CapturedOn = DateTimeOffset.UtcNow
            }).ToList();

            await _context.FarmCoordinates.AddRangeAsync(newCoords);

            // Build polygon
            var polygon = BuildPolygon(newCoords);

            farm.FarmPolygon = polygon;
            farm.MappingStatus = MappingStatus.InProgress;

            //  Detect overlaps
            var overlaps = await _context.Farms
                .Where(f => f.Id != args.farmId)
                .Where(f => f.FarmPolygon != null)
                .Where(f => f.FarmPolygon.Intersects(polygon))
                .Where(f => f.FarmerId != args.farmerId)
                .Select(f => f.FarmerId)
                .Distinct()
                .ToListAsync();

            if (overlaps.Any())
            {
                farm.IsMappingFlagged = true;
                farm.MappingFlagReason =
                    $"Overlaps with FarmerIds: {string.Join(",", overlaps)}";

                // flag each coordinate
                foreach (var coord in newCoords)
                {
                    coord.IsSystemOverlap = true;
                    coord.FlaggedOn = DateTimeOffset.UtcNow;
                    coord.FlaggedReason = farm.MappingFlagReason;
                    coord.CoordinateOverlapWithFarmerIds = string.Join(",", overlaps);
                }

                await _context.FarmMappingAuditTrails.AddAsync(
                    new FarmMappingAuditTrail
                    {
                        FarmId = args.farmId,
                        Action = "MappingFlagged",
                        PerformedByUserId = args.performedByUserId,
                        PerformedByName = args.performedByName,
                        Remarks = farm.MappingFlagReason
                    });

            }
            else
            {
                farm.IsMappingFlagged = false;
                farm.MappingFlagReason = null;
            }

            // 6️⃣ Audit mapping submission
            await _context.FarmMappingAuditTrails.AddAsync(
                new FarmMappingAuditTrail
                {
                    FarmId = args.farmId,
                    Action = "MappingSubmitted",
                    PerformedByUserId = args.performedByUserId,
                    PerformedByName = args.performedByName,
                    Remarks = "Farm mapping submitted"
                });

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }

        private Polygon BuildPolygon(List<FarmCoordinate> coords)
        {
            var ordered = coords
                .OrderBy(c => c.Order)
                .Select(c => new Coordinate(c.Longitude, c.Latitude))
                .ToList();

            ordered.Add(ordered.First()); // close polygon

            return _geometryFactory.CreatePolygon(ordered.ToArray());
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
