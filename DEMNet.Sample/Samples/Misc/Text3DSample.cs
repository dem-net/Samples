using DEM.Net.Core;
using DEM.Net.Core.Imagery;
using DEM.Net.Core.Voronoi;
using DEM.Net.glTF.SharpglTF;
using Microsoft.Extensions.Logging;
using SharpGLTF.Schema2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace SampleApp
{
    public class Text3DSample
    {
        private readonly ILogger<Text3DSample> _logger;
        private readonly ElevationService _elevationService;
        private readonly RasterService _rasterService;
        private readonly ImageryService _imageryService;
        private readonly SharpGltfService _sharpGltfService;
        private readonly MeshService _meshService;

        public Text3DSample(ILogger<Text3DSample> logger
                , ElevationService elevationService
                , MeshService meshService
                , RasterService rasterService
                , ImageryService imageryService
                , SharpGltfService sharpGltfService)
        {
            _logger = logger;
            _elevationService = elevationService;
            _rasterService = rasterService;
            _imageryService = imageryService;
            _sharpGltfService = sharpGltfService;
            _meshService = meshService;
        }
        public void Run()
        {
            var dataset = DEMDataSet.SRTM_GL3;
            var model = GenerateSampleModel(dataset, withTexture: true);
            if (model != null)
            {
                string modelName = $"{dataset.Name}_{DateTime.Now:yyyyMMdd_HHmmss}";
                string outputDir = Directory.GetCurrentDirectory();


                // 
                float PI = (float)Math.PI;
                var arrow = _meshService.CreateArrow(20, 500, 25, 100, 10).Transform(System.Numerics.Matrix4x4.CreateRotationX(-PI / 2f));

                model = _sharpGltfService.AddMesh(model, "Arrow", arrow);

                //var bar1 = _meshService.CreateCylinder(new Vector3(0, 0, 0), 50, 250, VectorsExtensions.CreateColor(255, 0, 0)).RotateX(PI/2f);
                var bar2 = _meshService.CreateCylinder(new Vector3(0, 0, 0), 50, 250, VectorsExtensions.CreateColor(0, 255, 0)).RotateY(PI / 2f);
                //var bar3 = _meshService.CreateCylinder(new Vector3(0, 0, 0), 50, 250, VectorsExtensions.CreateColor(0, 0, 255)).RotateZ(PI/2f);


                //model = _sharpGltfService.AddMesh(model, nameof(bar1), bar1);
                model = _sharpGltfService.AddMesh(model, nameof(bar2), bar2);
                //model = _sharpGltfService.AddMesh(model, nameof(bar3), bar3);




                model.SaveGLB(Path.Combine(outputDir, modelName + ".glb"));



                _logger.LogInformation($"Model exported as {Path.Combine(outputDir, modelName + ".glb")}");
            }
        }

        private ModelRoot GenerateSampleModel(DEMDataSet dataset, bool withTexture = true)
        {

            try
            {

                int TEXTURE_TILES = 4; // 4: med, 8: high
                string outputDir = Directory.GetCurrentDirectory();

                //_rasterService.GenerateDirectoryMetadata(dataset, false);

                ImageryProvider provider = ImageryProvider.EsriWorldImagery;// new TileDebugProvider(new GeoPoint(43.5,5.5));


                // DjebelMarra
                var bbox = new BoundingBox(24.098067346557492, 24.42468219234563, 12.7769822830208, 13.087504129660111);

                _logger.LogInformation($"Getting height map data...");

                var heightMap = _elevationService.GetHeightMap(ref bbox, dataset);
                ModelGenerationTransform transform = new ModelGenerationTransform(bbox, 3857, true, 1.5f, true);

                _logger.LogInformation($"Processing height map data ({heightMap.Count} coordinates)...");
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
                return model;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }
    }

}
