//
// glTF3DSamples.cs
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

using DEM.Net.Core;
using DEM.Net.glTF.SharpglTF;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DEM.Net.Core.Imagery;
using SharpGLTF.Schema2;
using g3;

namespace SampleApp
{
    /// <summary>
    /// Extracts a DEM from a bbox and generates a 3D export in glTF format
    /// </summary>
    public class OBJSamples
    {
        private readonly ILogger<OBJSamples> _logger;
        private readonly ElevationService _elevationService;
        private readonly ImageryService _imageryService;
        private readonly SharpGltfService _sharpGltfService;

        public OBJSamples(ILogger<OBJSamples> logger
                , ElevationService elevationService
                , SharpGltfService sharpGltfService
                , ImageryService imageryService)
        {
            _logger = logger;
            _elevationService = elevationService;
            _sharpGltfService = sharpGltfService;
            _imageryService = imageryService;
        }
        public void Run()
        {
            try
            {
                var path = @"C:\Users\xavier.fischer\Downloads\Telegram Desktop\SinGeo\Torre Badúm.obj";
                var mtlPath = @"C:\Users\xavier.fischer\Downloads\Telegram Desktop\Torre BadúmGeoR38571\Torre BadúmGeoR3857.obj";
                DMesh3 mesh = StandardMeshReader.ReadMesh(path);

                DMesh3Builder builder = new DMesh3Builder();

                OBJReader reader = new OBJReader();
                using (var fs = File.OpenText(path))
                {
                    var options = ReadOptions.Defaults;
                    options.ReadMaterials = false;
                    IOReadResult result = reader.Read(fs, options, builder);
                    if (result.code == IOCode.Ok)
                    {

                    }


                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }
    }
}
