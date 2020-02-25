//
// DownloaderSample.cs
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
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SampleApp
{
    /// <summary>
    /// DownloaderSample: Show how to download all data into specified directory 
    /// </summary>
    public class DownloaderSample
    {
        private readonly ILogger<DatasetSamples> _logger;
        private readonly IRasterService _rasterService;
        private readonly IElevationService _elevationService;


        public DownloaderSample(ILogger<DatasetSamples> logger
                , IRasterService rasterService
                , IElevationService elevationService)
        {
            _logger = logger;
            _rasterService = rasterService;
            _elevationService = elevationService;
        }
        public void Run(DEMDataSet specificDataset = null)
        {
            try
            {
                _logger.LogInformation($"Downloading all files to {_rasterService.LocalDirectory}");
                Stopwatch sw = new Stopwatch();

                var datasetsQuery = DEMDataSet.RegisteredNonLocalDatasets;
                if (specificDataset != null)
                    datasetsQuery = datasetsQuery.Where(d => d.Name == specificDataset.Name);

                foreach (var dataset in datasetsQuery)
                //Parallel.ForEach(datasetsQuery, dataset =>
                {
                    _logger.LogInformation($"{dataset.Name}:");

                    _elevationService.DownloadMissingFiles(dataset);

                }
                //);


            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }




    }
}
