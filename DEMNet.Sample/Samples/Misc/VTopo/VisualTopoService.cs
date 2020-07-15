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
using DEM.Net.Core.Graph;
using DEM.Net.glTF.SharpglTF;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.ColorSpaces.Conversion;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Numerics;
using System.Text;

namespace SampleApp
{
    public class VisualTopoService
    {
        public VisualTopoModel LoadFile(string vtopoFile, Encoding encoding, bool decimalDegrees, bool ignoreRadialBeams, float zFactor)
        {
            var model = ParseFile(vtopoFile, encoding, decimalDegrees, ignoreRadialBeams);

            model = ComputeTopology(model, zFactor);

            return model;
        }

        private VisualTopoModel ParseFile(string vtopoFile, Encoding encoding, bool decimalDegrees, bool ignoreRadialBeams)
        {
            VisualTopoModel model = new VisualTopoModel();

            // ========================
            // Parsing
            using (StreamReader sr = new StreamReader(vtopoFile, encoding))
            {
                model = this.ParseHeader(model, sr);

                while (!sr.EndOfStream)
                {
                    model = this.ParseSet(model, sr, decimalDegrees, ignoreRadialBeams);
                }
            }

            return model;
        }
        private VisualTopoModel ComputeTopology(VisualTopoModel model, float zFactor)
        {
            // ========================
            // Graph
            CreateGraph(model);

            // ========================
            // 3D model
            Build3DTopology_Lines(model, zFactor);
            Build3DTopology_Triangulation(model, zFactor);

            return model;
        }

