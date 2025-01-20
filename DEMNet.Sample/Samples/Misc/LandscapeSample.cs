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
using NetTopologySuite.Geometries;
using System.Runtime.CompilerServices;
using NetTopologySuite.IO;
using ScottPlot.Palettes;
using NetTopologySuite.Triangulate;

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

        bool drawWireFrame = false;
        Vector3 lightDirection = Vector3.Normalize(new Vector3(-5, -5, 1));

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

            var dataset = DEMDataSet.NASADEM;
            double heightAboveObservationPoint = 100;
            var observer = GetObserver(43.544450, 5.444728, dataset); // terrain des peintres
            //var observer = GetObserver(43.504066, 5.530160, dataset); // bimont
            float zFactor = 2f;
            observer.Elevation += heightAboveObservationPoint;

            int width = 2580;
            int height = 1200;
            float rangeMeters = 20000;
            // Define the perspective projection matrix
            float fieldOfView = 60 * (float)(Math.PI / 180);  // 60 degree field of view
            float aspectRatio = (float)width / height;           // Aspect ratio of the image
            float nearPlane = 0.1f;
            float farPlane = rangeMeters;

            _logger.LogInformation("Getting terrain");

            // Define a triangle in 3D space
            Triangulation3DModel triangulation = GetTerrain(observer, dataset, rangeMeters, zFactor); //GetSampleTriangulation();

            _logger.LogInformation("Updating");
            // Define the camera position and direction
            Vector3 cameraPosition = new Vector3(0, (float)observer.Elevation * zFactor, 0); ;//  new(0, 2, -5);  // Camera at the origin
            Vector3 cameraTarget = new(0, (float)observer.Elevation * zFactor, 5000);    // Looking down the Z axis
            Vector3 upVector = Vector3.UnitY;               // "Up" is in the Y direction

            Matrix4x4 terrainWorld = Matrix4x4.Identity;// Matrix4x4.CreateScale(0.001f);

            Matrix4x4 viewMatrix = Matrix4x4.CreateLookAt(cameraPosition, cameraTarget, upVector);

            Matrix4x4 projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
                fieldOfView, aspectRatio, nearPlane, farPlane);

            // Map from normalized device coordinates (-1 to 1) to screen coordinates (0 to width/height)
            Matrix4x4 viewport = Matrix4x4.CreateViewport(0, 0, width, height, 0, 1);

            // Project each 3D vertex to 2D screen space
            //var projectedTrianglesRaw = ModelToScreen(triangulation, terrainWorld * viewMatrix * projectionMatrix, viewport);
            //var projectedTriangles = projectedTrianglesRaw.OrderBy(t=>t.A.Z).Take(20000).ToList();
            triangulation.ProjectedTriangles = ModelToScreen(triangulation, terrainWorld * viewMatrix * projectionMatrix, viewport);

            _logger.LogInformation("Drawing");

            //DrawToImage("triangle.png", width, height, viewMatrix, projectionMatrix, viewport, projectedTriangles);
            DrawToGDIImage("triangle.bmp", width, height, viewMatrix, projectionMatrix, viewport, cameraPosition, triangulation);

            _logger.LogInformation("Done");
            Environment.Exit(0);
        }

        // Calculate the normal of the triangle using cross product of two edges
        Vector3 CalculateNormal(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;
            Vector3 normal = Vector3.Cross(edge1, edge2);
            normal = Vector3.Normalize(normal); // Normalize the normal vector
            return normal;
        }
        Vector3 CalculateNormal(Triangle3D triangle)
        {
            var v0 = ToVector3(triangle.A);
            var v1 = ToVector3(triangle.B);
            var v2 = ToVector3(triangle.C);
            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;
            Vector3 normal = Vector3.Cross(edge1, edge2);
            normal = Vector3.Normalize(normal); // Normalize the normal vector
            return normal;
        }

        Vector3 ToVector3(Vector4 v) => new Vector3(v.X, v.Y, v.Z);

        // Calculate the color intensity based on the Lambertian shading model (diffuse shading)
        float CalculateLighting(Vector3 normal, Vector3 lightDirection)
        {
            float intensity = Vector3.Dot(normal, -lightDirection); // Invert light direction since it's directional
            return Math.Clamp(intensity, 0, 1); // Clamp intensity between 0 and 1
        }
        // Check if the triangle is facing the camera using back-face culling
        bool IsTriangleFacingCamera(Vector3 triangleNormal, Vector3 anyTriangleVertex, Vector3 cameraPosition)
        => NormalDotCamera(triangleNormal, anyTriangleVertex, cameraPosition) < 0;

        float NormalDotCamera(Vector3 triangleNormal, Vector3 anyTriangleVertex, Vector3 cameraPosition)
        {
            // Check if the triangle is facing the camera (dot product > 0)
            Vector3 viewDirection = Vector3.Normalize(anyTriangleVertex - cameraPosition);  // Direction from vertex to the camera

            // Check if the triangle is facing the camera (dot product > 0)
            return Vector3.Dot(triangleNormal, viewDirection);
        }

        private void DrawToImage(string fileName, int width, int height, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix, Matrix4x4 viewport, List<Triangle3D> projectedTriangles)
        {
            // Create an 800x600 image to draw on
            using (Image<Rgba32> image = new Image<Rgba32>(width, height, Color.Black))
            {

                var penColor = Color.White;
                float thickness = 1.0f;

                image.Mutate(ctx =>
                {
                    DrawAxis(ctx, viewMatrix * projectionMatrix, viewport);

                    foreach (var tri in projectedTriangles)
                    {
                        DrawTriangle(ctx, tri, Color.White, 1f);
                    }

                });


                // Save the image
                image.Save(fileName);
            }
        }
        private void DrawToGDIImage(string fileName, int width, int height, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix, Matrix4x4 viewport, Vector3 cameraPosition, Triangulation3DModel model)
        {
            // Create an 800x600 image to draw on
            using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height))
            {
                using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp))
                {
                    g.Clear(System.Drawing.Color.LightBlue);

                    // Draw Axis
                    Matrix4x4 viewProj = viewMatrix * projectionMatrix;
                    // Draw axis
                    var zero = ProjectVertex(new Vector4(Vector3.Zero, 1), viewProj, viewport);
                    var x = ProjectVertex(new Vector4(Vector3.UnitX, 1), viewProj, viewport);
                    var y = ProjectVertex(new Vector4(Vector3.UnitY, 1), viewProj, viewport);
                    var z = ProjectVertex(new Vector4(Vector3.UnitZ, 1), viewProj, viewport);
                    g.DrawLine(System.Drawing.Pens.Red, new System.Drawing.PointF(zero.X, zero.Y), new System.Drawing.PointF(x.X, x.Y));
                    g.DrawLine(System.Drawing.Pens.Green, new System.Drawing.PointF(zero.X, zero.Y), new System.Drawing.PointF(y.X, y.Y));
                    g.DrawLine(System.Drawing.Pens.Blue, new System.Drawing.PointF(zero.X, zero.Y), new System.Drawing.PointF(z.X, z.Y));

                    if (drawWireFrame)
                    {
                        foreach (var triangle in model.ProjectedTriangles)
                        {
                            g.DrawPolygon(pen: System.Drawing.Pens.White, points: [
                                new System.Drawing.PointF(triangle.A.X, triangle.A.Y),
                            new System.Drawing.PointF(triangle.B.X, triangle.B.Y),
                            new System.Drawing.PointF(triangle.C.X, triangle.C.Y)]); // Close the triangle
                        }
                    }
                    else
                    {
                        var cameraProjectedPosition = ProjectVertex(new Vector4(cameraPosition, 1), viewProj, viewport);
                        for (int t = 0; t < model.ProjectedTriangles.Count; t++)
                        {
                            var modelTriangle = model.Triangles[t];
                            var triangle = model.ProjectedTriangles[t];

                            if (!IsTriangleInView(triangle, viewport))
                            { 
                                continue;
                            }

                            // Calculate the normal of the triangle using the original 3D vertices
                            Vector3 normal = CalculateNormal(modelTriangle);

                            // Draw normals
                            //var projNormal = ProjectVertex(new Vector4(normal, 1), viewProj, viewport);
                            //float normalLength = 50f * NormalDotCamera(normal, ToVector3(modelTriangle.A), cameraPosition);
                            //g.DrawLine(System.Drawing.Pens.Red, new System.Drawing.PointF(triangle.A.X, triangle.B.Y)
                            //    , new System.Drawing.PointF(triangle.A.X + normal.X * normalLength, triangle.B.Y + normal.Y * normalLength));

                            // Back - face culling: Check if the triangle is facing the camera
                            if (!IsTriangleFacingCamera(normal, ToVector3(modelTriangle.A), cameraPosition))
                            {
                                continue;  // Skip this triangle
                            }

                            // Calculate the lighting intensity for each vertex
                            float vertexIntensitiesSum = 0f;
                            for (int i = 0; i < 3; i++)
                            {
                                vertexIntensitiesSum += CalculateLighting(normal, lightDirection);
                            }
                            var averageIntensity = vertexIntensitiesSum / 3f;
                            int tint = (int)(averageIntensity * 255);
                            System.Drawing.Color fillColor = System.Drawing.Color.FromArgb(255, tint, tint, tint);

                            using (var b = new System.Drawing.SolidBrush(fillColor))
                            {
                                g.FillPolygon(b, 
                                    new System.Drawing.PointF(triangle.A.X, triangle.A.Y),
                                    new System.Drawing.PointF(triangle.B.X, triangle.B.Y),
                                    new System.Drawing.PointF(triangle.C.X, triangle.C.Y)); // Close the triangle
                            }
                        }
                    }
                }

                bmp.Save(fileName);
            }
        }

        private void DrawAxis(IImageProcessingContext ctx, Matrix4x4 viewProj, Matrix4x4 viewport)
        {
            var zero = ProjectVertex(new Vector4(Vector3.Zero, 1), viewProj, viewport);

            var x = ProjectVertex(new Vector4(Vector3.UnitX, 1), viewProj, viewport);
            var y = ProjectVertex(new Vector4(Vector3.UnitY, 1), viewProj, viewport);
            var z = ProjectVertex(new Vector4(Vector3.UnitZ, 1), viewProj, viewport);

            ctx.DrawLine(color: Color.Red, 1f, new PointF(zero.X, zero.Y), new PointF(x.X, x.Y));
            ctx.DrawLine(color: Color.Green, 1f, new PointF(zero.X, zero.Y), new PointF(y.X, y.Y));
            ctx.DrawLine(color: Color.Blue, 1f, new PointF(zero.X, zero.Y), new PointF(z.X, z.Y));
        }

        private Triangulation3DModel GetSampleTriangulation() => new Triangulation3DModel(positions: [
                new (-3, -1, 5), // Vertex 1 (in front of the camera, at Z=5)
                new (1, -1, 5),  // Vertex 2 (in front of the camera, at Z=5)
                new (0, 1, 5),    // Vertex 3 (in front of the camera, at Z=5)
                new (-3, -1, 10), // Vertex 1 (in front of the camera, at Z=10)
                new (1, -1, 10),  // Vertex 2 (in front of the camera, at Z=10)
                new (0, 1, 10)    // Vertex 3 (in front of the camera, at Z=10)
            ], indices: [0, 1, 2, 3, 4, 5]);

        List<Triangle3D> ModelToScreen(Triangulation3DModel triangulation, Matrix4x4 viewProj, Matrix4x4 viewport)
        {
            List<Triangle3D> triangles = new List<Triangle3D>(triangulation.Triangles.Count);
            foreach (var tri in triangulation.Triangles)
            {
                var triangle = new Triangle3D(ProjectVertex(tri.A, viewProj, viewport),
                    ProjectVertex(tri.B, viewProj, viewport),
                    ProjectVertex(tri.C, viewProj, viewport));

                triangles.Add(triangle);
            }
            return triangles;
        }
        bool IsTriangleInView(Triangle3D triangle, Matrix4x4 viewport)
            => IsVectorInView(triangle.A, viewport)
            || IsVectorInView(triangle.B, viewport)
            || IsVectorInView(triangle.C, viewport);

        bool IsVectorInView(Vector4 vec, Matrix4x4 viewport)
            => vec.X >= 0 && vec.X <= viewport.M11 * 2
            && vec.Y >= 0 && vec.Y <= viewport.M42 * 2;

        Vector4 VertexToScreen(Vector3 vector, Matrix4x4 viewProj, int width, int height)
        {
            // Map from normalized device coordinates (-1 to 1) to screen coordinates (0 to width/height)
            Matrix4x4 viewport = Matrix4x4.CreateViewport(0, 0, width, height, 0, 1);

            return ProjectVertex(new Vector4(vector, 1), viewProj, viewport);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void DrawTriangle(IImageProcessingContext ctx, Triangle3D triangle, Color penColor, float thickness)
        {

            // Draw lines between vertices
            ctx.DrawLine(penColor, thickness,
                new PointF(triangle.A.X, triangle.A.Y),
                new PointF(triangle.B.X, triangle.B.Y),
                new PointF(triangle.C.X, triangle.C.Y),
                new PointF(triangle.A.X, triangle.A.Y) // Close the triangle
            );
        }

        Vector4 ProjectVertex(Vector4 vertex, Matrix4x4 viewProj, Matrix4x4 viewport)
        {
            // Apply the view and projection transformation
            Vector4 transformedVertex = Vector4.Transform(vertex, viewProj);

            // Perform perspective divide to normalize
            if (transformedVertex.W != 0)
            {
                transformedVertex = new Vector4(
                transformedVertex.X / transformedVertex.W,
                transformedVertex.Y / transformedVertex.W,
                transformedVertex.Z,
                1);
            }
            var projectedVertex = Vector4.Transform(transformedVertex, viewport);

            //Vector3 projectedVertex = new Vector3(
            //    (transformedVertex.X + 1f) * 0.5f * width,      // X coordinate
            //    (1f - transformedVertex.Y) * 0.5f * height,     // Y coordinate (inverted due to screen coordinate system)
            //    transformedVertex.Z);                           // Z coordinate (for depth testing, if needed)

            return projectedVertex;
        }

        public GeoPoint GetObserver(double lat, double lon, DEMDataSet dataset)
        {
            GeoPoint observer = new GeoPoint(lat, lon);
            _elevationService.DownloadMissingFiles(dataset, observer);
            return _elevationService.GetPointElevation(observer, dataset);
        }

        public Triangulation3DModel GetTerrain(GeoPoint observer, DEMDataSet dataSet, double rangeMeters, double zFactor = 1.5f)
        {
            double observerAzimuth = 90;
            double observerFieldOfViewDeg = 60;

            var fovLeft = observer.GetDestinationPointFromPointAndBearing(observerAzimuth - observerFieldOfViewDeg / 2, rangeMeters);
            var fovRight = observer.GetDestinationPointFromPointAndBearing(observerAzimuth + observerFieldOfViewDeg / 2, rangeMeters);
            var fovBbox = new BoundingBox(
                xmin: Math.Min(Math.Min(observer.Longitude, fovLeft.Longitude), fovRight.Longitude),
                xmax: Math.Max(Math.Max(observer.Longitude, fovLeft.Longitude), fovRight.Longitude),
                ymin: Math.Min(Math.Min(observer.Latitude, fovLeft.Latitude), fovRight.Latitude),
                ymax: Math.Max(Math.Max(observer.Latitude, fovLeft.Latitude), fovRight.Latitude));

            // Get required tiles
            // 1st pass: rangeMeters bbox arount point
            // for each tile, check if it is in view

            var hMapBase = _elevationService.GetHeightMap(ref fovBbox, dataSet, downloadMissingFiles: true, generateMissingData: false);
            var hMap = hMapBase.ReprojectRelativeToObserver(observer, rangeMeters);
            hMap.BakeCoordinates();
            var triangulation = _meshService.TriangulateHeightMap(hMap);
            var indexedTriangulation = new Triangulation3DModel(triangulation, p => new Vector3((float)p.Latitude, (float)(p.Elevation * zFactor), (float)p.Longitude));

            return indexedTriangulation;
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

        private static PointF VecToPointF(Vector4 pos) => new PointF(pos.X / pos.W, pos.Y / pos.W);


        private static Bgra32 FromVec3NormalToColor(Vector3 normal)
        {
            return new Bgra32((byte)Math.Round(MathHelper.Map(-1, 1, 0, 255, normal.X, true), 0),
                (byte)Math.Round(MathHelper.Map(-1, 1, 0, 255, normal.Y, true), 0),
                (byte)Math.Round(MathHelper.Map(0, -1, 128, 255, -normal.Z, true), 0));
        }


    }
}
