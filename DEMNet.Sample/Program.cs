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
        public static IConfigurationRoot Configuration { get; set; }

        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder();
            // tell the builder to look for the appsettings.json file
            builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            builder.AddUserSecrets<AppSecrets>();
            //builder.AddUserSecrets(typeof(AppSecrets).Assembly);

            Configuration = builder.Build();

            IServiceCollection services = new ServiceCollection();

            //Map the implementations of your classes here ready for DI
            services
                .Configure<AppSecrets>(Configuration.GetSection(nameof(AppSecrets)))
                .AddOptions();

            // Configure container
            RegisterServices(services);

            // Configure samples
            RegisterSamples(services);

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            // Get main app
            var app = serviceProvider.GetService<SampleApplication>();

            // RUN
            app.Run(serviceProvider);

            System.Threading.Thread.Sleep(100);
            Console.Write("End of DEM.Net Samples. Press any key to contine...");
            Console.ReadLine();
        }

        private static void RegisterServices(IServiceCollection services)
        {

            services.AddSingleton<IConfiguration>(Configuration);
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
           .AddDemNetCore()
           .AddDemNetglTF();

        }

        /// <summary>
        /// Register additionnal samples here
        /// </summary>
        /// <param name="services"></param>
        private static void RegisterSamples(IServiceCollection services)
        {
            services.AddTransient<SampleApplication>()
                    .AddTransient<STLSamples>()
                    .AddTransient<ElevationSamples>()
                    .AddTransient<GpxSamples>()
                    .AddTransient<Gpx3DSamples>()
                    .AddTransient<DatasetSamples>()
                    .AddTransient<TINSamples>()
                    .AddTransient<glTF3DSamples>()
                    .AddTransient<DownloaderSample>();
            // .. more samples here
        }
    }
}
