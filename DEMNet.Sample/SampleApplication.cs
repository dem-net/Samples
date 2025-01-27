﻿//
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
using Microsoft.Extensions.Options;
using DEM.Net.Core.Configuration;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace SampleApp
{
    /// <summary>
    /// Main sample application
    /// </summary>
    public class SampleApplication
    {
        private readonly ILogger<SampleApplication> _logger;
        private readonly RasterService rasterService;
        private const string DATA_FILES_PATH = null; //@"C:\Users\ElevationAPI\AppData\Local\Temp"; // Leave to null for default location (Environment.SpecialFolder.LocalApplicationData)

        public SampleApplication(ILogger<SampleApplication> logger,
            RasterService rasterService
            )
        {
            _logger = logger;
            this.rasterService = rasterService;
        }


        public void Start(IServiceProvider services)
        {
            //Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));

            Stopwatch sw = Stopwatch.StartNew();
            _logger.LogInformation("Application started");

            bool pauseAfterEachSample = true;

            // Change data dir if not null
            if (!string.IsNullOrWhiteSpace(DATA_FILES_PATH))
            {
                rasterService.SetLocalDirectory(DATA_FILES_PATH);
            }
            using (_logger.BeginScope($"Running {nameof(LandscapeSample)}.."))
            {
                var sample = services.GetService<LandscapeSample>();
                sample.Run();
                _logger.LogInformation($"Sample {sample.GetType().Name} done. Press any key to run the next sample...");
                if (pauseAfterEachSample) Console.ReadLine();
            }
            using (_logger.BeginScope($"Running {nameof(PolygonMaskSample)}.."))
            {
                var sample = services.GetService<PolygonMaskSample>();
                sample.Run(DEMDataSet.ETOPO1);
                _logger.LogInformation($"Sample {sample.GetType().Name} done. Press any key to run the next sample...");
                if (pauseAfterEachSample) Console.ReadLine();
            }            
            using (_logger.BeginScope($"Running {nameof(Gpx3DSamples)}.."))
            {
                var gpx3DSamples = services.GetService<Gpx3DSamples>();
                gpx3DSamples.Run(DEMDataSet.SRTM_GL3, true, false, Reprojection.SRID_PROJECTED_MERCATOR);
                _logger.LogInformation($"Sample {gpx3DSamples.GetType().Name} done. Press any key to run the next sample...");
                if (pauseAfterEachSample) Console.ReadLine();
            }
            using (_logger.BeginScope($"Running {nameof(OBJSamples)}.."))
            {
                var sample = services.GetRequiredService<OBJSamples>();
                sample.Run();
                _logger.LogInformation($"Sample {sample.GetType().Name} done. Press any key to run the next sample...");
                if (pauseAfterEachSample) Console.ReadLine();
            }
            using (_logger.BeginScope($"Running {nameof(DownloaderSample)}.."))
            {
                var sample = services.GetRequiredService<DownloaderSample>();

                sample.Run(DEMDataSet.swissALTI3D2m);
                rasterService.GenerateDirectoryMetadata(DEMDataSet.swissALTI3D50cm, force: true);
                sample.ExtractCopernicEuDem(@"C:\Data\EuDEM");
                sample.DeduplicateCopernicEuDem(@"C:\Data\EuDEM");
                //sample.PrepareIgn5_2_AfterDezip_MoveAndCompressAsc(@"C:\Users\admin\Downloads\IGN5\France");

                
                rasterService.GenerateDirectoryMetadata(DEMDataSet.FABDEM, force: false);
                rasterService.GenerateDirectoryMetadata(DEMDataSet.IGN_5m, force: true);
                rasterService.GenerateDirectoryMetadata(DEMDataSet.IGN_1m, force: true);
                //sample.Run(DEMDataSet.swissALTI3D2m);
                //sample.Run(DEMDataSet.swissALTI3D50cm);

                //IGN
                //sample.PrepareIgn5_2_AfterDezip_MoveAndCompressAsc(@"E:\Perso\data\RGE_Alti5m\DEM.Net\IGN_5m");
                //sample.PrepareIgn5_1_Deduplicate_DezipManualAfter();
                //sample.Generate_Ign5_Metadata();


                _logger.LogInformation($"Sample {sample.GetType().Name} done. Press any key to run the next sample...");
                if (pauseAfterEachSample) Console.ReadLine();
            }
           
            //using (_logger.BeginScope($"Running {nameof(Gpx3DSamples)}.."))
            //{
            //    var gpx3DSamples = services.GetService<Gpx3DSamples>();
            //    gpx3DSamples.Run(gpx3DSamples.GetUSGSNED(@"C:\Repos\DEM.Net.Samples\DEMNet.Sample\SampleData",true), true, false, Reprojection.SRID_PROJECTED_MERCATOR);
            //    //gpx3DSamples.Run(DEMDataSet.SRTM_GL1, true, false, Reprojection.SRID_PROJECTED_MERCATOR);
            //    //gpx3DSamples.Run(DEMDataSet.SRTM_GL3, true, false, Reprojection.SRID_PROJECTED_MERCATOR);
            //    //gpx3DSamples.Run(DEMDataSet.ASTER_GDEMV3, true, false, Reprojection.SRID_PROJECTED_MERCATOR);
            //    _logger.LogInformation($"Sample {gpx3DSamples.GetType().Name} done. Press any key to run the next sample...");
            //    if (pauseAfterEachSample) Console.ReadLine();
            //}
            //using (_logger.BeginScope($"Running {nameof(CustomRasterElevationSample)}.."))
            //{
            //    var sample = services.GetService<CustomRasterElevationSample>();
            //    sample.Run();
            //    _logger.LogInformation($"Sample {sample.GetType().Name} done. Press any key to run the next sample...");
            //    if (pauseAfterEachSample) Console.ReadLine();
            //}

            //using (_logger.BeginScope($"Running {nameof(Text3DSample)}.."))
            //{
            //    var sample = services.GetService<Text3DSample>();
            //    sample.Run();
            //    _logger.LogInformation($"Sample {sample.GetType().Name} done. Press any key to run the next sample...");
            //    if (pauseAfterEachSample) Console.ReadLine();
            //}
            
            //using (_logger.BeginScope($"Running {nameof(ElevationSamples)}.."))
            //{
            //    var elevationSamples = services.GetService<ElevationSamples>();
            //    elevationSamples.Run();
            //    _logger.LogInformation($"Sample {elevationSamples.GetType().Name} done. Press any key to run the next sample...");
            //    if (pauseAfterEachSample) Console.ReadLine();
            //}
            //using (_logger.BeginScope($"Running {nameof(GpxSTLSample)}.."))
            //{
            //    var sample = services.GetService<GpxSTLSample>();
            //    sample.Run(Path.Combine("SampleData", "GPX", "Mt_Whitney_2017.gpx"), DEMDataSet.NASADEM);
            //    _logger.LogInformation($"Sample {sample.GetType().Name} done. Press any key to run the next sample...");
            //    if (pauseAfterEachSample) Console.ReadLine();
            //}
            // Visual topo sample moved to DEM.Net.Extensions repo
            //using (_logger.BeginScope($"Running {nameof(VisualTopoSample)}.."))
            //{
            //    var sample = services.GetService<VisualTopoSample>();
            //    sample.Run();
            //    _logger.LogInformation($"Sample {sample.GetType().Name} done. Press any key to run the next sample...");
            //    if (pauseAfterEachSample) Console.ReadLine();
            //}

            //using (_logger.BeginScope($"Running {nameof(AerialGpxSample)}.."))
            //{
            //    var aerialGpxSample = services.GetService<AerialGpxSample>();
            //    aerialGpxSample.Run(DEMDataSet.SRTM_GL3, DEMDataSet.ASTER_GDEMV3, useSensorLog: false);
            //    //aerialGpxSample.Run(DEMDataSet.SRTM_GL1);
            //    //aerialGpxSample.Run(DEMDataSet.AW3D30);
            //    //gpx3DSamples.Run(DEMDataSet.SRTM_GL1, true, false, Reprojection.SRID_PROJECTED_MERCATOR);
            //    //gpx3DSamples.Run(DEMDataSet.SRTM_GL3, true, false, Reprojection.SRID_PROJECTED_MERCATOR);
            //    //gpx3DSamples.Run(DEMDataSet.ASTER_GDEMV3, true, false, Reprojection.SRID_PROJECTED_MERCATOR);
            //    _logger.LogInformation($"Sample {aerialGpxSample.GetType().Name} done. Press any key to run the next sample...");
            //    if (pauseAfterEachSample) Console.ReadLine();
            //}
            //using (_logger.BeginScope($"Running {nameof(IntervisibilitySample)}.."))
            //{
            //    var intervisibilitySample = services.GetService<IntervisibilitySample>();
            //    intervisibilitySample.Run();
            //    _logger.LogInformation($"Sample {nameof(IntervisibilitySample)} done. Press any key to run the next sample...");

            //    if (pauseAfterEachSample) Console.ReadLine();
            //}
            //using (_logger.BeginScope($"Running {nameof(glTF3DSamples)}.."))
            //{
            //    var glTF3DSamples = services.GetService<glTF3DSamples>();
            //    //glTF3DSamples.Run(DEMDataSet.GEBCO_2019, withTexture: true);
            //    //glTF3DSamples.Run(DEMDataSet.ASTER_GDEMV3, withTexture:true);
            //    glTF3DSamples.Run(DEMDataSet.SRTM_GL3, withTexture: true);
            //    //glTF3DSamples.Run(DEMDataSet.SRTM_GL3, withTexture: true);
            //    //glTF3DSamples.Run(DEMDataSet.ETOPO1, withTexture:true);
            //    _logger.LogInformation($"Sample {glTF3DSamples.GetType().Name} done. Press any key to run the next sample...");
            //    if (pauseAfterEachSample) Console.ReadLine();
            //}

            //using (_logger.BeginScope($"Running {nameof(TINSamples)}.."))
            //{
            //    var tinSamples = services.GetService<TINSamples>();
            //    tinSamples.Run(TINSamples.WKT_STE_VICTOIRE, nameof(TINSamples.WKT_STE_VICTOIRE), DEMDataSet.AW3D30, 50);
            //    tinSamples.Run(TINSamples.WKT_EIGER, nameof(TINSamples.WKT_EIGER), DEMDataSet.SRTM_GL3, 50);
            //    tinSamples.Run(TINSamples.WKT_GORGES_VERDON, nameof(TINSamples.WKT_GORGES_VERDON), DEMDataSet.AW3D30, 50);
            //    _logger.LogInformation($"Sample {tinSamples.GetType().Name} done. Press any key to run the next sample...");
            //    if (pauseAfterEachSample) Console.ReadLine();
            //}
            //using (_logger.BeginScope($"Running {nameof(ImagerySample)}.."))
            //{
            //    var imagerySample = services.GetService<ImagerySample>();
            //    imagerySample.Run();
            //    _logger.LogInformation($"Sample {imagerySample.GetType().Name} done. Press any key to run the next sample...");
            //    if (pauseAfterEachSample) Console.ReadLine();
            //}
            //using (_logger.BeginScope($"Running {nameof(STLSamples)}.."))
            //{
            //    var stLSamples = services.GetService<STLSamples>();
            //    stLSamples.Run();
            //    _logger.LogInformation($"Sample {stLSamples.GetType().Name} done. Press any key to run the next sample...");
            //    if (pauseAfterEachSample) Console.ReadLine();
            //}


            //using (_logger.BeginScope($"Running {nameof(DatasetSamples)}.."))
            //{
            //    var datasetSamples = services.GetService<DatasetSamples>();

            //    datasetSamples.Run();
            //    _logger.LogInformation($"Sample {datasetSamples.GetType().Name} done. Press any key to run the next sample...");
            //    if (pauseAfterEachSample) Console.ReadLine();
            //}
            //using (_logger.BeginScope($"Running {nameof(GpxSamples)}.."))
            //{

            //    var gpxSamples = services.GetService<GpxSamples>();
            //    gpxSamples.Run();
            //    _logger.LogInformation($"Sample {gpxSamples.GetType().Name} done. Press any key to run the next sample...");
            //    if (pauseAfterEachSample) Console.ReadLine();
            //}


           
            //using (_logger.BeginScope($"Running {nameof(CustomSamples)}.."))
            //{
            //    customSamples.Run(cancellationToken);
            //    _logger.LogInformation($"Sample {customSamples.GetType().Name} done. Press any key to run the next sample...");
            //    if (pauseAfterEachSample) Console.ReadLine();
            //    if (cancellationToken.IsCancellationRequested) return Task.FromCanceled(cancellationToken);
            //}


            _logger.LogTrace($"Application ran in : {sw.Elapsed:g}");

        }

    }
}
