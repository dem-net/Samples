//
// CustomSamples.cs
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
using DEM.Net.glTF;
using DEM.Net.glTF.Export;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SampleApp
{
    public class CustomSamples
    {
        private readonly ILogger<ElevationSamples> _logger;
        private readonly IElevationService _elevationService;
        private readonly IglTFService _glTFService;
        private readonly ISTLExportService _stlService;
        private readonly IRasterService _rasterService;

        public CustomSamples(ILogger<ElevationSamples> logger
                , IElevationService elevationService
                , IglTFService glTFService
                , ISTLExportService stlService
                , IRasterService rasterService)
        {
            _logger = logger;
            _elevationService = elevationService;
            _glTFService = glTFService;
            _stlService = stlService;
            _rasterService = rasterService;
        }
        public void Run(CancellationToken cancellationToken)
        {
            try
            {


                double lat = 46.00000000000004;
                double lon = 10.000000000000007;

                //// Regenerates all metadata
                //foreach (var dataset in DEMDataSet.RegisteredNonSingleFileDatasets)
                //{
                //    _rasterService.GenerateDirectoryMetadata(dataset, true);
                //}
                foreach (var dataset in DEMDataSet.RegisteredNonSingleFileDatasets)
                {

                    _elevationService.DownloadMissingFiles(dataset, lat, lon);
                    foreach (var file in _rasterService.GenerateReportForLocation(dataset, lat, lon))
                    {
                        _rasterService.GenerateFileMetadata(file.LocalName, dataset.FileFormat, true);
                    }
                    GeoPoint geoPoint = _elevationService.GetPointElevation(lat, lon, dataset);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

    }
}
