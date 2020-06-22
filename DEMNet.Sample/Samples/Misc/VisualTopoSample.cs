//
// VisualTopoSample.cs
//
// Author:
//       Xavier Fischer 2020-6
//
// Copyright (c) 2020 Xavier Fischer
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
using DEM.Net.Core.Services.Lab;
using DEM.Net.Core.Services.VisualisationServices;
using DEM.Net.glTF.SharpglTF;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Microsoft.Extensions.Logging;
using Microsoft.Research.Science.Data;
using Newtonsoft.Json;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DEM.Net.Graph;
using DEM.Net.Graph.GenericWeightedGraph;
using SharpGLTF.Schema2;
using SixLabors.ImageSharp.PixelFormats;
using System.Drawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.ColorSpaces;
using DEM.Net.Core.Imagery;

namespace SampleApp
{
    /// <summary>
    /// VisualTopo integration
    /// Goal: generate 3D model from visual topo file
    /// </summary>
    public partial class VisualTopoSample
    {
        private readonly ILogger<VisualTopoSample> _logger;
        private readonly SharpGltfService _gltfService;
        private readonly ImageryService _imageryService;
        private readonly IElevationService _elevationService;

        public VisualTopoSample(ILogger<VisualTopoSample> logger
                , SharpGltfService gltfService
            , IElevationService elevationService
                , ImageryService imageryService)
        {
            _logger = logger;
            _gltfService = gltfService;
            _elevationService = elevationService;
            _imageryService = imageryService;
        }

