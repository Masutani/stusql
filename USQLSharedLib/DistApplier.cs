using Microsoft.Analytics.Interfaces;
using System;
using System.Collections.Generic;
using System.Device.Location;

namespace USQLSharedLib
{
    [SqlUserDefinedApplier]
    public class DistApplier : IApplier
    {
        private string lat1Column;
        private string lon1Column;
        private string lat2Column;
        private string lon2Column;
        private int type;

        public DistApplier(string lat1Column, string lon1Column, string lat2Column, string lon2Column, int type)
        {
            this.lat1Column = lat1Column;
            this.lon1Column = lon1Column;
            this.lat2Column = lat2Column;
            this.lon2Column = lon2Column;
            this.type = type;
        }
        public override IEnumerable<IRow> Apply(IRow input, IUpdatableRow output)
        {
            Nullable<double> lat1 = input.Get<Nullable<double>>(lat1Column);
            Nullable<double> lon1 = input.Get<Nullable<double>>(lon1Column);
            Nullable<double> lat2 = input.Get<Nullable<double>>(lat2Column);
            Nullable<double> lon2 = input.Get<Nullable<double>>(lon2Column);

            var lat1n = lat1 ?? 0;
            var lon1n = lon1 ?? 0;
            var lat2n = lat2 ?? 0;
            var lon2n = lon2 ?? 0;
            if( (lat1n == 0 & lon1n == 0) | (lat2n == 0 & lon2n == 0))
            {
                output.Set<double>("distance", 0);
                yield return output.AsReadOnly();
            }

            double distance = 0;
            if (type == 1)
            {
                distance = harversineDist1(lat1n, lon1n, lat2n, lon2n);
            }
            else if (type == 2)
            {
                distance = harversineDist2(lat1n, lon1n, lat2n, lon2n);
            }
            else if (type == 3)
            {
                distance = harversineDist3(lat1n, lon1n, lat2n, lon2n);
            }
            output.Set<double>("distance", distance);

            yield return output.AsReadOnly();
        }
        
        public static double harversineDist1(double lat1, double lon1, double lat2, double lon2)
        {
            double R = 6371; // km
            double sLat1 = Math.Sin(Radians(lat1));
            double sLat2 = Math.Sin(Radians(lat2));
            double cLat1 = Math.Cos(Radians(lat1));
            double cLat2 = Math.Cos(Radians(lat2));
            double cLon = Math.Cos(Radians(lon1) - Radians(lon2));
            double cosD = sLat1 * sLat2 + cLat1 * cLat2 * cLon;
            double d = Math.Acos(cosD);
            double dist = R * d;
            return dist;
        }
        public static double Radians(double degrees)
        {
            double radians = (Math.PI / 180) * degrees;
            return (radians);
        }

        public static double harversineDist2(double lon1, double lat1, double lon2, double lat2)
        {
            const double r = 6371; // meters
            var sdlat = Math.Sin((lat2 - lat1) / 2);
            var sdlon = Math.Sin((lon2 - lon1) / 2);
            var q = sdlat * sdlat + Math.Cos(lat1) * Math.Cos(lat2) * sdlon * sdlon;
            double d = 2 * r * Math.Asin(Math.Sqrt(q));
            return d;
        }
        public static double harversineDist3(double lon1, double lat1, double lon2, double lat2)
        {
            GeoCoordinate c1 = new GeoCoordinate(lat1, lon1);
            GeoCoordinate c2 = new GeoCoordinate(lat2, lon2);

            double distanceInKm = c1.GetDistanceTo(c2) / 1000;
            return distanceInKm;
        }
    }
}