//
// CustomSamples.cs
//
// Author:
//       Xavier Fischer
//
// Copyright (c) 2025 
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
using DEM.Net.Core.Interpolation;
using DEM.Net.glTF.SharpglTF;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using SixLabors.ImageSharp.Formats.Png;

namespace SampleApp
{
    //
    // See sample pics in Landscape directory
    // Great formulas here : https://www.movable-type.co.uk/scripts/latlong.html
    //
    public class LandscapeSample
    {
        private readonly ILogger<LandscapeSample> _logger;
        private readonly ElevationService _elevationService;
        private readonly RasterService _rasterService;
        private readonly MeshService _meshService;
        private const double RADIAN = Math.PI / 180;

        public LandscapeSample(ILogger<LandscapeSample> logger,
                ElevationService elevationService,
                RasterService rasterService,
                MeshService meshService)
        {
            _logger = logger;
            _elevationService = elevationService;
            _rasterService = rasterService;
            _meshService = meshService;
        }

        public void Run()
        {
            // Define a triangle in 3D space
            Vector3[] vertices = [
                new (-1, -1, 5), // Vertex 1 (in front of the camera, at Z=5)
                new (1, -1, 5),  // Vertex 2 (in front of the camera, at Z=5)
                new (0, 1, 5)    // Vertex 3 (in front of the camera, at Z=5)
            ];

            // Define the camera position and direction
            Vector3 cameraPosition = new(0, 0, 0);  // Camera at the origin
            Vector3 cameraTarget = new(0, 0, 1);    // Looking down the Z axis
            Vector3 upVector = Vector3.UnitY;               // "Up" is in the Y direction

            Matrix4x4 viewMatrix = Matrix4x4.CreateLookAt(cameraPosition, cameraTarget, upVector);

            // Define the perspective projection matrix
            float fieldOfView = (float)(Math.PI / 4);  // 45 degree field of view
            float aspectRatio = 800f / 600f;           // Aspect ratio of the image
            float nearPlane = 0.1f;
            float farPlane = 1000f;

            Matrix4x4 projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
                fieldOfView, aspectRatio, nearPlane, farPlane);


            // Create an 800x600 image to draw on
            using (Image<Rgba32> image = new Image<Rgba32>(800, 600, Color.Black))
            {
                // Project each 3D vertex to 2D screen space
                Vector3[] projectedVertices = new Vector3[3];
                for (int i = 0; i < 3; i++)
                {
                    projectedVertices[i] = ProjectVertex(vertices[i], viewMatrix, projectionMatrix, 800, 600);
                }

                // Draw the triangle
                DrawTriangle(image, projectedVertices);

                // Save the image
                image.Save("triangle.png");
            }
        }

        void DrawTriangle(Image<Rgba32> image, Vector3[] vertices2D)
        {
            var penColor = Color.White;
            float thickness = 1.0f;

            image.Mutate(ctx =>
            {
                // Draw lines between vertices
                ctx.DrawLine(penColor, thickness,
                    new PointF(vertices2D[0].X, vertices2D[0].Y),
                    new PointF(vertices2D[1].X, vertices2D[1].Y),
                    new PointF(vertices2D[2].X, vertices2D[2].Y),
                    new PointF(vertices2D[0].X, vertices2D[0].Y) // Close the triangle
                );
            });
        }

        Vector3 ProjectVertex(Vector3 vertex, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix, int width, int height)
        {
            // Apply the view and projection transformation
            Vector4 transformedVertex = Vector4.Transform(new Vector4(vertex, 1), viewMatrix);
            transformedVertex = Vector4.Transform(transformedVertex, projectionMatrix);

            // Perform perspective divide to normalize
            if (transformedVertex.W != 0)
            {
                transformedVertex.X /= transformedVertex.W;
                transformedVertex.Y /= transformedVertex.W;
                transformedVertex.Z /= transformedVertex.W;
            }

            // Map from normalized device coordinates (-1 to 1) to screen coordinates (0 to width/height)
            Vector3 projectedVertex = new Vector3(
                (transformedVertex.X + 1f) * 0.5f * width,      // X coordinate
                (1f - transformedVertex.Y) * 0.5f * height,     // Y coordinate (inverted due to screen coordinate system)
                transformedVertex.Z);                           // Z coordinate (for depth testing, if needed)

            return projectedVertex;
        }

