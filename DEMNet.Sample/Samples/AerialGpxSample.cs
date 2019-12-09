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

using DEM.Net.Core;
using DEM.Net.Core.Gpx;
using DEM.Net.Core.Imagery;
using DEM.Net.glTF.SharpglTF;
using Microsoft.Extensions.Logging;
using SharpGLTF.Schema2;
using SharpGLTF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace SampleApp
{
    public class AerialGpxSample
    {
        private readonly ILogger<Gpx3DSamples> _logger;
        private readonly IRasterService _rasterService;
        private readonly IElevationService _elevationService;
        private readonly IImageryService _imageryService;
        private readonly SharpGltfService _sharpGltfService;
        private readonly int outputSrid = Reprojection.SRID_PROJECTED_MERCATOR;
        private readonly int imageryNbTiles = 4;

        public AerialGpxSample(ILogger<Gpx3DSamples> logger
                , IRasterService rasterService
                , IElevationService elevationService
                , IImageryService imageryService
                , SharpGltfService sharpGltfService)
        {
            _logger = logger;
            _rasterService = rasterService;
            _elevationService = elevationService;
            _imageryService = imageryService;
            _sharpGltfService = sharpGltfService;
        }
        internal void Run(DEMDataSet largeDataSet, DEMDataSet localDataset)
        {

            try
            {
                string outputDir = Path.GetFullPath(".");
                string _gpxFile = Path.Combine("SampleData", "20191022-Puch-Pöllau.gpx");
                float Z_FACTOR = 2f;
                float trailWidthMeters = 5f;

                //=======================
                /// Line strip from GPX
                ///
                // Get GPX points
                var segments = GpxImport.ReadGPX_Segments<GpxTrackPoint>(_gpxFile, p => p);
                var pointsGpx = segments.SelectMany(seg => seg);
                var geoPoints = pointsGpx.ToGeoPoints();

                var model = _sharpGltfService.CreateNewModel();
                //var largeMesh = GetMeshFromGpxTrack(outputDir, largeDataSet, geoPoints
                //                                , bboxScale: 5
                //                                , zFactor: Z_FACTOR
                //                                , generateTIN: false
                //                                , tinPrecision: 500d
                //                                , drawGpxOnTexture: false
                //                                , ImageryProvider.OpenTopoMap);
                //meshes.Add(largeMesh);

                model = GetMeshFromGpxTrack(model, outputDir, localDataset, geoPoints
                                            , bboxScale: (1.3, 1.5)
                                            , zFactor: Z_FACTOR
                                            , generateTIN: false
                                            , tinPrecision: 50d
                                            , drawGpxOnTexture: true
                                            , ImageryProvider.OpenTopoMap);


                var gpxPoints = geoPoints.ReprojectGeodeticToCartesian().ZScale(Z_FACTOR);

                model = _sharpGltfService.AddLine(model, gpxPoints, new Vector4(0, 1, 0, 0.5f), trailWidthMeters);


                // model export
                Console.WriteLine("GenerateModel...");

                var node = model.LogicalNodes.First();
                pointsGpx = pointsGpx.ReprojectGeodeticToCartesian().ToList();
                // animations
                node = CreateAnimationFromGpx("GPX", node, pointsGpx, 1f);
                node = CreateAnimationFromGpx("GPX x500", node, pointsGpx, 500f);
                model.SaveGLB(Path.Combine(Directory.GetCurrentDirectory(), $"{GetType().Name}.glb"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

        }

        private Node CreateAnimationFromGpx(string name, Node node, IEnumerable<GpxTrackPoint> points, float timeFactor)
        {
            timeFactor = timeFactor <= 0f ? 1f : timeFactor;
            GpxTrackPoint initialPoint = points.First();
            Vector3 initialPointVec3 = initialPoint.ToGeoPoint().ToVector3();

            var curve = points
                .Select(p => ((float)(p.Time.Value - initialPoint.Time.Value).TotalSeconds / timeFactor
                                , (initialPointVec3 - p.ToGeoPoint().ToVector3())))
                .ToArray();

            node = node.WithTranslationAnimation(name, curve);
            return node;
        }

        internal ModelRoot GetMeshFromGpxTrack(ModelRoot model, string outputDir, DEMDataSet dataSet, IEnumerable<GeoPoint> gpxPoints4326, (double x, double y) bboxScale, float zFactor, bool generateTIN,
            double tinPrecision, bool drawGpxOnTexture, ImageryProvider imageryProvider)
        {
            using (TimeSpanBlock chrono = new TimeSpanBlock($"{nameof(AerialGpxSample)} {dataSet.Name}", _logger))
            {
                if (model == null)
                    model = _sharpGltfService.CreateNewModel();

                var bbox = gpxPoints4326.GetBoundingBox().Scale(bboxScale.x, bboxScale.y);

                //
                //=======================

                //=======================
                /// Height map (get dem elevation for bbox)
                ///
                HeightMap hMap = _elevationService.GetHeightMap(ref bbox, dataSet);

                hMap = hMap.ReprojectTo(4326, outputSrid)
                    //.CenterOnOrigin()
                    .ZScale(zFactor)
                    .BakeCoordinates();
                //
                //=======================

                //=======================
                // Textures
                //
                TextureInfo texInfo = null;
                if (imageryProvider != null)
                {
                    Console.WriteLine($"Download image tiles from {imageryProvider.Name}...");
                    TileRange tiles = _imageryService.DownloadTiles(bbox, imageryProvider, imageryNbTiles);
                    string fileName = Path.Combine(outputDir, $"Texture{imageryProvider.Name}.jpg");

                    Console.WriteLine($"{dataSet.Name} Construct texture...");

                    texInfo = drawGpxOnTexture ?
                           _imageryService.ConstructTextureWithGpxTrack(tiles, bbox, fileName, TextureImageFormat.image_jpeg, gpxPoints4326, false)
                           : _imageryService.ConstructTexture(tiles, bbox, fileName, TextureImageFormat.image_jpeg);
                }
                //
                //=======================

                //=======================
                // Normal map
                Console.WriteLine($"{dataSet.Name} Normal map...");

                var normalMap = _imageryService.GenerateNormalMap(hMap, outputDir, $"normalmap.png");

                PBRTexture pbrTexture = PBRTexture.Create(texInfo, normalMap);

                //=======================



                //=======================
                // MESH 3D terrain

                Console.WriteLine($"{dataSet.Name} GenerateTriangleMesh...");
                MeshPrimitive triangleMesh = null;
                //hMap = _elevationService.GetHeightMap(bbox, _dataSet);
                if (generateTIN)
                {

                    model = TINGeneration.GenerateTIN(hMap, tinPrecision, _sharpGltfService, pbrTexture, outputSrid);

                }
                else
                {
                    // generate mesh with texture
                    model = _sharpGltfService.AddTerrainMesh(model, hMap, pbrTexture);
                }

                return model;
            }

        }



    }
}
