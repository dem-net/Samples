//
// glTF3DSamples.cs
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
using DEM.Net.glTF.SharpglTF;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DEM.Net.Core.Imagery;
using SharpGLTF.Schema2;

namespace SampleApp
{
    /// <summary>
    /// Extracts a DEM from a bbox and generates a 3D export in glTF format
    /// </summary>
    public class glTF3DSamples
    {
        private readonly ILogger<glTF3DSamples> _logger;
        private readonly ElevationService _elevationService;
        private readonly ImageryService _imageryService;
        private readonly SharpGltfService _sharpGltfService;

        public glTF3DSamples(ILogger<glTF3DSamples> logger
                , ElevationService elevationService
                , SharpGltfService sharpGltfService
                , ImageryService imageryService)
        {
            _logger = logger;
            _elevationService = elevationService;
            _sharpGltfService = sharpGltfService;
            _imageryService = imageryService;
        }
        public void Run(DEMDataSet dataset, bool withTexture = true)
        {
            try
            {

                int TEXTURE_TILES = 24; // 4: med, 8: high

                //_rasterService.GenerateDirectoryMetadata(dataset, false);
                Stopwatch sw = Stopwatch.StartNew();
                string modelName = $"Dolomites_{dataset.Name}";
                string outputDir = Directory.GetCurrentDirectory();
                ImageryProvider provider = ImageryProvider.MapTilerSatellite;// new TileDebugProvider(new GeoPoint(43.5,5.5));


                //// You can get your boox from https://geojson.net/ (save as WKT)
                //string bboxWKT = "POLYGON((5.54888 43.519525, 5.61209 43.519525, 5.61209 43.565225, 5.54888 43.565225, 5.54888 43.519525))";
                ////                string bboxWKT =
                ////                    "POLYGON((5.594457381483949 43.545276557046044,5.652135604140199 43.545276557046044,5.652135604140199 43.52038635099936,5.594457381483949 43.52038635099936,5.594457381483949 43.545276557046044))";
                ////                _logger.LogInformation($"Processing model {modelName}...");
                ////
                ////
                ////                _logger.LogInformation($"Getting bounding box geometry...");
                //var bbox = GeometryService.GetBoundingBox(bboxWKT);

                // DjebelMarra
                //var bbox = new BoundingBox(24.098067346557492, 24.42468219234563, 12.7769822830208, 13.087504129660111);

                // Dolomites
                var bbox = GeometryService.GetBoundingBox("POLYGON ((12.309158326668324 46.452834489592334, 12.309158326668324 46.19110950009852, 12.784644435942425 46.19110950009852, 12.784644435942425 46.452834489592334, 12.309158326668324 46.452834489592334))");
                //GeometryService.GetBoundingBox("POLYGON((12.377647 46.462563, 12.436746 46.462563, 12.436746 46.434706, 12.377647 46.434706, 12.377647 46.462563))");
                
                bbox = bbox.ReprojectTo(4326, dataset.SRID); 

                // MontBlanc
                //var bbox = GeometryService.GetBoundingBox("POLYGON((6.618804355541963 45.9658287141746,7.052764316479463 45.9658287141746,7.052764316479463 45.72379929776474,6.618804355541963 45.72379929776474,6.618804355541963 45.9658287141746))");

                //var bbox = new BoundingBox(5.5613898348431485,5.597185285307553,43.49372969433046,43.50939068558466);
                _logger.LogInformation($"Getting height map data...");

                var heightMap = _elevationService.GetHeightMap(ref bbox, dataset).ReprojectTo(dataset.SRID, 4326).BakeCoordinates();
                bbox = heightMap.BoundingBox;
                var mask = "POLYGON ((12.4991537 46.4125171, 12.4960917 46.4123447, 12.4895958 46.4086339, 12.4860739 46.4043662, 12.4810379 46.4037916, 12.4774473 46.3982347, 12.475017 46.3972249, 12.4685179 46.3899619, 12.463809 46.3882879, 12.4549382 46.3915468, 12.4562914 46.385984, 12.4606888 46.3802756, 12.4632678 46.3694263, 12.4513469 46.3706098, 12.4551628 46.3671928, 12.4549647 46.3648296, 12.4487875 46.3605274, 12.4425416 46.3599444, 12.4284437 46.3517052, 12.4176869 46.3547772, 12.4210023 46.3423474, 12.4141597 46.335949, 12.4111207 46.3309179, 12.3980248 46.3289102, 12.3956632 46.330091, 12.3852704 46.3288142, 12.3782393 46.3312578, 12.3749913 46.3298963, 12.3715984 46.3262471, 12.3647144 46.3259647, 12.3621183 46.3226555, 12.3529746 46.3188005, 12.3556541 46.3164041, 12.3573308 46.3124468, 12.3557547 46.3093814, 12.3518772 46.3074803, 12.3539079 46.3034663, 12.3526794 46.3004205, 12.3547047 46.2989436, 12.354241 46.2962773, 12.3562776 46.2938684, 12.3496148 46.2936862, 12.3484029 46.2922433, 12.3443777 46.2910042, 12.3440886 46.2881958, 12.3419638 46.2863374, 12.3387703 46.2856206, 12.3371561 46.2867589, 12.3348024 46.2848678, 12.3296532 46.2838514, 12.3296379 46.2821105, 12.3236103 46.273829, 12.3259488 46.2731504, 12.3318902 46.2746866, 12.3352973 46.2726999, 12.3423767 46.2742082, 12.3434012 46.2730552, 12.3468509 46.2729586, 12.357084 46.2772059, 12.3609035 46.2771527, 12.3656017 46.2836857, 12.3686911 46.2825224, 12.3683985 46.2836996, 12.373825 46.285087, 12.3760291 46.2837841, 12.3762291 46.2785156, 12.3723114 46.273627, 12.3544624 46.2678115, 12.3530316 46.2688912, 12.3479815 46.268321, 12.3399744 46.2700689, 12.3369183 46.2685487, 12.3339593 46.2699732, 12.3297719 46.2680669, 12.3236368 46.2673955, 12.321577 46.2643013, 12.3244799 46.2557194, 12.3266733 46.2539091, 12.3324583 46.2534928, 12.3344958 46.2515345, 12.3360982 46.2486698, 12.3330904 46.2469762, 12.3355262 46.2422004, 12.338002 46.2403212, 12.3381416 46.2383689, 12.3396829 46.2399341, 12.3425881 46.2402222, 12.3433905 46.2434381, 12.3451495 46.2438064, 12.3461564 46.2465263, 12.3528681 46.2518633, 12.3556109 46.2571852, 12.3601203 46.2610068, 12.3582682 46.2625627, 12.3591384 46.2646823, 12.3627957 46.2654409, 12.3663926 46.2639829, 12.3640176 46.2608079, 12.3647759 46.2583351, 12.3664368 46.2575388, 12.368178 46.2613725, 12.3729511 46.2615941, 12.3706946 46.2633556, 12.3724008 46.2653099, 12.3828623 46.2713564, 12.3913813 46.2746686, 12.3930374 46.2764375, 12.3909113 46.2787234, 12.399954 46.2839856, 12.4068925 46.2871745, 12.4094704 46.2854581, 12.424523 46.2877218, 12.4264126 46.2869599, 12.4346206 46.2888931, 12.440054 46.2935438, 12.4383299 46.3018461, 12.4436259 46.3018948, 12.4421553 46.2950073, 12.4458967 46.2948461, 12.4456434 46.288353, 12.4493526 46.2839768, 12.4617729 46.2811989, 12.4773316 46.2706086, 12.481627 46.2731689, 12.4915897 46.2761741, 12.4949654 46.2759626, 12.49626 46.2735603, 12.4992337 46.2747095, 12.5016051 46.2758814, 12.5010111 46.2809801, 12.5061065 46.2799657, 12.5081813 46.2764787, 12.5105246 46.2769796, 12.5158869 46.2754458, 12.5206902 46.277592, 12.5266364 46.2779236, 12.535501 46.2802927, 12.5394644 46.2826733, 12.5397281 46.284104, 12.5439543 46.2852004, 12.5435956 46.2861719, 12.5467765 46.2872369, 12.5479928 46.2865461, 12.5601589 46.2871999, 12.5634035 46.2885455, 12.5688778 46.2868173, 12.5698241 46.2833839, 12.5774227 46.2846555, 12.579658 46.2837236, 12.5825577 46.2811621, 12.5843878 46.275098, 12.5891327 46.2716961, 12.5875707 46.270784, 12.577374 46.2700148, 12.5819926 46.2639974, 12.5836027 46.2538278, 12.5949215 46.2541705, 12.5981748 46.2557976, 12.6033513 46.2553482, 12.5997873 46.24374, 12.6028928 46.2423353, 12.6067732 46.2430697, 12.6096235 46.2423099, 12.6117653 46.2384368, 12.6153545 46.2368517, 12.6136902 46.2349498, 12.6157483 46.2323344, 12.6217386 46.2322977, 12.6190568 46.2218706, 12.6161916 46.2202249, 12.6124102 46.213415, 12.6087569 46.2102391, 12.6122573 46.2059468, 12.6232023 46.2067503, 12.6261737 46.2052247, 12.6269182 46.2061366, 12.6284814 46.2025017, 12.6325407 46.2005525, 12.6349391 46.1997301, 12.6420365 46.1999054, 12.6552019 46.2049804, 12.6584048 46.2103548, 12.6579279 46.2170507, 12.6621351 46.2200361, 12.6664598 46.2194358, 12.670326 46.2201331, 12.6740677 46.2237764, 12.6780332 46.2255686, 12.6806361 46.2306844, 12.6834657 46.2323959, 12.68848 46.2334179, 12.6930235 46.2328193, 12.6946375 46.233873, 12.691275 46.2374441, 12.6962289 46.2463609, 12.7004755 46.2509053, 12.6987941 46.2536392, 12.6985354 46.2581298, 12.7012975 46.2570347, 12.7022516 46.2612769, 12.697788 46.2640982, 12.6977952 46.2673921, 12.7012657 46.2687132, 12.7040787 46.2718765, 12.7070686 46.2722961, 12.7105181 46.2756302, 12.7118842 46.2798686, 12.7103335 46.2815524, 12.711035 46.2836376, 12.7135051 46.2856, 12.7171906 46.2857743, 12.7185768 46.2868663, 12.7194301 46.2898031, 12.7156495 46.2910719, 12.7150873 46.2929526, 12.712873 46.2933111, 12.7110406 46.2979489, 12.7164138 46.3054637, 12.7239319 46.3104327, 12.7229808 46.3132608, 12.7181195 46.3141414, 12.7058107 46.3192339, 12.7052406 46.3221531, 12.7071319 46.3247392, 12.703927 46.3275491, 12.7022132 46.3313271, 12.6932043 46.338326, 12.6916666 46.3464852, 12.6979665 46.3511116, 12.7015491 46.3521577, 12.705373 46.3623226, 12.7178532 46.3723677, 12.7160036 46.374685, 12.7174868 46.3755283, 12.7164393 46.377745, 12.7145306 46.3788765, 12.714295 46.3774817, 12.7121113 46.3775737, 12.711053 46.381214, 12.7045298 46.3814053, 12.7024685 46.3791603, 12.6977899 46.3798914, 12.6983541 46.3785689, 12.6900971 46.3780634, 12.6848655 46.3789916, 12.6831329 46.3741335, 12.6806507 46.3751295, 12.6754575 46.3744443, 12.6736208 46.3727118, 12.6719613 46.3754581, 12.665483 46.3780551, 12.6638631 46.3758353, 12.6573099 46.3744469, 12.6559403 46.3721134, 12.6465093 46.3693262, 12.6440826 46.37109, 12.6441067 46.3745147, 12.6391016 46.3764286, 12.6313939 46.3751335, 12.6224464 46.378711, 12.6164078 46.3773308, 12.6165555 46.3791657, 12.613331 46.3821825, 12.6131091 46.3842564, 12.6062123 46.3881361, 12.6029055 46.3937285, 12.597862 46.3914191, 12.5967808 46.3962061, 12.5925485 46.3963757, 12.5902774 46.3954534, 12.5922017 46.3981313, 12.5915002 46.4007101, 12.5852769 46.3991369, 12.5839588 46.4011443, 12.5852113 46.4027472, 12.582365 46.4052715, 12.579533 46.4044975, 12.5714985 46.4065132, 12.5782977 46.4116868, 12.5763941 46.4142739, 12.5638583 46.412032, 12.5621557 46.415257, 12.5579379 46.4170511, 12.5556217 46.4217141, 12.5508859 46.4240381, 12.5499754 46.4297614, 12.5353885 46.431792, 12.5282906 46.4382921, 12.5242617 46.4399376, 12.5225094 46.4391301, 12.523317 46.441261, 12.5218516 46.4431938, 12.5174017 46.4444664, 12.5128829 46.4449204, 12.5043547 46.4398466, 12.4987903 46.4336281, 12.4999794 46.4314912, 12.4944709 46.4267195, 12.4934293 46.4225311, 12.4953091 46.4172911, 12.4991537 46.4125171))";

                heightMap = heightMap.ApplyGeometryMask(mask).BakeCoordinates();

                var hmapSRID = 4326; // dataset.SRID
                //    .BakeCoordinates();
                ModelGenerationTransform transform = new ModelGenerationTransform(bbox,
                    datasetSrid: hmapSRID,
                    outputSrid: Reprojection.SRID_PROJECTED_MERCATOR,
                    centerOnOrigin: true,
                    zFactor: 1.5f,
                    centerOnZOrigin: true);

                _logger.LogInformation($"Processing height map data ({heightMap.Count} coordinates)...");
                heightMap = transform.TransformHeightMap(heightMap).BakeCoordinates();

                //=======================
                // Textures
                //
                PBRTexture pbrTexture = null;
                if (withTexture)
                {


                    Console.WriteLine("Download image tiles...");
                    var bboxImagery = bbox.ReprojectTo(hmapSRID, 4326);
                    TileRange tiles = _imageryService.ComputeBoundingBoxTileRange(bboxImagery, provider, TEXTURE_TILES);
                    tiles = _imageryService.DownloadTiles(tiles, provider);
                    string fileName = Path.Combine(outputDir, "Texture.jpg");

                    Console.WriteLine("Construct texture...");
                    TextureInfo texInfo = _imageryService.ConstructTexture(tiles, bboxImagery, fileName, TextureImageFormat.image_jpeg);

                    //
                    //=======================

                    //=======================
                    // Normal map
                    Console.WriteLine("Height map...");
                    //float Z_FACTOR = 0.00002f;

                    //hMap = hMap.CenterOnOrigin().ZScale(Z_FACTOR);
                    //var normalMap = _imageryService.GenerateNormalMap(heightMap, outputDir);

                    pbrTexture = PBRTexture.Create(texInfo);

                    //hMap = hMap.CenterOnOrigin(Z_FACTOR);
                    //
                    //=======================
                }
                // Triangulate height map
                // and add base and sides
                _logger.LogInformation($"Triangulating height map and generating 3D mesh...");

                var model = _sharpGltfService.CreateTerrainMesh(heightMap, pbrTexture, reduceFactor: 0.025f);
                model.SaveGLB(Path.Combine(Directory.GetCurrentDirectory(), modelName + ".glb"));
                //model.SaveAsWavefront(Path.Combine(Directory.GetCurrentDirectory(), modelName + ".obj"));

                //model = _sharpGltfService.CreateTerrainMesh(heightMap, GenOptions.Normals | GenOptions.BoxedBaseElevationMin);
                //model.SaveGLB(Path.Combine(Directory.GetCurrentDirectory(), modelName + "_normalsBox.glb"));
                //model.SaveAsWavefront(Path.Combine(Directory.GetCurrentDirectory(), modelName + "_normalsBox.obj"));

                _logger.LogInformation($"Model exported as {Path.Combine(Directory.GetCurrentDirectory(), modelName + ".gltf")} and .glb");

                _logger.LogInformation($"Done in {sw.Elapsed:g}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }
    }
}
