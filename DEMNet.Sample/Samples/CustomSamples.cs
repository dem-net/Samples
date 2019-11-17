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
using DEM.Net.glTF;
using DEM.Net.glTF.Export;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
        private readonly IglTFService _glTFService;
        private readonly ISTLExportService _stlService;
        private readonly IRasterService _rasterService;

        public CustomSamples(ILogger<ElevationSamples> logger
                , IElevationService elevationService
                , IglTFService glTFService
                , ISTLExportService stlService
                , IRasterService rasterService)
        {
            _logger = logger;
            _elevationService = elevationService;
            _glTFService = glTFService;
            _stlService = stlService;
            _rasterService = rasterService;
        }
        public void Run(CancellationToken cancellationToken)
        {
            try
            {

                double amountx = .00000000000004;
                double amounty = .000000000000007;

                double lat = 46;
                double lon = 10;

                
                TestEdges(DEMDataSet.AW3D30, lat, lon, "N045E009_AVE_DSM.tif", "N045E010_AVE_DSM.tif", "N046E009_AVE_DSM.tif", "N046E010_AVE_DSM.tif"
                    , fileType: DEMFileType.GEOTIFF, 3600);

                DEMDataSet dataSet = DEMDataSet.SRTM_GL1;
                _rasterService.GenerateDirectoryMetadata(dataSet, true, false, 1);
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


                foreach (var dataset in DEMDataSet.RegisteredNonSingleFileDatasets)
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

        void TestEdges(DEMDataSet dataSet, double lat, double lon
            , string rasterSouthWestName, string rasterSouthEastName
            , string rasterNorthWestName, string rasterNorthEastName
            , DEMFileType fileType, int rasterSize)
        {
            double amountx = (1d / dataSet.PointsPerDegree) / 4d;
            double amounty = (1d / dataSet.PointsPerDegree) / 4d;

            // Regenerates all metadata            
            _rasterService.GenerateDirectoryMetadata(dataSet
                                                    , force: true
                                                    , deleteOnError: false
                                                    , maxDegreeOfParallelism: 1);
            _elevationService.DownloadMissingFiles(dataSet, lat, lon);
            var tiles = _rasterService.GenerateReportForLocation(dataSet, lat, lon);
            Debug.Assert(tiles.Count == 4);
            Debug.Assert(tiles.Any(t => string.Equals(Path.GetFileName(t.LocalName), rasterSouthWestName, StringComparison.OrdinalIgnoreCase)));
            Debug.Assert(tiles.Any(t => string.Equals(Path.GetFileName(t.LocalName), rasterSouthEastName, StringComparison.OrdinalIgnoreCase)));
            Debug.Assert(tiles.Any(t => string.Equals(Path.GetFileName(t.LocalName), rasterNorthWestName, StringComparison.OrdinalIgnoreCase)));
            Debug.Assert(tiles.Any(t => string.Equals(Path.GetFileName(t.LocalName), rasterNorthEastName, StringComparison.OrdinalIgnoreCase)));


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

    }
}