        public void Run()
        {


            float zFactor = 3F;
            float lineWidth = 1.0F;
            float scaleMargin = 1.5F;
            DEMDataSet dataset = DEMDataSet.AW3D30;
            ImageryProvider provider = ImageryProvider.MapBoxSatellite;// new TileDebugProvider(new GeoPoint(43.5,5.5));
            int TEXTURE_TILES = 12; // 4: med, 8: high
            //string vtopoFile = Path.Combine("SampleData", "VisualTopo", "topo asperge avec ruisseau.TRO");
            string vtopoFile = Path.Combine("SampleData", "VisualTopo", "Olivier4326.TRO");
            //string vtopoFile = Path.Combine("SampleData", "VisualTopo", "topo asperge avec ruisseau - set1.TRO");
            //string vtopoFile = Path.Combine("SampleData", "VisualTopo", "LA SALLE.TRO");

            VisualTopoModel model = VisualTopoParser.ParseFile(vtopoFile, Encoding.GetEncoding("ISO-8859-1"), decimalDegrees: true, ignoreStars: true);
            CreateGraph(model);
            // Olivier Mag Dec : 1° 15.44' East
            var b = GetBranches(model); // for debug
            var topo3DLine = GetBranchesVectors(model, zFactor: 1f);

            var pt = new GeoPoint(43.5435507, 2.8975941).ReprojectTo(4326, 3857);
            pt = pt.ReprojectTo(3857, 4326);
            var entryPoint4326 = model.EntryPoint.Clone().ReprojectTo(model.SRID, dataset.SRID);
            SexagesimalAngle lat = SexagesimalAngle.FromDouble(entryPoint4326.Latitude);
            SexagesimalAngle lon = SexagesimalAngle.FromDouble(entryPoint4326.Longitude);

            // Z fixed to DEM
            _elevationService.DownloadMissingFiles(dataset, entryPoint4326);
            model.EntryPoint.Elevation = _elevationService.GetPointElevation(entryPoint4326, dataset).Elevation ?? 0;
            var origin = model.EntryPoint.Clone().ReprojectTo(model.SRID, 3857);

            IEnumerable<GeoPoint> Transform(IEnumerable<GeoPoint> line)
            {
                var newLine = line.Translate(origin).ReprojectTo(model.SRID, 3857);
                return newLine;
            };

            var gltfModel = _gltfService.CreateNewModel();
            int i = 0;
            var rnd = new Random();
            foreach (var line in topo3DLine)
            {
                //var color = VectorsExtensions.CreateColor((byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255));
                var color = VectorsExtensions.CreateColor(255, 0, 0, 255);
                gltfModel = _gltfService.AddLine(gltfModel, "GPX" + (i++), Transform(line), color, lineWidth);
            }
            var repereX = new List<GeoPoint>() { GeoPoint.Zero, GeoPoint.UnitX * 50F };
            var repereY = new List<GeoPoint>() { GeoPoint.Zero, GeoPoint.UnitY * 50F };
            var repereZ = new List<GeoPoint>() { GeoPoint.Zero, GeoPoint.UnitZ * 50F };
            gltfModel = _gltfService.AddLine(gltfModel, "X", Transform(repereX), VectorsExtensions.CreateColor(255, 0, 0, 255), 5F);
            gltfModel = _gltfService.AddLine(gltfModel, "Y", Transform(repereY), VectorsExtensions.CreateColor(0, 255, 0, 255), 5F);
            gltfModel = _gltfService.AddLine(gltfModel, "Z", Transform(repereZ), VectorsExtensions.CreateColor(0, 0, 255, 255), 5F);

            var lineString = "LINESTRING (2.9010048 43.5436306, 2.9011189 43.5436159, 2.9011484 43.5435731, 2.9011376 43.5435226, 2.9009874 43.5432776, 2.9009096 43.5430948, 2.9008533 43.5429237, 2.900805 43.5426671, 2.9007568 43.5425446, 2.9006629 43.5423366, 2.9005422 43.5421966, 2.9002123 43.5418583, 2.9001989 43.5417941, 2.9002364 43.541763, 2.9003196 43.541763, 2.9004107 43.5418, 2.9006548 43.5419691, 2.9010089 43.5421849, 2.9014193 43.5424921, 2.90154 43.5425621, 2.9016446 43.5426166, 2.9016714 43.5426535, 2.9017009 43.5427604, 2.9017519 43.5428674, 2.9018913 43.5430579, 2.902122 43.5433165, 2.9023554 43.5435712, 2.9024465 43.5437073, 2.9026343 43.544065, 2.9027094 43.5441777, 2.9028006 43.5442847, 2.9028113 43.5443469, 2.9027711 43.5443974, 2.9026933 43.5444344, 2.9024331 43.5444772, 2.9021086 43.5445646, 2.9020415 43.5446152, 2.902012 43.5446832, 2.9020228 43.5447688, 2.9020737 43.5448621, 2.902181 43.5449982, 2.9023983 43.5451945, 2.9026772 43.5453948, 2.9029749 43.5455834, 2.9031439 43.5456689, 2.9033478 43.5457525, 2.9034148 43.5457856, 2.9034685 43.5458303, 2.9035892 43.5460364, 2.9036482 43.5461005, 2.9037555 43.5461763, 2.9039727 43.5462891, 2.9045011 43.5465457, 2.9046621 43.546604, 2.9049437 43.546674, 2.9051717 43.5466954, 2.9053836 43.5466954, 2.9055874 43.5466662, 2.9057913 43.5466157, 2.9059737 43.5465496, 2.9061829 43.5464427, 2.9063545 43.5463824, 2.9070599 43.5461919, 2.9073684 43.5460791, 2.9075132 43.5460033, 2.9076554 43.5459255, 2.907811 43.545875, 2.9078914 43.5458303, 2.908098 43.5456689, 2.9081623 43.5456514, 2.9082508 43.5456475, 2.9083689 43.545667, 2.908798 43.5457234, 2.9089455 43.5457311, 2.9091843 43.5457331, 2.9095329 43.545735, 2.9099219 43.5457506, 2.9100399 43.5457428, 2.9105146 43.5456456, 2.9106112 43.5456028, 2.9107319 43.5455484, 2.9108204 43.5455289, 2.9111262 43.5455017, 2.9113381 43.5454939, 2.9115151 43.5455056, 2.9116653 43.5455231, 2.9119282 43.5455834, 2.912191 43.545665, 2.9125424 43.5458147, 2.912647 43.5458361, 2.9127543 43.54584, 2.9128884 43.5458303, 2.9130118 43.545805, 2.91328 43.5457409, 2.9134195 43.5457253, 2.913736 43.545702, 2.9138674 43.5456806, 2.9141168 43.5456203, 2.9144897 43.54556, 2.9147257 43.5455289, 2.9151844 43.5454434, 2.9155116 43.5453929, 2.915702 43.5454026, 2.9158361 43.5454376, 2.9160588 43.5455173, 2.9164155 43.5456884, 2.9166971 43.5458575, 2.9168125 43.54591, 2.9172175 43.5460558, 2.9173221 43.5461102, 2.917703 43.5463435, 2.9182716 43.5467071, 2.9183574 43.546746, 2.9184727 43.5467673, 2.9187088 43.5467596, 2.918867 43.5467401, 2.9190655 43.546709, 2.9194491 43.5466196, 2.9195805 43.546604, 2.9197441 43.5465982, 2.9198809 43.5465788, 2.9200687 43.5465302, 2.9209672 43.5462891, 2.9211013 43.5462774, 2.9213937 43.5462697, 2.9215653 43.5462463, 2.9216833 43.5462074, 2.921796 43.5461433, 2.921855 43.5460558, 2.9220481 43.5457856, 2.9221635 43.5456728, 2.9223727 43.5455076, 2.9224961 43.5454473, 2.9226463 43.5453948, 2.923121 43.5452995, 2.9232927 43.5452859, 2.9239364 43.545282, 2.9246526 43.5452743, 2.9249637 43.5452937, 2.9250549 43.5452937, 2.9251649 43.5452781, 2.9253177 43.5452334, 2.9254358 43.545177, 2.9257281 43.5449807, 2.9259373 43.5448777, 2.926278 43.544761, 2.9266763 43.5446763)";
            //_elevationService.GetLineGeometryElevation(lineString, dataset);
            var linePoints = GeometryService.ParseWKTAsGeometry(lineString).Segments().Select(s => s.Start).ZTranslate(350).ReprojectTo(4326, 3857);
            gltfModel = _gltfService.AddLine(gltfModel, "Road", linePoints, VectorsExtensions.CreateColor(0,0,255), lineWidth);

            gltfModel.SaveGLB(string.Concat(Path.GetFileNameWithoutExtension(vtopoFile) + "_TopoOnly.glb"));

            BoundingBox bbox = model.BoundingBox;
            bbox = bbox.Translate(model.EntryPoint.Longitude, model.EntryPoint.Latitude, model.EntryPoint.Elevation ?? 0)
                        .Pad(1000)
                        .ReprojectTo(model.SRID, dataset.SRID);          

            string outputDir = Directory.GetCurrentDirectory();
            var heightMap = _elevationService.GetHeightMap(ref bbox, dataset, true);

            _logger.LogInformation($"Processing height map data ({heightMap.Count} coordinates)...");

            heightMap = heightMap.ReprojectTo(dataset.SRID, 3857).BakeCoordinates();

            //if (_centerOnOrigin)
            //{
            //    hMap = hMap.CenterOnOrigin(this.BoundingBox.ReprojectTo(this.BoundingBox.SRID, _outputSrid), _centerOnZOrigin);
            //}
            //hMap = hMap.ZScale(_zFactor);

            //=======================
            // Textures
            //
            PBRTexture pbrTexture = null;
            bool withTexture = true;
            if (withTexture)
            {


                Console.WriteLine("Download image tiles...");
                TileRange tiles = _imageryService.DownloadTiles(bbox, provider, TEXTURE_TILES);
                string fileName = Path.Combine(outputDir, "Texture.jpg");

                Console.WriteLine("Construct texture...");
                var topoTexture = topo3DLine.First().Translate(model.EntryPoint).ReprojectTo(model.SRID, 4326);
                //TextureInfo texInfo = _imageryService.ConstructTexture(tiles, bbox, fileName, TextureImageFormat.image_jpeg);
                TextureInfo texInfo = _imageryService.ConstructTextureWithGpxTrack(tiles, bbox, fileName, TextureImageFormat.image_jpeg
                    , topoTexture, false);

                //
                //=======================

                //=======================
                // Normal map
                Console.WriteLine("Height map...");
                //float Z_FACTOR = 0.00002f;

                //hMap = hMap.CenterOnOrigin().ZScale(Z_FACTOR);
                TextureInfo normalMap = null; //_imageryService.GenerateNormalMap(heightMap, outputDir);

                pbrTexture = PBRTexture.Create(texInfo, normalMap);

                //hMap = hMap.CenterOnOrigin(Z_FACTOR);
                //
                //=======================
            }
            // Triangulate height map
            // and add base and sides
            _logger.LogInformation($"Triangulating height map and generating 3D mesh...");

            gltfModel = _gltfService.AddTerrainMesh(gltfModel, heightMap, pbrTexture);

            gltfModel.SaveGLB(string.Concat(Path.GetFileNameWithoutExtension(vtopoFile) + ".glb"));
        }

