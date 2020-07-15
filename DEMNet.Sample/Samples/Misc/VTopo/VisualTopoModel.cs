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
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using System.Runtime.InteropServices;
using DEM.Net.Core.Graph;
using System;

namespace SampleApp
{
    public class VisualTopoModel
    {
        public string Name { get; internal set; }
        public GeoPoint EntryPoint { get; internal set; }
        public string EntryPointProjectionCode { get; internal set; }

        public Graph<VisualTopoData> Graph { get; set; } = new Graph<VisualTopoData>();
        public List<VisualTopoSet> Sets { get; set; } = new List<VisualTopoSet>();

        public Dictionary<string, VisualTopoData> GlobalPosPerSortie { get; internal set; }
        public string Author { get; internal set; }
        public bool TopoRobot { get; internal set; }
        public Vector4 DefaultColor { get; internal set; }
        public string Entree { get; internal set; }
        public int SRID { get; internal set; }

        public BoundingBox BoundingBox
        {
            get
            {
                var bbox = new BoundingBox() { SRID = this.SRID };
                Graph.AllNodes.ForEach(n => { if (n.Model.GlobalGeoPoint != null) bbox.UnionWith(n.Model.GlobalGeoPoint.Longitude, n.Model.GlobalGeoPoint.Latitude, n.Model.GlobalGeoPoint.Elevation ?? 0); });
                return bbox;
            }
        }

        public List<List<GeoPointRays>> Topology3D { get; internal set; }
        public TriangulationList<Vector3> TriangulationFull3D { get; internal set; }
    }

    public class VisualTopoSet
    {
        public List<VisualTopoData> Data { get; private set; } = new List<VisualTopoData>();
        public string Name { get; internal set; }
        public Vector4 Color { get; internal set; }

        public override string ToString()
        {
            return $"Set {Name} with {Data.Count} data entries";
        }

        internal void Add(VisualTopoData topoData)
        {
            Data.Add(topoData);
            topoData.Set = this;
            topoData.IsSectionStart = this.Data.Count == 1;
        }
    }
    public class VisualTopoData
    {
        public string Comment { get; internal set; }
        public string Entree { get; internal set; }
        public string Sortie { get; internal set; }
        public float Longueur { get; internal set; }
        public float Cap { get; internal set; }
        public float Pente { get; internal set; }
        public (float left, float right, float up, float down) CutSection { get; internal set; }
        public Vector3 GlobalVector { get; internal set; }
        public GeoPointRays GlobalGeoPoint { get; internal set; }
        public bool IsRoot { get; internal set; }
        public VisualTopoSet Set { get; internal set; }
        public bool IsSectionStart { get; internal set; }
    }



}

