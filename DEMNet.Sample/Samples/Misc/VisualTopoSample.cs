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
using DEM.Net.Core.Imagery;
using DEM.Net.glTF.SharpglTF;
using DEM.Net.Graph.GenericWeightedGraph;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Windows.Markup;

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

            int outputSRID = 3857;
            float zFactor = 2F;
            float lineWidth = 1.0F;
            float marginMeters = 1000;
            DEMDataSet dataset = DEMDataSet.AW3D30;
            ImageryProvider provider = ImageryProvider.MapBoxSatelliteStreet;// new TileDebugProvider(new GeoPoint(43.5,5.5));
            int TEXTURE_TILES = 12; // 4: med, 8: high
            //string vtopoFile = Path.Combine("SampleData", "VisualTopo", "topo asperge avec ruisseau.TRO");
            //string vtopoFile = Path.Combine("SampleData", "VisualTopo", "Olivier4326.TRO");
            //string vtopoFile = Path.Combine("SampleData", "VisualTopo", "topo asperge avec ruisseau - set1.TRO");
            string vtopoFile = Path.Combine("SampleData", "VisualTopo", "LA SALLE.TRO");

            VisualTopoModel model = VisualTopoParser.ParseFile(vtopoFile, Encoding.GetEncoding("ISO-8859-1"), decimalDegrees: true, ignoreStars: true);
            CreateGraph(model);
            // Olivier Mag Dec : 1° 15.44' East
            //var b = GetBranches(model); // for debug
            var topo3DLine = GetBranchesVectors(model, zFactor: 1f);
            var lowestPoint = model.Sets.Min(s => s.Data.Min(d => d.GlobalGeoPoint?.Elevation??0));

            var entryPoint4326 = model.EntryPoint.Clone().ReprojectTo(model.SRID, dataset.SRID);
            //SexagesimalAngle lat = SexagesimalAngle.FromDouble(entryPoint4326.Latitude);
            //SexagesimalAngle lon = SexagesimalAngle.FromDouble(entryPoint4326.Longitude);

            BoundingBox bbox = model.BoundingBox;
            bbox = bbox.Translate(model.EntryPoint.Longitude, model.EntryPoint.Latitude, model.EntryPoint.Elevation ?? 0)
                        .Pad(marginMeters)
                        .ReprojectTo(model.SRID, dataset.SRID);
            var heightMap = _elevationService.GetHeightMap(ref bbox, dataset, true);

            var bboxoutputSRID = bbox.ReprojectTo(dataset.SRID, outputSRID);

            // Z fixed to DEM
            _elevationService.DownloadMissingFiles(dataset, entryPoint4326);
            model.EntryPoint.Elevation = _elevationService.GetPointElevation(entryPoint4326, dataset).Elevation ?? 0;
            var origin = model.EntryPoint.Clone().ReprojectTo(model.SRID, outputSRID);

            IEnumerable<GeoPoint> Transform(IEnumerable<GeoPoint> line)
            {
                //var newLine = line.Translate(model.EntryPoint).ToList();
                //newLine = newLine.ReprojectTo(model.SRID, outputSRID).ToList();
                //newLine = newLine.CenterOnOrigin(bboxoutputSRID).ToList();
                var newLine = line.Translate(model.EntryPoint)
                                    .ReprojectTo(model.SRID, outputSRID)
                                    .ZScale(zFactor)
                                    .CenterOnOrigin(bboxoutputSRID);
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
            gltfModel = _gltfService.AddLine(gltfModel, "Axis", Transform(repereX), VectorsExtensions.CreateColor(255, 0, 0, 255), 5F);
            gltfModel = _gltfService.AddLine(gltfModel, "Axis", Transform(repereY), VectorsExtensions.CreateColor(0, 255, 0, 255), 5F);
            gltfModel = _gltfService.AddLine(gltfModel, "Axis", Transform(repereZ), VectorsExtensions.CreateColor(0, 0, 255, 255), 5F);

            gltfModel.SaveGLB(string.Concat(Path.GetFileNameWithoutExtension(vtopoFile) + "_TopoOnly.glb"));            

            string outputDir = Directory.GetCurrentDirectory();

            _logger.LogInformation($"Processing height map data ({heightMap.Count} coordinates)...");

            heightMap = heightMap.ReprojectTo(dataset.SRID, outputSRID)
                                .CenterOnOrigin(bboxoutputSRID)
                                .ZScale(zFactor)
                                .BakeCoordinates();
            
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
                
                TextureInfo texInfo = _imageryService.ConstructTexture(tiles, bbox, fileName, TextureImageFormat.image_jpeg);
                //var topoTexture = topo3DLine.First().Translate(model.EntryPoint).ReprojectTo(model.SRID, 4326);
                //TextureInfo texInfo = _imageryService.ConstructTextureWithGpxTrack(tiles, bbox, fileName, TextureImageFormat.image_jpeg
                //    , topoTexture, false);

                pbrTexture = PBRTexture.Create(texInfo, null);

                //
                //=======================
            }
            // Triangulate height map
            // and add base and sides
            _logger.LogInformation($"Triangulating height map and generating 3D mesh...");

            gltfModel = _gltfService.AddTerrainMesh(gltfModel, heightMap, pbrTexture);
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

