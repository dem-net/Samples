﻿//
// ElevationSamples.cs
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
using DEM.Net.glTF;
using DEM.Net.glTF.Export;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq;

namespace SampleApp
{
    public class ElevationSamples
    {
        private readonly IElevationService _elevationService;
        private readonly IglTFService _glTFService;
        private readonly ISTLExportService _stlService;
        private readonly ILogger<ElevationSamples> _logger;

        public ElevationSamples(ILogger<ElevationSamples> logger
                , IElevationService elevationService
                , IglTFService glTFService
                , ISTLExportService stlService)
        {
            _elevationService = elevationService;
            _glTFService = glTFService;
            _stlService = stlService;
            _logger = logger;
        }
        public void Run()
        {
            string sampleName = nameof(ElevationSamples);




            double lat1 = 45.179337;
            double lon1 = 5.721421;
            double lat2 = 45.212278;
            double lont2 = 5.468857;

            _logger.LogInformation($"Getting location elevation for each dataset (location lat: {lat1:N2}, lon: {lon1:N2})");
            Stopwatch sw = new Stopwatch();
            foreach (var dataSet in DEMDataSet.RegisteredDatasets)
            {
                sw.Restart();

                _elevationService.DownloadMissingFiles(dataSet, lat1, lon1);
                GeoPoint geoPoint = _elevationService.GetPointElevation(lat1, lon1, dataSet);

                _logger.LogInformation($"{dataSet.Name} elevation: {geoPoint.Elevation:N2} (time taken: {sw.Elapsed:g})");
            }


            _logger.LogInformation($"Multiple point elevation");

            sw.Restart();

            GeoPoint pt1 = new GeoPoint(lat1, lon1);
            GeoPoint pt2 = new GeoPoint(lat2, lont2);
            GeoPoint[] points = { pt1, pt2 };
            foreach (var dataSet in DEMDataSet.RegisteredDatasets)
            {
                sw.Restart();
                var geoPoints = _elevationService.GetPointsElevation(points, dataSet);
                _logger.LogInformation($"{dataSet.Name} elevation: {string.Join(" / ", geoPoints.Select(e => e.Elevation.GetValueOrDefault().ToString("N2")))} (time taken: {sw.Elapsed:g})");
            }


            _logger.LogInformation($"= {sampleName} : Line elevation");
            sw.Restart();
            var elevationLine = GeometryService.ParseGeoPointAsGeometryLine(new GeoPoint(lat1, lon1), new GeoPoint(lat2, lont2));
            foreach (var dataSet in DEMDataSet.RegisteredDatasets)
            {
                _elevationService.DownloadMissingFiles(dataSet, elevationLine.GetBoundingBox());
                var geoPoints = _elevationService.GetLineGeometryElevation(elevationLine, dataSet);
                var metrics = GeometryService.ComputeMetrics(geoPoints);
                //_logger.LogInformation($"{dataSet.Name} elevation: {string.Join(", ", elevations.Select(e => e.Elevation))}");
                _logger.LogInformation($"{dataSet.Name} metrics: {metrics.ToString()}");
            }
            _logger.LogInformation($"Done in {sw.Elapsed:g}");



        }

    }
}
