using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Utilities
{
    public static class GeometryHelper
    {
        // Shoelace formula for area computation (returns hectares)
        
        public static double ComputePolygonArea(List<(double lat, double lon)> coords)
        {
            if (coords.Count < 3) return 0;

            double area = 0;
            for (int i = 0; i < coords.Count; i++)
            {
                int j = (i + 1) % coords.Count;
                area += coords[i].lon * coords[j].lat - coords[j].lon * coords[i].lat;
            }

            area = Math.Abs(area) / 2.0;
            double sqm = area * 12365.1613; // rough conversion
            return sqm / 10000; // convert to hectares
        }

        // Simple overlap check — compares bounding boxes
        public static bool IsOverlapping(List<FarmCoordinate> existing, List<FarmCoordinate> incoming)
        {
            if (!existing.Any() || !incoming.Any()) return false;

            double minLat1 = existing.Min(c => c.Latitude);
            double maxLat1 = existing.Max(c => c.Latitude);
            double minLon1 = existing.Min(c => c.Longitude);
            double maxLon1 = existing.Max(c => c.Longitude);

            double minLat2 = incoming.Min(c => c.Latitude);
            double maxLat2 = incoming.Max(c => c.Latitude);
            double minLon2 = incoming.Min(c => c.Longitude);
            double maxLon2 = incoming.Max(c => c.Longitude);

            // Bounding box intersection check
            bool latOverlap = minLat1 <= maxLat2 && maxLat1 >= minLat2;
            bool lonOverlap = minLon1 <= maxLon2 && maxLon1 >= minLon2;

            return latOverlap && lonOverlap;
        }
    }
}
