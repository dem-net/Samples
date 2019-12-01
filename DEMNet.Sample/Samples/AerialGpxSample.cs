﻿//
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
using System.Threading.Tasks;

namespace SampleApp
{
    public class AerialGpxSample
    {
        private readonly ILogger<Gpx3DSamples> _logger;
        private readonly IRasterService _rasterService;
        private readonly IElevationService _elevationService;
        private readonly IglTFService _glTFService;
        private readonly IImageryService _imageryService;
        private readonly int outputSrid = Reprojection.SRID_PROJECTED_MERCATOR;
        private readonly int imageryNbTiles = 4;

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
                var segments = GpxImport.ReadGPX_Segments(_gpxFile);
                var points = segments.SelectMany(seg => seg);

                List<MeshPrimitive> meshes = new List<MeshPrimitive>();

                //var largeMesh = GetMeshFromGpxTrack(outputDir, largeDataSet, points
                //                                , bboxScale: 5
                //                                , zFactor: Z_FACTOR
                //                                , generateTIN: false
                //                                , tinPrecision: 500d
                //                                , drawGpxOnTexture: false
                //                                , ImageryProvider.OpenTopoMap);
                //meshes.Add(largeMesh);

                var localMesh = GetMeshFromGpxTrack(outputDir, localDataset, points
                                                , bboxScale: 1.3
                                                , zFactor: Z_FACTOR
                                                , generateTIN: false
                                                , tinPrecision: 500d
                                                , drawGpxOnTexture: true
                                                , null);
                //localMesh.Translate(0, 500, 0);
                meshes.Add(localMesh);


                var gpxPoints = points.ReprojectGeodeticToCartesian().ZScale(Z_FACTOR);
                MeshPrimitive gpxLine = _glTFService.GenerateLine(gpxPoints, new Vector4(0, 1, 0, 0.5f), trailWidthMeters);
                meshes.Add(gpxLine);


                // model export
                Console.WriteLine("GenerateModel...");
                Model model = _glTFService.GenerateModel(meshes, this.GetType().Name);


                List<AnimationChannel> channels = new List<AnimationChannel>();
                List<float> timeSteps = new List<float> { 0, 1, 2 };
                List<Vector3> translations = new List<Vector3> { new Vector3(0, 0, 0), new Vector3(0, 1000, 0), new Vector3(5000, 10000, 0) };
                AnimationSampler sampler = new LinearAnimationSampler<Vector3>(timeSteps, translations);
                AnimationChannelTarget target = new AnimationChannelTarget() { Node = model.GLTF.Scenes.First().Nodes.First(), Path = AnimationChannelTarget.PathEnum.TRANSLATION };
                AnimationChannel channel = new AnimationChannel() { Sampler = sampler, Target = target };
                channels.Add(channel);
                Animation animation = new Animation() { Name = "Test", Channels = channels };
                model.GLTF.Animations = Enumerable.Repeat(animation, 1);
                _glTFService.Export(model, ".", $"{GetType().Name}", exportglTF: true, exportGLB: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

        }

        internal MeshPrimitive GetMeshFromGpxTrack(string outputDir, DEMDataSet dataSet, IEnumerable<GeoPoint> gpxPoints4326, double bboxScale, float zFactor, bool generateTIN,
            double tinPrecision, bool drawGpxOnTexture, ImageryProvider imageryProvider)
        {
            using (TimeSpanBlock chrono = new TimeSpanBlock($"{nameof(AerialGpxSample)} {dataSet.Name}", _logger))
            {
                var bbox = gpxPoints4326.GetBoundingBox().Scale(bboxScale, bboxScale);

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

                    triangleMesh = TINGeneration.GenerateTIN(hMap, tinPrecision, _glTFService, pbrTexture, outputSrid);

                }
                else
                {
                    // generate mesh with texture
                    triangleMesh = _glTFService.GenerateTriangleMesh(hMap, null, pbrTexture);
                }

                return triangleMesh;
            }

        }



    }
}