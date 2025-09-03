using DEM.Net.Core;
using DEM.Net.glTF.Export;
using DEM.Net.glTF.SharpglTF;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SampleApp
{
    public class PolygonMaskSample
    {
        //const string UK_BBOX = "POLYGON((-10.848751844518207 61.09112960974638,2.1590606554817926 61.09112960974638,2.1590606554817926 49.85343376382465,-10.848751844518207 49.85343376382465,-10.848751844518207 61.09112960974638))";
        //const string UK_POLYGON = "POLYGON((-11.240241527557368 53.019643960864066,-6.933600902557369 59.56068565869839,-0.6713938713073686 61.05156171858632,2.5585865974426314 51.9763232490077,0.4931569099426314 50.06937823002705,-10.141608715057368 49.387632635919935,-11.240241527557368 53.019643960864066))";
        //const string TEST_POLY = "POLYGON ((6.394043 44.617844, 6.547852 44.758436, 6.531372 44.962855, 6.344604 45.063822, 5.96283 44.9784, 5.960083 44.887012, 6.160583 44.715514, 6.394043 44.617844))";

        // same, concave
        //const string TEST_POLY = "POLYGON ((6.394043 44.614912, 6.465454 44.629573, 6.531372 44.710634, 6.490173 44.744783, 6.453094 44.781835, 6.421509 44.831526, 6.50528 44.875336, 6.490173 44.972571, 6.455841 45.043448, 6.306152 45.054121, 6.215515 44.991998, 6.248474 44.948277, 6.223755 44.944389, 6.105652 45.000738, 5.965576 44.997825, 5.94223 44.943417, 5.953217 44.881174, 6.05484 44.851001, 6.135864 44.872416, 6.101532 44.815941, 6.137238 44.78086, 6.122131 44.73893, 6.174316 44.705754, 6.252594 44.715514, 6.286926 44.759411, 6.362457 44.731126, 6.369324 44.691112, 6.308899 44.662793, 6.317139 44.625664, 6.394043 44.614912))";
        const string TEST_POLY = "POLYGON ((4.746094 44.918139, 4.460449 45.39845, 3.647461 45.197522, 3.669434 44.559163, 4.394531 43.961191, 5.053711 43.405047, 5.449219 43.165123, 6.020508 43.405047, 6.328125 44.260937, 6.547852 44.840291, 6.899414 45.305803, 6.28418 45.675482, 5.251465 45.444717, 4.746094 44.918139))";

        private readonly ILogger<PolygonMaskSample> _logger;
        private readonly ElevationService _elevationService;
        private readonly SharpGltfService _sharpGltfService;
        private readonly ISTLExportService _stlService;

        public PolygonMaskSample(ILogger<PolygonMaskSample> logger
                , ElevationService elevationService
                , SharpGltfService sharpGltfService
                , ISTLExportService stlService)
        {
            _logger = logger;
            _elevationService = elevationService;
            _sharpGltfService = sharpGltfService;
            _stlService = stlService;
        }
        public void Run(DEMDataSet dataset)
        {
            try
            {


                Stopwatch sw = Stopwatch.StartNew();
                string modelName = $"Model {dataset.Name}";


                _logger.LogInformation($"Processing model {modelName}...");

                // You can get your boox from https://geojson.net/ (save as WKT)
                //string bboxWKT = "POLYGON((5.54888 43.519525, 5.61209 43.519525, 5.61209 43.565225, 5.54888 43.565225, 5.54888 43.519525))";
                var bbox = GeometryService.GetBoundingBox(TEST_POLY);

                _logger.LogInformation($"Getting height map data...");
                var heightMap = _elevationService.GetHeightMap(ref bbox, dataset);

                _logger.LogInformation($"Processing height map data ({heightMap.Count} coordinates)...");
                heightMap = heightMap
                                        .ApplyGeometryMask(TEST_POLY)
                                        .ZClamp(0, null)
                                        .ReprojectGeodeticToCartesian() // Reproject to 3857 (useful to get coordinates in meters)
                                        .ZScale(5f)                     // Elevation exageration
                                        .CenterOnOrigin()               //
                                        .FitInto(250f)                 // Make sure model fits into 250 coordinates units (3D printer size was 30x30cm)
                                        .BakeCoordinates();

                // Triangulate height map
                // and add base and sides
                _logger.LogInformation($"Triangulating height map and generating box (5mm thick)...");

                // STL axis differ from glTF 
                float reduceFactor = 1f; // 0: full reduction -> 1: no reduction
                var model = _sharpGltfService.CreateTerrainMesh(heightMap, GenOptions.CropToNonEmpty, Matrix4x4.CreateRotationX((float)Math.PI / 2f), doubleSided: false, meshReduceFactor: reduceFactor);


                _logger.LogInformation($"Exporting STL model...");
                var stlFilePath = Path.Combine(Directory.GetCurrentDirectory(), $"{modelName}.stl");
                _stlService.STLExport(model.LogicalMeshes[0].Primitives[0], stlFilePath, ascii: false);

                _logger.LogInformation($"Model exported in {stlFilePath}.");

                _logger.LogInformation($"Done in {sw.Elapsed:g}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }
    }
}