        public void ExportVTopoToGlTF()
        {
            //string vtopoFile = Path.Combine("SampleData", "VisualTopo", "topo asperge avec ruisseau.TRO");
            string vtopoFile = Path.Combine("SampleData", "VisualTopo", "LA SALLE.TRO");

            VisualTopoModel model = VisualTopoParser.ParseFile(vtopoFile, Encoding.GetEncoding("ISO-8859-1"), decimalDegrees: true, ignoreStars: true);
            CreateGraph(model);
            var b = GetBranches(model); // for debug
            var topo3DLine = GetBranchesVectors(model, zFactor: 1f);

            var origin = model.EntryPoint.Clone().ReprojectTo(model.SRID, 3857);
            var bbox = model.BoundingBox
                            .Translate(model.EntryPoint.Longitude, model.EntryPoint.Latitude, model.EntryPoint.Elevation ?? 0)
                            .ReprojectTo(model.SRID, 3857);

            IEnumerable<GeoPoint> Transform(IEnumerable<GeoPoint> line)
            {
                return line.Translate(origin);
            };

            var gltfModel = _gltfService.CreateNewModel();
            int i = 0;
            var rnd = new Random();
            foreach (var line in topo3DLine)
            {
                var color = VectorsExtensions.CreateColor((byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255), (byte)rnd.Next(0, 255));
                //var color = VectorsExtensions.CreateColor(255, 0, 0, 255);
                gltfModel = _gltfService.AddLine(gltfModel, "GPX" + (i++), Transform(line), color, 5F);
            }
            var repereX = new List<GeoPoint>() { GeoPoint.Zero, GeoPoint.UnitX * 10F };
            var repereY = new List<GeoPoint>() { GeoPoint.Zero, GeoPoint.UnitY * 10F };
            var repereZ = new List<GeoPoint>() { GeoPoint.Zero, GeoPoint.UnitZ * 10F };
            gltfModel = _gltfService.AddLine(gltfModel, "X", Transform(repereX), VectorsExtensions.CreateColor(255, 0, 0, 255), 0.5F);
            gltfModel = _gltfService.AddLine(gltfModel, "Y", Transform(repereY), VectorsExtensions.CreateColor(0, 255, 0, 255), 0.5F);
            gltfModel = _gltfService.AddLine(gltfModel, "Z", Transform(repereZ), VectorsExtensions.CreateColor(0, 0, 255, 255), 0.5F);
            gltfModel.SaveGLB(string.Concat(Path.GetFileNameWithoutExtension(vtopoFile) + ".glb"));
        }

