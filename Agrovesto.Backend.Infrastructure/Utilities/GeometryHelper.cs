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

        public static double CalculatePolygonAreaDegrees(List<(double Lat, double Lng)> coords)
        {
            if (coords.Count < 3) return 0;

            double area = 0;
            int j = coords.Count - 1;

            for (int i = 0; i < coords.Count; i++)
            {
                area += (coords[j].Lng + coords[i].Lng) * (coords[j].Lat - coords[i].Lat);
                j = i;
            }

            return Math.Abs(area / 2.0);
        }


        public static double CalculatePolygonAreaSqMeters(List<(double Lat, double Lng)> coords)
        {
            if (coords.Count < 3) return 0;

            const double R = 6378137; // Earth radius in meters
            double area = 0;

            for (int i = 0; i < coords.Count; i++)
            {
                var (lat1, lon1) = coords[i];
                var (lat2, lon2) = coords[(i + 1) % coords.Count];

                // Convert degrees to radians
                double phi1 = lat1 * Math.PI / 180;
                double phi2 = lat2 * Math.PI / 180;
                double lambda1 = lon1 * Math.PI / 180;
                double lambda2 = lon2 * Math.PI / 180;

                area += (lambda2 - lambda1) * (2 + Math.Sin(phi1) + Math.Sin(phi2));
            }

            area = area * R * R / 2.0;
            return Math.Abs(area); // in square meters
        }

        public static double DegreesToRadians(double deg) => deg * (Math.PI / 180.0);

    }
}
