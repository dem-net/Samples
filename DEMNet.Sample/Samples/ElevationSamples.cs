//
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
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SampleApp
{
    public class ElevationSamples
    {
        private readonly ILogger<ElevationSamples> _logger;
        private readonly IElevationService _elevationService;

        public ElevationSamples(ILogger<ElevationSamples> logger
                , IElevationService elevationService)
        {
            _logger = logger;
            _elevationService = elevationService;
        }
        public void Run(CancellationToken cancellationToken)
        {
            try
            {


                double lat1 = 46.00000000000004;
                double lon1 = 10.000000000000007;
                double lat2 = 45.212278;
                double lont2 = 5.468857;

                _logger.LogInformation($"Getting location elevation for each dataset (location lat: {lat1:N2}, lon: {lon1:N2})");
                Stopwatch sw = new Stopwatch();
                Parallel.ForEach(DEMDataSet.RegisteredNonLocalDatasets, (dataSet, loopState) =>
                //foreach (var dataSet in DEMDataSet.RegisteredNonSingleFileDatasets)
                {
                    if (cancellationToken.IsCancellationRequested) loopState.Stop();
                    sw.Restart();

                    _elevationService.DownloadMissingFiles(dataSet, lat1, lon1);
                    GeoPoint geoPoint = _elevationService.GetPointElevation(lat1, lon1, dataSet);

                    _logger.LogInformation($"{dataSet.Name} elevation: {geoPoint.Elevation:N2} (time taken: {sw.Elapsed.TotalMilliseconds:N1}ms)");
                }
                );

                if (cancellationToken.IsCancellationRequested) return;
                _logger.LogInformation("Multiple point elevation");

                sw.Restart();

                GeoPoint pt1 = new GeoPoint(lat1, lon1);
                GeoPoint pt2 = new GeoPoint(lat2, lont2);
                GeoPoint[] points = { pt1, pt2 };
                Parallel.ForEach(DEMDataSet.RegisteredNonLocalDatasets, (dataSet, loopState) =>
                //foreach (var dataSet in DEMDataSet.RegisteredNonSingleFileDatasets)
                {
                    if (cancellationToken.IsCancellationRequested) loopState.Stop();
                    sw.Restart();
                    var geoPoints = _elevationService.GetPointsElevation(points, dataSet);
                    _logger.LogInformation($"{dataSet.Name} elevation: {string.Join(" / ", geoPoints.Select(e => e.Elevation.GetValueOrDefault().ToString("N2")))} (time taken: {sw.Elapsed.TotalMilliseconds:N1}ms)");
                }
                );

                if (cancellationToken.IsCancellationRequested) return;
                _logger.LogInformation("Line elevation");

                sw.Restart();
                // Line passing by mont ventoux peak [5.144899, 44.078873], [5.351516, 44.225876]
                var elevationLine = GeometryService.ParseGeoPointAsGeometryLine(new GeoPoint(44.078873, 5.144899), new GeoPoint(44.225876, 5.351516));
                Parallel.ForEach(DEMDataSet.RegisteredNonLocalDatasets, (dataSet, loopState) =>
                //foreach (var dataSet in DEMDataSet.RegisteredNonSingleFileDatasets)
                {
                    if (cancellationToken.IsCancellationRequested) loopState.Stop();
                    _elevationService.DownloadMissingFiles(dataSet, elevationLine.GetBoundingBox());
                    var geoPoints = _elevationService.GetLineGeometryElevation(elevationLine, dataSet);
                    var metrics = geoPoints.ComputeMetrics();
                    _logger.LogInformation($"{dataSet.Name} metrics: {metrics.ToString()}");


                    var simplified = geoPoints.Simplify(50 /* meters */);
                    _logger.LogInformation($"{dataSet.Name} after reduction : {simplified.Count} points");

                    var geoJson = ConvertLineElevationResultToGeoJson(simplified);
                }
                );
                _logger.LogInformation($"Done in {sw.Elapsed.TotalMilliseconds:N1}ms");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        private string ConvertLineElevationResultToGeoJson(List<GeoPoint> linePoints)
        {
            FeatureCollection fc = new FeatureCollection(linePoints.Select(ConvertGeoPointToFeature).ToList());

            return JsonConvert.SerializeObject(fc);
        }
        private Feature ConvertGeoPointToFeature(GeoPoint point)
        {
            return new Feature(
                    new Point(
                            new Position(point.Latitude, point.Longitude, point.Elevation)
                            )
                    , point
                    );
        }

    }
}
