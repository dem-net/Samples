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

        public VisualTopoSample(ILogger<VisualTopoSample> logger
                , SharpGltfService gltfService)
        {
            _logger = logger;
            _gltfService = gltfService;
        }

        public void Run()
        {

            string vtopoFile = Path.Combine("SampleData", "VisualTopo", "topo asperge avec ruisseau.TRO");
            //string vtopoFile = Path.Combine("SampleData", "LA SALLE.TRO");

            VisualTopoModel model = VisualTopoParser.ParseFile(vtopoFile, Encoding.GetEncoding("ISO-8859-1"), decimalDegrees: true);
            CreateGraph(model);


            //VisualTopoModel modelTest = new VisualTopoModel();
            //var set = new VisualTopoSet();
            //set.Data.Add(new VisualTopoData() { Cap = 0, Entree = "A", Sortie = "A", Section = new BoundingBox(2, 2, 2, 2), Longueur = 0, Pente = 0 });
            //set.Data.Add(new VisualTopoData() { Cap = 10, Entree = "A", Sortie = "B", Section = new BoundingBox(2, 2, 2, 2), Longueur = 10, Pente = -10.0F });
            //set.Data.Add(new VisualTopoData() { Cap = 20, Entree = "B", Sortie = "C", Section = new BoundingBox(2, 2, 2, 2), Longueur = 15, Pente = -45.0F });
            //set.Data.Add(new VisualTopoData() { Cap = 20, Entree = "C", Sortie = "D", Section = new BoundingBox(2, 2, 2, 2), Longueur = 20, Pente = -90.0F });
            //set.Data.Add(new VisualTopoData() { Cap = 20, Entree = "D", Sortie = "E", Section = new BoundingBox(2, 2, 2, 2), Longueur = 50, Pente = -90.0F });
            //modelTest.Sets.Add(set);

            //ComputeVectors(modelTest);
            //foreach (var pt in modelTest.Sets.First().Data) Debug.WriteLine(pt.GlobalVector.ToString());
            //var gltfModel = _gltfService.AddLine(_gltfService.CreateNewModel(), "GPX", modelTest.Sets.First().Data.Select(d => d.GlobalGeoPoint), VectorsExtensions.CreateColor(255, 0, 0, 192), 0.25F);
            //gltfModel.SaveGLB("TopoViewGlob.glb");

            //ComputeVectorsUsingGraph(model);
            var b = GetBranches(model);
            var topo3DLine = GetBranchesVectors(model);

            var gltfModel = _gltfService.CreateNewModel();
            int i = 0;
            foreach( var line in topo3DLine)
            {
                gltfModel = _gltfService.AddLine(gltfModel, "GPX" + (i++), line, VectorsExtensions.CreateColor(255, 0, 0, 255), 0.25F);
            }
            gltfModel.SaveGLB("TopoViewGlobFull.glb");
        }

        private void CreateGraph(VisualTopoModel model)
        {
            Dictionary<string, Node<VisualTopoData>> nodesByName = new Dictionary<string, Node<VisualTopoData>>();
            Dictionary<int, Node<VisualTopoData>> nodesByIndex = new Dictionary<int, Node<VisualTopoData>>();
            int i = 0;

            foreach (var data in model.Sets.SelectMany(s => s.Data))
            {
                if (data.Entree == data.Sortie && data.Entree == model.Entree)
                {
                    var node = model.Graph.CreateRoot(data, data.Entree);
                    nodesByName[node.Key] = node;
                    nodesByIndex[i++] = node;
                }
                else
                {

                    var node = model.Graph.CreateNode(data, data.Sortie);
                    nodesByName[data.Entree].AddArc(node, data.Longueur);
                    nodesByName[node.Key] = node;
                    nodesByIndex[i++] = node;
                }
            }
        }
        
        private List<List<GeoPoint>> GetBranchesVectors(VisualTopoModel model)
        {
            List<List<GeoPoint>> branches = new List<List<GeoPoint>>();
            GetBranchesVectors(model.Graph.Root, branches, null, Vector3.Zero);
            return branches;
        }

        private void GetBranchesVectors(Node<VisualTopoData> node, List<List<GeoPoint>> branches, List<GeoPoint> current, Vector3 local)
        {

            var p = node.Model;
            var currentVec = Vector3.UnitX * p.Longueur;
            var matrix = Matrix4x4.CreateRotationZ((float)MathHelper.ToRadians(p.Cap))
                        * Matrix4x4.CreateRotationY((float)MathHelper.ToRadians(-p.Pente));
            currentVec = Vector3.Transform(currentVec, matrix);
            currentVec += local;
            p.GlobalVector = currentVec;
            p.GlobalGeoPoint = new GeoPoint(p.GlobalVector.X, p.GlobalVector.Y, p.GlobalVector.Z);

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

                        GetBranchesVectors(arc.Child, branches, current, node.Model.GlobalVector);
                    }
                    else
                    {
                        var newBranch = new List<GeoPoint>();
                        newBranch.Add(node.Model.GlobalGeoPoint);
                        GetBranchesVectors(arc.Child, branches, newBranch, node.Model.GlobalVector);
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

