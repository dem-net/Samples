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
    public class DatasetSamples : SampleLogger
    {
        private readonly IRasterService _rasterService;

        public DatasetSamples(ILogger<DatasetSamples> logger
                , IRasterService rasterService) : base(logger)
        {
            _rasterService = rasterService;
        }
        public void Run()
        {
            try
            {


                LogInfo("GenerateReportAsString() will generate a report of all local datasets.");
                LogInfo($"Local data directory : {_rasterService.LocalDirectory}");
                Stopwatch sw = new Stopwatch();

                sw.Restart();

                LogInfo($"Generating report...");
                LogInfo(_rasterService.GenerateReportAsString());

                LogInfo($"time taken: {sw.Elapsed:g}");


                GeoPoint geoPoint = new GeoPoint(45.179337, 5.721421);
                LogInfo($"Getting raster file for dataset at location {geoPoint}");

                foreach (var dataset in DEMDataSet.RegisteredDatasets)
                {
                    LogInfo($"{dataset.Name}:");

                    DemFileReport report = _rasterService.GenerateReportForLocation(dataset, geoPoint.Latitude, geoPoint.Longitude);
                    if (report == null)
                    {
                        LogInfo($"> Location is not covered by dataset");
                    }
                     else
                    { 
                        LogInfo($"> Remote file URL: {report.URL}");

                        if (report.IsExistingLocally)
                        {
                            LogInfo($"> Local file: {report.LocalName}");
                        } else
                        {
                            LogInfo($"> Local file: <not dowloaded>");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
            }
        }

       


    }
}
