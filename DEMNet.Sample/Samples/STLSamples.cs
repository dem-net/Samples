//
// STLSamples.cs
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
using System;
using System.Diagnostics;
using System.IO;

namespace SampleApp
{
    /// <summary>
    /// STLSamples: This sample will generate a STL file from a bounding box using SRTM_GL3 dataset
    /// </summary>
    public class STLSamples : SampleLogger
    {
        private readonly IElevationService _elevationService;
        private readonly IglTFService _glTFService;
        private readonly ISTLExportService _stlService;

        public STLSamples(ILogger<STLSamples> logger
                , IElevationService elevationService
                , IglTFService glTFService
                , ISTLExportService stlService) : base(logger)
        {
            _elevationService = elevationService;
            _glTFService = glTFService;
            _stlService = stlService;
        }
        public void Run()
        {
            DEMDataSet dataset = DEMDataSet.SRTM_GL3;
            Stopwatch sw = Stopwatch.StartNew();
            string modelName = $"Montagne Sainte Victoire {dataset.Name}";

            // You can get your boox from https://geojson.net/ (save as WKT)
            string bboxWKT = "POLYGON((5.54888 43.519525, 5.61209 43.519525, 5.61209 43.565225, 5.54888 43.565225, 5.54888 43.519525))";
            
            LogInfo($"Processing model {modelName}...");
            

            LogInfo($"Getting bounding box geometry...");
            var bbox = GeometryService.GetBoundingBox(bboxWKT);

            LogInfo($"Getting height map data...");
            var heightMap = _elevationService.GetHeightMap(bbox, dataset);

            LogInfo($"Processing height map data ({heightMap.Count} coordinates)...");
            heightMap = heightMap
                                    .ReprojectGeodeticToCartesian() // Reproject to 3857 (useful to get coordinates in meters)
                                    .ZScale(2f)                     // Elevation exageration
                                    .CenterOnOrigin()               //
                                    .FitInto(250f);                 // Make sure model fits into 250 coordinates units (3D printer size was 30x30cm)

            // Triangule Irregular Network (not implemented to STL yet)
            //var TINmesh =TINGeneration.GenerateTIN(heightMap, 2, _glTFService, null, 3857);

            // Triangulate height map
            // and add base and sides
            LogInfo($"Triangulating height map and generating box (5mm thick)...");
            var mesh = _glTFService.GenerateTriangleMesh_Boxed(heightMap, BoxBaseThickness.FromMinimumPoint, 5);

            
            // STL axis differ from glTF 
            LogInfo($"Rotating mesh...");
            mesh.RotateX((float)Math.PI / 2f);
            
            LogInfo($"Exporting STL model...");
            var stlFilePath = Path.Combine(Directory.GetCurrentDirectory(), $"{modelName}.stl");
            _stlService.STLExport(mesh, stlFilePath, false);

            LogInfo($"Model exported in {stlFilePath}.");

            LogInfo($"Done in {sw.Elapsed:g}");

        }

    }
}
