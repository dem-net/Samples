﻿//
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
using ScottPlot;
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
                double lat1 = 43.532456;
                double lon1 = 5.612444;

                // bottom ste victoire (fuveau)
                //double lat1 = 43.49029208393125;
                //double lon1 = 5.587234497070313;
                //double lat2 = 43.53013134607789;
                //double lon2 = 5.581398010253906;

                // geneva
                //double lat1 = 46.08129825372404;
                //double lon1 = 3.382026672363281;
                // mont blanc
                double lat2 = 45.833;
                double lon2 = 6.864;

                // bob
                //double lat1 = 37.212627;
                //double lon1 = 22.321612;

                //double lat2 = 37.208179;
                //double lon2 = 22.324373;

                Stopwatch sw = new Stopwatch();

                if (cancellationToken.IsCancellationRequested) return;
                _logger.LogInformation("Line elevation");

                sw.Restart();
                // Line starting at mont ventoux peak to Mont Blanc
                DEMDataSet dataSet = DEMDataSet.ASTER_GDEMV3;
                var metrics = _elevationService.GetIntervisibilityReport(new GeoPoint(lat1, lon1), new GeoPoint(lat2, lon2), dataSet);

                PlotVisibilityReport(metrics);

                _logger.LogInformation($"{dataSet.Name} metrics: {metrics.ToString()}");

                //var geoJson = ConvertLineElevationResultToGeoJson(simplified);

                _logger.LogInformation($"Done in {sw.Elapsed.TotalMilliseconds:N1}ms");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        private void PlotVisibilityReport(IntervisibilityReport metrics)
        {
            try
            {
                double[] distancesX = metrics.GeoPoints.Select(p => p.DistanceFromOriginMeters ?? 0).ToArray();
                double[] elevationsY = metrics.GeoPoints.Select(p => p.Elevation ?? 0).ToArray();

                

                var plt = new Plot(800, 600);
                plt.PlotScatter(distancesX, elevationsY, lineWidth:2, markerSize:0, label: "profile");
                plt.PlotLine(0, elevationsY[0], distancesX.Last(), elevationsY.Last(), color: System.Drawing.Color.Red, 1, "ray");
                plt.Title("Visiblity report");
                plt.XLabel("Distance (meters)");
                plt.YLabel("Elevation (meters)");

                if (metrics.HasObstacles)
                {
                    var obstacles = metrics.Metrics.Obstacles;
                    double[] obstacleEntriesX = obstacles.Select(o => o.EntryPoint.DistanceFromOriginMeters ?? 0).ToArray();
                    double[] obstacleEntriesY = obstacles.Select(o => o.EntryPoint.Elevation ?? 0).ToArray();
                    double[] obstaclePeaksX = obstacles.Select(o => o.PeakPoint.DistanceFromOriginMeters ?? 0).ToArray();
                    double[] obstaclePeaksY = obstacles.Select(o => o.PeakPoint.Elevation ?? 0).ToArray();
                    double[] obstacleExitsX = obstacles.Select(o => o.ExitPoint.DistanceFromOriginMeters ?? 0).ToArray();
                    double[] obstacleExitsY = obstacles.Select(o => o.ExitPoint.Elevation ?? 0).ToArray();

                    plt.PlotScatter(obstacleEntriesX, obstacleEntriesY, lineWidth: 0, markerSize: 5, color: System.Drawing.Color.Green, markerShape: MarkerShape.cross, label: "entry");
                    plt.PlotScatter(obstaclePeaksX, obstaclePeaksY, lineWidth: 0, markerSize: 5, color: System.Drawing.Color.Black, markerShape: MarkerShape.cross, label: "peak");
                    plt.PlotScatter(obstacleExitsX, obstacleExitsY, lineWidth: 0, markerSize: 5, color: System.Drawing.Color.Violet, markerShape: MarkerShape.cross, label: "exit");
                }

                plt.Legend(enableLegend: true, fixedLineWidth: false);

                plt.SaveFig("VisibilityReport.png");
            }
            catch (Exception)
            {

                throw;
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
