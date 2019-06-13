//
// Program.cs
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
using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    /// <summary>
    /// Main sample application
    /// </summary>
    public class SampleApplication
    {
        private readonly ILogger<SampleApplication> _logger;

        public SampleApplication(ILogger<SampleApplication> logger)
        {
            _logger = logger;
        }

        internal void Run(IServiceProvider serviceProvider)
        {
            //Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));

            Stopwatch sw = Stopwatch.StartNew();
            _logger.LogInformation("Application started");

            bool pauseAfterEachSample = true;


            //using (_logger.BeginScope($"Running {nameof(ElevationSamples)}.."))
            //{
            //    var sample = serviceProvider.GetRequiredService<ElevationSamples>();
            //    sample.Run();
            //    _logger.LogInformation($"Sample {sample.GetType().Name} done. Press any key to run the next sample...");
            //    if (pauseAfterEachSample) Console.ReadLine();
            //}

            using (_logger.BeginScope($"Running {nameof(GpxSamples)}.."))
            {
                var sample = serviceProvider.GetRequiredService<GpxSamples>();
                sample.Run();
                _logger.LogInformation($"Sample {sample.GetType().Name} done. Press any key to run the next sample...");
                if (pauseAfterEachSample) Console.ReadLine();
            }


            using (_logger.BeginScope($"Running {nameof(Gpx3DSamples)}.."))
            {
                var sample = serviceProvider.GetRequiredService<Gpx3DSamples>();
                sample.Run(DEMDataSet.AW3D30, false, true, Reprojection.SRID_PROJECTED_MERCATOR);
                _logger.LogInformation($"Sample {sample.GetType().Name} done. Press any key to run the next sample...");
                if (pauseAfterEachSample) Console.ReadLine();
            }
            using (_logger.BeginScope($"Running {nameof(TINSamples)}.."))
            {
                var sample = serviceProvider.GetRequiredService<TINSamples>();
                sample.Run(TINSamples.WKT_STE_VICTOIRE, nameof(TINSamples.WKT_STE_VICTOIRE), DEMDataSet.AW3D30);
                sample.Run(TINSamples.WKT_EIGER, nameof(TINSamples.WKT_EIGER), DEMDataSet.SRTM_GL3, 25);
                sample.Run(TINSamples.WKT_GORGES_VERDON, nameof(TINSamples.WKT_GORGES_VERDON), DEMDataSet.AW3D30);
                _logger.LogInformation($"Sample {sample.GetType().Name} done. Press any key to run the next sample...");
                if (pauseAfterEachSample) Console.ReadLine();
            }
            using (_logger.BeginScope($"Running {nameof(DatasetSamples)}.."))
            {
                var sample = serviceProvider.GetRequiredService<DatasetSamples>();
                sample.Run();
                _logger.LogInformation($"Sample {sample.GetType().Name} done. Press any key to run the next sample...");
                if (pauseAfterEachSample) Console.ReadLine();
            }
            
            
            using (_logger.BeginScope($"Running {nameof(glTF3DSamples)}.."))
            {
                var sample = serviceProvider.GetRequiredService<glTF3DSamples>();
                sample.Run();
                _logger.LogInformation($"Sample {sample.GetType().Name} done. Press any key to run the next sample...");
                if (pauseAfterEachSample) Console.ReadLine();
            }
            using (_logger.BeginScope($"Running {nameof(STLSamples)}.."))
            {
                var sample = serviceProvider.GetRequiredService<STLSamples>();
                sample.Run();
                _logger.LogInformation($"Sample {sample.GetType().Name} done. Press any key to run the next sample...");
                if (pauseAfterEachSample) Console.ReadLine();
            }

            _logger.LogTrace($"Application ran in : {sw.Elapsed:g}");
        }


    }
}
