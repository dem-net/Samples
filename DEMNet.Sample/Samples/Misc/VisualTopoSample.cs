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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SampleApp
{
    /// <summary>
    /// VisualTopo integration
    /// Goal: generate 3D model from visual topo file
    /// </summary>
    public class VisualTopoSample
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

            VisualTopoParser parser = new VisualTopoParser(decimalDegrees: true);

            using (StreamReader sr = new StreamReader(vtopoFile, Encoding.GetEncoding("ISO-8859-1")))
            {
                parser.ParseHeader(sr);

                while (!sr.EndOfStream)
                {
                    parser.ParseSet(sr);
                }
            }
            VisualTopoModel model = parser.Model;

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

            ComputeVectors(model);
            var gltfModel = _gltfService.AddLine(_gltfService.CreateNewModel(), "GPX1", model.Sets.First().Data.Select(d => d.GlobalGeoPoint), VectorsExtensions.CreateColor(255, 0, 0, 255), 0.25F);
            gltfModel = _gltfService.AddLine(gltfModel, "GPX2", model.Sets.Skip(1).First().Data.Select(d => d.GlobalGeoPoint), VectorsExtensions.CreateColor(0, 255, 0, 255), 0.25F);
            gltfModel.SaveGLB("TopoViewGlob.glb");
        }

        private void ComputeVectors(VisualTopoModel model)
        {

            foreach (var set in model.Sets)
            {
                set.GlobalPosPerSortie = new Dictionary<string, VisualTopoData>();
                Vector3 lastGlobal = Vector3.Zero;
                foreach (var p in set.Data)
                {
                    if (p.Entree == p.Sortie)
                    {
                        p.GlobalVector = Vector3.Zero;
                    }
                    else
                    {
                        var current = Vector3.UnitX * (float)p.Longueur;
                        var matrix = Matrix4x4.CreateRotationZ((float)MathHelper.ToRadians(p.Cap))
                                    * Matrix4x4.CreateRotationY((float)MathHelper.ToRadians(-p.Pente));

                        current = Vector3.Transform(current, matrix);
                        p.GlobalVector = lastGlobal + current;

                        set.GlobalPosPerSortie.Add(p.Sortie, p);
                    }
                    p.GlobalGeoPoint = new GeoPoint(p.GlobalVector.X, p.GlobalVector.Y, p.GlobalVector.Z);
                    lastGlobal = p.GlobalVector;
                }
            }

        }

        public class VisualTopoModel
        {
            public string Name { get; internal set; }
            public GeoPoint EntryPoint { get; internal set; }
            public string EntryPointProjectionCode { get; internal set; }

            public List<VisualTopoSet> Sets { get; set; } = new List<VisualTopoSet>();
        }
        public class VisualTopoSet
        {
            public List<VisualTopoData> Data { get; set; } = new List<VisualTopoData>();
            public string Name { get; internal set; }
            public Vector4 Color { get; internal set; }
            public Dictionary<string, VisualTopoData> GlobalPosPerSortie { get; internal set; }

            public override string ToString()
            {
                return $"Set {Name} with {Data.Count} data entries";
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
            public BoundingBox Section { get; internal set; }
            public Vector3 GlobalVector { get; internal set; }
            public GeoPoint GlobalGeoPoint { get; internal set; }
        }
        public class VisualTopoParser
        {
            private readonly bool _decimalDegrees;
            private VisualTopoModel model = new VisualTopoModel();

            public VisualTopoModel Model => model;

            public VisualTopoParser(bool decimalDegrees)
            {
                this._decimalDegrees = decimalDegrees;
            }
            internal void ParseEntryHeader(VisualTopoModel model, string entry)
            {
                var data = entry.Split(',');
                model.Name = data[0];
                model.EntryPoint = new GeoPoint(
                    double.Parse(data[2], CultureInfo.InvariantCulture) * 1000d
                    , double.Parse(data[1], CultureInfo.InvariantCulture) * 1000d
                    , double.Parse(data[3], CultureInfo.InvariantCulture));
                model.EntryPointProjectionCode = data[4];
                int srid = 0;
                switch (model.EntryPointProjectionCode)
                {
                    case "UTM31": srid = 32631; break;
                    case "LT3": srid = 27573; break;
                    default: throw new NotImplementedException($"Projection not {model.EntryPointProjectionCode} not implemented");
                };
                model.EntryPoint = model.EntryPoint.ReprojectTo(srid, 4326);
            }

            internal void ParseHeader(StreamReader sr)
            {
                sr.Skip(3);
                this.ParseEntryHeader(model, sr.ReadLine());
                sr.Skip(6);
            }

            internal void ParseSet(StreamReader sr)
            {
                VisualTopoSet set = new VisualTopoSet();

                string setHeader = sr.ReadLine();

                if (setHeader.StartsWith("[Configuration "))
                {
                    sr.ReadToEnd(); // skip until end of stream
                    return;
                }

                // Set header
                var data = setHeader.Split(';', StringSplitOptions.RemoveEmptyEntries);
                set.Color = this.ParseColor(data[0].Split(' ')[8]);
                set.Name = data[1].Trim();

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
                    topoData = this.ParseData(topoData, slots);

                    set.Data.Add(topoData);
                    dataLine = sr.ReadLine();
                }
                while (dataLine != string.Empty);

                model.Sets.Add(set);

            }

            private VisualTopoData ParseData(VisualTopoData topoData, string[] slots)
            {
                const string DefaultSize = "2";

                topoData.Entree = slots[0];
                topoData.Sortie = slots[1];
                topoData.Longueur = float.Parse(slots[2], CultureInfo.InvariantCulture);
                topoData.Cap = float.Parse(slots[3], CultureInfo.InvariantCulture);
                topoData.Pente = ParsePente(float.Parse(slots[4], CultureInfo.InvariantCulture));
                topoData.Section = new BoundingBox(
                                        float.Parse(slots[5] == "*" ? DefaultSize : slots[5], CultureInfo.InvariantCulture),
                                        float.Parse(slots[6] == "*" ? DefaultSize : slots[6], CultureInfo.InvariantCulture),
                                        float.Parse(slots[8] == "*" ? DefaultSize : slots[8], CultureInfo.InvariantCulture),
                                        float.Parse(slots[7] == "*" ? DefaultSize : slots[7], CultureInfo.InvariantCulture)
                                        );

                return topoData;
            }

            private float ParsePente(float degMinSec)
            {
                // 125 deg 30min is not 125,5 BUT 125,3
                if (_decimalDegrees)
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
        }

    }
    public static class StreamReaderExtensions
    {
        public static void Skip(this StreamReader sr, int numLines)
        {
            for (int i = 1; i <= numLines; i++)
            {
                sr.ReadLine();
            }
        }

    }

}

