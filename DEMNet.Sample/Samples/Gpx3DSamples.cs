//
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

using AssetGenerator;
using AssetGenerator.Runtime;
using DEM.Net.glTF;
using DEM.Net.Core;
using DEM.Net.Core.Imagery;
using DEM.Net.Core.Services.Lab;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using DEM.Net.Core.Gpx;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace SampleApp
{

    public class Gpx3DSamples
    {
        private readonly ILogger<Gpx3DSamples> _logger;
        private readonly IRasterService _rasterService;
        private readonly IElevationService _elevationService;
        private readonly IglTFService _glTFService;
        private readonly IImageryService _imageryService;

        public Gpx3DSamples(ILogger<Gpx3DSamples> logger
                , IRasterService rasterService
                , IElevationService elevationService
                , IglTFService glTFService
                , IImageryService imageryService)
        {
            _logger = logger;
            _rasterService = rasterService;
            _elevationService = elevationService;
            _glTFService = glTFService;
            _imageryService = imageryService;
        }


        internal void Run(DEMDataSet dataSet, bool trackIn3D = true, bool generateTIN = false, int outputSrid = Reprojection.SRID_PROJECTED_LAMBERT_93)
        {
            try
            {
                string _gpxFile = Path.Combine("SampleData", "BikeRide.gpx");
                bool withTexture = true;
                float Z_FACTOR = 4f;
                float Z_TRANSLATE_GPX_TRACK_METERS = 5;
                float trailWidthMeters = 5f;
                int skipGpxPointsEvery = 1;
              
                ImageryProvider provider = new TileDebugProvider(maxDegreeOfParallelism: 1);//  ImageryProvider.MapBoxSatellite;

                List<MeshPrimitive> meshes = new List<MeshPrimitive>();
                string outputDir = Path.GetFullPath(".");

                //=======================
                /// Line strip from GPX
                ///
                // Get GPX points
                var segments = GpxImport.ReadGPX_Segments(_gpxFile);
                var points = segments.SelectMany(seg => seg);
                var bbox = points.GetBoundingBox().Scale(1.3, 1.3);
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
                HeightMap hMap = _elevationService.GetHeightMap(bbox, dataSet);
                bbox = hMap.BoundingBox;
                
//                var refPoint = new GeoPoint(43.5, 5.5);
//                hMap = hMap.BakeCoordinates();
//                var hMapRefPoint = hMap.Coordinates.OrderBy(c => c.DistanceSquaredTo(refPoint)).First();
//                var gpxRefPoint = gpxPointsElevated.OrderBy(c => c.DistanceSquaredTo(refPoint)).First();
//                hMapRefPoint.Elevation += 60;
//                gpxRefPoint.Elevation += 60;
                
                hMap = hMap.ReprojectTo(4326, outputSrid)
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
                    TileRange tiles = _imageryService.DownloadTiles(bbox, provider, 8);
                    string fileName = Path.Combine(outputDir, "Texture.jpg");

                    Console.WriteLine("Construct texture...");
                    TextureInfo texInfo = _imageryService.ConstructTextureWithGpxTrack(tiles, bbox, fileName, TextureImageFormat.image_jpeg, gpxPointsElevated, false);

                    //
                    //=======================

                    //=======================
                    // Normal map
                    Console.WriteLine("Height map...");
                    //float Z_FACTOR = 0.00002f;

                    //hMap = hMap.CenterOnOrigin().ZScale(Z_FACTOR);
                    var normalMap = _imageryService.GenerateNormalMap(hMap, outputDir);

                    pbrTexture = PBRTexture.Create(texInfo, normalMap);

                    //hMap = hMap.CenterOnOrigin(Z_FACTOR);
                    //
                    //=======================
                }


                //=======================
                // MESH 3D terrain
                Console.WriteLine("Height map...");

                Console.WriteLine("GenerateTriangleMesh...");
                MeshPrimitive triangleMesh = null;
                //hMap = _elevationService.GetHeightMap(bbox, _dataSet);
                if (generateTIN)
                {

                    triangleMesh = TINGeneration.GenerateTIN(hMap, 10d, _glTFService, pbrTexture, outputSrid);

                }
                else
                {
                    //hMap = hMap.CenterOnOrigin().ZScale(Z_FACTOR);
                    // generate mesh with texture
                    triangleMesh = _glTFService.GenerateTriangleMesh(hMap, null, pbrTexture);
                }
                meshes.Add(triangleMesh);

                if (trackIn3D)
                {
                    // take 1 point evert nth
                    gpxPointsElevated = gpxPointsElevated.Where((x, i) => (i + 1) % skipGpxPointsEvery == 0);
                    gpxPointsElevated = gpxPointsElevated.ZTranslate(Z_TRANSLATE_GPX_TRACK_METERS)
                                                            .ReprojectTo(4326, outputSrid)
                                                            //.CenterOnOrigin()
                                                            //.CenterOnOrigin(hMap.BoundingBox)
                                                            .ZScale(Z_FACTOR);


                    MeshPrimitive gpxLine = _glTFService.GenerateLine(gpxPointsElevated, new Vector4(0, 1, 0, 0.5f), trailWidthMeters);
                    meshes.Add(gpxLine);
                }

                // model export
                Console.WriteLine("GenerateModel...");
                Model model = _glTFService.GenerateModel(meshes, this.GetType().Name);
                _glTFService.Export(model, ".", $"{GetType().Name} dst{dataSet.Name} TIN{generateTIN} Srid{outputSrid}", false, true);
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
    }

}
