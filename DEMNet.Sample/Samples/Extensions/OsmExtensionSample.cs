using DEM.Net.Core;
using DEM.Net.Extension.Osm;
using DEM.Net.Extension.Osm.Buildings;
using DEM.Net.Extension.Osm.OverpassAPI;
using GeoJSON.Net.Feature;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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
            GetBuildings3D(bbox);

            Task.Delay(1000).GetAwaiter().GetResult();
            // Aix en provence / slope
            bbox = new BoundingBox(5.434828019053151, 5.4601480721537365, 43.5386672180082, 43.55272718416761);
            GetBuildings3D(bbox);

            Task.Delay(1000).GetAwaiter().GetResult();
            // POLYGON((5.526716197512567 43.56457608971906,5.6334895739774105 43.56457608971906,5.6334895739774105 43.49662332237486,5.526716197512567 43.49662332237486,5.526716197512567 43.56457608971906))
            // Aix en provence / ste victoire
            bbox = new BoundingBox(5.526716197512567, 5.6334895739774105, 43.49662332237486, 43.56457608971906);
            GetBuildings3D(bbox);

        }



        private void GetBuildings3D(BoundingBox bbox, string modelName = "buildings")
        {
            try
            {
                // debug: write geojson to file
                //File.WriteAllText("buildings.json", JsonConvert.SerializeObject(buildingService.GetBuildingsGeoJson(bbox)));

                var model = buildingService.GetBuildings3DModel(bbox, DEMDataSet.ASTER_GDEMV3, false);
                model.SaveGLB(Path.Combine(Directory.GetCurrentDirectory(), modelName + ".glb"));

            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
