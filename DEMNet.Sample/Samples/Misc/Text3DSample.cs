using DEM.Net.Core;
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
            var bbox = GeometryService.GetBoundingBox("POLYGON((5.393053755022272 43.653840622859114,5.816027387834772 43.653840622859114,5.816027387834772 43.37498595774968,5.393053755022272 43.37498595774968,5.393053755022272 43.653840622859114))");
            var dataset = DEMDataSet.SRTM_GL3;

            string modelName = $"{dataset.Name}_{DateTime.Now:yyyyMMdd_HHmmss}";
            string outputDir = Directory.GetCurrentDirectory();

            var modelAndBbox = GenerateSampleModel(bbox, dataset, withTexture: true);
            if (modelAndBbox.Model != null)
            {
                var model = modelAndBbox.Model;

                // bbox size
                float height = (float)modelAndBbox.ProjectedBbox.Height;
                float arrowSizeFactor = height / 3f;
                float width = (float)modelAndBbox.ProjectedBbox.Width;
                float zCenter = (float)modelAndBbox.ProjectedBbox.Center[2];
                // 
                float PI = (float)Math.PI;

                // Arrow
                TriangulationList<Vector3> adornments = _meshService.CreateArrow().ToGlTFSpace()
                    .Scale(arrowSizeFactor)
                    .Translate(new Vector3(-width * 0.6f, 0, zCenter));

                // North text 'N'
                adornments += CreateText("N", VectorsExtensions.CreateColor(255, 255, 255)).ToGlTFSpace()
                           .Scale(height / 200f / 5f)
                           .RotateX(-PI / 2)
                           .Translate(new Vector3(-width * 0.6f, arrowSizeFactor * 1.1f, zCenter));

                // Scale bar
                adornments += CreateScaleBar(width, radius: height / 200f).ToGlTFSpace()
                    .RotateZ(PI / 2f)
                    .Translate(new Vector3(width / 2, -height / 2 - height * 0.05f, zCenter));

                // add adornments
                model = _sharpGltfService.AddMesh(model, "Adornments", adornments);

                // Save model
                model.SaveGLB(Path.Combine(outputDir, modelName + ".glb"));

                _logger.LogInformation($"Model exported as {Path.Combine(outputDir, modelName + ".glb")}");
            }
        }

        private TriangulationList<Vector3> CreateScaleBar(float modelSize, float radius = 10f)
        {
            int nSteps = 4;
            ScaleBarInfo scaleInfo = GetScaleBarWidth(modelSize, scaleBarSizeRelativeToModel: 0.5f, nSteps);

            Vector3 currentPosition = Vector3.Zero;
            TriangulationList<Vector3> triangulation = new TriangulationList<Vector3>();
            for (int i = 0; i < nSteps; i++)
            {
                currentPosition.Z = scaleInfo.StepSize * i;

                triangulation += _meshService.CreateCylinder(currentPosition, radius, scaleInfo.StepSize
                        , color: i % 2 == 0 ? VectorsExtensions.CreateColor(0, 0, 0) : VectorsExtensions.CreateColor(255, 255, 255));
            }

            // scale units (m or km ?)
            string scaleLabel = (scaleInfo.TotalSize / 1000f > 1) ? $"{scaleInfo.TotalSize / 1000:F0} km" : $"{scaleInfo.TotalSize} m";

            triangulation += CreateText(scaleLabel, color: VectorsExtensions.CreateColor(255, 255, 255))
                            .Scale(radius / 5)
                            .RotateY((float)Math.PI / 2)
                            .RotateZ((float)Math.PI / 2)
                            .Translate(new Vector3(radius * 5, 0, scaleInfo.TotalSize / 2));

            return triangulation;
        }

        private struct ScaleBarInfo
        {
            public float TotalSize;
            public int NumSteps;
            public float StepSize;
        }
        private ScaleBarInfo GetScaleBarWidth(float totalWidth, float scaleBarSizeRelativeToModel = 0.5f, int nSteps = 4)
        {
            // must be divisible by 4
            float[] smallestScaleStep = { 1, 2, 5, 10, 20, 25, 50, 100, 250, 500, 1000, 2000, 2500, 5000, 10000, 20000, 25000, 50000, 100000, 200000, 500000, 1000000, 2000000, 5000000, 10000000 };

            var scaleBarTotalSize = totalWidth * scaleBarSizeRelativeToModel;
            var bestScale = smallestScaleStep.Select(s => new { Step = s, diff = Math.Abs(1 - (scaleBarTotalSize / (s * nSteps))) })
                              .OrderBy(s => s.diff)
                              .First();


            return new ScaleBarInfo { NumSteps = nSteps, TotalSize = bestScale.Step * nSteps, StepSize = bestScale.Step };

        }

        public TriangulationList<Vector3> CreateText(string text, Vector4 color)
        {
            Dictionary<int, Polygon<Vector3>> letterPolygons = GetTextPolygons(text);
            TriangulationList<Vector3> triangulation = new TriangulationList<Vector3>();

            foreach (var letter in letterPolygons)
            {
                triangulation += _meshService.Tesselate(letter.Value.ExteriorRing, letter.Value.InteriorRings)
                                             .Extrude(10);
            }
            triangulation.Colors = triangulation.Positions.Select(p => color).ToList();
            triangulation = triangulation.CenterOnOrigin();

            int numFootPrintIndices = triangulation.Indices.Count;

            /////
            // Now extrude it (build the sides)
            // Algo
            // First triangulate the foot print (with inner rings if existing)
            // This triangulation is the roof top if building is flat

            int totalPoints = triangulation.Positions.Count;
            int totalCheck = letterPolygons.Values.Sum(v=> v.ExteriorRing.Count-1 + v.InteriorRings.Sum(r=>r.Count-1));

            // Triangulate wall for each ring
            // (We add floor indices before copying the vertices, they will be duplicated and z shifted later on)
            List<int> numVerticesPerRing = new List<int>();
            numVerticesPerRing.Add(building.ExteriorRing.Count - 1);
            numVerticesPerRing.AddRange(building.InteriorRings.Select(r => r.Count - 1));
            triangulation = this.TriangulateRingsWalls(triangulation, numVerticesPerRing, totalPoints);

            // Roof
            // Building has real elevations

            // Create floor vertices by copying roof vertices and setting their z min elevation (floor or min height)
            var floorVertices = triangulation.Positions.Select(pt => pt.Clone(building.ComputedFloorAltitude)).ToList();
            triangulation.Positions.AddRange(floorVertices);

            // Take the first vertices and z shift them
            foreach (var pt in triangulation.Positions.Take(totalPoints))
            {
                pt.Elevation = building.ComputedRoofAltitude;
            }

            //==========================
            // Colors: if walls and roof color is the same, all vertices can have the same color
            // otherwise we must duplicate vertices to ensure consistent triangles color (avoid unrealistic shades)
            // AND shift the roof triangulation indices
            // Before:
            //      Vertices: <roof_wallcolor_0..i> / <floor_wallcolor_i..j>
            //      Indices: <roof_triangulation_0..i> / <roof_wall_triangulation_0..j>
            // After:
            //      Vertices: <roof_wallcolor_0..i> / <floor_wallcolor_i..j> // <roof_roofcolor_j..k>
            //      Indices: <roof_triangulation_j..k> / <roof_wall_triangulation_0..j>
            Vector4 DefaultColor = Vector4.One;
            bool mustCopyVerticesForRoof = (building.Color ?? DefaultColor) != (building.RoofColor ?? building.Color);
            // assign wall or default color to all vertices
            triangulation.Colors = triangulation.Positions.Select(p => building.Color ?? DefaultColor).ToList();

            if (mustCopyVerticesForRoof)
            {
                triangulation.Positions.AddRange(triangulation.Positions.Take(totalPoints));
                triangulation.Colors.AddRange(Enumerable.Range(1, totalPoints).Select(_ => building.RoofColor ?? DefaultColor));

                // shift roof triangulation indices
                for (int i = 0; i < numFootPrintIndices; i++)
                {
                    triangulation.Indices[i] += (triangulation.Positions.Count - totalPoints);
                }

            }

            Debug.Assert(triangulation.Colors.Count == 0 || triangulation.Colors.Count == triangulation.Positions.Count);

            return triangulation;




            //if (currentLetterPoints.Count > 0)
            //{
            //    triangulation += _meshService.Tesselate(currentLetterPoints, currentLetterPointsInt);
            //}


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



        }

        public Dictionary<int, Polygon<Vector3>> GetTextPolygons(string text)
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

            return letterPolygons;
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