        public void RunOld()
        {
            double lat = 43.52922;
            double lon = 5.53142;
            DEMDataSet dataSet = DEMDataSet.SRTM_GL3;
            double heightAboveObservationPoint = 30;
            double observerAzimuth = 90;
            double observerFieldOfViewDeg = 60;
            double maxDistanceMeters = 5000;


            GeoPoint observer = new GeoPoint(lat, lon);
            _elevationService.DownloadMissingFiles(dataSet, observer);
            observer = _elevationService.GetPointElevation(observer, dataSet);
            observer.Elevation += heightAboveObservationPoint;

            var fovLeft = observer.GetDestinationPointFromPointAndBearing(observerAzimuth - observerFieldOfViewDeg / 2, maxDistanceMeters);
            var fovRight = observer.GetDestinationPointFromPointAndBearing(observerAzimuth + observerFieldOfViewDeg / 2, maxDistanceMeters);
            var fovBbox = new BoundingBox(
                xmin: Math.Min(Math.Min(observer.Longitude, fovLeft.Longitude), fovRight.Longitude),
                xmax: Math.Max(Math.Max(observer.Longitude, fovLeft.Longitude), fovRight.Longitude),
                ymin: Math.Min(Math.Min(observer.Latitude, fovLeft.Latitude), fovRight.Latitude),
                ymax: Math.Max(Math.Max(observer.Latitude, fovLeft.Latitude), fovRight.Latitude));

            // Get required tiles
            // 1st pass: maxDistanceMeters bbox arount point
            // for each tile, check if it is in view

            var hMapBase = _elevationService.GetHeightMap(ref fovBbox, dataSet, downloadMissingFiles: true, generateMissingData: false);
            var hMap = hMapBase.ReprojectRelativeToObserver(observer, maxDistanceMeters);
            hMap.BakeCoordinates();
            var triangulation = _meshService.TriangulateHeightMap(hMap);
            var indexedTriangulation = new IndexedTriangulation(triangulation, vectorTransform: default, transformToGltfCoords: false);
            var normals = _meshService.ComputeMeshNormals(indexedTriangulation.Positions, indexedTriangulation.Indices).ToList();

            List<float> dotProducts = new(normals.Count);
            for (var x = 0; x < hMap.Width; x++)
                for (var y = 0; y < hMap.Height; y++)
                {
                    var normal = normals[x + y * hMap.Width];
                    var pos = Vector3.Normalize(indexedTriangulation[x + y * hMap.Width]);
                    var dotProduct = Vector3.Dot(pos, normal);
                    dotProduct = (float)(Math.Acos(dotProduct) / RADIAN);
                    dotProducts.Add(dotProduct);
                }

            GenerateHeightMapImage(hMap, indexedTriangulation);

            GenerateNormalMapImage(hMap, normals);

            float viewportWidth = 1920;
            float viewportHeight = 800;

            var projectionMatrix = Matrix4x4.CreatePerspective(viewportWidth, viewportHeight, 10, (float)maxDistanceMeters);
            Vector3 cameraPosition = new Vector3(0, 0, (float)observer.Elevation);
            Vector3 cameraTarget = Vector3.UnitX;
            Vector3 cameraUp = Vector3.UnitZ;
            var modelMatrix = Matrix4x4.Identity; //Matrix4x4.CreateScale(new Vector3(1f / (float)maxDistanceMeters, 1f / (float)maxDistanceMeters, 1 / 1500));
            var viewMatrix = Matrix4x4.CreateLookAt(cameraPosition, cameraTarget, cameraUp);
            var translate = Matrix4x4.CreateTranslation(0, 0, 0);
            var screenMat = projectionMatrix * viewMatrix * modelMatrix;

            var pts = new List<PointF>();
            using (Image<Bgra32> outputImage = new Image<Bgra32>((int)viewportWidth, (int)viewportHeight))
            {
                outputImage.Mutate(img => img.Clear(Color.White));

                SolidBrush brush = new SolidBrush(Color.Black);
                SolidPen pen = new SolidPen(brush, 0.5f);
                //List<LinearLineSegment> segments = new List<LinearLineSegment>(3);
                outputImage.Mutate(img =>
                {

                    var points = new PointF[4];
                    foreach (var tri in indexedTriangulation.Triangles)
                    {
                        // World * View * Projection
                        // https://juhakeranen.com/winui3/directx-11-8-camera-transformations.html
                        var a = Vector4.Transform(tri.A, screenMat);
                        var b = Vector4.Transform(tri.B, screenMat);
                        var c = Vector4.Transform(tri.C, screenMat);

                        a.W = b.W = c.W = 1;
                        a = Vector4.Transform(a, translate);
                        //if (IsClipping(a, viewportWidth, viewportHeight)
                        //    || IsClipping(b, viewportWidth, viewportHeight)
                        //    || IsClipping(c, viewportWidth, viewportHeight))
                        //    break;

                        points = [VecToPointF(a), VecToPointF(b)];//, VecToPointF(c), VecToPointF(a)];
                        //pts.AddRange(points);

                        //segments.Add( new LinearLineSegment(Vec3ToPointF(a),Vec3ToPointF(b)));
                        //segments.Add(new LinearLineSegment(Vec3ToPointF(b), Vec3ToPointF(c)));
                        //segments.Add(new LinearLineSegment(Vec3ToPointF(c), Vec3ToPointF(a)));

                        img.Draw(pen, new SixLabors.ImageSharp.Drawing.Path(points));

                    }
                    //points = [new(10, 20), new(100, 200), new(30, 250), new(10, 20)];
                    //img.DrawLine(color: Color.Red, thickness: 1f, points);
                }
                    );
                outputImage.Save("Landscape3D.png");
            }
            //var screenBbox = new BoundingBox(pts.Min(p => p.X), pts.Max(p => p.X), pts.Min(p => p.Y), pts.Max(p => p.Y));

        }

