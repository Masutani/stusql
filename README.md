# stusql
Spatio temporal libirary for U-SQL.

## spatial operations summary

- reverse geocoding
- geohash decoding, encoding
- harversine distance calculation

## temporal operations summary

- interpolation and resample fixed tick time series from time span data for arbitral bin size
- interpolation with LOCF (Last Observation Carried Forward) fixed tick time series from time span data for arbitral bin size
- Split time duration rows to in-day durations.


# Usage

## spatial operations

### revoerse geocoding

Use ReverseGeocoder : IProcessor in USQLSharedLib

### geohash decoding, encoding

Use GeoHashDecoder : IProcessor in USQLSharedLib

### harversine distance calculation

Use DistApplier : IApplier in USQLSharedLib



# Release note

2019.06.24
  - geohash, distance applier added

2018.07.12
  - Duration splitter into in-day durations added

2018.07.03
  - LOCF is added

2018.05.09
  - first version
