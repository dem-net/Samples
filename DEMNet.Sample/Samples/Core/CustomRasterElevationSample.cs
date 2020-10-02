//
// CustomRasterElevationSample.cs
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
using DEM.Net.Core.Datasets;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;

namespace SampleApp
{
    /// <summary>
    /// CustomRasterElevationSample: Show how to interact with a custom dataset
    /// </summary>
    public class CustomRasterElevationSample
    {
        private readonly ILogger<CustomRasterElevationSample> _logger;
        private readonly RasterService _rasterService;
        private readonly ElevationService _elevationService;

        public CustomRasterElevationSample(ILogger<CustomRasterElevationSample> logger
                , RasterService rasterService
            , ElevationService elevationService)
        {
            _logger = logger;
            _rasterService = rasterService;
            _elevationService = elevationService;
        }
        public void Run()
        {
            try
            {
                var dataSetLitto3D = new DEMDataSet()
                {
                    Name = "Litto3D_PortCros",
                    Description = "Litto3D_PortCros",
                    PublicUrl = null,
                    DataSource = new LocalFileSystem(localDirectory: @"C:\Repos\DEM.Net.Samples\DEMNet.Sample\SampleData\MNT1m PortCros"),
                    FileFormat = new DEMFileDefinition("ASCIIGrid", DEMFileType.ASCIIGrid, ".asc", DEMFileRegistrationMode.Cell),
                    ResolutionMeters = 1,
                    SRID= Reprojection.SRID_PROJECTED_LAMBERT_93,
                    Attribution = new Attribution("Dataset","Litto3D","www.shom.fr","Licence ouverte Etalab")
                };
                _rasterService.GenerateDirectoryMetadata(dataSetLitto3D, force: false);

                var point = new GeoPoint(43.01142119356318, 6.385200681010872).ReprojectTo(4326, 2154);
                point = _elevationService.GetPointElevation(point, dataSetLitto3D);
              
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }




    }
}
