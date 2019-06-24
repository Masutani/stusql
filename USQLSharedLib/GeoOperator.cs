using Microsoft.Analytics.Interfaces;
using System;
using Wibci.CountryReverseGeocode;
using Wibci.CountryReverseGeocode.Models;

namespace USQLSharedLib
{
    [SqlUserDefinedProcessor]

    // reverse geocder 
    // using geocoding library : https://github.com/InquisitorJax/Wibci.CountryReverseGeocode
    // only country and USStates are supported
    //
    public class ReverseGeocoder : IProcessor
    {
        #region member variables
        private string latColumn;
        private string lonColumn;
        private CountryReverseGeocodeService _service = new CountryReverseGeocodeService();
        #endregion

        public ReverseGeocoder(string latColumn, string lonColumn)
        {
            this.latColumn = latColumn;
            this.lonColumn = lonColumn;
        }

        public override IRow Process(IRow input, IUpdatableRow output)
        {
            double lat = input.Get<double>(latColumn);
            double lon = input.Get<double>(lonColumn);
            GeoLocation loc = new GeoLocation { Longitude = lon, Latitude = lat };
            var country = _service.FindCountry(loc);
            var USstates = _service.FindUsaState(loc);
            if (country != null && country.Name != null)
            {
                output.Set<string>("country", country.Name);
            }
            else
            {
                output.Set<string>("country", "");
            }
            if (USstates != null && USstates.Name != null)
            {
                output.Set<string>("USstates", USstates.Name);
            }
            else
            {
                output.Set<string>("USstates", "");
            }
            return output.AsReadOnly();
        }
    }
    
    [SqlUserDefinedProcessor]
    public class GeoHashDecoder : IProcessor
    {
        public override IRow Process(IRow input, IUpdatableRow output)
        {
            string geohash7 = input.Get<string>("geohash7");
            var coord = Geohash.Decode(geohash7);
            output.Set<string>("geohash7", geohash7);
            output.Set<double>("lat", coord[0]);
            output.Set<double>("lon", coord[1]);
            return output.AsReadOnly();
        }
    }

    // inline geo hash decoder helper class
    // using geohash library : https://github.com/sharonjl/geohash-net
    //
    public static class Geohash
    {
        #region Direction enum
        public enum Direction
        {
            Top = 0,
            Right = 1,
            Bottom = 2,
            Left = 3
        }
        #endregion

        #region consts list
        private const string Base32 = "0123456789bcdefghjkmnpqrstuvwxyz";
        private static readonly int[] Bits = new[] { 16, 8, 4, 2, 1 };
        private static readonly string[][] Neighbors = {
                                                           new[]
                                                               {
                                                                   "p0r21436x8zb9dcf5h7kjnmqesgutwvy", // Top
                                                                   "bc01fg45238967deuvhjyznpkmstqrwx", // Right
                                                                   "14365h7k9dcfesgujnmqp0r2twvyx8zb", // Bottom
                                                                   "238967debc01fg45kmstqrwxuvhjyznp", // Left
                                                               }, new[]
                                                                      {
                                                                          "bc01fg45238967deuvhjyznpkmstqrwx", // Top
                                                                          "p0r21436x8zb9dcf5h7kjnmqesgutwvy", // Right
                                                                          "238967debc01fg45kmstqrwxuvhjyznp", // Bottom
                                                                          "14365h7k9dcfesgujnmqp0r2twvyx8zb", // Left
                                                                      }
                                                       };
        private static readonly string[][] Borders = {
                                                         new[] {"prxz", "bcfguvyz", "028b", "0145hjnp"},
                                                         new[] {"bcfguvyz", "prxz", "0145hjnp", "028b"}
                                                     };
        #endregion

        public static String CalculateAdjacent(String hash, Direction direction)
        {
            hash = hash.ToLower();

            char lastChr = hash[hash.Length - 1];
            int type = hash.Length % 2;
            var dir = (int)direction;
            string nHash = hash.Substring(0, hash.Length - 1);

            if (Borders[type][dir].IndexOf(lastChr) != -1)
            {
                nHash = CalculateAdjacent(nHash, (Direction)dir);
            }
            return nHash + Base32[Neighbors[type][dir].IndexOf(lastChr)];
        }

        public static void RefineInterval(ref double[] interval, int cd, int mask)
        {
            if ((cd & mask) != 0)
            {
                interval[0] = (interval[0] + interval[1]) / 2;
            }
            else
            {
                interval[1] = (interval[0] + interval[1]) / 2;
            }
        }

        public static double[] Decode(String geohash)
        {
            bool even = true;
            double[] lat = { -90.0, 90.0 };
            double[] lon = { -180.0, 180.0 };

            foreach (char c in geohash)
            {
                int cd = Base32.IndexOf(c);
                for (int j = 0; j < 5; j++)
                {
                    int mask = Bits[j];
                    if (even)
                    {
                        RefineInterval(ref lon, cd, mask);
                    }
                    else
                    {
                        RefineInterval(ref lat, cd, mask);
                    }
                    even = !even;
                }
            }

            return new[] { (lat[0] + lat[1]) / 2, (lon[0] + lon[1]) / 2 };
        }

        public static String Encode(double latitude, double longitude, int precision = 12)
        {
            bool even = true;
            int bit = 0;
            int ch = 0;
            string geohash = "";

            double[] lat = { -90.0, 90.0 };
            double[] lon = { -180.0, 180.0 };

            if (precision < 1 || precision > 20) precision = 12;

            while (geohash.Length < precision)
            {
                double mid;

                if (even)
                {
                    mid = (lon[0] + lon[1]) / 2;
                    if (longitude > mid)
                    {
                        ch |= Bits[bit];
                        lon[0] = mid;
                    }
                    else
                        lon[1] = mid;
                }
                else
                {
                    mid = (lat[0] + lat[1]) / 2;
                    if (latitude > mid)
                    {
                        ch |= Bits[bit];
                        lat[0] = mid;
                    }
                    else
                        lat[1] = mid;
                }

                even = !even;
                if (bit < 4)
                    bit++;
                else
                {
                    geohash += Base32[ch];
                    bit = 0;
                    ch = 0;
                }
            }
            return geohash;
        }
    }


}