//
// GpxSamples.cs
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using GeoJSON.Net.Feature;
using Newtonsoft.Json;
using GeoJSON.Net.Geometry;

namespace SampleApp
{
    /// <summary>
    /// GpxSamples : GPX draping onto a DEM (get elevation from raster files)
    /// </summary>
    /// <summary>
    /// GpxSamples : GPX draping onto a DEM (get elevation from raster files)
    /// </summary>
    public class GpxSamples
    {
        private readonly ILogger<GpxSamples> _logger;
        private readonly RasterService _rasterService;
        private readonly ElevationService _elevationService;

        public GpxSamples(ILogger<GpxSamples> logger
                , RasterService rasterService
                , ElevationService elevationService)
        {
            _logger = logger;
            _rasterService = rasterService;
            _elevationService = elevationService;
        }

        internal void Run()
        {
            try
            {


                string _gpxFile = Path.Combine("SampleData", "trail.gpx");
                if (!File.Exists(_gpxFile))
                {
                    _logger.LogError($"Cannot run sample: {_gpxFile} is missing !");
                }
                DEMDataSet _dataSet = DEMDataSet.SRTM_GL3;

                // Read GPX points
                var segments = GpxImport.ReadGPX_Segments(_gpxFile);
                var points = segments.SelectMany(seg => seg);

                // Retrieve elevation for each point on DEM
                var gpxPointsElevated = _elevationService.GetPointsElevation(points, _dataSet)
                                        .ToList();

                _logger.LogInformation($"{gpxPointsElevated.Count} GPX points elevation calculated");


                // Get metrics (stats)
                var metrics = gpxPointsElevated.ComputeMetrics();

                _logger.LogInformation($"GPX points stats: {metrics}");


                var gpxPointsSimplified = gpxPointsElevated.Simplify(50);
                _logger.LogInformation($"GPX track is reduced with 50m tolerance.");
                var metricsWithReducedPoints = gpxPointsSimplified.ComputeMetrics();

                _logger.LogInformation($"GPX points stats after reduction: {metricsWithReducedPoints}");

                var geoJson = ConvertLineElevationResultToGeoJson(gpxPointsSimplified);
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