        private void CreateGraph(VisualTopoModel model)
        {
            Dictionary<string, Node<VisualTopoData>> nodesByName = new Dictionary<string, Node<VisualTopoData>>();

            foreach (var data in model.Sets.SelectMany(s => s.Data))
            {
                if (data.Entree == data.Sortie && data.Entree == model.Entree)
                {
                    var node = model.Graph.CreateRoot(data, data.Entree);
                    nodesByName[node.Key] = node;
                }
                else
                {

                    var node = model.Graph.CreateNode(data, data.Sortie);
                    if (!nodesByName.ContainsKey(data.Entree))
                    {
                        // Début graphe disjoint
                        nodesByName[data.Entree] = node;
                    }
                    nodesByName[data.Entree].AddArc(node, data.Longueur);
                    nodesByName[node.Key] = node;
                }
            }
        }

        private List<List<GeoPoint>> GetBranchesVectors(VisualTopoModel model, float zFactor)
        {
            List<List<GeoPoint>> branches = new List<List<GeoPoint>>();
            GetBranchesVectors(model.Graph.Root, branches, null, Vector3.Zero, zFactor);
            return branches;
        }

        private void GetBranchesVectors(Node<VisualTopoData> node, List<List<GeoPoint>> branches, List<GeoPoint> current, Vector3 local, float zFactor)
        {

            var p = node.Model;
            var currentVec = Vector3.UnitX * p.Longueur;
            var matrix = Matrix4x4.CreateRotationY((float)MathHelper.ToRadians(-p.Pente)) * Matrix4x4.CreateRotationZ((float)MathHelper.ToRadians(p.Cap));
            currentVec = Vector3.Transform(currentVec, matrix);
            currentVec += local;
            p.GlobalVector = currentVec;
            p.GlobalGeoPoint = new GeoPoint(p.GlobalVector.X, p.GlobalVector.Y, p.GlobalVector.Z * zFactor);

            if (current == null) current = new List<GeoPoint>();
            if (node.Arcs.Count == 0)
            {
                current.Add(node.Model.GlobalGeoPoint);
                branches.Add(current);
                return;
            }
            else
            {
                bool firstArc = true;
                foreach (var arc in node.Arcs)
                {
                    if (firstArc)
                    {
                        firstArc = false;

                        current.Add(node.Model.GlobalGeoPoint);

                        GetBranchesVectors(arc.Child, branches, current, node.Model.GlobalVector, zFactor);
                    }
                    else
                    {
                        var newBranch = new List<GeoPoint>();
                        newBranch.Add(node.Model.GlobalGeoPoint);
                        GetBranchesVectors(arc.Child, branches, newBranch, node.Model.GlobalVector, zFactor);
                    }
                }
            }
        }
        private void GetBranches<T>(Node<VisualTopoData> node, List<List<T>> branches, List<T> current, Func<VisualTopoData, T> extractInfo)
        {
            if (current == null) current = new List<T>();

            T info = extractInfo(node.Model);
            if (node.Arcs.Count == 0)
            {
                current.Add(info);
                branches.Add(current);
                return;
            }
            else
            {
                bool firstArc = true;
                foreach (var arc in node.Arcs)
                {
                    if (firstArc)
                    {
                        firstArc = false;
                        current.Add(info);
                        GetBranches(arc.Child, branches, current, extractInfo);
                    }
                    else
                    {
                        var newBranch = new List<T>();
                        newBranch.Add(info);
                        GetBranches(arc.Child, branches, newBranch, extractInfo);
                    }
                }
            }
        }
        private List<List<string>> GetBranches(VisualTopoModel model)
        {
            List<List<string>> branches = new List<List<string>>();
            GetBranches(model.Graph.Root, branches, null, n => n.Sortie);
            return branches;
        }


    }


}

