using Domain.Entities.FireData;
using Infrastructure.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Application.Service.BackService
{
    public class SatelliteImageService
    {
        private readonly ILogger<SatelliteImageService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IMongoCollection<FireData> _fireCollection;
        private const double KM_PER_DEGREE_LAT = 111.32; 
        private const double BBOX_SIZE_KM = 5.0; 

        public SatelliteImageService(
            ILogger<SatelliteImageService> logger,
            HttpClient httpClient,
            IOptions<MongoDbSettings> mongoSettings)
        {
            _logger = logger;
            _httpClient = httpClient;

            var mongoClient = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = mongoClient.GetDatabase(mongoSettings.Value.DatabaseName);
            _fireCollection = database.GetCollection<FireData>(nameof(FireData));
        }

        public async Task ProcessSatelliteImage(FireData fireData)
        {
            try
            {
                var bbox = CalculateBoundingBox(fireData.Latitude, fireData.Longitude);
                var imageBase64 = await GetSatelliteImage(bbox, fireData.Time_fire);

                if (!string.IsNullOrEmpty(imageBase64))
                {
                    await UpdateFireDataWithImage(fireData, imageBase64);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing satellite image for coordinates: Lat={fireData.Latitude}, Lon={fireData.Longitude}");
            }
        }

        private (double minLon, double minLat, double maxLon, double maxLat) CalculateBoundingBox(double centerLat, double centerLon)
        {
            double kmPerDegreeLon = KM_PER_DEGREE_LAT * Math.Cos(Math.PI * centerLat / 180.0);

            double latOffset = BBOX_SIZE_KM / KM_PER_DEGREE_LAT;
            double lonOffset = BBOX_SIZE_KM / kmPerDegreeLon;

            return (
                minLon: centerLon - lonOffset,
                minLat: centerLat - latOffset,
                maxLon: centerLon + lonOffset,
                maxLat: centerLat + latOffset
            );
        }

        private async Task<string> GetSatelliteImage((double minLon, double minLat, double maxLon, double maxLat) bbox, DateTime fireTime)
        {
            try
            {
                var date = fireTime.ToString("yyyy-MM-dd");
                var bboxString = $"{bbox.minLon},{bbox.minLat},{bbox.maxLon},{bbox.maxLat}";

                var url = $"https://wvs.earthdata.nasa.gov/api/v1/snapshot" +
                    $"?REQUEST=GetSnapshot" +
                    $"&LAYERS=VIIRS_NOAA20_CorrectedReflectance_TrueColor,VIIRS_NOAA20_Thermal_Anomalies_375m_Day" +
                    $"&CRS=EPSG:4326" +
                    $"&TIME={date}" +
                    $"&WRAP=DAY,DAY" +
                    $"&BBOX={bboxString}" +
                    $"&FORMAT=image/jpeg" +
                    $"&WIDTH=800" +
                    $"&HEIGHT=800" +
                    $"&AUTOSCALE=TRUE";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to get satellite image. Status code: {response.StatusCode}");
                    return null;
                }

                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                return Convert.ToBase64String(imageBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting satellite image");
                return null;
            }
        }

        private async Task UpdateFireDataWithImage(FireData fireData, string imageBase64)
        {
            var filter = Builders<FireData>.Filter.And(
                Builders<FireData>.Filter.Eq(x => x.Latitude, fireData.Latitude),
                Builders<FireData>.Filter.Eq(x => x.Longitude, fireData.Longitude)
            );

            var update = Builders<FireData>.Update.Set(x => x.Photo, imageBase64);

            var result = await _fireCollection.UpdateOneAsync(filter, update);

            if (result.ModifiedCount == 0)
            {
                _logger.LogWarning($"No document was updated for coordinates: Lat={fireData.Latitude}, Lon={fireData.Longitude}, Time={fireData.Time_fire}");
            }
            else
            {
                _logger.LogInformation($"Successfully updated fire data with image for coordinates: Lat={fireData.Latitude}, Lon={fireData.Longitude}");
            }
        }
    }
}
