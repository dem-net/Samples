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

using AssetGenerator;
using AssetGenerator.Runtime;
using DEM.Net.glTF;
using DEM.Net.Core;
using DEM.Net.Core.Imagery;
using DEM.Net.Core.Services.Lab;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    /// <summary>
    /// GpxSamples : GPX draping onto a DEM (get elevation from raster files)
    /// </summary>
    /// <summary>
    /// GpxSamples : GPX draping onto a DEM (get elevation from raster files)
    /// </summary>
    public class GpxSamples : SampleLogger
    {

        private readonly IRasterService _rasterService;
        private readonly IElevationService _elevationService;

        public GpxSamples(ILogger<GpxSamples> logger
                , IRasterService rasterService
                , IElevationService elevationService) : base(logger)
        {
            _rasterService = rasterService;
            _elevationService = elevationService;
        }

        internal void Run()
        {
            string _gpxFile = Path.Combine("SampleData", "lauzannier.gpx");
            if (!File.Exists(_gpxFile))
            {
                LogError($"Cannot run sample: {_gpxFile} is missing !");
            }
            DEMDataSet _dataSet = DEMDataSet.AW3D30;

            // Read GPX points
            var segments = GpxImport.ReadGPX_Segments(_gpxFile);
            var points = segments.SelectMany(seg => seg);

            // Retrieve elevation for each point on DEM
            var gpxPointsElevated = _elevationService.GetPointsElevation(points, DEMDataSet.AW3D30)
                                    .ToList();

            LogInfo($"{gpxPointsElevated.Count} GPX points elevation calculated");

            // TODO : pipeline processor to rewrite a GPX track with updated elevations

        }

      
    }
}
