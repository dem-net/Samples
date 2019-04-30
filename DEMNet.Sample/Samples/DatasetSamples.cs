//
// DatasetSamples.cs
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

//
// DatasetSamples.cs
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
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;

namespace SampleApp
{
    /// <summary>
    /// DatasetSamples: Show how to interact with the datasets
    /// - Get report of local files
    /// - Get remote raster file information for location
    /// </summary>
    public class DatasetSamples 
    {
        private readonly ILogger<DatasetSamples> _logger;
        private readonly IRasterService _rasterService;

        public DatasetSamples(ILogger<DatasetSamples> logger
                , IRasterService rasterService) 
        {
            _logger = logger;
            _rasterService = rasterService;
        }
        public void Run()
        {
            try
            {


                _logger.LogInformation("GenerateReportAsString() will generate a report of all local datasets.");
                _logger.LogInformation($"Local data directory : {_rasterService.LocalDirectory}");
                Stopwatch sw = new Stopwatch();

                sw.Restart();

                _logger.LogInformation($"Generating report...");
                _logger.LogInformation(_rasterService.GenerateReportAsString());

                _logger.LogInformation($"time taken: {sw.Elapsed:g}");


                GeoPoint geoPoint = new GeoPoint(45.179337, 5.721421);
                _logger.LogInformation($"Getting raster file for dataset at location {geoPoint}");

                foreach (var dataset in DEMDataSet.RegisteredDatasets)
                {
                    _logger.LogInformation($"{dataset.Name}:");

                    DemFileReport report = _rasterService.GenerateReportForLocation(dataset, geoPoint.Latitude, geoPoint.Longitude);
                    if (report == null)
                    {
                        _logger.LogInformation($"> Location is not covered by dataset");
                    }
                     else
                    { 
                        _logger.LogInformation($"> Remote file URL: {report.URL}");

                        if (report.IsExistingLocally)
                        {
                            _logger.LogInformation($"> Local file: {report.LocalName}");
                        } else
                        {
                            _logger.LogInformation($"> Local file: <not dowloaded>");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

       


    }
}
