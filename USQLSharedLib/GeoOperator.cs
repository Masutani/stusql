using Microsoft.Analytics.Interfaces;
using Microsoft.Analytics.Types.Sql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Spatial;
using Wibci.CountryReverseGeocode;
using Wibci.CountryReverseGeocode.Models;

namespace USQLSharedLib
{
    [SqlUserDefinedProcessor]
    public class ReverseGeocoder : IProcessor
    {
        private string latColumn;
        private string lonColumn;
        private CountryReverseGeocodeService _service = new CountryReverseGeocodeService();
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

}