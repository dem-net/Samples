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
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SampleApp
{
    public class VisualTopoSample
    {
        private readonly ILogger<VisualTopoSample> _logger;
        private readonly IElevationService _elevationService;

        public VisualTopoSample(ILogger<VisualTopoSample> logger
                , IElevationService elevationService)
        {
            _logger = logger;
            _elevationService = elevationService;
        }

        public void Run()
        {
            string vtopoFile = Path.Combine("SampleData", "topo asperge avec ruisseau.TRO");
            //string vtopoFile = Path.Combine("SampleData", "LA SALLE.TRO");

            VisualTopoModel model = new VisualTopoModel();

            using (StreamReader sr = new StreamReader(vtopoFile, Encoding.GetEncoding("ISO-8859-1")))
            {
                sr.ReadLine(); // Version
                sr.ReadLine(); // Verification
                sr.ReadLine(); // Blank
                string entry = sr.ReadLine(); // Entry point Trou ASPERGE ,492.28800,4818.79400,465.00,UTM31
                VisualTopoParser.ParseEntry(model, entry);
            }
        }

        public class VisualTopoModel
        {
            public string Name { get; internal set; }
            public GeoPoint EntryPoint { get; internal set; }
            public string EntryPointProjectionCode { get; internal set; }
        }
        public static class VisualTopoParser
        {
            internal static void ParseEntry(VisualTopoModel model, string entry)
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
                    default:
                        break;
                };
                model.EntryPoint = model.EntryPoint.ReprojectTo(srid, 4326);

            }
        }

    }

}

