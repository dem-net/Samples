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
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
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

                // 5.98485,46.14738
                GeoPoint geoPoint = new GeoPoint(46.14738, 5.98485);
                geoPoint = geoPoint.ReprojectTo(4326, 2056);
                var fileName = @"C:\Users\xavier.fischer\Downloads\swissalti3d_2019_2487-1111_0.5_2056_5728.tif";
                using (IRasterFile raster = _rasterService.OpenFile(fileName, DEMFileType.GEOTIFF))
                {
                    FileMetadata metaData = raster.ParseMetaData(new DEMFileDefinition(DEMFileType.GEOTIFF, DEMFileRegistrationMode.Cell));

                    float elevation = raster.GetElevationAtPoint(metaData, 1000, 200);

                    elevation = raster.GetElevationAtPoint(metaData, 200, 1000);

                }
                float zFactor = 1.5f;
                string outputDir = Directory.GetCurrentDirectory();
                var codePath = string.Join(Path.DirectorySeparatorChar, outputDir.Split(Path.DirectorySeparatorChar).TakeWhile(s => s != "bin"));
                ImageryProvider imageryProvider = ImageryProvider.MapBoxSatellite;


                //DEMDataSet litto3DDataset1m = GetLitto3D_1Metre(Path.Combine("SampleData","MNT1m Large"), firstRun: false);
                DEMDataSet litto3DDataset5m = GetLitto3D_5Metres(Path.Combine(codePath, "SampleData", "MNT5m Large"), firstRun: false);
                DEMDataSet gebco2020 = GetGebco2020(@"D:\Data\ELEVATION_DO_NOT_DELETE\GEBCO2020\cote azur", false);
                DEMDataSet litto3DDataset = litto3DDataset5m;

                //var metadata = _rasterService.LoadManifestMetadata(litto3DDataset, false);
                //var mpoly = "MULTIPOLYGON(" + string.Join(",", metadata.Select(m => m.BoundingBox.ReprojectTo(litto3DDataset.SRID, 4326).WKT.Replace("POLYGON", ""))) + ")";

                // export line elevation
                //var lineElevated = _elevationService.GetLineGeometryElevation(GetGeoPointFromGeoJson(BoatCourseGeoJson).ReprojectTo(4326, 2154), litto3DDataset);
                //lineElevated = lineElevated.ReprojectTo(litto3DDataset.SRID, Reprojection.SRID_GEODETIC).ToList();
                //var metrics = lineElevated.ComputeMetrics();
                //StringBuilder sb = new StringBuilder();
                //sb.AppendLine(string.Join(";", "Latitude", "Longitude", "Elevation", "DistanceFromOriginMeters"));
                //foreach ( var pt in lineElevated)
                //{
                //    sb.AppendLine(string.Join(";", pt.Latitude, pt.Longitude, pt.Elevation ?? 0, pt.DistanceFromOriginMeters ?? 0));
                //}
                //File.WriteAllText("Elevation.csv", sb.ToString());

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



                //var bboxWkt = "POLYGON ((977978.21701945551 6217156.6239589937, 980875.37294482859 6217156.6239589937, 980875.37294482859 6219837.3511766745, 977978.21701945551 6219837.3511766745, 977978.21701945551 6217156.6239589937))";
                //var b = GeometryService.GetBoundingBox(bboxWkt);
                //b.SRID = 2154;
                //b = b.ReprojectTo(2154, 4326);
                //bboxWkt = b.WKT;
                //
                //

                // zone sympa identifiée avec FAU
                //var bboxWkt = "POLYGON((6.362128423047508 43.015629205135106,6.4158584340338365 43.015629205135106,6.4158584340338365 42.99190262031489,6.362128423047508 42.99190262031489,6.362128423047508 43.015629205135106))";

                // some missing tiles
                string bboxWkt = "POLYGON((6.358674615263018 43.02928802256248,6.379445641874346 43.02928802256248,6.379445641874346 43.014855514067236,6.358674615263018 43.014855514067236,6.358674615263018 43.02928802256248))";

                //// FULL Port-Cros
                //bboxWkt = "POLYGON((6.354481172211255 43.023253715296505,6.429325532562817 43.023253715296505,6.429325532562817 42.98458826852103,6.354481172211255 42.98458826852103,6.354481172211255 43.023253715296505))";

                //// PortCros + Levant - buggy
                //bboxWkt = "POLYGON((6.353542565309054 43.05907106004858,6.517307518922335 43.05907106004858,6.517307518922335 42.9827655965132,6.353542565309054 42.9827655965132,6.353542565309054 43.05907106004858))";

                //bboxWkt = "POLYGON((6.355278210872166 43.02187519105359,6.403000073665135 43.02187519105359,6.403000073665135 42.98421350408587,6.355278210872166 42.98421350408587,6.355278210872166 43.02187519105359))";

                // les medes
                bboxWkt = "POLYGON((6.237930076749669 43.02881092834541,6.244667785795079 43.02881092834541,6.244667785795079 43.02360302605932,6.237930076749669 43.02360302605932,6.237930076749669 43.02881092834541))";

                bboxWkt = "POLYGON((6.302766091236887 43.10689293065665,6.549958474049387 43.10689293065665,6.549958474049387 42.94023304061307,6.302766091236887 42.94023304061307,6.302766091236887 43.10689293065665))";

                //// zoom zone FAU
                //bboxWkt = "POLYGON((6.374880326536019 43.009262752246585,6.379128945615609 43.009262752246585,6.379128945615609 43.00643834657017,6.374880326536019 43.00643834657017,6.374880326536019 43.009262752246585))";

                GenerateModel(bboxWkt, litto3DDataset, ImageryProvider.EsriWorldImagery, zFactor, withTexture: true, withboat: false, withWaterSurface: true, fallbackDataset: gebco2020);
                GenerateModel(bboxWkt, litto3DDataset, ImageryProvider.MapBoxSatellite, zFactor, withTexture: true, withboat: true, withWaterSurface: true);
                GenerateModel(bboxWkt, litto3DDataset, ImageryProvider.ThunderForestLandscape);
                GenerateModel(bboxWkt, litto3DDataset, ImageryProvider.OpenTopoMap);

            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        private List<GeoPoint> GetGeoPointFromGeoJson(string geoJson)
        {
            FeatureCollection fc = JsonConvert.DeserializeObject<FeatureCollection>(geoJson);

            var lineCoords = ((LineString)fc.Features.First().Geometry).Coordinates.Select(c => new GeoPoint(c.Latitude, c.Longitude, 0.1)).ToList();
            return lineCoords;
        }



        public void GenerateModel(string bboxWkt, DEMDataSet litto3DDataset, ImageryProvider imageryProvider, float zFactor = 3f, bool withTexture = true, bool withboat = true, bool withWaterSurface = true, DEMDataSet fallbackDataset = null)
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

                heightMap = heightMap.BakeCoordinates();
                var nullCoordsEnumerator = heightMap.Coordinates.Where(c => (c.Elevation ?? litto3DDataset.NoDataValue) == litto3DDataset.NoDataValue);

                var interpolator = _elevationService.GetInterpolator(InterpolationMode.Bilinear);
                using (IRasterFile raster = _rasterService.OpenFile(@"D:\Data\ELEVATION_DO_NOT_DELETE\GEBCO2020\cote azur\gebco_2020_subset.tif", DEMFileType.GEOTIFF))
                using (RasterFileDictionary dic = new RasterFileDictionary())
                {
                    var metadata = raster.ParseMetaData(fallbackDataset.FileFormat);
                    dic.Add(metadata, raster);
                    foreach (var pt in nullCoordsEnumerator)
                    {
                        var proj = pt.ReprojectTo(litto3DDataset.SRID, fallbackDataset.SRID);
                        pt.Elevation = _elevationService.GetElevationAtPoint(raster, dic, metadata, proj.Latitude, proj.Longitude, 0, interpolator, NoDataBehavior.UseNoDataDefinedInDem);
                    }
                }

                var nullCoords = heightMap.Coordinates.Where(c => (c.Elevation ?? litto3DDataset.NoDataValue) == litto3DDataset.NoDataValue).ToList();
                var nullCoordsFallbackProj = nullCoords.ReprojectTo(litto3DDataset.SRID, fallbackDataset.SRID, nullCoords.Count);
                var nullCoordsFallbackProj2 = _elevationService.GetPointsElevation(nullCoordsFallbackProj, fallbackDataset, InterpolationMode.Bilinear, NoDataBehavior.UseNoDataDefinedInDem);

                ModelGenerationTransform transform = new ModelGenerationTransform(bbox, datasetSrid: 4326, outputSrid: 3857, centerOnOrigin, zFactor, centerOnZOrigin: false);
                ModelGenerationTransform transformFrom4326 = new ModelGenerationTransform(bbox.ReprojectTo(2154, 4326), 4326, 3857, centerOnOrigin, zFactor, centerOnZOrigin: false);

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
                    //TextureInfo texInfo = _imageryService.ConstructTexture(tiles, bbox4326, fileName, TextureImageFormat.image_jpeg);

                    TextureInfo texInfo;
                    if (withboat)
                    {
                        var trackPoints = GetGeoPointFromGeoJson(BoatCourseGeoJson);
                        texInfo = _imageryService.ConstructTextureWithGpxTrack(tiles, bbox4326, fileName, TextureImageFormat.image_jpeg, trackPoints, drawGpxVertices: true, color: Color.Green, 30);
                    }
                    else
                    {
                        texInfo = _imageryService.ConstructTexture(tiles, bbox4326, fileName, TextureImageFormat.image_jpeg);
                    }


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

                var model = _sharpGltfService.CreateTerrainMesh(heightMap, pbrTexture, reduceFactor: 0.75f);



                if (withWaterSurface)
                {
                    var bottomLeft = coords[heightMap.Width * (heightMap.Height - 1)].AsVector3(); bottomLeft.Z = 0;
                    var topRight = coords[heightMap.Width - 1].AsVector3(); topRight.Z = 0;
                    var topLeft = coords[0].AsVector3(); topLeft.Z = 0;
                    var bottomRight = coords.Last().AsVector3(); bottomRight.Z = 0;

                    var waterSurface = _meshService.CreateWaterSurface(bottomLeft, topRight, topLeft, bottomRight,
                                            minZ: (float)min,
                                            color: VectorsExtensions.CreateColor(0, 150, 255, 64));
                    model = _sharpGltfService.AddMesh(model, "Water", waterSurface, doubleSided: true);
                }

                if (withboat)
                {
                    var boatInitPos = centerOnOrigin ? new GeoPoint(0, 0).AsVector3() : new GeoPoint(43.010625204304304, 6.3711613671060086).ReprojectTo(4326, 3857).AsVector3();
                    var axis = _meshService.CreateAxis(2, 10, 3, 3).Translate(boatInitPos);
                    model = _sharpGltfService.AddMesh(model, "Boat", axis, doubleSided: false);
                    var boatCourse = transformFrom4326.TransformPoints(GetGeoPointFromGeoJson(BoatCourseGeoJson)).ToList(); //ReprojectGeodeticToCartesian().CenterOnOrigin().ToList();
                    model = _sharpGltfService.AddLine(model, "BoatCourse", boatCourse, VectorsExtensions.CreateColor(255, 0, 0, 128), 4);

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
            _rasterService.GenerateDirectoryMetadata(dataSetLitto3D, force: firstRun);

            return dataSetLitto3D;
        }
        private DEMDataSet GetGebco2020(string datasetPath, bool firstRun)
        {
            // D:\Data\ELEVATION_DO_NOT_DELETE\GEBCO2020\gebco_2020_n43.8079833984375_s42.176513671875_w4.47967529296875_e7.19329833984375.tif
            var dataset = new DEMDataSet()
            {
                Name = "GEBCO_2020",
                Description = "GEBCO_2020",
                PublicUrl = "https://www.gebco.net/data_and_products/gridded_bathymetry_data/#a1",
                DataSource = new LocalFileSystem(datasetPath),
                FileFormat = new DEMFileDefinition("GeoTiff", DEMFileType.GEOTIFF, ".tif", DEMFileRegistrationMode.Cell),
                ResolutionMeters = 464,
                SRID = Reprojection.SRID_GEODETIC,
                Attribution = new Attribution("Dataset", "GEBCO_2020", "https://www.gebco.net", "GEBCO Compilation Group (2020) GEBCO 2020 Grid (doi:10.5285/a29c5465-b138-234d-e053-6c86abc040b9)"),
                NoDataValue = -99999
            };
            _rasterService.GenerateDirectoryMetadata(dataset, force: firstRun);

            return dataset;
        }

        private const string BoatCourseGeoJsonOld = @"{
  'type': 'FeatureCollection',
  'features': [
    {
      'type': 'Feature',
      'properties': {},
      'geometry': {
        'type': 'LineString',
        'coordinates': [
          [
            6.376361846923828,
            43.00660074586711
          ],
          [
            6.37630820274353,
            43.00725978373205
          ],
          [
            6.375524997711182,
            43.00774621190648
          ],
          [
            6.375299692153931,
            43.00816202648563
          ],
          [
            6.3754069805145255,
            43.00879751125322
          ],
          [
            6.375975608825684,
            43.00897795634676
          ],
          [
            6.376737356185913,
            43.00882104759982
          ],
          [
            6.376866102218627,
            43.008318936916055
          ]
        ]
      }
    }
  ]
}";

        private const string BoatCourseGeoJson = @"{
  'type': 'FeatureCollection',
  'features': [
    {
      'type': 'Feature',
      'properties': {},
      'geometry': {
        'type': 'LineString',
        'coordinates': [
          [
            6.376361846923828,
            43.00660074586711
          ],
          [
            6.376383304595947,
            43.00692241999404
          ],
          [
            6.37630820274353,
            43.00725978373205
          ],
          [
            6.375524997711182,
            43.00774621190648
          ],
          [
            6.375299692153931,
            43.00816202648563
          ],
          [
            6.37530505657196,
            43.008562147291066
          ],
          [
            6.3754069805145255,
            43.00879751125322
          ],
          [
            6.375685930252075,
            43.00891519289609
          ],
          [
            6.375975608825684,
            43.00897795634676
          ],
          [
            6.376367211341858,
            43.00895049734498
          ],
          [
            6.376737356185913,
            43.00882104759982
          ],
          [
            6.376850008964539,
            43.008562147291066
          ],
          [
            6.376866102218627,
            43.008318936916055
          ]
        ]
      }
    }
  ]
}";



    }
}
