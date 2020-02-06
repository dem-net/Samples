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
using DEM.Net.glTF.SharpglTF;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DEM.Net.Core.Imagery;

namespace SampleApp
{
    /// <summary>
    /// Extracts a DEM from a bbox and generates a 3D export in glTF format
    /// </summary>
    public class glTF3DSamples
    {
        private readonly ILogger<glTF3DSamples> _logger;
        private readonly IElevationService _elevationService;
        private readonly IImageryService _imageryService;
        private readonly IRasterService _rasterService;
        private readonly SharpGltfService _sharpGltfService;
        private int TEXTURE_TILES = 2; // 4: med, 8: high

        public glTF3DSamples(ILogger<glTF3DSamples> logger
                , IElevationService elevationService
                , SharpGltfService sharpGltfService
                , IImageryService imageryService
                , IRasterService rasterService)
        {
            _logger = logger;
            _elevationService = elevationService;
            _sharpGltfService = sharpGltfService;
            _imageryService = imageryService;
            _rasterService = rasterService;
        }
        public void Run(DEMDataSet dataset, bool withTexture = true)
        {
            try
            {
                //_rasterService.GenerateDirectoryMetadata(dataset, false);
                Stopwatch sw = Stopwatch.StartNew();
                string modelName = $"Montagne Sainte Victoire {dataset.Name}";
                string outputDir = Directory.GetCurrentDirectory();
                
                ImageryProvider provider = ImageryProvider.MapBoxSatelliteStreet;// new TileDebugProvider(new GeoPoint(43.5,5.5));

                // You can get your boox from https://geojson.net/ (save as WKT)
                string bboxWKT = "POLYGON((5.54888 43.519525, 5.61209 43.519525, 5.61209 43.565225, 5.54888 43.565225, 5.54888 43.519525))";
//                string bboxWKT =
//                    "POLYGON((5.594457381483949 43.545276557046044,5.652135604140199 43.545276557046044,5.652135604140199 43.52038635099936,5.594457381483949 43.52038635099936,5.594457381483949 43.545276557046044))";
//                _logger.LogInformation($"Processing model {modelName}...");
//
//
//                _logger.LogInformation($"Getting bounding box geometry...");
                var bbox = GeometryService.GetBoundingBox(bboxWKT);

                //var bbox = new BoundingBox(5.5613898348431485,5.597185285307553,43.49372969433046,43.50939068558466);
                _logger.LogInformation($"Getting height map data...");

                var heightMap = _elevationService.GetHeightMap(ref bbox, dataset);

                _logger.LogInformation($"Processing height map data ({heightMap.Count} coordinates)...");
                heightMap = heightMap
                    .ReprojectGeodeticToCartesian() // Reproject to 3857 (useful to get coordinates in meters)
                    .ZScale(2f);                    // Elevation exageration

                //=======================
                // Textures
                //
                PBRTexture pbrTexture = null;
                if (withTexture)
                {


                    Console.WriteLine("Download image tiles...");
                    TileRange tiles = _imageryService.DownloadTiles(bbox, provider, TEXTURE_TILES);
                    string fileName = Path.Combine(outputDir, "Texture.jpg");

                    Console.WriteLine("Construct texture...");
                    TextureInfo texInfo = _imageryService.ConstructTexture(tiles, bbox, fileName, TextureImageFormat.image_jpeg);

                    //
                    //=======================

                    //=======================
                    // Normal map
                    Console.WriteLine("Height map...");
                    //float Z_FACTOR = 0.00002f;

                    //hMap = hMap.CenterOnOrigin().ZScale(Z_FACTOR);
                    var normalMap = _imageryService.GenerateNormalMap(heightMap, outputDir);
    
                    pbrTexture = PBRTexture.Create(texInfo, normalMap);

                    //hMap = hMap.CenterOnOrigin(Z_FACTOR);
                    //
                    //=======================
                }
                // Triangulate height map
                // and add base and sides
                _logger.LogInformation($"Triangulating height map and generating 3D mesh...");

                var model = _sharpGltfService.CreateTerrainMesh(heightMap, pbrTexture);
                model.SaveGLB(Path.Combine(Directory.GetCurrentDirectory(), modelName + ".glb"));

                model = _sharpGltfService.CreateTerrainMesh(heightMap, GenOptions.Normals | GenOptions.BoxedBaseElevationMin);
                model.SaveGLB(Path.Combine(Directory.GetCurrentDirectory(), modelName + "_normalsBox.glb"));

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
