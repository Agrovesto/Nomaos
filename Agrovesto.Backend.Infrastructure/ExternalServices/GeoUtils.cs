using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using NetTopologySuite.Geometries;
using NetTopologySuite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Agrovesto.Backend.Core.Models.Businesses;

namespace Agrovesto.Backend.Infrastructure.ExternalServices
{
    public static class GeoUtils
    {
        private static readonly GeometryFactory geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        public static Polygon CreatePolygonFromFarm(FarmPlotDetailsModel farm)
        {
            var coordinates = new[]
            {
            new Coordinate(farm.Longitude1, farm.Latitude1),
            new Coordinate(farm.Longitude2, farm.Latitude2),
            new Coordinate(farm.Longitude3, farm.Latitude3),
            new Coordinate(farm.Longitude4, farm.Latitude4),
            new Coordinate(farm.Longitude1, farm.Latitude1) // close the loop
        };

            var linearRing = geometryFactory.CreateLinearRing(coordinates);
            return geometryFactory.CreatePolygon(linearRing);
        }

        public static bool IsSamePolygon(FarmPlotDetailsModel a, FarmPlotDetailsModel b)
        {
            var polyA = GeoUtils.CreatePolygonFromFarm(a);
            var polyB = GeoUtils.CreatePolygonFromFarm(b);

            return polyA.EqualsExact(polyB); // or use polyA.EqualsTopologically(polyB)
        }
        public static bool IsOverlapping(FarmPlotDetailsModel a, FarmPlotDetailsModel b)
        {
            var polyA = GeoUtils.CreatePolygonFromFarm(a);
            var polyB = GeoUtils.CreatePolygonFromFarm(b);

            return polyA.Intersects(polyB); // returns true if they touch or overlap
        }

    }
}