        private static void GenerateNormalMapImage(HeightMap hMap, List<Vector3> normals, string filename = "LandscapeNormal.png")
        {
            using (Image<Bgra32> outputImage = new Image<Bgra32>(hMap.Width, hMap.Height))
            {
                int hMapIndex = 0;
                foreach (var data in normals)
                {
                    var j = hMapIndex / hMap.Width;
                    var i = hMapIndex - j * hMap.Width;

                    Bgra32 color = FromVec3NormalToColor(data);

                    outputImage[i, j] = color;
                    hMapIndex++;
                }

                outputImage.Save(filename);
            }
        }

        private static void GenerateHeightMapImage(HeightMap hMap, IndexedTriangulation indexedTriangulation, string filename = "LandscapeHMap.png")
        {
            using (Image<L16> outputImage = new Image<L16>(hMap.Width, hMap.Height))
            {
                int hMapIndex = 0;
                foreach (var coord in indexedTriangulation.Positions)
                {
                    // index is i + (j * heightMap.Width);
                    var j = hMapIndex / hMap.Width;
                    var i = hMapIndex - j * hMap.Width;

                    float gray = MathHelper.Map(0, 1200, 0, ushort.MaxValue, coord.Z, true);

                    outputImage[i, j] = new L16((ushort)Math.Round(gray, 0));

                    hMapIndex++;
                }

                outputImage.Save(filename, new PngEncoder() { BitDepth = PngBitDepth.Bit16 });
            }
        }

