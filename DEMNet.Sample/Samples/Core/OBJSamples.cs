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
using SharpGLTF.Schema2;
using g3;
using DEM.Net.glTF;
using DEM.Net.glTF.Export;

namespace SampleApp
{
    /// <summary>
    /// Extracts a DEM from a bbox and generates a 3D export in glTF format
    /// </summary>
    public class OBJSamples
    {
        private readonly ILogger<OBJSamples> _logger;
        private readonly ElevationService _elevationService;
        private readonly ImageryService _imageryService;
        private readonly MeshReducer _meshReducer;
        private readonly SharpGltfService _sharpGltfService;
        private readonly OBJExportService _objExporter;
        private readonly AdornmentsService _adornmentsService;

        public OBJSamples(ILogger<OBJSamples> logger
                , ElevationService elevationService
                , SharpGltfService sharpGltfService
                , MeshReducer meshReducer
                , AdornmentsService adornmentsService
                , OBJExportService oBJExportService 
                , ImageryService imageryService)
        {
            _logger = logger;
            _elevationService = elevationService;
            _sharpGltfService = sharpGltfService;
            _imageryService = imageryService;
            _meshReducer = meshReducer;
            _adornmentsService = adornmentsService;
            _objExporter = oBJExportService;
        }
        public void Run()
        {
            try
            {

                TestObjExport(DEMDataSet.AW3D30, true);


                var path = @"C:\Users\xavier.fischer\Downloads\Telegram Desktop\SinGeo\Torre Badúm.obj";
                //var mtlPath = @"C:\Users\xavier.fischer\Downloads\Telegram Desktop\Torre BadúmGeoR38571\Torre BadúmGeoR3857.obj";
                DMesh3 mesh = StandardMeshReader.ReadMesh(path);

                DMesh3Builder builder = new DMesh3Builder();

                OBJReader reader = new OBJReader();
                using (var fs = File.OpenText(path))
                {
                    var options = ReadOptions.Defaults;
                    options.ReadMaterials = false;
                    IOReadResult result = reader.Read(fs, options, builder);
                    if (result.code == IOCode.Ok)
                    {

                    }


                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        public void TestObjExport(DEMDataSet dataset, bool withTexture = true)
        {
            try
            {

                int TEXTURE_TILES = 4; // 4: med, 8: high

                //_rasterService.GenerateDirectoryMetadata(dataset, false);
                Stopwatch sw = Stopwatch.StartNew();
                string modelName = $"MontBlanc_{dataset.Name}";
                string outputDir = Directory.GetCurrentDirectory();
                ImageryProvider provider = ImageryProvider.EsriWorldImagery;// new TileDebugProvider(new GeoPoint(43.5,5.5));


                //// You can get your boox from https://geojson.net/ (save as WKT)
                //string bboxWKT = "POLYGON((5.54888 43.519525, 5.61209 43.519525, 5.61209 43.565225, 5.54888 43.565225, 5.54888 43.519525))";
                ////                string bboxWKT =
                ////                    "POLYGON((5.594457381483949 43.545276557046044,5.652135604140199 43.545276557046044,5.652135604140199 43.52038635099936,5.594457381483949 43.52038635099936,5.594457381483949 43.545276557046044))";
                ////                _logger.LogInformation($"Processing model {modelName}...");
                ////
                ////
                ////                _logger.LogInformation($"Getting bounding box geometry...");
                //var bbox = GeometryService.GetBoundingBox(bboxWKT);

                // DjebelMarra
                //var bbox = new BoundingBox(24.098067346557492, 24.42468219234563, 12.7769822830208, 13.087504129660111);

                // Ste
                var bbox = GeometryService.GetBoundingBox("POLYGON((5.580963505931105 43.504438070335866,5.583152188487257 43.504438070335866,5.583152188487257 43.50303732097129,5.580963505931105 43.50303732097129,5.580963505931105 43.504438070335866))");

                // MontBlanc
                //var bbox = GeometryService.GetBoundingBox("POLYGON((6.618804355541963 45.9658287141746,7.052764316479463 45.9658287141746,7.052764316479463 45.72379929776474,6.618804355541963 45.72379929776474,6.618804355541963 45.9658287141746))");

                //var bbox = new BoundingBox(5.5613898348431485,5.597185285307553,43.49372969433046,43.50939068558466);
                _logger.LogInformation($"Getting height map data...");


                ModelGenerationTransform transform = new ModelGenerationTransform(bbox, dataset.SRID, Reprojection.SRID_PROJECTED_MERCATOR, true, 1.5f, false);
                bbox = bbox.ReprojectTo(4326, dataset.SRID);
                var heightMap = _elevationService.GetHeightMap(ref bbox, dataset);
                _logger.LogInformation($"Processing height map data ({heightMap.Count} coordinates)...");
                transform.BoundingBox = bbox;
                heightMap = transform.TransformHeightMap(heightMap);


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
                    //var normalMap = _imageryService.GenerateNormalMap(heightMap, outputDir);

                    pbrTexture = PBRTexture.Create(texInfo, null);

                    //hMap = hMap.CenterOnOrigin(Z_FACTOR);
                    //
                    //=======================
                }
                // Triangulate height map
                // and add base and sides
                _logger.LogInformation($"Triangulating height map and generating 3D mesh...");

                var model = _sharpGltfService.CreateTerrainMesh(heightMap, pbrTexture, reduceFactor: 1f);

                bool adornments = false;
                if (adornments)
                {
                    var adornmentsModel = _adornmentsService.CreateModelAdornments(dataset, provider, bbox.ReprojectTo(dataset.SRID, 4326), heightMap.BoundingBox);
                    model = _sharpGltfService.AddMesh(model, "Adornments", adornmentsModel);
                }

                _objExporter.ExportGlTFModelToWaveFrontObj(model, "obj_export", "test1.obj", overwrite: true, zip: false);
                model.SaveGLB(Path.Combine(Directory.GetCurrentDirectory(), modelName + ".glb"));
                model.SaveAsWavefront(Path.Combine(Directory.GetCurrentDirectory(), modelName + ".obj"));

                model = _sharpGltfService.CreateTerrainMesh(heightMap, GenOptions.Normals | GenOptions.BoxedBaseElevationMin);
                model.SaveGLB(Path.Combine(Directory.GetCurrentDirectory(), modelName + "_normalsBox.glb"));
                model.SaveAsWavefront(Path.Combine(Directory.GetCurrentDirectory(), modelName + "_normalsBox.obj"));

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
