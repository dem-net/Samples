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

namespace SampleApp
{
    public class PolygonMaskSample
    {
        //const string UK_BBOX = "POLYGON((-10.848751844518207 61.09112960974638,2.1590606554817926 61.09112960974638,2.1590606554817926 49.85343376382465,-10.848751844518207 49.85343376382465,-10.848751844518207 61.09112960974638))";
        const string UK_POLYGON = "POLYGON((-11.240241527557368 53.019643960864066,-6.933600902557369 59.56068565869839,-0.6713938713073686 61.05156171858632,2.5585865974426314 51.9763232490077,0.4931569099426314 50.06937823002705,-10.141608715057368 49.387632635919935,-11.240241527557368 53.019643960864066))";

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
                var bbox = GeometryService.GetBoundingBox(UK_POLYGON);

                _logger.LogInformation($"Getting height map data...");
                var heightMap = _elevationService.GetHeightMap(ref bbox, dataset);

                _logger.LogInformation($"Processing height map data ({heightMap.Count} coordinates)...");
                heightMap = heightMap
                                        .ApplyGeometryMask(UK_POLYGON)
                                        .ZClamp(0, null)
                                        .ReprojectGeodeticToCartesian() // Reproject to 3857 (useful to get coordinates in meters)
                                        .ZScale(20f)                     // Elevation exageration
                                        .CenterOnOrigin()               //
                                        .FitInto(250f)                 // Make sure model fits into 250 coordinates units (3D printer size was 30x30cm)
                                        .BakeCoordinates();

                // Triangulate height map
                // and add base and sides
                _logger.LogInformation($"Triangulating height map and generating box (5mm thick)...");

                // STL axis differ from glTF 
                float reduceFactor = 1f; // 0: full reduction -> 1: no reduction
                var model = _sharpGltfService.CreateTerrainMesh(heightMap, GenOptions.BoxedBaseElevationMin, Matrix4x4.CreateRotationX((float)Math.PI / 2f), doubleSided: false, meshReduceFactor: reduceFactor);


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
