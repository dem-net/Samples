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
        private readonly IMeshService _meshService;
        private readonly ILogger _logger;

        private float ZScale = 2f;

        public OsmExtensionSample(BuildingService buildingService
                , IImageryService imageryService
                , IElevationService elevationService
                , SharpGltfService gltfService
                , IMeshService meshService
                , ILogger<OsmExtensionSample> logger)
        {
            this._buildingService = buildingService;
            this._imageryService = imageryService;
            this._elevationService = elevationService;
            this._gltfService = gltfService;
            this._meshService = meshService;
            this._logger = logger;
        }
        public void Run()
        {

            //RunOsmPbfSample(@"D:\Temp\provence-alpes-cote-d-azur-latest.osm.pbf");

            Run3DModelSamples();

            //RunTesselationSample();

        }

        private void RunTesselationSample()
        {
            List<GeoPoint> geoPoints = new List<GeoPoint>();
            geoPoints.Add(new GeoPoint(0, 0));
            geoPoints.Add(new GeoPoint(10, 0));
            geoPoints.Add(new GeoPoint(10, 10));
            geoPoints.Add(new GeoPoint(0, 10));

            List<List<GeoPoint>> inners = new List<List<GeoPoint>>();
            List<GeoPoint> inner = new List<GeoPoint>();
            inner.Add(new GeoPoint(3, 3));
            inner.Add(new GeoPoint(7, 3));
            inner.Add(new GeoPoint(7, 7));
            inner.Add(new GeoPoint(3, 7));
            inners.Add(inner);

            //_meshService.Tesselate(geoPoints, null);
            _meshService.Tesselate(geoPoints, inners);
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

            // Simple 4 vertex poly
            //bbox = GeometryService.GetBoundingBox("POLYGON((5.418905095715298 43.55466923119226,5.419768767018094 43.55466923119226,5.419768767018094 43.55411328949576,5.418905095715298 43.55411328949576,5.418905095715298 43.55466923119226))");
            //GetBuildings3D(bbox);

            // Aix / ZA les Milles
            bbox = GeometryService.GetBoundingBox("POLYGON((5.337387271772482 43.49858292942485,5.3966104468213105 43.49858292942485,5.3966104468213105 43.46781823961212,5.337387271772482 43.46781823961212,5.337387271772482 43.49858292942485))");
            GetBuildings3D(bbox);

            // Aix Mignet / polygon with inner ring
            bbox = GeometryService.GetBoundingBox("POLYGON((5.448310034686923 43.52504334503996,5.44888402741611 43.52504334503996,5.44888402741611 43.524666052953144,5.448310034686923 43.524666052953144,5.448310034686923 43.52504334503996))");
            GetBuildings3D(bbox);

            // Manhattan
            bbox = GeometryService.GetBoundingBox("POLYGON((-74.02606764542348 40.74041375581217,-73.97697249161489 40.74041375581217,-73.97697249161489 40.699301026594576,-74.02606764542348 40.699301026594576,-74.02606764542348 40.74041375581217))");
            GetBuildings3D(bbox);

            // Aix en provence / rotonde
            bbox = new BoundingBox(5.444927726471018, 5.447502647125315, 43.52600685540608, 43.528138282848076);
            GetBuildings3D(bbox);

            //Task.Delay(1000).GetAwaiter().GetResult();
            // Aix en provence / slope
            bbox = new BoundingBox(5.434828019053151, 5.4601480721537365, 43.5386672180082, 43.55272718416761);
            GetBuildings3D(bbox);

            //// BIG one Aix
            bbox = GeometryService.GetBoundingBox("POLYGON((5.396107779203061 43.618902041686354,5.537556753812436 43.618902041686354,5.537556753812436 43.511932043620725,5.396107779203061 43.511932043620725,5.396107779203061 43.618902041686354))");
            GetBuildings3D(bbox);

            //Task.Delay(1000).GetAwaiter().GetResult();
            // POLYGON((5.526716197512567 43.56457608971906,5.6334895739774105 43.56457608971906,5.6334895739774105 43.49662332237486,5.526716197512567 43.49662332237486,5.526716197512567 43.56457608971906))
            // Aix en provence / ste victoire
            bbox = new BoundingBox(5.526716197512567, 5.6334895739774105, 43.49662332237486, 43.56457608971906);
            GetBuildings3D(bbox);


        }

        private void GetBuildings3D(BoundingBox bbox, string modelName = "buildings")
        {
            try
            {
                // debug: write geojson to file
                //File.WriteAllText("buildings.json", JsonConvert.SerializeObject(buildingService.GetBuildingsGeoJson(bbox)));

                var model = _buildingService.GetBuildings3DModel(bbox, DEMDataSet.ASTER_GDEMV3, downloadMissingFiles: true, ZScale);
                model = AddTerrainModel(model, bbox, DEMDataSet.ASTER_GDEMV3, withTexture: true);

                model.SaveGLB(Path.Combine(Directory.GetCurrentDirectory(), modelName + ".glb"));

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void TestBuildingTriangulation(int osmId)
        {
            //TODO: fix overpass id filter way(id:<id>)
            FeatureCollection buildings = _buildingService.GetBuildingsGeoJson(osmId);

            var triangulation = _buildingService.GetBuildings3DTriangulation(buildings, DEMDataSet.ASTER_GDEMV3, downloadMissingFiles: true, ZScale);

        }


        private ModelRoot AddTerrainModel(ModelRoot model, BoundingBox bbox, DEMDataSet dataset, bool withTexture = true, int numTiles = 4)
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
                        TileRange tiles = _imageryService.DownloadTiles(bbox, provider, numTiles);
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
