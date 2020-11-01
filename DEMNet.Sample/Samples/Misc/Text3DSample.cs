﻿using DEM.Net.Core;
using DEM.Net.Core.Imagery;
using DEM.Net.Core.Voronoi;
using DEM.Net.glTF.SharpglTF;
using Microsoft.Extensions.Logging;
using SharpGLTF.Schema2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SampleApp
{
    public class Text3DSample
    {
        private readonly ILogger<Text3DSample> _logger;
        private readonly ElevationService _elevationService;
        private readonly RasterService _rasterService;
        private readonly ImageryService _imageryService;
        private readonly SharpGltfService _sharpGltfService;
        private readonly MeshService _meshService;

        public Text3DSample(ILogger<Text3DSample> logger
                , ElevationService elevationService
                , MeshService meshService
                , RasterService rasterService
                , ImageryService imageryService
                , SharpGltfService sharpGltfService)
        {
            _logger = logger;
            _elevationService = elevationService;
            _rasterService = rasterService;
            _imageryService = imageryService;
            _sharpGltfService = sharpGltfService;
            _meshService = meshService;
        }
        public void Run()
        {




            // DjebelMarra
            var bbox = GeometryService.GetBoundingBox("POLYGON((7.713614662985839 46.03014517094771,7.990332802634277 46.03014517094771,7.990332802634277 45.86877753239648,7.713614662985839 45.86877753239648,7.713614662985839 46.03014517094771))");
            var dataset = DEMDataSet.NASADEM;

            string modelName = $"{dataset.Name}_{DateTime.Now:yyyyMMdd_HHmmss}";
            string outputDir = Directory.GetCurrentDirectory();

            //var modelTest = _sharpGltfService.CreateNewModel();
            //var triangulation = CreateText("20 km", VectorsExtensions.CreateColor(255, 255, 255));
            //_sharpGltfService.AddMesh(modelTest, "Text", triangulation);

            //// Save model
            //modelTest.SaveGLB(Path.Combine(outputDir, modelName + ".glb"));



            var modelAndBbox = GenerateSampleModel(bbox, dataset, withTexture: true);
            if (modelAndBbox.Model != null)
            {
                var model = modelAndBbox.Model;

                // bbox size
                float projHeight = (float)modelAndBbox.projectedBbox.Height;
                float arrowSizeFactor = projHeight / 3f;
                float width = (float)modelAndBbox.widthMeters;
                float zCenter = (float)modelAndBbox.averageElevation;
                float projWidth = (float)modelAndBbox.projectedBbox.Width;
                // 
                float PI = (float)Math.PI;

                // Arrow
                TriangulationList<Vector3> adornments = _meshService.CreateArrow().ToGlTFSpace()
                    .Scale(arrowSizeFactor)
                    .Translate(new Vector3(-projWidth * 0.55f, 0, zCenter));

                // North text 'N'
                adornments += CreateText("N", VectorsExtensions.CreateColor(255, 255, 255)).ToGlTFSpace()
                           .Scale(projHeight / 200f / 5f)
                           .RotateX(-PI / 2)
                           .Translate(new Vector3(-projWidth * 0.55f, arrowSizeFactor * 1.1f, zCenter));

                // Scale bar
                var scaleBar = CreateScaleBar(width, projWidth, radius: projHeight / 200f).ToGlTFSpace();
                var scaleBarSize = scaleBar.GetBoundingBox().Height;
                adornments += scaleBar
                    .RotateZ(PI / 2f)
                    .Translate(new Vector3(projWidth / 2, -projHeight / 2 - projHeight * 0.05f, zCenter));

                var text = CreateText($"{dataset.Attribution.Subject}: {dataset.Attribution.Text}{Environment.NewLine}{ImageryProvider.MapBoxSatellite.Attribution.Subject}: {ImageryProvider.MapBoxSatellite.Attribution.Text}", VectorsExtensions.CreateColor(255, 255, 255)).ToGlTFSpace();
                var scale = ((projWidth - scaleBarSize) * 0.9f) / text.GetBoundingBox().Width;

                text = text.Scale((float)scale)
                                .RotateX(-PI / 2)
                                .Translate(new Vector3(-projWidth * 0.25f, -projHeight * 0.55f, zCenter));
                adornments += text;


                // add adornments
                model = _sharpGltfService.AddMesh(model, "Adornments", adornments, default(Vector4), doubleSided: true);

                // Save model
                model.SaveGLB(Path.Combine(outputDir, modelName + ".glb"));

                _logger.LogInformation($"Model exported as {Path.Combine(outputDir, modelName + ".glb")}");
            }
        }

        private TriangulationList<Vector3> CreateScaleBar(float modelSize4326, float modelSizeProjected, float radius = 10f)
        {
            int nSteps = 4;
            ScaleBarInfo scaleInfo = GetScaleBarWidth(modelSize4326, modelSizeProjected, scaleBarSizeRelativeToModel: 0.5f, nSteps);

            Vector3 currentPosition = Vector3.Zero;
            TriangulationList<Vector3> triangulation = new TriangulationList<Vector3>();
            for (int i = 0; i < nSteps; i++)
            {
                currentPosition.Z = scaleInfo.StepSizeProjected * i;

                triangulation += _meshService.CreateCylinder(currentPosition, radius, scaleInfo.StepSizeProjected
                        , color: i % 2 == 0 ? VectorsExtensions.CreateColor(0, 0, 0) : VectorsExtensions.CreateColor(255, 255, 255));
            }

            // scale units (m or km ?)
            string scaleLabel = (scaleInfo.TotalSize / 1000f > 1) ? $"{scaleInfo.TotalSize / 1000:F0} km" : $"{scaleInfo.TotalSize} m";

            triangulation += CreateText(scaleLabel, color: VectorsExtensions.CreateColor(255, 255, 255))
                            .Scale(radius / 5)
                            .RotateY((float)Math.PI / 2)
                            .RotateZ((float)Math.PI / 2)
                            .Translate(new Vector3(radius * 5, -radius, scaleInfo.TotalSizeProjected / 2));

            return triangulation;
        }

        private struct ScaleBarInfo
        {
            public float TotalSize;
            public float TotalSizeProjected;
            public int NumSteps;
            public float StepSize;
            public float StepSizeProjected;
        }
        private ScaleBarInfo GetScaleBarWidth(float totalWidth, float modelSizeProjected, float scaleBarSizeRelativeToModel = 0.5f, int nSteps = 4)
        {
            // must be divisible by 4
            float[] smallestScaleStep = { 1, 2, 5, 10, 20, 25, 50, 100, 250, 500, 1000, 2000, 2500, 5000, 10000, 20000, 25000, 50000, 100000, 200000, 500000, 1000000, 2000000, 5000000, 10000000 };

            var scaleBarTotalRequestedSize = totalWidth * scaleBarSizeRelativeToModel;
            var bestScale = smallestScaleStep.Select(s => new { Step = s, diff = Math.Abs(1 - (scaleBarTotalRequestedSize / (s * nSteps))) })
                              .OrderBy(s => s.diff)
                              .First();
            var scaleBarTotalSize = bestScale.Step * nSteps;

            var projSize = MathHelper.Map(0, totalWidth, 0, modelSizeProjected, scaleBarTotalSize, false);
            var projStepSize = MathHelper.Map(0, totalWidth, 0, modelSizeProjected, bestScale.Step, false);
            return new ScaleBarInfo
            {
                NumSteps = nSteps,
                TotalSizeProjected = projSize,
                StepSizeProjected = projStepSize,
                StepSize = bestScale.Step,
                TotalSize = scaleBarTotalSize
            };

        }

        public TriangulationList<Vector3> CreateText(string text, Vector4 color)
        {
            List<Polygon<Vector3>> letterPolygons = GetTextPolygons(text);
            TriangulationList<Vector3> triangulation = _meshService.Extrude(letterPolygons);
            triangulation.Colors = triangulation.Positions.Select(p => color).ToList();
            triangulation = triangulation.CenterOnOrigin();
            return triangulation;
        }

        public List<Polygon<Vector3>> GetTextPolygons(string text)
        {
            Dictionary<int, Polygon<Vector3>> letterPolygons = new Dictionary<int, Polygon<Vector3>>();

            using (Bitmap bmp = new Bitmap(400, 400))
            using (GraphicsPath gp = new GraphicsPath())
            using (Graphics g = Graphics.FromImage(bmp))
            using (Font f = new Font("Tahoma", 40f))
            {
                //g.ScaleTransform(4, 4);
                gp.AddString(text, f.FontFamily, 0, 40f, new Point(0, 0), StringFormat.GenericDefault);
                g.DrawPath(Pens.Gray, gp);
                gp.Flatten(new Matrix(), 0.1f);  // <<== *
                //g.DrawPath(Pens.DarkSlateBlue, gp);
                //gp.SetMarkers();

                using (GraphicsPathIterator gpi = new GraphicsPathIterator(gp))
                {

                    gpi.Rewind();

                    var triangulation = new TriangulationList<Vector3>();
                    using (GraphicsPath gsubPath = new GraphicsPath())
                    {


                        // Read all subpaths and their properties  
                        for (int i = 0; i < gpi.SubpathCount; i++)
                        {
                            gpi.NextSubpath(gsubPath, out bool bClosedCurve);
                            Debug.Assert(bClosedCurve, "Unclosed character. That's not possible");

                            var currentRing = gsubPath.PathPoints.Select(p => new Vector3(p.X, p.Y, 0)).ToList();

                            List<int> childs = GetIncludedPolygons(currentRing, letterPolygons);
                            List<int> parents = GetContainerPolygons(currentRing, letterPolygons);
                            // contains other polygon ?
                            if (childs.Any())
                            {
                                Polygon<Vector3> newPoly = new Polygon<Vector3>(currentRing);
                                foreach (var key in childs)
                                {
                                    letterPolygons.Remove(key, out var child);
                                    newPoly.InteriorRings.Add(child.ExteriorRing);
                                }
                                letterPolygons.Add(i, newPoly);
                            }
                            else if (parents.Any())
                            {
                                Debug.Assert(parents.Count == 1);
                                letterPolygons[parents.First()].InteriorRings.Add(currentRing);
                            }
                            else
                            {
                                letterPolygons.Add(i, new Polygon<Vector3>(currentRing));
                            }

                            // triangulation += _meshService.Tesselate(currentLetterPoints, currentLetterPointsInt);

                            gsubPath.Reset();
                        }
                    }
                }
            }

            return letterPolygons.Values.ToList();
        }

        private List<int> GetContainerPolygons(List<Vector3> currentPolygon, Dictionary<int, Polygon<Vector3>> polygons)
        {
            List<int> parents = polygons.Where(p => IsPointInPolygon(p.Value.ExteriorRing, currentPolygon[0]))
                                        .Select(p => p.Key)
                                        .ToList();
            return parents;
        }

        private List<int> GetIncludedPolygons(List<Vector3> currentPolygon, Dictionary<int, Polygon<Vector3>> polygons)
        {
            List<int> childs = polygons.Where(p => IsPointInPolygon(currentPolygon, p.Value.ExteriorRing[0]))
                                        .Select(p => p.Key)
                                        .ToList();
            return childs;
        }



        /// <summary>
        /// https://stackoverflow.com/questions/4243042/c-sharp-point-in-polygon
        /// Determines if the given point is inside the polygon. (does not support inner 'holes' rings)
        /// </summary>
        /// <param name="polygon">the vertices of polygon</param>
        /// <param name="testPoint">the given point</param>
        /// <returns>true if the point is inside the polygon; otherwise, false</returns>
        private bool IsPointInPolygon(List<Vector3> polygon, Vector3 testPoint)
        {
            bool result = false;
            int j = polygon.Count - 1;
            for (int i = 0; i < polygon.Count; i++)
            {
                if (polygon[i].Y < testPoint.Y && polygon[j].Y >= testPoint.Y || polygon[j].Y < testPoint.Y && polygon[i].Y >= testPoint.Y)
                {
                    if (polygon[i].X + (testPoint.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) * (polygon[j].X - polygon[i].X) < testPoint.X)
                    {
                        result = !result;
                    }
                }
                j = i;
            }
            return result;
        }

        private (ModelRoot Model, double widthMeters, double heightMeters, double averageElevation, BoundingBox projectedBbox) GenerateSampleModel(BoundingBox bbox, DEMDataSet dataset, bool withTexture = true)
        {

            try
            {

                int TEXTURE_TILES = 8; // 4: med, 8: high
                string outputDir = Directory.GetCurrentDirectory();

                //_rasterService.GenerateDirectoryMetadata(dataset, false);

                ImageryProvider provider = ImageryProvider.MapBoxSatellite;// new TileDebugProvider(new GeoPoint(43.5,5.5));


                _logger.LogInformation($"Getting height map data...");

                var heightMap = _elevationService.GetHeightMap(ref bbox, dataset);

                var wgs84bbox = bbox;
                ModelGenerationTransform transform = new ModelGenerationTransform(bbox, 3857, true, 1.5f, true);

                _logger.LogInformation($"Processing height map data ({heightMap.Count} coordinates)...");
                heightMap = transform.TransformHeightMap(heightMap);


                //=======================
                // Textures
                //
                PBRTexture pbrTexture = null;
                if (withTexture)
                {


                    Console.WriteLine("Download image tiles...");
                    TileRange tiles = _imageryService.DownloadTiles(bbox, provider, TEXTURE_TILES);
                    string fileName = Path.Combine(outputDir, "Texture.jpg");

                    Console.WriteLine("Construct texture...");
                    TextureInfo texInfo = _imageryService.ConstructTexture(tiles, bbox, fileName, TextureImageFormat.image_jpeg);

                    //
                    //=======================

                    //=======================
                    // Normal map
                    Console.WriteLine("Height map...");
                    //float Z_FACTOR = 0.00002f;

                    //hMap = hMap.CenterOnOrigin().ZScale(Z_FACTOR);
                    //var normalMap = _imageryService.GenerateNormalMap(heightMap, outputDir);

                    pbrTexture = PBRTexture.Create(texInfo, null);

                    //hMap = hMap.CenterOnOrigin(Z_FACTOR);
                    //
                    //=======================
                }
                // Triangulate height map
                // and add base and sides
                _logger.LogInformation($"Triangulating height map and generating 3D mesh...");

                var model = _sharpGltfService.CreateTerrainMesh(heightMap, pbrTexture);

                var width = new GeoPoint(wgs84bbox.Center[1], bbox.Center[0] - bbox.Width / 2f).DistanceTo(new GeoPoint(wgs84bbox.Center[1], bbox.Center[0] + bbox.Width / 2f));
                var height = new GeoPoint(bbox.Center[1] - bbox.Height / 2f, wgs84bbox.Center[0]).DistanceTo(new GeoPoint(bbox.Center[1] + bbox.Height, wgs84bbox.Center[0]));

                return (model, width, height, wgs84bbox.Center[2], heightMap.BoundingBox);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return (null, 0, 0, 0, null);
            }
        }
    }

}