        private void CreateGraph(VisualTopoModel model)
        {
            Dictionary<string, Node<VisualTopoData>> nodesByName = new Dictionary<string, Node<VisualTopoData>>();

            foreach (var data in model.Sets.SelectMany(s => s.Data))
            {
                if (data.Entree == model.Entree && model.Graph.Root == null) // Warning! Entrance may not be the start node
                {
                    data.IsRoot = true;
                    var node = model.Graph.CreateRoot(data, data.Entree);
                    nodesByName[node.Key] = node;
                }

                if (data.Entree != data.Sortie)
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

        #region Graph Traversal (full 3D)
        private void Build3DTopology_Triangulation(VisualTopoModel model, float zFactor)
        {
            float minElevation = model.Graph.AllNodes.Min(n => n.Model.GlobalVector.Z);
            Func<Vector3, Vector4> colorFunc = (data) =>
            {
                float lerpAmout = minElevation == 0 ? 0 : Math.Abs(data.Z / minElevation);
                Hsv hsvColor = new Hsv(MathHelper.Lerp(0f, 360f, lerpAmout), 1, 1);
                var rgb = new ColorSpaceConverter().ToRgb(hsvColor);


                return new Vector4(rgb.R, rgb.G, rgb.B, 255);
                //return Vector4.Lerp(VectorsExtensions.CreateColor(0, 255, 255), VectorsExtensions.CreateColor(0, 255, 0), lerpAmout);
            };


            TriangulationList<Vector3> triangulation = new TriangulationList<Vector3>();
            GraphTraversal_Triangulation(model.Graph.Root, ref triangulation, model.Graph.Root.Model, zFactor, colorFunc);
            model.TriangulationFull3D = triangulation;
        }


        private void GraphTraversal_Triangulation(Node<VisualTopoData> node, ref TriangulationList<Vector3> triangulation, VisualTopoData local, float zFactor, Func<Vector3, Vector4> colorFunc)
        {

            var p = node.Model;

            if (node.Arcs.Count == 0) // leaf
            {
                Debug.Assert(triangulation.NumPositions > 0, "Triangulation should not be empty");

                // Make a rectangle perpendicual to direction centered on point (should be centered at human eye (y = 2m)
                AddCorridorRectangleSection(ref triangulation, p, p, triangulation.NumPositions - 4, isLeaf: true, colorFunc);
            }
            else
            {
                int posIndex = triangulation.NumPositions - 4;
                foreach (var arc in node.Arcs)
                {
                    AddCorridorRectangleSection(ref triangulation, p, arc.Child.Model, posIndex, false, colorFunc);
                    posIndex = triangulation.NumPositions - 4;

                    GraphTraversal_Triangulation(arc.Child, ref triangulation, p, zFactor, colorFunc);

                }
            }
        }
        /// <summary>
        /// // Make a rectangle perpendicual to direction centered on point (should be centered at human eye (y = 2m)
        /// </summary>
        /// <param name="triangulation"></param>
        /// <param name="current"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        private void AddCorridorRectangleSection(ref TriangulationList<Vector3> triangulation, VisualTopoData current, VisualTopoData nextData, int startIndex, bool isLeaf, Func<Vector3, Vector4> colorFunc)
        {
            Vector3 next = nextData.GlobalVector;
            GeoPointRays rays = current.GlobalGeoPoint;
            Vector3 direction = next - current.GlobalVector;
            if (direction == Vector3.Zero)
            {
                direction = Vector3.UnitZ * -1;
            }
            Vector3 side = Vector3.Normalize(Vector3.Cross(direction, Vector3.UnitY));
            if (IsInvalid(side)) // Vector3 is UnitY
            {
                side = Vector3.UnitX; // set it to UnitX
            }
            Vector3 up = Vector3.Normalize(Vector3.Cross(direction, side));

            if (IsInvalid(side) || IsInvalid(up))
            {
                return;
            }
            //var m = Matrix4x4.CreateWorld(next, direction, Vector3.UnitZ);

            var position = isLeaf ? next : current.GlobalVector;
            triangulation.Positions.Add(position - side * rays.Left - up * rays.Down);
            triangulation.Positions.Add(position - side * rays.Left + up * rays.Up);
            triangulation.Positions.Add(position + side * rays.Right + up * rays.Up);
            triangulation.Positions.Add(position + side * rays.Right - up * rays.Down);

            //Vector4 color = (colorIndex++) % 2 == 0 ? VectorsExtensions.CreateColor(0, 255, 0) : VectorsExtensions.CreateColor(0, 0, 255);

            triangulation.Colors.AddRange(Enumerable.Repeat(colorFunc(position), 4));

            // corridor sides
            if (triangulation.NumPositions > 4)
            {
                int i = startIndex; // triangulation.NumPositions - 8;
                int lastIndex = triangulation.NumPositions - 4;
                for (int n = 0; n < 4; n++)
                {
                    AddFace(ref triangulation, i + n, i + (n + 1) % 4
                                             , lastIndex + n, lastIndex + (n + 1) % 4);
                }
            }
        }

        private bool IsInvalid(Vector3 vector)
        {
            return float.IsNaN(vector.X) || float.IsNaN(vector.Y) || float.IsNaN(vector.Z)
                || float.IsInfinity(vector.X) || float.IsInfinity(vector.Y) || float.IsInfinity(vector.Z);
        }

        private void AddFace(ref TriangulationList<Vector3> triangulation, int i0, int i1, int i4, int i5)
        {
            // left side tri low
            triangulation.Indices.Add(i0);
            triangulation.Indices.Add(i4);
            triangulation.Indices.Add(i5);

            // left side tri high
            triangulation.Indices.Add(i0);
            triangulation.Indices.Add(i5);
            triangulation.Indices.Add(i1);
        }

        #endregion

        #region Graph Traversal (lines)

        private void Build3DTopology_Lines(VisualTopoModel model, float zFactor)
        {
            List<List<GeoPointRays>> branches = new List<List<GeoPointRays>>();
            GraphTraversal_Lines(model.Graph.Root, branches, null, Vector3.Zero, zFactor);
            model.Topology3D = branches;
        }

        private void GraphTraversal_Lines(Node<VisualTopoData> node, List<List<GeoPointRays>> branches, List<GeoPointRays> current, Vector3 local, float zFactor)
        {

            var p = node.Model;
            var direction = Vector3.UnitX * p.Longueur;
            var matrix = Matrix4x4.CreateRotationY((float)MathHelper.ToRadians(-p.Pente)) * Matrix4x4.CreateRotationZ((float)(Math.PI / 2f - MathHelper.ToRadians(p.Cap)));
            direction = Vector3.Transform(direction, matrix);
            p.GlobalVector = direction + local;
            p.GlobalGeoPoint = new GeoPointRays(p.GlobalVector.Y, p.GlobalVector.X, p.GlobalVector.Z * zFactor
                                                , Vector3.Normalize(direction)
                                                , p.Section.left, p.Section.right, p.Section.up, p.Section.down);

            if (current == null) current = new List<GeoPointRays>();
            if (node.Arcs.Count == 0) // leaf
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

                        GraphTraversal_Lines(arc.Child, branches, current, node.Model.GlobalVector, zFactor);
                    }
                    else
                    {
                        var newBranch = new List<GeoPointRays>();
                        newBranch.Add(node.Model.GlobalGeoPoint);
                        GraphTraversal_Lines(arc.Child, branches, newBranch, node.Model.GlobalVector, zFactor);
                    }
                }
            }
        }

        #endregion

        #region DebugBranches

