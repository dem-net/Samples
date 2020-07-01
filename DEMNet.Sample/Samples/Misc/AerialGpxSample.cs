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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using DEM.Net.Core.IO.SensorLog;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Schema2;
using SharpGLTF.Scenes;

namespace SampleApp
{
    using VERTEX = SharpGLTF.Geometry.VertexTypes.VertexPosition;
    public class AerialGpxSample
    {
        private readonly ILogger<AerialGpxSample> _logger;
        private readonly RasterService _rasterService;
        private readonly ElevationService _elevationService;
        private readonly ImageryService _imageryService;
        private readonly SharpGltfService _sharpGltfService;
        private readonly int outputSrid = Reprojection.SRID_PROJECTED_MERCATOR;
        private readonly int imageryNbTiles = 4;

        public AerialGpxSample(ILogger<AerialGpxSample> logger
                , RasterService rasterService
                , ElevationService elevationService
                , ImageryService imageryService
                , SharpGltfService sharpGltfService)
        {
            _logger = logger;
            _rasterService = rasterService;
            _elevationService = elevationService;
            _imageryService = imageryService;
            _sharpGltfService = sharpGltfService;
        }


        internal void Run(DEMDataSet largeDataSet, DEMDataSet localDataset, bool useSensorLog)
        {

            if (useSensorLog)
            {
                RunSimpleAnimation("0", Vector3.Zero, Vector3.Zero);

                RunSimpleAnimation("1", new Vector3(10, 0, 10), Vector3.Zero);
                RunSimpleAnimation("2", Vector3.Zero, new Vector3(10, 0, 10));

                RunSensorLog(largeDataSet, localDataset);
            }
            else
            {
                RunGPX(largeDataSet, localDataset);
            }

        }

        private void RunSimpleAnimation(string name, Vector3 translation, Vector3 localTrans)
        {
            float xTrans = 10;
            float zTrans = 10;
            // create two materials

            var material1 = new MaterialBuilder()
                .WithDoubleSide(true)
                .WithMetallicRoughnessShader()
                .WithChannelParam("BaseColor", new Vector4(1, 0, 0, 1));

            var material2 = new MaterialBuilder()
                .WithDoubleSide(true)
                .WithMetallicRoughnessShader()
                .WithChannelParam("BaseColor", new Vector4(1, 0, 1, 1));

            // create a mesh with two primitives, one for each material

            var node = new NodeBuilder("Node");
            node.LocalTransform = Matrix4x4.CreateTranslation(localTrans);
            node = node.CreateNode();
            node.UseTranslation("track1")
                .WithPoint(0, translation)
                .WithPoint(1, translation);
            node.UseRotation("track1")
               .WithPoint(0, Quaternion.Identity)
               .WithPoint(0.5f, Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI))
               .WithPoint(1, Quaternion.CreateFromAxisAngle(Vector3.UnitY, 1.5f * MathF.PI));
            var mesh = new MeshBuilder<VERTEX>("mesh");

            var prim = mesh.UsePrimitive(material1);
            prim.AddTriangle(new VERTEX(-10 + xTrans, 0, 0 + zTrans), new VERTEX(10 + xTrans, 0, 0 + zTrans), new VERTEX(0 + xTrans, 0, 10 + zTrans));
            prim.AddTriangle(new VERTEX(10 + xTrans, 0, 0 + zTrans), new VERTEX(-10 + xTrans, 0, 0 + zTrans), new VERTEX(0 + xTrans, 0, -10 + zTrans));

            prim = mesh.UsePrimitive(material2);
            prim.AddQuadrangle(new VERTEX(-5 + xTrans, 3, 0 + zTrans), new VERTEX(0 + xTrans, 3, -5 + zTrans), new VERTEX(5 + xTrans, 3, 0 + zTrans), new VERTEX(0 + xTrans, 3, 5 + zTrans));

            // create a scene

            var scene = new SceneBuilder();

            scene.AddRigidMesh(mesh, node);
            // save the model in different formats

            var model = scene.ToGltf2(new SceneBuilderSchema2Settings());
            model.SaveGLB($"{name}.glb");
        }

