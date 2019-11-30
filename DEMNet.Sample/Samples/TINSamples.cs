//
// TINSamples.cs
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

using AssetGenerator;
using DEM.Net.Core;
using DEM.Net.glTF;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampleApp
{
    public class TINSamples 
    {
        public const string WKT_STE_VICTOIRE = "POLYGON((5.523314005345696 43.576096090257955, 5.722441202611321 43.576096090257955, 5.722441202611321 43.46456490270913, 5.523314005345696 43.46456490270913, 5.523314005345696 43.576096090257955))";
        public const string WKT_EIGER = "Polygon((8.12951188622090193 46.634254667789655, 7.8854960299327308 46.63327193616965616, 7.89909222133881617 46.4319282954101098, 8.13595218741325965 46.43143509785498679, 8.12951188622090193 46.634254667789655))";
        public const string WKT_GORGES_VERDON = "Polygon ((6.14901771150602894 43.8582708438193265, 6.30590241369230409 43.8575166880815317, 6.32080646040000005 43.74636314919661828, 6.14561854295865828 43.74579647280887684, 6.14901771150602894 43.8582708438193265))";

        private readonly ILogger<TINSamples> _logger;
        private readonly IRasterService _rasterService;
        private readonly IElevationService _elevationService;
        private readonly IglTFService _glTFService;

        public TINSamples(ILogger<TINSamples> logger
                , IRasterService rasterService
                , IElevationService elevationService
                , IglTFService glTFService) 
        {
            _logger = logger;
            _rasterService = rasterService;
            _elevationService = elevationService;
            _glTFService = glTFService;
        }

        internal void Run(string wkt, string name, DEMDataSet dataSet, int precisionMeters = 10)
        {
            try
            {
                int outputSrid = Reprojection.SRID_PROJECTED_MERCATOR;

                var bbox = GeometryService.GetBoundingBox(wkt);

                _logger.LogInformation($"Getting height map...");
                HeightMap hMap = _elevationService.GetHeightMap(ref bbox, dataSet);
                hMap = hMap.ZScale(2);


                _logger.LogInformation($"Generating TIN with {precisionMeters}m precision...");
                hMap = hMap.ReprojectTo(4326, outputSrid);
                var mesh = TINGeneration.GenerateTIN(hMap, (double)precisionMeters, _glTFService, null, outputSrid);

                _logger.LogInformation($"Generating model...");

                Model model = _glTFService.GenerateModel(mesh, $"TIN {dataSet.Name}");
                string v_nomFichierOut = $"{name}_TIN_{dataSet.Name}";

                _glTFService.Export(model, ".", v_nomFichierOut, false, true);
                _logger.LogInformation($"Model {v_nomFichierOut} generated.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
           
        }

    }
}
