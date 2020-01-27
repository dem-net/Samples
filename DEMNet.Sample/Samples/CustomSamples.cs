//
// CustomSamples.cs
//
// Author:
//       Xavier Fischer
//
// Copyright (c) 2019 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using DEM.Net.Core;
using DEM.Net.Core.Interpolation;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SampleApp
{
    public class CustomSamples
    {
        private readonly ILogger<ElevationSamples> _logger;
        private readonly IElevationService _elevationService;
        private readonly IRasterService _rasterService;

        public CustomSamples(ILogger<ElevationSamples> logger
                , IElevationService elevationService
                , IRasterService rasterService)
        {
            _logger = logger;
            _elevationService = elevationService;
            _rasterService = rasterService;
        }
        public void Run(CancellationToken cancellationToken)
        {
            try
            {
                // Sample testing bad calculation on tile edges with tile registration modes (cell/grid)

                double amountx = .00000000000004;
                double amounty = .000000000000007;

                double lat = 46;
                double lon = 10;

                LineSample(DEMDataSet.ASTER_GDEMV3, latStart: 45.9993826389, lonStart: 9.9997211693, latEnd: 46.00002905, lonEnd: 10.00063093);

                TestEdges(DEMDataSet.ASTER_GDEMV3, lat, lon, "ASTGTMV003_N45E009_dem.tif", "ASTGTMV003_N45E010_dem.tif", "ASTGTMV003_N46E009_dem.tif", "ASTGTMV003_N46E010_dem.tif");
                TestEdges(DEMDataSet.SRTM_GL3, lat, lon, "N45E009.hgt", "N45E010.hgt", "N46E009.hgt", "N46E010.hgt");
                TestEdges(DEMDataSet.SRTM_GL1, lat, lon, "N45E009.hgt", "N45E010.hgt", "N46E009.hgt", "N46E010.hgt");
                TestEdges(DEMDataSet.AW3D30, lat, lon, "N045E009_AVE_DSM.tif", "N045E010_AVE_DSM.tif", "N046E009_AVE_DSM.tif", "N046E010_AVE_DSM.tif");


                DEMDataSet dataSet = DEMDataSet.SRTM_GL1;
                //_rasterService.GenerateDirectoryMetadata(dataSet, true, false, 1);
                _elevationService.DownloadMissingFiles(dataSet, lat, lon);
                var tiles = _rasterService.GenerateReportForLocation(dataSet, lat, lon);
                Debug.Assert(tiles.Count == 4);
                //_rasterService.GenerateFileMetadata(tile.LocalName, dataSet.FileFormat, true);
                GeoPoint pt = null;


                pt = _elevationService.GetPointElevation(lat + amounty, lon + amountx, dataSet);
                pt = _elevationService.GetPointElevation(lat - amounty, lon + amountx, dataSet);
                pt = _elevationService.GetPointElevation(lat + amounty, lon - amountx, dataSet);
                pt = _elevationService.GetPointElevation(lat - amounty, lon - amountx, dataSet);
                pt = _elevationService.GetPointElevation(lat + (1 / 3600d) / 2d, lon + (1 / 3600d) / 2, dataSet);

                pt = _elevationService.GetPointElevation(lat + 0.5, lon + 0.5, dataSet);
                pt = _elevationService.GetPointElevation(lat, lon, dataSet);


                foreach (var dataset in DEMDataSet.RegisteredNonLocalDatasets)
                {

                    _elevationService.DownloadMissingFiles(dataset, lat, lon);
                    //foreach (var file in _rasterService.GenerateReportForLocation(dataset, lat, lon))
                    //{
                    //    _rasterService.GenerateFileMetadata(file.LocalName, dataset.FileFormat, true);
                    //}
                    GeoPoint geoPoint = _elevationService.GetPointElevation(lat, lon, dataset);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        private void LineSample(DEMDataSet dataSet, double latStart, double lonStart, double latEnd, double lonEnd)
        {
            var elevationLine = GeometryService.ParseGeoPointAsGeometryLine(new GeoPoint(latStart, lonStart), new GeoPoint(latEnd, lonEnd));
            
            _elevationService.DownloadMissingFiles(dataSet, elevationLine.GetBoundingBox());

            var geoPoints = _elevationService.GetLineGeometryElevation(elevationLine, dataSet);
           
        }

        void TestEdges(DEMDataSet dataSet, double lat, double lon
            , string rasterSouthWestName, string rasterSouthEastName
            , string rasterNorthWestName, string rasterNorthEastName)
        {
            DEMFileType fileType = dataSet.FileFormat.Type;
            int rasterSize = dataSet.PointsPerDegree;
            double amountx = (1d / rasterSize) / 4d;
            double amounty = (1d / rasterSize) / 4d;

            // Regenerates all metadata            
            //_rasterService.GenerateDirectoryMetadata(dataSet
            //                                        , force: true
            //                                        , deleteOnError: false
            //                                        , maxDegreeOfParallelism: 1);
            _elevationService.DownloadMissingFiles(dataSet, lat, lon);
            var tiles = _rasterService.GenerateReportForLocation(dataSet, lat, lon);
            Debug.Assert(tiles.Count == 4);
            Debug.Assert(tiles.Any(t => string.Equals(Path.GetFileName(t.LocalName), rasterSouthWestName, StringComparison.OrdinalIgnoreCase)));
            Debug.Assert(tiles.Any(t => string.Equals(Path.GetFileName(t.LocalName), rasterSouthEastName, StringComparison.OrdinalIgnoreCase)));
            Debug.Assert(tiles.Any(t => string.Equals(Path.GetFileName(t.LocalName), rasterNorthWestName, StringComparison.OrdinalIgnoreCase)));
            Debug.Assert(tiles.Any(t => string.Equals(Path.GetFileName(t.LocalName), rasterNorthEastName, StringComparison.OrdinalIgnoreCase)));

            if (dataSet.FileFormat.Registration == DEMFileRegistrationMode.Cell)
            {
                using (var rasterNW = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterNorthWestName, Path.GetFileName(t.LocalName))).LocalName, fileType))
                using (var rasterNE = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterNorthEastName, Path.GetFileName(t.LocalName))).LocalName, fileType))
                using (var rasterSW = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterSouthWestName, Path.GetFileName(t.LocalName))).LocalName, fileType))
                using (var rasterSE = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterSouthEastName, Path.GetFileName(t.LocalName))).LocalName, fileType))
                {
                    var elevNW = rasterNW.GetElevationAtPoint(rasterNW.ParseMetaData(dataSet.FileFormat), rasterSize - 1, rasterSize - 1);
                    var elevNE = rasterNE.GetElevationAtPoint(rasterNE.ParseMetaData(dataSet.FileFormat), 0, rasterSize - 1);
                    var elevSW = rasterSW.GetElevationAtPoint(rasterSW.ParseMetaData(dataSet.FileFormat), rasterSize - 1, 0);
                    var elevSE = rasterSE.GetElevationAtPoint(rasterSE.ParseMetaData(dataSet.FileFormat), 0, 0);

                    BilinearInterpolator interpolator = new BilinearInterpolator();
                    var elev0 = interpolator.Interpolate(elevSW, elevSE, elevNW, elevNE, 0.25, 0.25);
                    var apiElev0 = _elevationService.GetPointElevation(lat + amounty, lon - amountx, dataSet);
                    Debug.Assert((elev0 - apiElev0.Elevation.Value) < double.Epsilon);

                    var elev1 = interpolator.Interpolate(elevSW, elevSE, elevNW, elevNE, 0.75, 0.25);
                    var apiElev1 = _elevationService.GetPointElevation(lat + amounty, lon + amountx, dataSet);
                    Debug.Assert((elev1 - apiElev1.Elevation.Value) < double.Epsilon);

                    var elev2 = interpolator.Interpolate(elevSW, elevSE, elevNW, elevNE, 0.25, 0.75);
                    var apiElev2 = _elevationService.GetPointElevation(lat - amounty, lon - amountx, dataSet);
                    Debug.Assert((elev2 - apiElev2.Elevation.Value) < double.Epsilon);

                    var elev3 = interpolator.Interpolate(elevSW, elevSE, elevNW, elevNE, 0.75, 0.75);
                    var apiElev3 = _elevationService.GetPointElevation(lat - amounty, lon + amountx, dataSet);
                    Debug.Assert((elev3 - apiElev3.Elevation.Value) < double.Epsilon);
                }
            }
            else
            {
                using (var rasterNW = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterNorthWestName, Path.GetFileName(t.LocalName))).LocalName, fileType))
                using (var rasterNE = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterNorthEastName, Path.GetFileName(t.LocalName))).LocalName, fileType))
                using (var rasterSW = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterSouthWestName, Path.GetFileName(t.LocalName))).LocalName, fileType))
                using (var rasterSE = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterSouthEastName, Path.GetFileName(t.LocalName))).LocalName, fileType))
                {
                    // Northen row, west to east
                    var elevN0 = rasterNW.GetElevationAtPoint(rasterNW.ParseMetaData(dataSet.FileFormat), rasterSize - 1, rasterSize - 1);
                    var elevN1 = rasterNW.GetElevationAtPoint(rasterNW.ParseMetaData(dataSet.FileFormat), rasterSize, rasterSize - 1);
                    var elevN2 = rasterNE.GetElevationAtPoint(rasterNE.ParseMetaData(dataSet.FileFormat), 1, rasterSize - 1);

                    // middle row, west to east
                    var elevM0 = rasterNW.GetElevationAtPoint(rasterNW.ParseMetaData(dataSet.FileFormat), rasterSize - 1, rasterSize);
                    var elevM1 = rasterNW.GetElevationAtPoint(rasterNW.ParseMetaData(dataSet.FileFormat), rasterSize, rasterSize);
                    var elevM2 = rasterNE.GetElevationAtPoint(rasterNE.ParseMetaData(dataSet.FileFormat), 1, rasterSize);

                    // Sourthen row, west to east
                    var elevS0 = rasterSW.GetElevationAtPoint(rasterSW.ParseMetaData(dataSet.FileFormat), rasterSize - 1, 1);
                    var elevS1 = rasterSW.GetElevationAtPoint(rasterSW.ParseMetaData(dataSet.FileFormat), rasterSize, 1);
                    var elevS2 = rasterSE.GetElevationAtPoint(rasterSE.ParseMetaData(dataSet.FileFormat), 1, 1);

                    BilinearInterpolator interpolator = new BilinearInterpolator();
                    var elev0 = interpolator.Interpolate(elevM0, elevM1, elevN0, elevN1, 0.75, 0.75);
                    var apiElev0 = _elevationService.GetPointElevation(lat + amounty, lon - amountx, dataSet);
                    Debug.Assert((elev0 - apiElev0.Elevation.Value) < double.Epsilon);

                    var elev1 = interpolator.Interpolate(elevM1, elevM2, elevN1, elevN2, 0.25, 0.75);
                    var apiElev1 = _elevationService.GetPointElevation(lat + amounty, lon + amountx, dataSet);
                    Debug.Assert((elev1 - apiElev1.Elevation.Value) < double.Epsilon);

                    var elev2 = interpolator.Interpolate(elevS0, elevS1, elevM0, elevM1, 0.75, 0.25);
                    var apiElev2 = _elevationService.GetPointElevation(lat - amounty, lon - amountx, dataSet);
                    Debug.Assert((elev2 - apiElev2.Elevation.Value) < double.Epsilon);

                    var elev3 = interpolator.Interpolate(elevS1, elevS2, elevM1, elevM2, 0.25, 0.25);
                    var apiElev3 = _elevationService.GetPointElevation(lat - amounty, lon + amountx, dataSet);
                    Debug.Assert((elev3 - apiElev3.Elevation.Value) < double.Epsilon);
                }

            }


        }
        //void TestPointElevation(DEMDataSet dataSet, double lat, double lon
        //    , string rasterName, DEMFileType fileType, int rasterSize)
        //{
        //    // Regenerates all metadata            
        //    _rasterService.GenerateDirectoryMetadata(dataSet
        //                                            , force: true
        //                                            , deleteOnError: false
        //                                            , maxDegreeOfParallelism: 1);
        //    _elevationService.DownloadMissingFiles(dataSet, lat, lon);
        //    var tiles = _rasterService.GenerateReportForLocation(dataSet, lat, lon);
        //    Debug.Assert(tiles.Any(t => string.Equals(Path.GetFileName(t.LocalName), rasterName, StringComparison.OrdinalIgnoreCase)));

        //    if (dataSet.FileFormat.Registration == DEMFileRegistrationMode.Cell)
        //    {
        //        using (var raster = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterName, Path.GetFileName(t.LocalName))).LocalName, fileType))
        //        {
        //            var elevNW = rasterNW.GetElevationAtPoint(rasterNW.ParseMetaData(dataSet.FileFormat), rasterSize - 1, rasterSize - 1);
        //            var elevNE = rasterNE.GetElevationAtPoint(rasterNE.ParseMetaData(dataSet.FileFormat), 0, rasterSize - 1);
        //            var elevSW = rasterSW.GetElevationAtPoint(rasterSW.ParseMetaData(dataSet.FileFormat), rasterSize - 1, 0);
        //            var elevSE = rasterSE.GetElevationAtPoint(rasterSE.ParseMetaData(dataSet.FileFormat), 0, 0);

        //            BilinearInterpolator interpolator = new BilinearInterpolator();
        //            var elev0 = interpolator.Interpolate(elevSW, elevSE, elevNW, elevNE, 0.25, 0.25);
        //            var apiElev0 = _elevationService.GetPointElevation(lat + amounty, lon - amountx, dataSet);
        //            Debug.Assert((elev0 - apiElev0.Elevation.Value) < double.Epsilon);

        //            var elev1 = interpolator.Interpolate(elevSW, elevSE, elevNW, elevNE, 0.75, 0.25);
        //            var apiElev1 = _elevationService.GetPointElevation(lat + amounty, lon + amountx, dataSet);
        //            Debug.Assert((elev1 - apiElev1.Elevation.Value) < double.Epsilon);

        //            var elev2 = interpolator.Interpolate(elevSW, elevSE, elevNW, elevNE, 0.25, 0.75);
        //            var apiElev2 = _elevationService.GetPointElevation(lat - amounty, lon - amountx, dataSet);
        //            Debug.Assert((elev2 - apiElev2.Elevation.Value) < double.Epsilon);

        //            var elev3 = interpolator.Interpolate(elevSW, elevSE, elevNW, elevNE, 0.75, 0.75);
        //            var apiElev3 = _elevationService.GetPointElevation(lat - amounty, lon + amountx, dataSet);
        //            Debug.Assert((elev3 - apiElev3.Elevation.Value) < double.Epsilon);
        //        }
        //    }
        //    else
        //    {
        //        using (var rasterNW = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterNorthWestName, Path.GetFileName(t.LocalName))).LocalName, fileType))
        //        using (var rasterNE = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterNorthEastName, Path.GetFileName(t.LocalName))).LocalName, fileType))
        //        using (var rasterSW = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterSouthWestName, Path.GetFileName(t.LocalName))).LocalName, fileType))
        //        using (var rasterSE = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterSouthEastName, Path.GetFileName(t.LocalName))).LocalName, fileType))
        //        {
        //            // Northen row, west to east
        //            var elevN0 = rasterNW.GetElevationAtPoint(rasterNW.ParseMetaData(dataSet.FileFormat), rasterSize - 1, rasterSize - 1);
        //            var elevN1 = rasterNW.GetElevationAtPoint(rasterNW.ParseMetaData(dataSet.FileFormat), rasterSize, rasterSize - 1);
        //            var elevN2 = rasterNE.GetElevationAtPoint(rasterNE.ParseMetaData(dataSet.FileFormat), 1, rasterSize - 1);

        //            // middle row, west to east
        //            var elevM0 = rasterNW.GetElevationAtPoint(rasterNW.ParseMetaData(dataSet.FileFormat), rasterSize - 1, rasterSize);
        //            var elevM1 = rasterNW.GetElevationAtPoint(rasterNW.ParseMetaData(dataSet.FileFormat), rasterSize, rasterSize);
        //            var elevM2 = rasterNE.GetElevationAtPoint(rasterNE.ParseMetaData(dataSet.FileFormat), 1, rasterSize);

        //            // Sourthen row, west to east
        //            var elevS0 = rasterSW.GetElevationAtPoint(rasterSW.ParseMetaData(dataSet.FileFormat), rasterSize - 1, 1);
        //            var elevS1 = rasterSW.GetElevationAtPoint(rasterSW.ParseMetaData(dataSet.FileFormat), rasterSize, 1);
        //            var elevS2 = rasterSE.GetElevationAtPoint(rasterSE.ParseMetaData(dataSet.FileFormat), 1, 1);

        //            BilinearInterpolator interpolator = new BilinearInterpolator();
        //            var elev0 = interpolator.Interpolate(elevM0, elevM1, elevN0, elevN1, 0.75, 0.75);
        //            var apiElev0 = _elevationService.GetPointElevation(lat + amounty, lon - amountx, dataSet);
        //            Debug.Assert((elev0 - apiElev0.Elevation.Value) < double.Epsilon);

        //            var elev1 = interpolator.Interpolate(elevM1, elevM2, elevN1, elevN2, 0.25, 0.75);
        //            var apiElev1 = _elevationService.GetPointElevation(lat + amounty, lon + amountx, dataSet);
        //            Debug.Assert((elev1 - apiElev1.Elevation.Value) < double.Epsilon);

        //            var elev2 = interpolator.Interpolate(elevS0, elevS1, elevM0, elevM1, 0.75, 0.25);
        //            var apiElev2 = _elevationService.GetPointElevation(lat - amounty, lon - amountx, dataSet);
        //            Debug.Assert((elev2 - apiElev2.Elevation.Value) < double.Epsilon);

        //            var elev3 = interpolator.Interpolate(elevS1, elevS2, elevM1, elevM2, 0.25, 0.25);
        //            var apiElev3 = _elevationService.GetPointElevation(lat - amounty, lon + amountx, dataSet);
        //            Debug.Assert((elev3 - apiElev3.Elevation.Value) < double.Epsilon);
        //        }

        //    }


        //}

    }
}
