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
using System.IO;
using System;
using Microsoft.Extensions.DependencyInjection;
using DEM.Net.glTF;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Configuration;
using DEM.Net.Core.Configuration;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace SampleApp
{

    /// <summary>
    /// Console program entry point. This is boilerplate code for .Net Core Console logging and DI
    /// except for the RegisterSamples() where samples are registered
    /// 
    /// Here we configure logging and services (via dependency injection)
    /// And setup and run the main Application
    /// </summary>
    class Program
    {

        static void Main(string[] args)
        {

            var config = new ConfigurationBuilder()
                        .SetBasePath(AppContext.BaseDirectory)
                        .AddJsonFile("appsettings.json", optional: false)
                        .AddJsonFile("secrets.json", optional: true, reloadOnChange: false)
                        .Build();
            var serviceCollection = new ServiceCollection();

            RegisterServices(config, serviceCollection);

            var services = serviceCollection.BuildServiceProvider();

            services.GetService<SampleApplication>().Start(services);
        }


        /// <summary>
        /// Register additionnal samples here
        /// </summary>
        /// <param name="services"></param>
        private static void RegisterSamples(IServiceCollection services)
        {
            services.AddTransient<STLSamples>()
                    .AddTransient<ElevationSamples>()
                    .AddTransient<GpxSamples>()
                    .AddTransient<Gpx3DSamples>()
                    .AddTransient<DatasetSamples>()
                    .AddTransient<TINSamples>()
                    .AddTransient<glTF3DSamples>()
                    .AddTransient<DownloaderSample>()
                    .AddTransient<CustomSamples>()
                    .AddTransient<AerialGpxSample>()
                    .AddTransient<ImagerySample>()
                    .AddTransient<IntervisibilitySample>()
                    .AddTransient<VisualTopoSample>();


            services.AddTransient<SampleApplication>();
            // .. more samples here
        }

        private static void RegisterServices(IConfiguration config, IServiceCollection services)
        {
            services.AddLogging(config =>
            {
                config.AddDebug(); // Log to debug (debug window in Visual Studio or any debugger attached)
                config.AddConsole(o =>
                {
                    o.IncludeScopes = false;
                    o.DisableColors = false;
                }); // Log to console (colored !)
            })
           .Configure<LoggerFilterOptions>(options =>
           {
               options.AddFilter<DebugLoggerProvider>(null /* category*/ , LogLevel.Trace /* min level */);
               options.AddFilter<ConsoleLoggerProvider>(null  /* category*/ , LogLevel.Trace /* min level */);

               // Comment this line to see all internal DEM.Net logs
               //options.AddFilter<ConsoleLoggerProvider>("DEM.Net", LogLevel.Information);
           })
           .Configure<AppSecrets>(config.GetSection(nameof(AppSecrets)))
           .Configure<DEMNetOptions>(config.GetSection(nameof(DEMNetOptions)))
           .AddDemNetCore()
           .AddDemNetglTF();

            RegisterSamples(services);

        }

    }
}
