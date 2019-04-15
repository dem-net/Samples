//
// STLSamples.cs
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
using DEM.Net.glTF;
using DEM.Net.glTF.Export;
using DEM.Net.Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using DEM.Net.Core.Services.Mesh;

namespace DEMNet.Sample
{
    class STLSamples
    {
        public static void Run(ServiceProvider serviceProvider, DEMDataSet dataset)
        {
            string modelName = "Montagne Sainte Victoire" + dataset.Name;

            // You can get your boox from https://geojson.net/ (save as WKT)
            string bboxWKT = "POLYGON((5.54888 43.519525, 5.61209 43.519525, 5.61209 43.565225, 5.54888 43.565225, 5.54888 43.519525))";
           
            Logger.Info($"Processing model {modelName}...");
            Logger.RestartPerf("STL");
        
            IElevationService elevationService = serviceProvider.GetService<IElevationService>();
            IglTFService glTFService = serviceProvider.GetService<IglTFService>();

            var bbox = GeometryService.GetBoundingBox( bboxWKT);
            
            var heightMap = elevationService.GetHeightMap(bbox, dataset);

            heightMap = heightMap
                                    .ReprojectGeodeticToCartesian()
                                    .ZScale(2f)
                                    .CenterOnOrigin()
                                    .FitInto(250f)
                                    .BakeCoordinates();

            // Triangulate height map
            // and add base and sides
            var mesh = glTFService.GenerateTriangleMesh_Boxed(heightMap, BoxBaseThickness.FromMinimumPoint, 5);

            // STL axis differ from glTF 
            mesh.RotateX((float)Math.PI / 2f);

            var stlFileName = $"{modelName}.stl";
            STLExportService stlService = new STLExportService();
            stlService.STLExport(mesh, Path.Combine(Directory.GetCurrentDirectory(), stlFileName), false);

        }

    }
}
