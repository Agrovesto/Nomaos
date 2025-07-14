using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.ExternalServices;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers
{
    public class FarmPlotDetailsManager : IFarmPlotDetailsManager
    {
        private readonly IBusinessRepository _businessRepository;
        private readonly IFarmPlotDetailRepository _farmPlotDetailRepository;
        private readonly IFarmerRepository _farmerRepository;
        private readonly DataContext _context;
        private readonly IAuthenticationRepository _authenticationRepository;
        public FarmPlotDetailsManager(IBusinessRepository businessRepository, IFarmPlotDetailRepository farmPlotDetailRepository,
            DataContext context, IFarmerRepository farmerRepository, IAuthenticationRepository authenticationRepository)
        {
            _farmPlotDetailRepository = farmPlotDetailRepository;
            _businessRepository = businessRepository;
            _context = context;
            _farmerRepository = farmerRepository;
            _authenticationRepository = authenticationRepository;

        }
        private const double EarthRadius = 6378137; // Earth's radius in meters

        public static double CalculateLandArea(double lat1, double lon1, double lat2, double lon2,
                                               double lat3, double lon3, double lat4, double lon4)
        {
            // Convert degrees to radians
            double ToRad(double value) => value * Math.PI / 180.0;

            // Convert latitudes and longitudes to radians
            double[,] points = {
            { ToRad(lon1), ToRad(lat1) },
            { ToRad(lon2), ToRad(lat2) },
            { ToRad(lon3), ToRad(lat3) },
            { ToRad(lon4), ToRad(lat4) }
        };

            double area = 0;
            int numPoints = points.GetLength(0);

            for (int i = 0; i < numPoints; i++)
            {
                int j = (i + 1) % numPoints;
                double xi = points[i, 0];
                double yi = points[i, 1];
                double xj = points[j, 0];
                double yj = points[j, 1];

                area += xi * yj - xj * yi;
            }

            area = Math.Abs(area) * (EarthRadius * EarthRadius) / 2.0;
            return area;
        }
        public static double ConvertDegreesToSquareMeters(double areaInDegrees)
        {
            const double earthRadius = 6371000; // Earth's radius in meters
            double areaInMeters = areaInDegrees * (Math.PI / 180) * Math.Pow(earthRadius, 2);
            return areaInMeters;
        }
        public static double ConvertSquareMetersToHectares(double areaInSquareMeters)
        {
            return areaInSquareMeters / 10000; // 1 hectare = 10,000 m²
        }
        private void AddAuditTrail(FarmPlotDetailsModel farm, string action, string userId, string userName, string remarks)
        {
            farm.AuditTrails.Add(new FarmFlagAuditTrailModel
            {
                Action = action,
                PerformedByUserId = userId,
                PerformedByName = userName,
                Remarks = remarks
            });
        }
        public static double GetAreaInSquareMeters(FarmPlotDetailsModel farm)
        {
            var polygon = GeoUtils.CreatePolygonFromFarm(farm);
            return polygon.Area * 111_139 * 111_139; // rough conversion assuming 1 deg = ~111.139 km
        }

         public async Task<bool> UnflagFarmAsync(ListQueryParams args,long farmId)
        {
            try
            {


                var farm = await _farmPlotDetailRepository.GetFarmDetailByIdAsync(farmId);
                if (farm == null || !farm.IsFarmFlaggedBySystem)
                    return false;
                var getUserCred = await _authenticationRepository.GetUserById(args.UserId);
                if (!getUserCred.status) throw new Exception("User taking coordinate not recognized");

                farm.UnflagFarm = true;
                farm.FarmUnflaggedByUserId = args.UserId;
                farm.FarmUnflaggedByName = getUserCred.model.Name;
                farm.UnflaggedOn = DateTimeOffset.UtcNow;


                farm.AuditTrails.Add(new FarmFlagAuditTrailModel
                {
                    FarmPlotDetailsId=farmId,
                    PerformedOn=DateTimeOffset.UtcNow,
                    Action = "Unflagged",
                    PerformedByUserId = args.UserId,
                    PerformedByName = getUserCred.model.Name,
                    Remarks = "Manually unflagged by agent."
                });

                await _farmPlotDetailRepository.UpdateFarmAsync(farm);
                return true;
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        } 
        public async Task<List<FarmPlotDetailsModel>> GetFarmDetailsByCooperative(ListQueryParams args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
              ?? throw new Exception("Business does not exist");

            try
            {
                var plots = await _farmPlotDetailRepository.GetFarmDetailsByCooperative(args.CooperativeId, args.businessCode, args.organizationCode);
                if (plots == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                return plots;
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }
        public async Task<List<FarmPlotDetailsModel>> GetFarmDetailsByBusiness(ListQueryParams args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
              ?? throw new Exception("Business does not exist");

            try
            {
                var plots = await _farmPlotDetailRepository.GetFarmDetailsByBusiness(args.businessCode, args.organizationCode);
                if (plots == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                return plots;
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }

        public async Task<List<FarmFlagAuditTrailModel>> GetAuditTrail(BusinessRegistrationArgs args, long plotId)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
              ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            try
            {
                var audit = await _farmPlotDetailRepository.GetAuditTrail(plotId, business.BusinessCode, args.organizationCode);
                if (audit == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                return audit;
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }


        public async Task<List<FarmPlotDetailsModel>> GetFlaggedFarmsAsync(BusinessRegistrationArgs args)
            => await _farmPlotDetailRepository.GetFlaggedFarmsAsync(args.businessCode, args.organizationCode);


        public async Task<(bool isValid, string message)> ValidateAndSaveFarm(FarmPlotDetailsModel newFarm, BusinessRegistrationArgs args)
        {
            string farmMappingmessage = "";
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;

            (bool status, FarmPlotDetailsModel data) update = new(false, new FarmPlotDetailsModel());
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var allFarms = await _farmPlotDetailRepository.GetFarmDetails("","");
                    List<string> overlapIds = new();
                    bool isFlagged = false;
                    string flagReason = "";

                    var farmer = await _farmerRepository.GetFarmerById(newFarm.FarmerId);
                    if (farmer == null) throw new Exception("Farmer does not exist");
                    var getUserCred = await _authenticationRepository.GetUserById(args.UserId);
                    if (!getUserCred.status) throw new Exception("User taking coordinate not recognized");

                    //newFarm.AuditTrails.Add(new FarmFlagAuditTrailModel
                    //{
                    //    Action = "Unflagged",
                    //    PerformedByName = "System",
                    //    PerformedByUserId = "System",
                    //    Remarks = flagReason
                    //});
                    foreach (var existing in allFarms.Where(f => f.FarmerId != newFarm.FarmerId))
                    {
                        if (GeoUtils.IsSamePolygon(existing, newFarm))
                        {
                            isFlagged = true;
                            flagReason = $"Exact match with FarmerId: {existing.FarmerId}";
                            overlapIds.Add(existing.FarmerId.ToString());
                            break;
                        }

                        if (GeoUtils.IsOverlapping(existing, newFarm))
                        {
                            isFlagged = true;
                            flagReason = $"Overlaps with Farmer: {existing.FarmerName} Id: {existing.FarmerId}";
                            overlapIds.Add(existing.FarmerId.ToString());
                        }
                    }
                    if (isFlagged)
                    {
                        newFarm.IsFarmFlaggedBySystem = true;
                        newFarm.FlaggedReason = flagReason;
                        newFarm.CoordinateOverlapWithFarmerIds = string.Join(",", overlapIds.Distinct());
                        newFarm.AuditTrails.Add(new FarmFlagAuditTrailModel
                        {
                            Action = "Flagged",
                            PerformedByName = "System",
                            PerformedByUserId = "System",
                            Remarks = flagReason
                        });
                    }
                    var getAreaInDegrees = CalculateLandArea(newFarm.Latitude1, newFarm.Longitude1, newFarm.Latitude2, newFarm.Longitude2,
                                                                     newFarm.Latitude3, newFarm.Longitude3, newFarm.Latitude4, newFarm.Longitude4);

                    //var getAreaInSqMetre = CalculateLandArea(newFarm.Latitude1, newFarm.Longitude1, newFarm.Latitude2, newFarm.Longitude2,
                                            //                 newFarm.Latitude3, newFarm.Longitude3, newFarm.Latitude4, newFarm.Longitude4);

                    var areaInSqMtr = GetAreaInSquareMeters(newFarm);

                   
                    var getAreaInHectare = ConvertSquareMetersToHectares(areaInSqMtr);// ConvertSquareMetersToHectares(getAreaInSqMetre);
                    newFarm.AreaInDegress = getAreaInDegrees;
                    newFarm.AreaInSqMetre = areaInSqMtr;//getAreaInSqMetre;
                    newFarm.AreaInHectare = getAreaInHectare;
                    newFarm.BelongToBusinessCode = business.BusinessCode;
                    newFarm.FarmerName = farmer.Name;
                    newFarm.CoordinateTakenByUserId = args.UserId;
                    newFarm.CoordinateTakenBy = getUserCred.model.FirstName;
                    newFarm.OrganizationCode = args.organizationCode;
                    update = await _farmPlotDetailRepository.UpdateFarmerFarmPlot(newFarm);


                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    farmMappingmessage = newFarm.IsFarmFlaggedBySystem ? "Farm saved but flagged for overlap." : "Farm saved successfully.";


                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    throw new Exception($"{ex.Message}", ex);
                }
            });

            return (true, farmMappingmessage);
        }

        public async Task<FarmPlotDetailsModel> UpdateFarmCoordinateDetails(FarmPlotDetailsModel model, BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;

            (bool status, FarmPlotDetailsModel data) update = new(false, new FarmPlotDetailsModel());
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var farmer = await _farmerRepository.GetFarmerByUserId(args.UserId);
                    if (farmer == null) throw new Exception("Farmer does not exist");
                    var getUserCred = await _authenticationRepository.GetUserById(args.UserId);
                    if (!getUserCred.status) throw new Exception("User taking coordinate not recognized");
                    var getAreaInDegrees = CalculateLandArea(model.Latitude1, model.Longitude1, model.Latitude2, model.Longitude2,
                                                             model.Latitude3, model.Longitude3, model.Latitude4, model.Longitude4);

                    var getAreaInSqMetre = CalculateLandArea(model.Latitude1, model.Longitude1, model.Latitude2, model.Longitude2,
                                                             model.Latitude3, model.Longitude3, model.Latitude4, model.Longitude4);
                    var getAreaInHectare = ConvertSquareMetersToHectares(getAreaInSqMetre);
                    model.AreaInDegress = getAreaInDegrees;
                    model.AreaInSqMetre = getAreaInSqMetre;
                    model.AreaInHectare = getAreaInHectare;
                    model.BelongToBusinessCode = business.BusinessCode;
                    model.FarmerName = farmer.Name;
                    model.CoordinateTakenByUserId = args.UserId;
                    model.CoordinateTakenBy = getUserCred.model.FirstName;
                    model.FarmerId = farmer.Id;

                    update = await _farmPlotDetailRepository.UpdateFarmerFarmPlot(model);



                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    return update.data;
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    throw new Exception($"{ex.Message}", ex);
                }
            });
            return update.data;
        }

        public async Task<FarmPlotDetailsModel> UpdateFarmPlotDetails(FarmPlotDetailsModel model, BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;

            (bool status, FarmPlotDetailsModel data) update = new(false, new FarmPlotDetailsModel());
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var farmer = await _farmerRepository.GetFarmerById(model.FarmerId);
                    if (farmer == null) throw new Exception("Farmer does not exist");
                    var getUserCred = await _authenticationRepository.GetUserById(args.UserId);
                    if (!getUserCred.status) throw new Exception("User taking coordinate not recognized");
                    var getAreaInDegrees = CalculateLandArea(model.Latitude1, model.Longitude1, model.Latitude2, model.Longitude2,
                                                             model.Latitude3, model.Longitude3, model.Latitude4, model.Longitude4);

                    var getAreaInSqMetre = CalculateLandArea(model.Latitude1, model.Longitude1, model.Latitude2, model.Longitude2,
                                                             model.Latitude3, model.Longitude3, model.Latitude4, model.Longitude4);
                    var getAreaInHectare = ConvertSquareMetersToHectares(getAreaInSqMetre);
                    model.AreaInDegress = getAreaInDegrees;
                    model.AreaInSqMetre = getAreaInSqMetre;
                    model.AreaInHectare = getAreaInHectare;
                    model.BelongToBusinessCode = business.BusinessCode;
                    model.FarmerName = farmer.Name;
                    model.CoordinateTakenByUserId = args.UserId;
                    model.CoordinateTakenBy = getUserCred.model.FirstName;

                    update = await _farmPlotDetailRepository.UpdateFarmerFarmPlot(model);



                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    return update.data;
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    throw new Exception($"{ex.Message}", ex);
                }
            });
            return update.data;
        }

        public async Task<List<FarmPlotDetailsModel>> GetFarmDetailByFarmer(long farmerId)
        {
            return await _farmPlotDetailRepository.GetFarmDetailByFarmer(farmerId);
        }


        public async Task<FarmActivityTypeModel> CreateActivityType(FarmActivityTypeModel model, BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
               ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;

            (bool status, FarmActivityTypeModel data) update = new(false, new FarmActivityTypeModel());
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    model.OrganizationCode = args.organizationCode;
                    model.BelongToBusinessCode = business.BusinessCode;

                    update = await _farmPlotDetailRepository.CreateVsitationLogType(model);
                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    return update.data;
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    throw new Exception($"{ex.Message}", ex);
                }

            });
            return update.data;
        }


        public async Task<List<FarmActivityTypeModel>> GetActivityTypes(BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
              ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            try
            {
                var types = await _farmPlotDetailRepository.GetVisitationLogtypesByBusiness(business.BusinessCode, args.organizationCode);
                if (types == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                return types;
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }
        public async Task<FarmActivityTypeModel> GetActivityTypeById(BusinessRegistrationArgs args, long farmerId)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
              ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            try
            {
                var types = await _farmPlotDetailRepository.GetVisitationLogtypebyId(business.BusinessCode, farmerId);
                if (types == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                return types;
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }

        public async Task<FarmVisitationActivityModel> LogFarmVisit(FarmVisitationActivityModel model, BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;

            (bool status, FarmVisitationActivityModel data) update = new(false, new FarmVisitationActivityModel());
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var farmer = await _farmerRepository.GetFarmerByUserId(model.FarmerUserId);
                    if (farmer == null) throw new Exception("Farmer does not exist");
                    var activityType = await _farmPlotDetailRepository.GetVisitationLogtypebyId(business.BusinessCode, model.FarmActivityId);
                    if (activityType == null) throw new Exception("Invalid Activity selected");

                    var getUserCred = await _authenticationRepository.GetUserById(args.UserId);
                    if (!getUserCred.status) throw new Exception("User taking log not recognized");


                    model.BelongToBusinessCode = business.BusinessCode;
                    model.FarmerName = farmer.Name;
                    model.FarmActivityId = activityType.Id;
                    model.CooperativeId = farmer.BelongToCooperativeId;
                    model.OfficerInChargeId = args.UserId;
                    model.OfficerName = getUserCred.model.FirstName;
                    model.CreatedBy = getUserCred.model.FirstName;
                    model.OrganizationCode = args.organizationCode;
                    model.SupportingImageUrl = model.SupportingImageUrl;
                    update = await _farmPlotDetailRepository.VisitationLog(model);



                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();

                    return update.data;
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    throw new Exception($"{ex.Message}", ex);
                }
            });
            return update.data;
        }


        public async Task<List<FarmVisitationActivityModel>> GetVisitationLogsByBusiness(BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
              ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            try
            {
                var types = await _farmPlotDetailRepository.GetVisitationLogsByBusiness(business.BusinessCode, args.organizationCode);
                if (types == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                return types;
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }

        public async Task<List<FarmVisitationActivityModel>> GetVisitationLogByFarmer(BusinessRegistrationArgs args, string farmerId)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
              ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            try
            {
                var types = await _farmPlotDetailRepository.GetVisitationLogByFarmer(business.BusinessCode, farmerId);
                if (types == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                return types;
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }

        public async Task<List<FarmVisitationActivityModel>> GetVisitationLogsByOfficerInCharge(BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
              ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            try
            {
                var types = await _farmPlotDetailRepository.GetVisitationLogsByOfficerInCharge(business.BusinessCode, args.UserId);
                if (types == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                return types;
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }

        public async Task<List<FarmVisitationActivityModel>> GetVisitationLogsByCooperative(BusinessRegistrationArgs args)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
              ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            try
            {
                var types = await _farmPlotDetailRepository.GetVisitationLogsByCooperative(business.BusinessCode, args.CooperativeId);
                if (types == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                return types;
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }

        public async Task<FarmVisitationActivityModel> GetVisitationLogById(BusinessRegistrationArgs args, long visitationId)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
              ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            try
            {
                var types = await _farmPlotDetailRepository.GetVisitationLogById(business.BusinessCode, visitationId);
                if (types == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                return types;
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }

        public async Task<List<FarmVisitationActivityModel>> GetVisitationLogsByActivityType(BusinessRegistrationArgs args, long activitytypeId)
        {
            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
              ?? throw new Exception("Business does not exist");

            args.UserId ??= business.UserId;
            try
            {
                var types = await _farmPlotDetailRepository.GetVisitationLogsByActivityType(business.BusinessCode, activitytypeId);
                if (types == null) throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found");

                return types;
            }
            catch (Exception ex)
            {
                throw new NotFoundException($"No Record Found");
            }
        }

    }
}
