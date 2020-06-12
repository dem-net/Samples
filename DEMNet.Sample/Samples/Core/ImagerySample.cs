//
// ImagerySample.cs
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
using DEM.Net.Core.Imagery;
using DEM.Net.glTF.SharpglTF;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;

namespace SampleApp
{
    public class ImagerySample
    {
        private readonly ILogger<glTF3DSamples> _logger;
        private readonly IElevationService _elevationService;
        private readonly SharpGltfService _sharpGltfService;
        private readonly ImageryService _imageryService;

        public ImagerySample(ILogger<glTF3DSamples> logger
                , IElevationService elevationService
                , SharpGltfService sharpGltfService
                , ImageryService imageryService)
        {
            _logger = logger;
            _elevationService = elevationService;
            _sharpGltfService = sharpGltfService;
            _imageryService = imageryService;
        }
        public void Run()
        {
            try
            {


                DEMDataSet dataset = DEMDataSet.AW3D30;
                ImageryProvider imageryProvider = ImageryProvider.MapBoxSatelliteStreet;
                Stopwatch sw = Stopwatch.StartNew();
                string modelName = $"Montagne Sainte Victoire {dataset.Name}";
                string outputDir = Directory.GetCurrentDirectory();

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


                TileRange tiles = _imageryService.DownloadTiles(bbox, imageryProvider, 8);
                var texture = _imageryService.ConstructTexture(tiles, bbox, Path.Combine(outputDir, modelName + "_texture.jpg"), TextureImageFormat.image_jpeg);
                var normalMap = _imageryService.GenerateNormalMap(heightMap, outputDir, modelName + "_normalmap.png");
                var pbrTexture = PBRTexture.Create(texture, normalMap);


                // Triangulate height map
                _logger.LogInformation($"Triangulating height map and generating 3D mesh...");

                var model = _sharpGltfService.CreateTerrainMesh(heightMap, pbrTexture);
                model.SaveGLB(Path.Combine(Directory.GetCurrentDirectory(), modelName + ".glb"));

                _logger.LogInformation($"Done in {sw.Elapsed:g}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }
    }
}