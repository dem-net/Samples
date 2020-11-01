using DEM.Net.Core;
using DEM.Net.Core.Imagery;
using DEM.Net.Core.Model;
using DEM.Net.Core.Voronoi;
using DEM.Net.glTF.SharpglTF;
using Microsoft.Extensions.Logging;
using SharpGLTF.Schema2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
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
        private readonly AdornmentsService _adornmentsService;

        public Text3DSample(ILogger<Text3DSample> logger
                , ElevationService elevationService
                , MeshService meshService
                , AdornmentsService adornmentsService
                , RasterService rasterService
                , ImageryService imageryService
                , SharpGltfService sharpGltfService)
        {
            _logger = logger;
            _elevationService = elevationService;
            _rasterService = rasterService;
            _imageryService = imageryService;
            _sharpGltfService = sharpGltfService;
            _adornmentsService = adornmentsService;
            _meshService = meshService;
        }
        public void Run()
        {




            // DjebelMarra
            var bbox = GeometryService.GetBoundingBox("POLYGON((7.713614662985839 46.03014517094771,7.990332802634277 46.03014517094771,7.990332802634277 45.86877753239648,7.713614662985839 45.86877753239648,7.713614662985839 46.03014517094771))");
            var dataset = DEMDataSet.NASADEM;

            string modelName = $"{dataset.Name}_{DateTime.Now:yyyyMMdd_HHmmss}";
            string outputDir = Directory.GetCurrentDirectory();

            //var modelTest = _sharpGltfService.CreateNewModel();
            //var triangulation = CreateText("20 km", VectorsExtensions.CreateColor(255, 255, 255));
            //_sharpGltfService.AddMesh(modelTest, "Text", triangulation);

            //// Save model
            //modelTest.SaveGLB(Path.Combine(outputDir, modelName + ".glb"));



            var modelAndBbox = GenerateSampleModel(bbox, dataset, withTexture: true);
            if (modelAndBbox.Model != null)
            {
                var model = modelAndBbox.Model;

                // add adornments
                Stopwatch swAdornments = Stopwatch.StartNew();
                TriangulationList<Vector3> adornments = _adornmentsService.CreateModelAdornments(dataset, ImageryProvider.MapBoxSatellite, bbox, modelAndBbox.projectedBbox);
                model = _sharpGltfService.AddMesh(model, "Adornments", adornments, default(Vector4), doubleSided: true);
                swAdornments.Stop();

                _logger.LogInformation($"Adornments generation: {swAdornments.ElapsedMilliseconds:N1} ms");

                // Save model
                model.SaveGLB(Path.Combine(outputDir, modelName + ".glb"));

                _logger.LogInformation($"Model exported as {Path.Combine(outputDir, modelName + ".glb")}");
            }
        }


        private (ModelRoot Model, double widthMeters, double heightMeters, double averageElevation, BoundingBox projectedBbox) GenerateSampleModel(BoundingBox bbox, DEMDataSet dataset, bool withTexture = true)
        {

            try
            {

                int TEXTURE_TILES = 8; // 4: med, 8: high
                string outputDir = Directory.GetCurrentDirectory();

                //_rasterService.GenerateDirectoryMetadata(dataset, false);

                ImageryProvider provider = ImageryProvider.MapBoxSatellite;// new TileDebugProvider(new GeoPoint(43.5,5.5));


                _logger.LogInformation($"Getting height map data...");

                var heightMap = _elevationService.GetHeightMap(ref bbox, dataset);

                var wgs84bbox = bbox;
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
                    //var normalMap = _imageryService.GenerateNormalMap(heightMap, outputDir);

                    pbrTexture = PBRTexture.Create(texInfo, null);

                    //hMap = hMap.CenterOnOrigin(Z_FACTOR);
                    //
                    //=======================
                }
                // Triangulate height map
                // and add base and sides
                _logger.LogInformation($"Triangulating height map and generating 3D mesh...");

                var model = _sharpGltfService.CreateTerrainMesh(heightMap, pbrTexture);

                var width = new GeoPoint(wgs84bbox.Center[1], bbox.Center[0] - bbox.Width / 2f).DistanceTo(new GeoPoint(wgs84bbox.Center[1], bbox.Center[0] + bbox.Width / 2f));
                var height = new GeoPoint(bbox.Center[1] - bbox.Height / 2f, wgs84bbox.Center[0]).DistanceTo(new GeoPoint(bbox.Center[1] + bbox.Height, wgs84bbox.Center[0]));

                return (model, width, height, wgs84bbox.Center[2], heightMap.BoundingBox);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return (null, 0, 0, 0, null);
            }
        }
    }

}
