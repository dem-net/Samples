using DEM.Net.Core;
using DEM.Net.Core.Imagery;
using DEM.Net.Extension.Osm;
using DEM.Net.Extension.Osm.Buildings;
using DEM.Net.Extension.Osm.OverpassAPI;
using DEM.Net.glTF.SharpglTF;
using GeoJSON.Net.Feature;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SharpGLTF.Schema2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleApp
{
    public class OsmExtensionSample
    {
        private readonly BuildingService _buildingService;
        private readonly IImageryService _imageryService;
        private readonly IElevationService _elevationService;
        private readonly SharpGltfService _gltfService;
        private readonly ILogger _logger;

        private float ZScale = 2f;

        public OsmExtensionSample(BuildingService buildingService
                , IImageryService imageryService
                , IElevationService elevationService
                , SharpGltfService gltfService
                , ILogger<OsmExtensionSample> logger)
        {
            this._buildingService = buildingService;
            this._imageryService = imageryService;
            this._elevationService = elevationService;
            this._gltfService = gltfService;
            this._logger = logger;
        }
        public void Run()
        {

            RunOsmPbfSample(@"D:\Temp\provence-alpes-cote-d-azur-latest.osm.pbf");

            Run3DModelSamples();

        }

        private void RunOsmPbfSample(string pbfFileName)
        {
            PbfOsmReader reader = new PbfOsmReader();
            AttributeRegistry registry = new AttributeRegistry();
            long count = 0;
            using (TimeSpanBlock timer = new TimeSpanBlock("ReadNodes", _logger))
            {
                foreach (var node in reader.ReadNodes(pbfFileName, registry))
                {
                    count++;
                    if (count % 1000000 == 0)
                        _logger.LogInformation($"{count} nodes read...");
                }
            }

            count = 0;
            using (TimeSpanBlock timer = new TimeSpanBlock("ReadWays", _logger))
            {
                foreach (var node in reader.ReadWays(pbfFileName, registry))
                {
                    count++;
                    if (count % 1000000 == 0)
                        _logger.LogInformation($"{count} ways read...");
                }
            }

            count = 0;
            using (TimeSpanBlock timer = new TimeSpanBlock("ReadRelations", _logger))
            {
                foreach (var node in reader.ReadRelations(pbfFileName, registry))
                {
                    count++;
                    if (count % 1000000 == 0)
                        _logger.LogInformation($"{count} relations read...");
                }
            }
        }

        private void Run3DModelSamples()
        {
            BoundingBox bbox;

            //// BIG one Aix
            //bbox = GeometryService.GetBoundingBox("POLYGON((5.396107779203061 43.618902041686354,5.537556753812436 43.618902041686354,5.537556753812436 43.511932043620725,5.396107779203061 43.511932043620725,5.396107779203061 43.618902041686354))");
            //GetBuildings3D(bbox);

            // Aix en provence / rotonde
            bbox = new BoundingBox(5.444927726471018, 5.447502647125315, 43.52600685540608, 43.528138282848076);
            GetBuildings3D(bbox);

            Task.Delay(1000).GetAwaiter().GetResult();
            // Aix en provence / slope
            bbox = new BoundingBox(5.434828019053151, 5.4601480721537365, 43.5386672180082, 43.55272718416761);
            GetBuildings3D(bbox);

            Task.Delay(1000).GetAwaiter().GetResult();
            // POLYGON((5.526716197512567 43.56457608971906,5.6334895739774105 43.56457608971906,5.6334895739774105 43.49662332237486,5.526716197512567 43.49662332237486,5.526716197512567 43.56457608971906))
            // Aix en provence / ste victoire
            bbox = new BoundingBox(5.526716197512567, 5.6334895739774105, 43.49662332237486, 43.56457608971906);
            GetBuildings3D(bbox);

            // Manhattan
            bbox = GeometryService.GetBoundingBox("POLYGON((-74.02606764542348 40.74041375581217,-73.97697249161489 40.74041375581217,-73.97697249161489 40.699301026594576,-74.02606764542348 40.699301026594576,-74.02606764542348 40.74041375581217))");
            GetBuildings3D(bbox);
        }

        private void GetBuildings3D(BoundingBox bbox, string modelName = "buildings")
        {
            try
            {
                // debug: write geojson to file
                //File.WriteAllText("buildings.json", JsonConvert.SerializeObject(buildingService.GetBuildingsGeoJson(bbox)));

                var model = _buildingService.GetBuildings3DModel(bbox, DEMDataSet.ASTER_GDEMV3, true, ZScale);
                model = AddTerrainModel(model, bbox, DEMDataSet.ASTER_GDEMV3, true);

                model.SaveGLB(Path.Combine(Directory.GetCurrentDirectory(), modelName + ".glb"));

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private ModelRoot AddTerrainModel(ModelRoot model, BoundingBox bbox, DEMDataSet dataset, bool withTexture = true)
        {
            try
            {
                string modelName = $"Terrain";
                string outputDir = Directory.GetCurrentDirectory();
                using (TimeSpanBlock timer = new TimeSpanBlock("Terrain", _logger))
                {
                    ImageryProvider provider = ImageryProvider.EsriWorldImagery;// new TileDebugProvider(new GeoPoint(43.5,5.5));

                    _logger.LogInformation($"Getting height map data...");

                    var heightMap = _elevationService.GetHeightMap(ref bbox, dataset);

                    _logger.LogInformation($"Processing height map data ({heightMap.Count} coordinates)...");
                    heightMap = heightMap
                        .ReprojectGeodeticToCartesian() // Reproject to 3857 (useful to get coordinates in meters)
                        .ZScale(ZScale);                    // Elevation exageration

                    //=======================
                    // Textures
                    //
                    PBRTexture pbrTexture = null;
                    if (withTexture)
                    {
                        Console.WriteLine("Download image tiles...");
                        TileRange tiles = _imageryService.DownloadTiles(bbox, provider, 10);
                        string fileName = Path.Combine(outputDir, "Texture.jpg");

                        Console.WriteLine("Construct texture...");
                        TextureInfo texInfo = _imageryService.ConstructTexture(tiles, bbox, fileName, TextureImageFormat.image_jpeg);

                        //
                        //=======================

                        //=======================
                        // Normal map
                        Console.WriteLine("Height map...");
                        var normalMap = _imageryService.GenerateNormalMap(heightMap, outputDir);

                        pbrTexture = PBRTexture.Create(texInfo, normalMap);

                        //
                        //=======================
                    }
                    // Triangulate height map
                    // and add base and sides
                    _logger.LogInformation($"Triangulating height map and generating 3D mesh...");

                    model = _gltfService.AddTerrainMesh(model, heightMap, pbrTexture);
                    return model;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }
    }
}
