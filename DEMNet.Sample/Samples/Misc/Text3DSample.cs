using DEM.Net.Core;
using DEM.Net.Core.Imagery;
using DEM.Net.Core.Voronoi;
using DEM.Net.glTF.SharpglTF;
using Microsoft.Extensions.Logging;
using SharpGLTF.Schema2;
using System;
using System.Collections.Generic;
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
            var bbox = new BoundingBox(24.098067346557492, 24.42468219234563, 12.7769822830208, 13.087504129660111);
            var dataset = DEMDataSet.SRTM_GL3;

            string modelName = $"{dataset.Name}_{DateTime.Now:yyyyMMdd_HHmmss}";
            string outputDir = Directory.GetCurrentDirectory();

            var northText = CreateText("8".ToUpper()).Scale(10)
                .ToGlTFSpace();
            var textModel = _sharpGltfService.AddMesh(_sharpGltfService.CreateNewModel(), "Text", northText);

            textModel.SaveGLB(Path.Combine(outputDir, modelName + ".glb"));


            var modelAndBbox = GenerateSampleModel(bbox, dataset, withTexture: true);
            if (modelAndBbox.Model != null)
            {
                var model = modelAndBbox.Model;

                // bbox size
                float arrowSizeFactor = (float)modelAndBbox.ProjectedBbox.Height / 3f;
                float width = (float)modelAndBbox.ProjectedBbox.Width;
                float zCenter = (float)modelAndBbox.ProjectedBbox.Center[1];
                // 
                float PI = (float)Math.PI;
                var arrow = _meshService.CreateArrow()
                    .Scale(arrowSizeFactor)
                    .Translate(new Vector3(-width * 0.6f, 0, zCenter))
                    .ToGlTFSpace();
                model = _sharpGltfService.AddMesh(model, "Arrow", arrow);



                //var northText = CreateText("N").Scale(arrowSizeFactor)
                //    .Translate(new Vector3(-width * 0.6f, arrowSizeFactor * 1.2f, zCenter))
                //    .ToGlTFSpace();

                //model = _sharpGltfService.AddMesh(_sharpGltfService.CreateNewModel(), "Text", northText);

                //arrow += _meshService.CreateCylinder(new Vector3(0, 0, 0), 50, 250, VectorsExtensions.CreateColor(0, 255, 0));



                model.SaveGLB(Path.Combine(outputDir, modelName + ".glb"));



                _logger.LogInformation($"Model exported as {Path.Combine(outputDir, modelName + ".glb")}");
            }
        }

        public TriangulationList<Vector3> CreateText(string text)
        {

            using (Bitmap bmp = new Bitmap(400, 400))
            using (GraphicsPath gp = new GraphicsPath())
            using (Graphics g = Graphics.FromImage(bmp))
            using (Font f = new Font("Tahoma", 40f))
            {
                g.ScaleTransform(4, 4);
                gp.AddString(text, f.FontFamily, 0, 40f, new Point(0, 0), StringFormat.GenericDefault);
                g.DrawPath(Pens.Gray, gp);
                gp.Flatten(new Matrix(), 0.1f);  // <<== *
                //g.DrawPath(Pens.DarkSlateBlue, gp);
                //gp.SetMarkers();

                var globalBounds = gp.GetBounds();
                var avgCharSize = globalBounds.Width / text.Length;

                using (GraphicsPathIterator gpi = new GraphicsPathIterator(gp))
                {

                    gpi.Rewind();

                    var triangulation = new TriangulationList<Vector3>();
                    using (GraphicsPath gsubPath = new GraphicsPath())
                    {
                        RectangleF bounds = RectangleF.Empty;
                        List<Vector3> currentLetterPoints = new List<Vector3>();
                        List<List<Vector3>> currentLetterPointsInt = new List<List<Vector3>>();
                        // Read all subpaths and their properties  
                        for (int i = 0; i < gpi.SubpathCount; i++)
                        {
                            bool bClosedCurve;
                            gpi.NextSubpath(gsubPath, out bClosedCurve);

                            var curBounds = gsubPath.GetBounds();
                            bool isInside = bounds.Contains(curBounds);
                            bool isEnclosing = curBounds.Contains(bounds);
                            bool isIntersecting = curBounds.IntersectsWith(bounds);
                            var closeness = (curBounds.X + curBounds.Width / 2f) + avgCharSize / 2f - bounds.X;
                            bool isAnotherChar = closeness < avgCharSize * 0.25f;

                            if (isInside)
                            {
                                var points = gsubPath.PathPoints.Select(p => new Vector3(p.X, p.Y, 0));
                                currentLetterPointsInt.Add(new List<Vector3>(points));
                            }
                            else
                            {
                                if (currentLetterPoints.Count > 0)
                                {
                                    triangulation += _meshService.Tesselate(currentLetterPoints, currentLetterPointsInt);
                                    currentLetterPoints.Clear();
                                    currentLetterPointsInt.Clear();
                                    bounds = curBounds;
                                }
                                else if (bounds == RectangleF.Empty)
                                {
                                    bounds = curBounds;
                                }
                                var points = gsubPath.PathPoints.Select(p => new Vector3(p.X, p.Y, 0));
                                currentLetterPoints.AddRange(points);
                               

                            }
                            gsubPath.Reset();


                        }
                        if (currentLetterPoints.Count > 0)
                        {
                            triangulation += _meshService.Tesselate(currentLetterPoints, currentLetterPointsInt);
                        }
                    }

                    ////var points = gp.PathPoints.Select(p => new Vector3(p.X, p.Y, 0)).ToList();
                    ////var triangulation = _meshService.Tesselate(points, null);

                    //// Triangulate wall for each ring
                    //// (We add floor indices before copying the vertices, they will be duplicated and z shifted later on)
                    //List<int> numVerticesPerRing = new List<int>();
                    //numVerticesPerRing.Add(points.Count - 1);
                    ////numVerticesPerRing.AddRange(building.InteriorRings.Select(r => r.Count - 1));
                    //triangulation = this.TriangulateRingsWalls(triangulation, numVerticesPerRing);

                    //// Roof
                    //// Building has real elevations

                    //// Create floor vertices by copying roof vertices and setting their z min elevation (floor or min height)
                    //var floorVertices = triangulation.Positions.Select(pt => new Vector3(pt.X, pt.Y, -10)).ToList();
                    //triangulation.Positions.AddRange(floorVertices);

                    return triangulation;
                }
                for (int i = 0; i < gp.PathPoints.Length; i++)
                {
                    PointF p = gp.PathPoints[i];
                    g.FillEllipse(Brushes.DarkOrange, p.X - 1, p.Y - 1, 2, 2);
                }
            }
        }

        /// <summary>
        /// https://stackoverflow.com/questions/4243042/c-sharp-point-in-polygon
        /// Determines if the given point is inside the polygon
        /// </summary>
        /// <param name="polygon">the vertices of polygon</param>
        /// <param name="testPoint">the given point</param>
        /// <returns>true if the point is inside the polygon; otherwise, false</returns>
        public static bool IsPointInPolygon4(PointF[] polygon, PointF testPoint)
        {
            bool result = false;
            int j = polygon.Count() - 1;
            for (int i = 0; i < polygon.Count(); i++)
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

        private TriangulationList<Vector3> TriangulateRingsWalls(TriangulationList<Vector3> triangulation, List<int> numVerticesPerRing)
        {
            int offset = numVerticesPerRing.Sum();

            int ringOffset = 0;
            foreach (var numRingVertices in numVerticesPerRing)
            {
                int i = 0;
                do
                {
                    triangulation.Indices.Add(ringOffset + i);
                    triangulation.Indices.Add(ringOffset + i + offset);
                    triangulation.Indices.Add(ringOffset + i + 1);

                    triangulation.Indices.Add(ringOffset + i + offset);
                    triangulation.Indices.Add(ringOffset + i + offset + 1);
                    triangulation.Indices.Add(ringOffset + i + 1);

                    i++;
                }
                while (i < numRingVertices - 1);

                // Connect last vertices to start vertices
                triangulation.Indices.Add(ringOffset + i);
                triangulation.Indices.Add(ringOffset + i + offset);
                triangulation.Indices.Add(ringOffset + 0);

                triangulation.Indices.Add(ringOffset + i + offset);
                triangulation.Indices.Add(ringOffset + 0 + offset);
                triangulation.Indices.Add(ringOffset + 0);

                ringOffset += numRingVertices;

            }
            return triangulation;
        }
        private (ModelRoot Model, BoundingBox ProjectedBbox) GenerateSampleModel(BoundingBox bbox, DEMDataSet dataset, bool withTexture = true)
        {

            try
            {

                int TEXTURE_TILES = 4; // 4: med, 8: high
                string outputDir = Directory.GetCurrentDirectory();

                //_rasterService.GenerateDirectoryMetadata(dataset, false);

                ImageryProvider provider = ImageryProvider.EsriWorldImagery;// new TileDebugProvider(new GeoPoint(43.5,5.5));


                _logger.LogInformation($"Getting height map data...");

                var heightMap = _elevationService.GetHeightMap(ref bbox, dataset);
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
                    var normalMap = _imageryService.GenerateNormalMap(heightMap, outputDir);

                    pbrTexture = PBRTexture.Create(texInfo, normalMap);

                    //hMap = hMap.CenterOnOrigin(Z_FACTOR);
                    //
                    //=======================
                }
                // Triangulate height map
                // and add base and sides
                _logger.LogInformation($"Triangulating height map and generating 3D mesh...");

                var model = _sharpGltfService.CreateTerrainMesh(heightMap, pbrTexture);
                return (model, heightMap.BoundingBox);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return (null, null);
            }
        }
    }

}
