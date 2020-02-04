using DEM.Net.Core;
using DEM.Net.Extension.Osm;
using DEM.Net.Extension.Osm.Buildings;
using DEM.Net.Extension.Osm.OverpassAPI;
using GeoJSON.Net.Feature;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SampleApp
{
    public class OsmExtensionSample
    {
        private readonly BuildingService buildingService;

        public OsmExtensionSample(BuildingService buildingService)
        {
            this.buildingService = buildingService;
        }
        public void Run()
        {
            BoundingBox bbox;
            // Aix en provence / rotonde
            bbox = new BoundingBox(5.444927726471018, 5.447502647125315, 43.52600685540608, 43.528138282848076);
            var triangulation = GetBuildings3D(bbox);

            Task.Delay(1000).GetAwaiter().GetResult();
            // Aix en provence / slope
            bbox = new BoundingBox(5.434828019053151, 5.4601480721537365, 43.5386672180082, 43.55272718416761);
            triangulation = GetBuildings3D(bbox);

            Task.Delay(1000).GetAwaiter().GetResult();
            // POLYGON((5.526716197512567 43.56457608971906,5.6334895739774105 43.56457608971906,5.6334895739774105 43.49662332237486,5.526716197512567 43.49662332237486,5.526716197512567 43.56457608971906))
            // Aix en provence / ste victoire
            bbox = new BoundingBox(5.526716197512567, 5.6334895739774105, 43.49662332237486, 43.56457608971906);
            triangulation = GetBuildings3D(bbox);

        }



        private Triangulation GetBuildings3D(BoundingBox bbox)
        {
            try
            {
                var task = new OverpassQuery(bbox)
                    .WithWays("building")
                    .ToGeoJSON();

                FeatureCollection buildings = task.GetAwaiter().GetResult();


                Triangulation triangulation = buildingService.Triangulate(buildings, DEMDataSet.AW3D30);
                return triangulation;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
