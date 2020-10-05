//
// CustomRasterElevationSample.cs
//
// Author:
//       Xavier Fischer
//
// Copyright (c) 2020 
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
using DEM.Net.Core.Datasets;
using DEM.Net.Core.Imagery;
using DEM.Net.glTF.SharpglTF;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace SampleApp
{
    /// <summary>
    /// CustomRasterElevationSample: Show how to interact with a custom dataset
    /// </summary>
    public class CustomRasterElevationSample
    {
        private readonly ILogger<CustomRasterElevationSample> _logger;
        private readonly RasterService _rasterService;
        private readonly ElevationService _elevationService;
        private readonly ImageryService _imageryService;
        private readonly MeshService _meshService;
        private readonly SharpGltfService _sharpGltfService;

        public CustomRasterElevationSample(ILogger<CustomRasterElevationSample> logger
                , RasterService rasterService
                , ElevationService elevationService
                , MeshService meshService
                , SharpGltfService sharpGltfService
                , ImageryService imageryService)
        {
            _logger = logger;
            _rasterService = rasterService;
            _elevationService = elevationService;
            _sharpGltfService = sharpGltfService;
            _imageryService = imageryService;
            _meshService = meshService;
        }
        public void Run()
        {
            try
            {
                
                float zFactor = 8f;
                string outputDir = Directory.GetCurrentDirectory();
                ImageryProvider imageryProvider = ImageryProvider.MapBoxSatellite;

                DEMDataSet litto3DDataset1m = GetLitto3D_1Metre(@"C:\Repos\DEM.Net.Samples\DEMNet.Sample\SampleData\MNT1m Large", firstRun: false);
                DEMDataSet litto3DDataset5m = GetLitto3D_5Metres(@"C:\Repos\DEM.Net.Samples\DEMNet.Sample\SampleData\MNT5m Large", firstRun: false);



                DEMDataSet litto3DDataset = litto3DDataset5m;

                string modelName = $"PortCros_NE_{litto3DDataset.ResolutionMeters}m_z{zFactor}_{imageryProvider.Name}";

                //string modelName = $"TourFondue_{litto3DDataset.ResolutionMeters}m_z{zFactor}_{imageryProvider.Name}";

                _logger.LogInformation($"Getting height map data...");

                // Giens
                //string bboxWkt = "POLYGON((6.087614563876178 43.07440002288367,6.1748185433683656 43.07440002288367,6.1748185433683656 43.026230173613094,6.087614563876178 43.026230173613094,6.087614563876178 43.07440002288367))";
                // Test Tour Fondue
                //string bboxWkt = "POLYGON((6.13964197790013 43.03635866284334,6.174650169962508 43.03635866284334,6.174650169962508 43.01528378155327,6.13964197790013 43.01528378155327,6.13964197790013 43.03635866284334))";

                //// OK NE for OH5
                //string bboxWkt = "POLYGON((6.3567260447069085 43.021436832948694,6.382389420561401 43.021436832948694,6.382389420561401 43.00041175430446,6.3567260447069085 43.00041175430446,6.3567260447069085 43.021436832948694))";
                //// OK SW for OH5
                //string bboxWkt = "POLYGON((6.359392181239976 43.012843324704065,6.385570541225327 43.012843324704065,6.385570541225327 42.991187491294845,6.359392181239976 42.991187491294845,6.359392181239976 43.012843324704065))";
                // bbox total MNT 5m
                //string bboxWkt = "POLYGON((6.379174977593318 43.01254201266237,6.382608205132381 43.01254201266237,6.382608205132381 43.00882338332376,6.379174977593318 43.00882338332376,6.379174977593318 43.01254201266237))";

                // OK
                //string bboxWkt = "POLYGON((6.363825088730257 43.01654549682949,6.39970231651346 43.01654549682949,6.39970231651346 42.99112418454671,6.363825088730257 42.99112418454671,6.363825088730257 43.01654549682949))";
                //string bboxWkt = "POLYGON((6.406395745104527 43.02231568794789,6.427166771715855 43.02231568794789,6.427166771715855 43.005339523906166,6.406395745104527 43.005339523906166,6.406395745104527 43.02231568794789))";
                //string bboxWkt  = "POLYGON((6.412103485888219 43.01676208054169,6.428497147387242 43.01676208054169,6.428497147387242 43.004900152380664,6.412103485888219 43.004900152380664,6.412103485888219 43.01676208054169))";
                ////string bboxWkt = "POLYGON((6.362806960421437 43.01537135435604,6.394821807223194 43.01537135435604,6.394821807223194 43.00112388903013,6.362806960421437 43.00112388903013,6.362806960421437 43.01537135435604))";
                //string bboxWkt = "POLYGON((6.362852061542235 43.008752396912236,6.377357447894774 43.008752396912236,6.377357447894774 43.00206770457925,6.362852061542235 43.00206770457925,6.362852061542235 43.008752396912236))";
                //string bboxWkt = "POLYGON((6.373237574847899 43.004667107932384,6.3788594849431135 43.004667107932384,6.3788594849431135 43.00127755196608,6.373237574847899 43.00127755196608,6.373237574847899 43.004667107932384))";

                var bboxWkt = "POLYGON ((977978.21701945551 6217156.6239589937, 980875.37294482859 6217156.6239589937, 980875.37294482859 6219837.3511766745, 977978.21701945551 6219837.3511766745, 977978.21701945551 6217156.6239589937))";
                var b = GeometryService.GetBoundingBox(bboxWkt);
                b.SRID = 2154;
                b = b.ReprojectTo(2154, 4326);
                bboxWkt = b.WKT;

                GenerateModel(bboxWkt, litto3DDataset, ImageryProvider.EsriWorldImagery, zFactor, withTexture: true, withboat: true); 
                GenerateModel(bboxWkt, litto3DDataset, ImageryProvider.MapBoxSatellite, zFactor, withTexture: true, withboat: false);
                GenerateModel(bboxWkt, litto3DDataset, ImageryProvider.ThunderForestLandscape);
                GenerateModel(bboxWkt, litto3DDataset, ImageryProvider.OpenTopoMap);

            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        public void GenerateModel(string bboxWkt, DEMDataSet litto3DDataset, ImageryProvider imageryProvider, float zFactor = 3f, bool withTexture = true, bool withboat = true)
        {
            try
            {
                bool centerOnOrigin = true;
                int TEXTURE_TILES = 20;
                string outputDir = Directory.GetCurrentDirectory();

                string modelName = $"{litto3DDataset.ResolutionMeters}m_z{zFactor}_{imageryProvider.Name}-{DateTime.Now:yyyyMMdd-hhmmss}";

                _logger.LogInformation($"Getting height map data...");


                var bbox = GeometryService.GetBoundingBox(bboxWkt).ReprojectTo(4326, litto3DDataset.SRID);
                var heightMap = _elevationService.GetHeightMap(ref bbox, litto3DDataset);
                ModelGenerationTransform transform = new ModelGenerationTransform(bbox, 3857, centerOnOrigin: centerOnOrigin, zFactor, centerOnZOrigin: false);

                _logger.LogInformation($"Processing height map data ({heightMap.Count} coordinates)...");
                heightMap = transform.TransformHeightMap(heightMap);

                var min = heightMap.Coordinates.Where(g => (g.Elevation ?? 0d) > litto3DDataset.NoDataValue).Min(g => g.Elevation ?? 0d);
                heightMap.Coordinates = heightMap.Coordinates.Select(p =>
                {
                    if (p.Elevation.GetValueOrDefault(0) <= litto3DDataset.NoDataValue)
                    {
                        p.Elevation = min;
                    }
                    return p;
                });

                //=======================
                // Textures
                //
                PBRTexture pbrTexture = null;
                if (withTexture)
                {
                    var bbox4326 = bbox.ReprojectTo(2154, 4326);
                    Console.WriteLine("Download image tiles...");
                    TileRange tiles = _imageryService.DownloadTiles(bbox4326, imageryProvider, TEXTURE_TILES);
                    string fileName = Path.Combine(outputDir, "Texture.jpg");

                    Console.WriteLine("Construct texture...");
                    TextureInfo texInfo = _imageryService.ConstructTexture(tiles, bbox4326, fileName, TextureImageFormat.image_jpeg);

                    //
                    //=======================

                    //=======================
                    // Normal map
                    Console.WriteLine("Height map...");
                    //float Z_FACTOR = 0.00002f;

                    //hMap = hMap.CenterOnOrigin().ZScale(Z_FACTOR);
                    //var normalMap = _imageryService.GenerateNormalMap(heightMap, outputDir);

                    pbrTexture = PBRTexture.Create(texInfo, null);// normalMap);

                    //hMap = hMap.CenterOnOrigin(Z_FACTOR);
                    //
                    //=======================
                }
                // Triangulate height map
                // and add base and sides
                _logger.LogInformation($"Triangulating height map and generating 3D mesh...");

                heightMap = heightMap.BakeCoordinates();
                var coords = heightMap.Coordinates.ToList();

                var model = _sharpGltfService.CreateTerrainMesh(heightMap, pbrTexture);

                var bottomLeft = coords[heightMap.Width * (heightMap.Height - 1)].AsVector3(); bottomLeft.Z = 0;
                var topRight = coords[heightMap.Width - 1].AsVector3(); topRight.Z = 0;
                var topLeft = coords[0].AsVector3(); topLeft.Z = 0;
                var bottomRight = coords.Last().AsVector3(); bottomRight.Z = 0;
                
                var waterSurface = _meshService.CreateWaterSurface(bottomLeft, topRight, topLeft, bottomRight,
                                        minZ: (float)min,
                                        color: VectorsExtensions.CreateColor(0, 150, 255, 64));
                model = _sharpGltfService.AddMesh(model, "Water", waterSurface, doubleSided: true);

                if (withboat)
                {
                    var boatInitPos = centerOnOrigin ? new GeoPoint(0,0).AsVector3() : new GeoPoint(43.010625204304304, 6.3711613671060086).ReprojectTo(4326, 3857).AsVector3();
                    var axis = _meshService.CreateAxis(2, 10, 3, 3).Translate(boatInitPos);
                    model = _sharpGltfService.AddMesh(model, "Boat", axis, doubleSided: false);
                }

                model.SaveGLB(Path.Combine(Directory.GetCurrentDirectory(), modelName + ".glb"));



                _logger.LogInformation($"Model exported as {Path.Combine(Directory.GetCurrentDirectory(), modelName + ".gltf")} and .glb");

                //var point = new GeoPoint(43.01142119356318, 6.385200681010872).ReprojectTo(4326, 2154);
                //point = _elevationService.GetPointElevation(point, litto3DDataset);

            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        private DEMDataSet GetLitto3D_1Metre(string datasetPath, bool firstRun)
        {
            var dataSetLitto3D = new DEMDataSet()
            {
                Name = "Litto3D_PortCros",
                Description = "Litto3D_PortCros",
                PublicUrl = null,
                DataSource = new LocalFileSystem(datasetPath),
                FileFormat = new DEMFileDefinition("ASCIIGrid", DEMFileType.ASCIIGrid, ".asc", DEMFileRegistrationMode.Cell),
                ResolutionMeters = 1,
                SRID = Reprojection.SRID_PROJECTED_LAMBERT_93,
                Attribution = new Attribution("Dataset", "Litto3D", "www.shom.fr", "Licence ouverte Etalab"),
                NoDataValue = -99999
            };
                _rasterService.GenerateDirectoryMetadata(dataSetLitto3D, force: firstRun);
            return dataSetLitto3D;
        }
        private DEMDataSet GetLitto3D_5Metres(string datasetPath, bool firstRun)
        {
            var dataSetLitto3D = new DEMDataSet()
            {
                Name = "Litto3D_PortCros5m",
                Description = "Litto3D_PortCros5m",
                PublicUrl = null,
                DataSource = new LocalFileSystem(datasetPath),
                FileFormat = new DEMFileDefinition("ASCIIGrid", DEMFileType.ASCIIGrid, ".asc", DEMFileRegistrationMode.Cell),
                ResolutionMeters = 5,
                SRID = Reprojection.SRID_PROJECTED_LAMBERT_93,
                Attribution = new Attribution("Dataset", "Litto3D", "www.shom.fr", "Licence ouverte Etalab"),
                NoDataValue = -99999
            };
            if (firstRun)
            {
                _rasterService.GenerateDirectoryMetadata(dataSetLitto3D, force: firstRun);
            }
            return dataSetLitto3D;
        }




    }
}