        // Useful to debug : output graph as node names
        public List<List<string>> GetBranchesNodeNames(VisualTopoModel model)
        {
            List<List<string>> branches = new List<List<string>>();
            GetBranches(model.Graph.Root, branches, null, n => n.Sortie);
            return branches;
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

        #endregion

        #region Parsing

        private void ParseEntryHeader(VisualTopoModel model, string entry)
        {
            var data = entry.Split(',');
            model.Name = data[0];
            model.EntryPointProjectionCode = data[4];
            double factor = 1d;
            int srid = 0;
            switch (model.EntryPointProjectionCode)
            {
                case "UTM31":
                    factor = 1000d;
                    srid = 32631;
                    break;
                case "LT3":
                    factor = 1000d;
                    srid = 27573;
                    break;
                case "WGS84": factor = 1d; srid = 4326; break;
                case "WebMercator": factor = 1d; srid = 3857; break;
                default: throw new NotImplementedException($"Projection not {model.EntryPointProjectionCode} not implemented");
            };
            model.EntryPoint = new GeoPoint(
                double.Parse(data[2], CultureInfo.InvariantCulture) * factor
                , double.Parse(data[1], CultureInfo.InvariantCulture) * factor
                , double.Parse(data[3], CultureInfo.InvariantCulture));
            model.SRID = srid;
            model.EntryPoint = model.EntryPoint;
        }

        private VisualTopoModel ParseHeader(VisualTopoModel model, StreamReader sr)
        {

            sr.ReadUntil(string.IsNullOrWhiteSpace);
            var headerLines = sr.ReadUntil(string.IsNullOrWhiteSpace)
                                .Where(s => !string.IsNullOrWhiteSpace(s))
                                .Select(s => s.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries))
                                .ToDictionary(s => s[0], s => s[1]);
            if (headerLines.TryGetValue("Trou", out string trou))
            {
                ParseEntryHeader(model, trou);
            }
            if (headerLines.TryGetValue("Club", out string club))
            {
                model.Author = club;
            }
            if (headerLines.TryGetValue("Entree", out string entree))
            {
                model.Entree = entree;
            }
            if (headerLines.TryGetValue("Toporobot", out string toporobot))
            {
                model.TopoRobot = toporobot == "1";
            }
            if (headerLines.TryGetValue("Couleur", out string couleur))
            {
                model.DefaultColor = ParseColor(couleur);
            }
            return model;
        }

        private VisualTopoModel ParseSet(VisualTopoModel model, StreamReader sr, bool decimalDegrees, bool ignoreRadialBeams)
        {
            VisualTopoSet set = new VisualTopoSet();

            string setHeader = sr.ReadLine();

            if (setHeader.StartsWith("[Configuration "))
            {
                sr.ReadToEnd(); // skip until end of stream
                return model;
            }

            // Set header
            var data = setHeader.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var headerSlots = data[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            set.Color = this.ParseColor(headerSlots[headerSlots.Length - 3]);
            set.Name = data.Length > 1 ? data[1].Trim() : string.Empty;

            sr.Skip(1);
            var dataLine = sr.ReadLine();
            do
            {
                VisualTopoData topoData = new VisualTopoData();

                var parts = dataLine.Split(';');
                if (parts.Length > 1) topoData.Comment = parts[1].Trim();
                var slots = parts[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);

                Debug.Assert(slots.Length == 13);

                // Parse data line
                topoData = this.ParseData(topoData, slots, decimalDegrees, ignoreRadialBeams);
                if (topoData != null)
                {
                    set.Data.Add(topoData);
                }
                dataLine = sr.ReadLine();
            }
            while (dataLine != string.Empty);

            model.Sets.Add(set);

            return model;
        }

        private VisualTopoData ParseData(VisualTopoData topoData, string[] slots, bool decimalDegrees, bool ignoreRadialBeams)
        {
            const string DefaultSize = "0.125";

            topoData.Entree = slots[0];
            topoData.Sortie = slots[1];

            if (topoData.Sortie == "*" && ignoreRadialBeams)
                return null;

            topoData.Longueur = float.Parse(slots[2], CultureInfo.InvariantCulture);
            topoData.Cap = ParseAngle(float.Parse(slots[3], CultureInfo.InvariantCulture), decimalDegrees);
            topoData.Pente = ParseAngle(float.Parse(slots[4], CultureInfo.InvariantCulture), decimalDegrees);
            topoData.Section = (left: float.Parse(slots[5] == "*" ? DefaultSize : slots[5], CultureInfo.InvariantCulture),
                                right: float.Parse(slots[6] == "*" ? DefaultSize : slots[6], CultureInfo.InvariantCulture),
                                up: float.Parse(slots[8] == "*" ? DefaultSize : slots[8], CultureInfo.InvariantCulture),
                                down: float.Parse(slots[7] == "*" ? DefaultSize : slots[7], CultureInfo.InvariantCulture));

            return topoData;
        }

        private float ParseAngle(float degMinSec, bool decimalDegrees)
        {
            // 125 deg 30min is not 125,5 BUT 125,3
            if (decimalDegrees)
            {
                return degMinSec;
            }
            else
            {
                // sexagecimal

                float intPart = (float)Math.Truncate(degMinSec);
                float decPart = degMinSec - intPart;

                return intPart + MathHelper.Map(0f, 0.6f, 0f, 1f, Math.Abs(decPart), false) * Math.Sign(degMinSec);

            }
        }

        private Vector4 ParseColor(string rgbCommaSeparated)
        {
            if (rgbCommaSeparated == "Std")
                return VectorsExtensions.CreateColor(255, 255, 255);

            var slots = rgbCommaSeparated.Split(',')
                        .Select(s => byte.Parse(s))
                        .ToArray();
            return VectorsExtensions.CreateColor(slots[0], slots[1], slots[2]);
        }

        #endregion

    }



}

