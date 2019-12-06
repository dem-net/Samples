//
// glTF3DSamples.cs
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
using DEM.Net.glTF.SharpglTF;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;

namespace SampleApp
{
    /// <summary>
    /// Extracts a DEM from a bbox and generates a 3D export in glTF format
    /// </summary>
    public class glTF3DSamples
    {
        private readonly ILogger<glTF3DSamples> _logger;
        private readonly IElevationService _elevationService;
        private readonly IglTFService _glTFService;
        private readonly SharpGltfService _sharpGltfService;

        public glTF3DSamples(ILogger<glTF3DSamples> logger
                , IElevationService elevationService
                , IglTFService glTFService
                , SharpGltfService sharpGltfService)
        {
            _logger = logger;
            _elevationService = elevationService;
            _glTFService = glTFService;
            _sharpGltfService = sharpGltfService;


        }
        public void Run()
        {
            try
            {


                DEMDataSet dataset = DEMDataSet.AW3D30;
                Stopwatch sw = Stopwatch.StartNew();
                string modelName = $"Montagne Sainte Victoire {dataset.Name}";

                // You can get your boox from https://geojson.net/ (save as WKT)
                string bboxWKT = "POLYGON((5.54888 43.519525, 5.61209 43.519525, 5.61209 43.565225, 5.54888 43.565225, 5.54888 43.519525))";

                _logger.LogInformation($"Processing model {modelName}...");


                _logger.LogInformation($"Getting bounding box geometry...");
                var bbox = GeometryService.GetBoundingBox(bboxWKT);

                _logger.LogInformation($"Getting height map data...");
                var heightMap = _elevationService.GetHeightMap(ref bbox, dataset);

                _logger.LogInformation($"Processing height map data ({heightMap.Count} coordinates)...");
                heightMap = heightMap
                                        .ReprojectGeodeticToCartesian() // Reproject to 3857 (useful to get coordinates in meters)
                                        .ZScale(2f)                     // Elevation exageration
                                        .CenterOnOrigin();

                // Triangulate height map
                // and add base and sides
                _logger.LogInformation($"Triangulating height map and generating 3D mesh...");
                var mesh = _glTFService.GenerateTriangleMesh(heightMap);

                var model3 = _sharpGltfService.CreateModel(heightMap);
                model3.SaveGLB(Path.Combine(Directory.GetCurrentDirectory(), modelName + "_3.glb"));

                var mesh2 = _sharpGltfService.CreateTerrainMesh_NoTextureColor(heightMap);
                var model2 = _sharpGltfService.CreateModel(mesh2);


                model2.SaveGLB(Path.Combine(Directory.GetCurrentDirectory(), modelName + "_2.glb"));

                _logger.LogInformation($"Creating glTF model...");
                var model = _glTFService.GenerateModel(mesh, modelName);

                _logger.LogInformation($"Exporting glTF model...");
                _glTFService.Export(model, Directory.GetCurrentDirectory(), modelName, exportglTF: false, exportGLB: true);

                _logger.LogInformation($"Model exported as {Path.Combine(Directory.GetCurrentDirectory(), modelName + ".gltf")} and .glb");

                _logger.LogInformation($"Done in {sw.Elapsed:g}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }
    }
}
