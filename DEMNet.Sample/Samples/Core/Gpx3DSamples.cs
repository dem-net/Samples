﻿//
// Gpx3DSamples.cs
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
using DEM.Net.Core.Datasets;
using DEM.Net.Core.Imagery;
using DEM.Net.glTF.SharpglTF;
using Microsoft.Extensions.Logging;
using SharpGLTF.Schema2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace SampleApp
{

    public class Gpx3DSamples
    {
        private readonly ILogger<Gpx3DSamples> _logger;
        private readonly RasterService _rasterService;
        private readonly ElevationService _elevationService;
        private readonly SharpGltfService _sharpGltfService;
        private readonly ImageryService _imageryService;

        public Gpx3DSamples(ILogger<Gpx3DSamples> logger
                , RasterService rasterService
                , ElevationService elevationService
                , SharpGltfService sharpGltfService
                , ImageryService imageryService)
        {
            _logger = logger;
            _rasterService = rasterService;
            _elevationService = elevationService;
            _sharpGltfService = sharpGltfService;
            _imageryService = imageryService;
        }


        internal void Run(DEMDataSet dataSet, bool trackIn3D = true, bool generateTIN = false, int outputSrid = Reprojection.SRID_PROJECTED_LAMBERT_93)
        {
            try
            {
                string _gpxFile = Path.Combine("SampleData", "GPX", "lake-pleasant-camping.gpx");
                bool withTexture = true;
                float Z_FACTOR = 1.8f;
                float Z_TRANSLATE_GPX_TRACK_METERS = 75;
                float trailWidthMeters = 100f;
                int skipGpxPointsEvery = 1;
                float gpxBboxScale = 2f;
                float reduceFactor = 0.5f;// 0.75f
                int zoomLevel = 12;
                int resolution = 1024;

                ImageryProvider provider = ImageryProvider.EsriWorldImagery; // new TileDebugProvider(null, maxDegreeOfParallelism: 1);//  ImageryProvider.MapBoxSatellite;

                string outputDir = Path.GetFullPath(".");

                //=======================
                /// Line strip from GPX
                ///
                // Get GPX points
                var segments = GpxImport.ReadGPX_Segments(_gpxFile);
                var points = segments.SelectMany(seg => seg);
                var bbox = points.GetBoundingBox()
                    .Scale(gpxBboxScale, gpxBboxScale)
                    .ReprojectTo(4326,dataSet.SRID);
                // DEBUG
                // Test case : ASTER GDEMv3 : 5.5 43.5 Z=315
                // 303     307     308
                // 309    *315*    317
                // 314     321     324
                //points = GenerateDebugTrailPointsGenerateDebugTrailPoints(5.003, 5.006, 43.995, 43.997, 0.0001, 0.001);
                //points = GenerateDebugTrailPointsGenerateDebugTrailPoints(5.4990, 5.501, 43.4990, 43.501, 0.0001, 0.001);
                //points = GenerateDebugTrailPointsGenerateDebugTrailPoints(5.49, 5.51, 43.49, 43.51, 0.0005, 0.001);
                //bbox = points.GetBoundingBox().Scale(1.3,1.3);
                IEnumerable<GeoPoint> gpxPointsElevated = _elevationService.GetPointsElevation(points, dataSet);


                //
                //=======================

                //=======================
                /// Height map (get dem elevation for bbox)
                ///
                HeightMap hMap = _elevationService.GetHeightMap(ref bbox, dataSet);

                //                var refPoint = new GeoPoint(43.5, 5.5);
                //                hMap = hMap.BakeCoordinates();
                //                var hMapRefPoint = hMap.Coordinates.OrderBy(c => c.DistanceSquaredTo(refPoint)).First();
                //                var gpxRefPoint = gpxPointsElevated.OrderBy(c => c.DistanceSquaredTo(refPoint)).First();
                //                hMapRefPoint.Elevation += 60;
                //                gpxRefPoint.Elevation += 60;

                hMap = hMap.ReprojectTo(dataSet.SRID, outputSrid)
                    //.CenterOnOrigin()
                    .ZScale(Z_FACTOR)
                    .BakeCoordinates();
                //
                //=======================

                //=======================
                // Textures
                //
                PBRTexture pbrTexture = null;
                if (withTexture)
                {


                    Console.WriteLine("Download image tiles...");
                    var tiles = _imageryService.ComputeBoundingBoxTileRangeForTargetResolution(bbox, provider, resolution, resolution);
                    tiles = _imageryService.DownloadTiles(tiles, provider);
                    //var tiles = _imageryService.ComputeBoundingBoxTileRangeForZoomLevel(bbox, provider, zoomLevel);
                    //tiles = _imageryService.DownloadTiles(tiles, provider);
                    string fileName = Path.Combine(outputDir, "Texture.jpg");

                    Console.WriteLine("Construct texture...");
                    //TextureInfo texInfo = _imageryService.ConstructTextureWithGpxTrack(tiles, bbox, fileName, TextureImageFormat.image_jpeg, gpxPointsElevated, false);
                    TextureInfo texInfo = _imageryService.ConstructTexture(tiles, bbox, fileName, TextureImageFormat.image_jpeg);

                    //
                    //=======================

                    //=======================
                    // Normal map
                    Console.WriteLine("Height map...");
                    //float Z_FACTOR = 0.00002f;

                    //hMap = hMap.CenterOnOrigin().ZScale(Z_FACTOR);
                    //var normalMap = _imageryService.GenerateNormalMap(hMap, outputDir);
                    //pbrTexture = PBRTexture.Create(texInfo, normalMap);

                    pbrTexture = PBRTexture.Create(texInfo, null);

                    //hMap = hMap.CenterOnOrigin(Z_FACTOR);
                    //
                    //=======================
                }


                //=======================
                // MESH 3D terrain
                Console.WriteLine("Height map...");

                Console.WriteLine("GenerateTriangleMesh...");
                //hMap = _elevationService.GetHeightMap(bbox, _dataSet);
                ModelRoot model = null;
                if (generateTIN)
                {

                    model = TINGeneration.GenerateTIN(hMap, 10d, _sharpGltfService, pbrTexture, outputSrid);

                }
                else
                {
                    //hMap = hMap.CenterOnOrigin().ZScale(Z_FACTOR);
                    // generate mesh with texture
                    model = _sharpGltfService.CreateTerrainMesh(hMap, pbrTexture, reduceFactor: reduceFactor);
                }

                if (trackIn3D)
                {
                    // take 1 point evert nth
                    gpxPointsElevated = gpxPointsElevated.Where((x, i) => (i + 1) % skipGpxPointsEvery == 0);
                    gpxPointsElevated = gpxPointsElevated.ZTranslate(Z_TRANSLATE_GPX_TRACK_METERS)
                                                            .ReprojectTo(dataSet.SRID, outputSrid)
                                                            //.CenterOnOrigin()
                                                            //.CenterOnOrigin(hMap.BoundingBox)
                                                            .ZScale(Z_FACTOR);


                    model = _sharpGltfService.AddLine(model, "GPX", gpxPointsElevated, VectorsExtensions.CreateColor(255,0,0,255), trailWidthMeters);

                }

                // model export
                Console.WriteLine("GenerateModel...");
                model.SaveGLB(Path.Combine(Directory.GetCurrentDirectory(), $"{GetType().Name} dst{dataSet.Name} TIN{generateTIN} Srid{outputSrid}.glb"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        private IEnumerable<GeoPoint> GenerateDebugTrailPointsGenerateDebugTrailPoints(double xmin, double xmax, double ymin, double ymax, double stepX, double stepY)
        {
            int xSign = 1;
            for (double y = ymax; y >= ymin; y -= stepY)
            {
                if (xSign == 1)
                {
                    for (double x = xmin; x <= xmax; x += stepX)
                    {
                        yield return new GeoPoint(y, x);
                    }
                    xSign = -xSign;
                }
                else
                {
                    for (double x = xmax; x >= xmin; x -= stepX)
                    {
                        yield return new GeoPoint(y, x);
                    }
                    xSign = -xSign;
                }

            }
        }

        public DEMDataSet GetUSGSNED(string datasetPath, bool firstRun)
        {
            var dst = new DEMDataSet()
            {
                Name = "USGS NED",
                Description = "USGS NED",
                PublicUrl = null,
                DataSource = new LocalFileSystem(datasetPath),
                FileFormat = new DEMFileDefinition("GeoTiff", DEMFileType.GEOTIFF, ".tif", DEMFileRegistrationMode.Cell),
                ResolutionMeters = 1,
                SRID = Reprojection.SRID_NAD83,
                Attribution = new Attribution("NED", "USGS NED", "https://www.sciencebase.gov/catalog/item/581d268ee4b08da350d5c59d", "USGS NED"),
                NoDataValue = -99999
            };
            if (firstRun)
            {
                _rasterService.GenerateDirectoryMetadata(dst, force: firstRun);
            }
            return dst;
        }
    }

}
