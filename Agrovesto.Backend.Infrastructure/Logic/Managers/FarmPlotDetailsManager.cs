using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
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
        public static double CalculateLandArea(double lat1, double lon1, double lat2, double lon2,
                                           double lat3, double lon3, double lat4, double lon4)
        {
            double[] x = { lon1, lon2, lon3, lon4 };
            double[] y = { lat1, lat2, lat3, lat4 };

            double area = 0;
            int j = x.Length - 1;  // Last vertex

            for (int i = 0; i < x.Length; i++)
            {
                area += (x[j] + x[i]) * (y[j] - y[i]);
                j = i; // j is previous vertex
            }
            return Math.Abs(area) / 2.0; // Return absolute value of area
        }
        public static double ConvertDegreesToSquareMeters(double areaInDegrees)
        {
            const double earthRadius = 6371000; // Earth's radius in meters
            double areaInMeters = areaInDegrees * (Math.PI / 180) * Math.Pow(earthRadius, 2);
            return areaInMeters;
        }
        public static double ConvertSquareMetersToHectares(double areaInSquareMeters)
        {
            return areaInSquareMeters / 10_000; // 1 hectare = 10,000 m²
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

                    var getAreaInSqMetre = ConvertDegreesToSquareMeters(getAreaInDegrees);
                    var getAreaInHectare = ConvertDegreesToSquareMeters(getAreaInSqMetre);
                    model.AreaInDegress = getAreaInDegrees;
                    model.AreaInSqMetre = getAreaInSqMetre;
                    model.AreaInHectare = getAreaInHectare;
                    model.BelongToBusinessCode = business.BusinessCode;
                    model.FarmerName = farmer.Name;
                    model.CoordinateTakenByUserId = args.UserId;
                    model.CoordinateTakenBy = getUserCred.model.Name;

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

        public async Task<FarmPlotDetailsModel> GetFarmDetailByFarmer(long farmerId)
        {
            return await _farmPlotDetailRepository.GetFarmDetailByFarmer(farmerId);
        }
    }
}