        internal void RunSensorLog(DEMDataSet largeDataSet, DEMDataSet localDataset)
        {
            //// sensor log needs data filtering
            //// => some coordinates are null (not in the json data)
            //// => most accurate is RelativeElevation 
            ////      => this is height above initial point, need to sum with start elevation
            //try
            //{
            //    string outputDir = Path.GetFullPath(".");
            //    //string _gpxFile = Path.Combine("SampleData", "20191022-Puch-Pöllau.gpx");
            //    string sensorLogFile = Path.Combine("SampleData", "20191023-Puch-Pöllau-sensorlog.json");
            //    var sensorLog = SensorLog.FromJson(sensorLogFile);
            //    //sensorLog.Plot("sensorLog.png");
            //    string balloonModel = Path.Combine("SampleData", "OE-SOE.glb");
            //    float Z_FACTOR = 2f;
            //    float trailWidthMeters = 5f;


            //    ModelRoot balloon = ModelRoot.Load(balloonModel);

            //    //=======================
            //    /// Line strip from SensorLog
            //    ///
            //    var pointsGpx = sensorLog.ToGPX().ToList();
            //    var geoPoints = sensorLog.ToGeoPoints().ToList();

            //    var firstElevation = _elevationService.GetPointElevation(geoPoints.First(), localDataset);
            //    foreach (var p in pointsGpx) p.Elevation += firstElevation.Elevation.Value;
            //    foreach (var p in geoPoints) p.Elevation += firstElevation.Elevation.Value;

            //    var model = _sharpGltfService.CreateNewModel();
            //    //var largeMesh = GetMeshFromGpxTrack(outputDir, largeDataSet, geoPoints
            //    //                                , bboxScale: 5
            //    //                                , zFactor: Z_FACTOR
            //    //                                , generateTIN: false
            //    //                                , tinPrecision: 500d
            //    //                                , drawGpxOnTexture: false
            //    //                                , ImageryProvider.OpenTopoMap);
            //    //meshes.Add(largeMesh);

            //    model = GetMeshFromGpxTrack(model, outputDir, localDataset, geoPoints
            //                                , bboxScale: (1.05, 1.05)
            //                                , zFactor: Z_FACTOR
            //                                , generateTIN: false
            //                                , tinPrecision: 50d
            //                                , drawGpxOnTexture: true
            //                                , ImageryProvider.EsriWorldImagery);


            //    var gpxPoints = geoPoints.ReprojectGeodeticToCartesian().ZScale(Z_FACTOR);

            //    model = _sharpGltfService.AddLine(model, gpxPoints, new Vector4(0, 1, 0, 0.5f), trailWidthMeters);

            //    // model export
            //    Console.WriteLine("GenerateModel...");

            //    var node = model.LogicalNodes.First();
            //    pointsGpx = pointsGpx.ReprojectGeodeticToCartesian().ZScale(Z_FACTOR);
            //    // animations
            //    node = CreateAnimationFromGpx("GPX", node, pointsGpx, 1f);
            //    node = CreateAnimationFromGpx("GPX x500", node, pointsGpx, 500f);


            //    //var sceneBuilderBalloon = balloon.DefaultScene.ToSceneBuilder();

            //    //var sceneBuilderTerrain = model.DefaultScene.ToSceneBuilder();
            //    //sceneBuilderBalloon.



            //    model.SaveGLB(Path.Combine(Directory.GetCurrentDirectory(), $"{GetType().Name}.glb"));
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, ex.Message);
            //}

        }

        internal void RunGPX(DEMDataSet largeDataSet, DEMDataSet localDataset)
        {

            try
            {
                string outputDir = Path.GetFullPath(".");
                string _gpxFile = Path.Combine("SampleData", "20191022-Puch-Pöllau.gpx");
                string sensorLogFile = Path.Combine("SampleData", "20191023-Puch-Pöllau-sensorlog.json");
                var sensorLog = SensorLog.FromJson(sensorLogFile);
                //sensorLog.Plot("sensorLog.png");
                string balloonModel = Path.Combine("SampleData", "OE-SOE.glb");
                float Z_FACTOR = 2f;
                float trailWidthMeters = 5f;


                ModelRoot balloon = ModelRoot.Load(balloonModel);

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
                                            , ImageryProvider.EsriWorldImagery);


                var gpxPoints = geoPoints.ReprojectGeodeticToCartesian().ZScale(Z_FACTOR);

                model = _sharpGltfService.AddLine(model, "GPX", gpxPoints, new Vector4(0, 1, 0, 0.5f), trailWidthMeters);

                // model export
                Console.WriteLine("GenerateModel...");

                var node = model.LogicalNodes.First();
                pointsGpx = pointsGpx.ReprojectGeodeticToCartesian().ZScale(Z_FACTOR);
                // animations
                node = CreateAnimationFromGpx("GPX", node, pointsGpx, 1f);
                node = CreateAnimationFromGpx("GPX x500", node, pointsGpx, 500f);


                var sceneBuilderBalloon = balloon.DefaultScene.ToSceneBuilder();

                var sceneBuilderTerrain = model.DefaultScene.ToSceneBuilder();
                //sceneBuilderBalloon.



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

            var translationCurve = points
                .Select(p => ((float)(p.Time.Value - initialPoint.Time.Value).TotalSeconds / timeFactor
                                , (initialPointVec3 - p.ToGeoPoint().ToVector3())))
                .ToArray();

            node = node.WithTranslationAnimation(name, translationCurve);

            // return new Vector3((float)geoPoint.Longitude, (float)geoPoint.Elevation, -(float)geoPoint.Latitude);
            // up vector is (0,1,0)


            double? lastBearing = points.FirstOrDefault(p => p.Bearing.HasValue)?.Bearing;
            if (lastBearing.HasValue)
            {
                var rotationCurve = points
                    .Select(p =>
                    {
                        Matrix4x4 mat;
                        Quaternion quaternion = Quaternion.CreateFromAxisAngle(Vector3.UnitY, GetAngleRadians(lastBearing.Value, p.Bearing));
                        lastBearing = p.Bearing ?? lastBearing;
                        return ((float)((p.Time.Value - initialPoint.Time.Value).TotalSeconds / timeFactor)
                                        , quaternion);
                    })
                    .ToArray();

                node = node.WithRotationAnimation(name + "rot", rotationCurve);
            }
            return node;
        }

        private float GetAngleRadians(double angle1Deg, double? angle2Deg)
        {
            var angle = (float)((angle2Deg ?? angle1Deg) - angle1Deg);
            //_logger.LogInformation($"Angle {angle:F2}");
            return (float)(angle * Math.PI / 180d);
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
