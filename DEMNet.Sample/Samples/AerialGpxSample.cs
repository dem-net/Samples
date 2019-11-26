//
// AerialGpxSample.cs
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
using DEM.Net.Core;
using DEM.Net.Core.Imagery;
using DEM.Net.glTF;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace SampleApp
{
    public class AerialGpxSample
    {
        private readonly ILogger<Gpx3DSamples> _logger;
        private readonly IRasterService _rasterService;
        private readonly IElevationService _elevationService;
        private readonly IglTFService _glTFService;
        private readonly IImageryService _imageryService;

        public AerialGpxSample(ILogger<Gpx3DSamples> logger
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
                string _gpxFile = Path.Combine("SampleData", "20191022-Puch-Pöllau.gpx");
                bool withTexture = true;
                float Z_FACTOR = 4f;
                float Z_TRANSLATE_GPX_TRACK_METERS = 5;
                float trailWidthMeters = 5f;
                int skipGpxPointsEvery = 1;

                ImageryProvider provider = ImageryProvider.MapBoxSatellite; // new TileDebugProvider(null, maxDegreeOfParallelism: 1);//  ImageryProvider.MapBoxSatellite;

                List<MeshPrimitive> meshes = new List<MeshPrimitive>();
                string outputDir = Path.GetFullPath(".");

                //=======================
                /// Line strip from GPX
                ///
                // Get GPX points
                var segments = GpxImport.ReadGPX_Segments(_gpxFile);
                var points = segments.SelectMany(seg => seg);
                var bbox = points.GetBoundingBox();//.Scale(1.3, 1.3);


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
                    TileRange tiles = _imageryService.DownloadTiles(bbox, provider, 4);
                    string fileName = Path.Combine(outputDir, "Texture.jpg");

                    Console.WriteLine("Construct texture...");
                    TextureInfo texInfo = _imageryService.ConstructTextureWithGpxTrack(tiles, bbox, fileName, TextureImageFormat.image_jpeg, points, false);

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
                    points = points.Where((x, i) => (i + 1) % skipGpxPointsEvery == 0);
                    points = points.ReprojectTo(4326, outputSrid)
                                    //.CenterOnOrigin()
                                    //.CenterOnOrigin(hMap.BoundingBox)
                                    .ZScale(Z_FACTOR);


                    MeshPrimitive gpxLine = _glTFService.GenerateLine(points, new Vector4(0, 1, 0, 0.5f), trailWidthMeters);
                    meshes.Add(gpxLine);
                }

                // model export
                Console.WriteLine("GenerateModel...");
                Model model = _glTFService.GenerateModel(meshes, this.GetType().Name);
                _glTFService.Export(model, ".", $"{GetType().Name} dst{dataSet.Name} TIN{generateTIN} Srid{outputSrid}", true, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        
    }
}
