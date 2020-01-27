    //
    // IntervisibilitySample.cs
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
        public class IntervisibilitySample
        {
            private readonly ILogger<ElevationSamples> _logger;
            private readonly IElevationService _elevationService;

            public IntervisibilitySample(ILogger<ElevationSamples> logger
                    , IElevationService elevationService)
            {
                _logger = logger;
                _elevationService = elevationService;
            }
            public void Run(CancellationToken cancellationToken)
            {
                try
                {

                // ventoux
                //double lat1 = 44.17346;
                //double lon1 = 5.27829;

                // ste victoire
                //double lat1 = 43.532456;
                //double lon1 = 5.612444;

                // bottom ste victoire (fuveau)
                //double lat1 = 43.479541;
                //double lon1 = 5.552377;

                // mont blanc
                //double lat2 = 45.833;
                //double lon2 = 6.864;

                // bob
                double lat1 = 37.212627;
                double lon1 = 22.321612;

                double lat2 = 37.208179;
                double lon2 = 22.324373;

                    Stopwatch sw = new Stopwatch();
                
                    if (cancellationToken.IsCancellationRequested) return;
                    _logger.LogInformation("Line elevation");

                    sw.Restart();
                    // Line starting at mont ventoux peak to Mont Blanc
                    var elevationLine = GeometryService.ParseGeoPointAsGeometryLine(new GeoPoint(lat1,lon1), new GeoPoint(lat2,lon2));
                    DEMDataSet dataSet = DEMDataSet.ASTER_GDEMV3;

                    _elevationService.DownloadMissingFiles(dataSet, elevationLine.GetBoundingBox());
                    var geoPoints = _elevationService.GetLineGeometryElevation(elevationLine, dataSet);
                
                    var metrics = geoPoints.ComputeVisibilityMetrics();
                    _logger.LogInformation($"{dataSet.Name} metrics: {metrics.ToString()}");

                    //var geoJson = ConvertLineElevationResultToGeoJson(simplified);
                
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