        private static bool IsClipping(Vector3 vector, float viewportWidth, float viewportHeight)
            => vector.X < 0 || vector.X > viewportWidth
            || vector.Y < 0 || vector.Y > viewportHeight;

        private static PointF VecToPointF(Vector4 pos) => new PointF(pos.X / pos.W, pos.Y / pos.W);


        private static Bgra32 FromVec3NormalToColor(Vector3 normal)
        {
            return new Bgra32((byte)Math.Round(MathHelper.Map(-1, 1, 0, 255, normal.X, true), 0),
                (byte)Math.Round(MathHelper.Map(-1, 1, 0, 255, normal.Y, true), 0),
                (byte)Math.Round(MathHelper.Map(0, -1, 128, 255, -normal.Z, true), 0));
        }

        private void LineSample(DEMDataSet dataSet, double latStart, double lonStart, double latEnd, double lonEnd)
        {
            var elevationLine = GeometryService.ParseGeoPointAsGeometryLine(new GeoPoint(latStart, lonStart), new GeoPoint(latEnd, lonEnd));

            _elevationService.DownloadMissingFiles(dataSet, elevationLine.GetBoundingBox());

            var geoPoints = _elevationService.GetLineGeometryElevation(elevationLine, dataSet);

        }

        void TestEdges(DEMDataSet dataSet, double lat, double lon
            , string rasterSouthWestName, string rasterSouthEastName
            , string rasterNorthWestName, string rasterNorthEastName)
        {
            DEMFileType fileType = dataSet.FileFormat.Type;
            int rasterSize = dataSet.PointsPerDegree;
            double amountx = (1d / rasterSize) / 4d;
            double amounty = (1d / rasterSize) / 4d;

            // Regenerates all metadata            
            //_rasterService.GenerateDirectoryMetadata(dataSet
            //                                        , force: true
            //                                        , deleteOnError: false
            //                                        , maxDegreeOfParallelism: 1);
            _elevationService.DownloadMissingFiles(dataSet, lat, lon);
            var tiles = _rasterService.GenerateReportForLocation(dataSet, lat, lon);
            Debug.Assert(tiles.Count == 4);
            Debug.Assert(tiles.Any(t => string.Equals(System.IO.Path.GetFileName(t.LocalName), rasterSouthWestName, StringComparison.OrdinalIgnoreCase)));
            Debug.Assert(tiles.Any(t => string.Equals(System.IO.Path.GetFileName(t.LocalName), rasterSouthEastName, StringComparison.OrdinalIgnoreCase)));
            Debug.Assert(tiles.Any(t => string.Equals(System.IO.Path.GetFileName(t.LocalName), rasterNorthWestName, StringComparison.OrdinalIgnoreCase)));
            Debug.Assert(tiles.Any(t => string.Equals(System.IO.Path.GetFileName(t.LocalName), rasterNorthEastName, StringComparison.OrdinalIgnoreCase)));

            if (dataSet.FileFormat.Registration == DEMFileRegistrationMode.Cell)
            {
                using (var rasterNW = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterNorthWestName, System.IO.Path.GetFileName(t.LocalName))).LocalName, fileType))
                using (var rasterNE = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterNorthEastName, System.IO.Path.GetFileName(t.LocalName))).LocalName, fileType))
                using (var rasterSW = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterSouthWestName, System.IO.Path.GetFileName(t.LocalName))).LocalName, fileType))
                using (var rasterSE = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterSouthEastName, System.IO.Path.GetFileName(t.LocalName))).LocalName, fileType))
                {
                    var elevNW = rasterNW.GetElevationAtPoint(rasterNW.ParseMetaData(dataSet.FileFormat), rasterSize - 1, rasterSize - 1);
                    var elevNE = rasterNE.GetElevationAtPoint(rasterNE.ParseMetaData(dataSet.FileFormat), 0, rasterSize - 1);
                    var elevSW = rasterSW.GetElevationAtPoint(rasterSW.ParseMetaData(dataSet.FileFormat), rasterSize - 1, 0);
                    var elevSE = rasterSE.GetElevationAtPoint(rasterSE.ParseMetaData(dataSet.FileFormat), 0, 0);

                    BilinearInterpolator interpolator = new BilinearInterpolator();
                    var elev0 = interpolator.Interpolate(elevSW, elevSE, elevNW, elevNE, 0.25, 0.25);
                    var apiElev0 = _elevationService.GetPointElevation(lat + amounty, lon - amountx, dataSet);
                    Debug.Assert((elev0 - apiElev0.Elevation.Value) < double.Epsilon);

                    var elev1 = interpolator.Interpolate(elevSW, elevSE, elevNW, elevNE, 0.75, 0.25);
                    var apiElev1 = _elevationService.GetPointElevation(lat + amounty, lon + amountx, dataSet);
                    Debug.Assert((elev1 - apiElev1.Elevation.Value) < double.Epsilon);

                    var elev2 = interpolator.Interpolate(elevSW, elevSE, elevNW, elevNE, 0.25, 0.75);
                    var apiElev2 = _elevationService.GetPointElevation(lat - amounty, lon - amountx, dataSet);
                    Debug.Assert((elev2 - apiElev2.Elevation.Value) < double.Epsilon);

                    var elev3 = interpolator.Interpolate(elevSW, elevSE, elevNW, elevNE, 0.75, 0.75);
                    var apiElev3 = _elevationService.GetPointElevation(lat - amounty, lon + amountx, dataSet);
                    Debug.Assert((elev3 - apiElev3.Elevation.Value) < double.Epsilon);
                }
            }
            else
            {
                using (var rasterNW = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterNorthWestName, System.IO.Path.GetFileName(t.LocalName))).LocalName, fileType))
                using (var rasterNE = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterNorthEastName, System.IO.Path.GetFileName(t.LocalName))).LocalName, fileType))
                using (var rasterSW = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterSouthWestName, System.IO.Path.GetFileName(t.LocalName))).LocalName, fileType))
                using (var rasterSE = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterSouthEastName, System.IO.Path.GetFileName(t.LocalName))).LocalName, fileType))
                {
                    // Northen row, west to east
                    var elevN0 = rasterNW.GetElevationAtPoint(rasterNW.ParseMetaData(dataSet.FileFormat), rasterSize - 1, rasterSize - 1);
                    var elevN1 = rasterNW.GetElevationAtPoint(rasterNW.ParseMetaData(dataSet.FileFormat), rasterSize, rasterSize - 1);
                    var elevN2 = rasterNE.GetElevationAtPoint(rasterNE.ParseMetaData(dataSet.FileFormat), 1, rasterSize - 1);

                    // middle row, west to east
                    var elevM0 = rasterNW.GetElevationAtPoint(rasterNW.ParseMetaData(dataSet.FileFormat), rasterSize - 1, rasterSize);
                    var elevM1 = rasterNW.GetElevationAtPoint(rasterNW.ParseMetaData(dataSet.FileFormat), rasterSize, rasterSize);
                    var elevM2 = rasterNE.GetElevationAtPoint(rasterNE.ParseMetaData(dataSet.FileFormat), 1, rasterSize);

                    // Sourthen row, west to east
                    var elevS0 = rasterSW.GetElevationAtPoint(rasterSW.ParseMetaData(dataSet.FileFormat), rasterSize - 1, 1);
                    var elevS1 = rasterSW.GetElevationAtPoint(rasterSW.ParseMetaData(dataSet.FileFormat), rasterSize, 1);
                    var elevS2 = rasterSE.GetElevationAtPoint(rasterSE.ParseMetaData(dataSet.FileFormat), 1, 1);

                    BilinearInterpolator interpolator = new BilinearInterpolator();
                    var elev0 = interpolator.Interpolate(elevM0, elevM1, elevN0, elevN1, 0.75, 0.75);
                    var apiElev0 = _elevationService.GetPointElevation(lat + amounty, lon - amountx, dataSet);
                    Debug.Assert((elev0 - apiElev0.Elevation.Value) < double.Epsilon);

                    var elev1 = interpolator.Interpolate(elevM1, elevM2, elevN1, elevN2, 0.25, 0.75);
                    var apiElev1 = _elevationService.GetPointElevation(lat + amounty, lon + amountx, dataSet);
                    Debug.Assert((elev1 - apiElev1.Elevation.Value) < double.Epsilon);

                    var elev2 = interpolator.Interpolate(elevS0, elevS1, elevM0, elevM1, 0.75, 0.25);
                    var apiElev2 = _elevationService.GetPointElevation(lat - amounty, lon - amountx, dataSet);
                    Debug.Assert((elev2 - apiElev2.Elevation.Value) < double.Epsilon);

                    var elev3 = interpolator.Interpolate(elevS1, elevS2, elevM1, elevM2, 0.25, 0.25);
                    var apiElev3 = _elevationService.GetPointElevation(lat - amounty, lon + amountx, dataSet);
                    Debug.Assert((elev3 - apiElev3.Elevation.Value) < double.Epsilon);
                }

            }


        }
        //void TestPointElevation(DEMDataSet dataSet, double lat, double lon
        //    , string rasterName, DEMFileType fileType, int rasterSize)
        //{
        //    // Regenerates all metadata            
        //    _rasterService.GenerateDirectoryMetadata(dataSet
        //                                            , force: true
        //                                            , deleteOnError: false
        //                                            , maxDegreeOfParallelism: 1);
        //    _elevationService.DownloadMissingFiles(dataSet, lat, lon);
        //    var tiles = _rasterService.GenerateReportForLocation(dataSet, lat, lon);
        //    Debug.Assert(tiles.Any(t => string.Equals(Path.GetFileName(t.LocalName), rasterName, StringComparison.OrdinalIgnoreCase)));

        //    if (dataSet.FileFormat.Registration == DEMFileRegistrationMode.Cell)
        //    {
        //        using (var raster = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterName, Path.GetFileName(t.LocalName))).LocalName, fileType))
        //        {
        //            var elevNW = rasterNW.GetElevationAtPoint(rasterNW.ParseMetaData(dataSet.FileFormat), rasterSize - 1, rasterSize - 1);
        //            var elevNE = rasterNE.GetElevationAtPoint(rasterNE.ParseMetaData(dataSet.FileFormat), 0, rasterSize - 1);
        //            var elevSW = rasterSW.GetElevationAtPoint(rasterSW.ParseMetaData(dataSet.FileFormat), rasterSize - 1, 0);
        //            var elevSE = rasterSE.GetElevationAtPoint(rasterSE.ParseMetaData(dataSet.FileFormat), 0, 0);

        //            BilinearInterpolator interpolator = new BilinearInterpolator();
        //            var elev0 = interpolator.Interpolate(elevSW, elevSE, elevNW, elevNE, 0.25, 0.25);
        //            var apiElev0 = _elevationService.GetPointElevation(lat + amounty, lon - amountx, dataSet);
        //            Debug.Assert((elev0 - apiElev0.Elevation.Value) < double.Epsilon);

        //            var elev1 = interpolator.Interpolate(elevSW, elevSE, elevNW, elevNE, 0.75, 0.25);
        //            var apiElev1 = _elevationService.GetPointElevation(lat + amounty, lon + amountx, dataSet);
        //            Debug.Assert((elev1 - apiElev1.Elevation.Value) < double.Epsilon);

        //            var elev2 = interpolator.Interpolate(elevSW, elevSE, elevNW, elevNE, 0.25, 0.75);
        //            var apiElev2 = _elevationService.GetPointElevation(lat - amounty, lon - amountx, dataSet);
        //            Debug.Assert((elev2 - apiElev2.Elevation.Value) < double.Epsilon);

        //            var elev3 = interpolator.Interpolate(elevSW, elevSE, elevNW, elevNE, 0.75, 0.75);
        //            var apiElev3 = _elevationService.GetPointElevation(lat - amounty, lon + amountx, dataSet);
        //            Debug.Assert((elev3 - apiElev3.Elevation.Value) < double.Epsilon);
        //        }
        //    }
        //    else
        //    {
        //        using (var rasterNW = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterNorthWestName, Path.GetFileName(t.LocalName))).LocalName, fileType))
        //        using (var rasterNE = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterNorthEastName, Path.GetFileName(t.LocalName))).LocalName, fileType))
        //        using (var rasterSW = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterSouthWestName, Path.GetFileName(t.LocalName))).LocalName, fileType))
        //        using (var rasterSE = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterSouthEastName, Path.GetFileName(t.LocalName))).LocalName, fileType))
        //        {
        //            // Northen row, west to east
        //            var elevN0 = rasterNW.GetElevationAtPoint(rasterNW.ParseMetaData(dataSet.FileFormat), rasterSize - 1, rasterSize - 1);
        //            var elevN1 = rasterNW.GetElevationAtPoint(rasterNW.ParseMetaData(dataSet.FileFormat), rasterSize, rasterSize - 1);
        //            var elevN2 = rasterNE.GetElevationAtPoint(rasterNE.ParseMetaData(dataSet.FileFormat), 1, rasterSize - 1);

        //            // middle row, west to east
        //            var elevM0 = rasterNW.GetElevationAtPoint(rasterNW.ParseMetaData(dataSet.FileFormat), rasterSize - 1, rasterSize);
        //            var elevM1 = rasterNW.GetElevationAtPoint(rasterNW.ParseMetaData(dataSet.FileFormat), rasterSize, rasterSize);
        //            var elevM2 = rasterNE.GetElevationAtPoint(rasterNE.ParseMetaData(dataSet.FileFormat), 1, rasterSize);

        //            // Sourthen row, west to east
        //            var elevS0 = rasterSW.GetElevationAtPoint(rasterSW.ParseMetaData(dataSet.FileFormat), rasterSize - 1, 1);
        //            var elevS1 = rasterSW.GetElevationAtPoint(rasterSW.ParseMetaData(dataSet.FileFormat), rasterSize, 1);
        //            var elevS2 = rasterSE.GetElevationAtPoint(rasterSE.ParseMetaData(dataSet.FileFormat), 1, 1);

        //            BilinearInterpolator interpolator = new BilinearInterpolator();
        //            var elev0 = interpolator.Interpolate(elevM0, elevM1, elevN0, elevN1, 0.75, 0.75);
        //            var apiElev0 = _elevationService.GetPointElevation(lat + amounty, lon - amountx, dataSet);
        //            Debug.Assert((elev0 - apiElev0.Elevation.Value) < double.Epsilon);

        //            var elev1 = interpolator.Interpolate(elevM1, elevM2, elevN1, elevN2, 0.25, 0.75);
        //            var apiElev1 = _elevationService.GetPointElevation(lat + amounty, lon + amountx, dataSet);
        //            Debug.Assert((elev1 - apiElev1.Elevation.Value) < double.Epsilon);

        //            var elev2 = interpolator.Interpolate(elevS0, elevS1, elevM0, elevM1, 0.75, 0.25);
        //            var apiElev2 = _elevationService.GetPointElevation(lat - amounty, lon - amountx, dataSet);
        //            Debug.Assert((elev2 - apiElev2.Elevation.Value) < double.Epsilon);

        //            var elev3 = interpolator.Interpolate(elevS1, elevS2, elevM1, elevM2, 0.25, 0.25);
        //            var apiElev3 = _elevationService.GetPointElevation(lat - amounty, lon + amountx, dataSet);
        //            Debug.Assert((elev3 - apiElev3.Elevation.Value) < double.Epsilon);
        //        }

        //    }


        //}

    }
}
