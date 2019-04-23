# DEM-Net Samples

## Summary

*Please note that the DEM tiles will be downloaded on first run if they are not present on the local system.*

- [Elevation samples](#elevation-samples)
- [GPX Samples](#gpx-samples)

## [Elevation Samples](DEMNet.Sample/Samples/ElevationSamples.cs)

- Get Elevation on SRTM_GL3 dataset from a location (lat/lng) :

```csharp
// download missing DEM files if necessary
ElevationService.DownloadMissingFiles(DEMDataSet.SRTM_GL3, lat, lon);

// get elevation
GeoPoint point = ElevationService.GetPointElevation(lat, lon, DEMDataSet.SRTM_GL3);
double? elevation = point.Elevation;
```

- Get Elevation from multiple locations at once :

*This sample get elevations for all line/DEM intersections (it can return a LOT of points).
The line can be generalized and return only points where elevation change is relevant AND keeping the local maximas.*


```csharp
IEnumerable<GeoPoint> geoPoints = ElevationService.GetPointsElevation(points, dataSet);
```

- Get line elevation : 

*This sample get elevations for all line/DEM intersections (it can return a LOT of points).
The line can be generalized and return only points where elevation change is relevant AND keeping the local maximas.*

```csharp
// Straight line crossing exactly Mt Ventoux peak
var elevationLine = GeometryService.ParseGeoPointAsGeometryLine(new GeoPoint(44.078873, 5.144899), new GeoPoint(44.225876, 5.351516));

// Download DEM tiles if necessary
ElevationService.DownloadMissingFiles(DEMDataSet.AW3D30, elevationLine.GetBoundingBox());

// Get line elevation : 1274 points !
var geoPoints = _elevationService.GetLineGeometryElevation(elevationLine, dataSet);

// Compute metrics (to get distance from origin)
var metrics = GeometryService.ComputeMetrics(geoPoints);

// Simplify line with 50m resolution
var simplified = DouglasPeucker.DouglasPeuckerReduction(geoPoints.ToList(), 50 /* meters */);

//
// Now we have only 20 points but all the peaks and coves
//
```

## [GPX Samples](DEMNet.Sample/Samples/GpxSamples.cs)

- Get elevations for a GPX track

```csharp
// Read GPX points and flatten the segments into a list of points
var gpxFile = Path.Combine("SampleData", "lauzannier.gpx");
var points = GpxImport.ReadGPX_Segments(gpxFile)
                      .SelectMany(segment => segment);

 // Retrieve elevation for each point on DEM
List<GeoPoint> gpxPointsElevated = ElevationService.GetPointsElevation(points, DEMDataSet.AW3D30)
                                        .ToList();

```

## Dataset samples

*Docs coming soon...*

## glTF 3D samples

*Docs coming soon...*


## STL samples

*Docs coming soon...*

## Imagery samples

*Docs coming soon...*
