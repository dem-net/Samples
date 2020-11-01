//
// GpxSTLSample.cs
//
// Author:
//       Xavier Fischer
//
// Copyright (c) 2020 
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
using DEM.Net.glTF.Export;
using DEM.Net.glTF.SharpglTF;
using Microsoft.Extensions.Logging;
using SharpGLTF.Schema2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;

namespace SampleApp
{
    /// <summary>
    /// Generate a STL model as usual AND and a GPX model correctly georeferenced
    /// </summary>
    public class GpxSTLSample
    {
        private readonly ILogger<STLSamples> _logger;
        private readonly ElevationService _elevationService;
        private readonly SharpGltfService _sharpGltfService;
        private readonly ISTLExportService _stlService;

        public GpxSTLSample(ILogger<STLSamples> logger
                , ElevationService elevationService
                , SharpGltfService sharpGltfService
                , ISTLExportService stlService)
        {
            _logger = logger;
            _elevationService = elevationService;
            _sharpGltfService = sharpGltfService;
            _stlService = stlService;
        }
        internal void Run(string gpxFile, DEMDataSet dataSet, float Z_FACTOR = 2f, int outputSrid = Reprojection.SRID_PROJECTED_MERCATOR)
        {
            try
            {

                float paddingFactor = 2f;
                float ModelMaxUnits = 250f;
                float GpxLineWidthSTLUnits = 0.1f;
                Matrix4x4 stlTransformMatrix = Matrix4x4.CreateRotationX((float)Math.PI / 2f);


                string outputDir = Path.GetFullPath(".");

                //=======================
                /// Line strip from GPX
                ///
                // Get GPX points
                var segments = GpxImport.ReadGPX_Segments(gpxFile);
                var points = segments.SelectMany(seg => seg);
                var bbox = points.GetBoundingBox().ReprojectTo(4326, outputSrid);
                bbox = bbox.Pad(bbox.Width * paddingFactor, bbox.Height * paddingFactor, 0)
                           .ReprojectTo(outputSrid, 4326);
                var gpxPointsElevated = _elevationService.GetPointsElevation(points, dataSet); 

                HeightMap hMap = _elevationService.GetHeightMap(ref bbox, dataSet);

                hMap = hMap.ReprojectTo(4326, outputSrid)
                            .ZScale(Z_FACTOR)
                            .CenterOnOrigin()
                            .FitInto(ModelMaxUnits)
                            .BakeCoordinates();

                // generate mesh
                ModelRoot model = _sharpGltfService.CreateTerrainMesh(hMap, GenOptions.BoxedBaseElevationMin, stlTransformMatrix);

                List<Attribution> attributions = new List<Attribution>();
                attributions.Add(dataSet.Attribution);
                attributions.Add(new Attribution("Generator", "DEM Net Elevation API", "https://elevationapi.com"));

                _stlService.STLExport(model.LogicalMeshes[0].Primitives[0], Path.ChangeExtension(gpxFile, ".stl"), false, attributions);



                var bboxPoints = bbox.ReprojectTo(4326, outputSrid).CenterOnOrigin();

                gpxPointsElevated = gpxPointsElevated.ReprojectTo(4326, outputSrid)
                            .ZScale(Z_FACTOR)
                            .CenterOnOrigin(bbox.ReprojectTo(4326, outputSrid))
                            .FitInto(bboxPoints, ModelMaxUnits)
                            .ToList();


                var gpxModel = _sharpGltfService.AddLine(null, "GPX", gpxPointsElevated, Vector4.One, GpxLineWidthSTLUnits, stlTransformMatrix);
                _stlService.STLExport(gpxModel.LogicalMeshes[0].Primitives[0], Path.ChangeExtension(gpxFile, ".gpx.stl"), false, attributions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }
    }
}
