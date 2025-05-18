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
        private const double BBOX_SIZE_KM = 25;

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
                centerLon - lonOffset,
                centerLat - latOffset,
                centerLon + lonOffset,
                centerLat + latOffset
            );
        }

        private async Task<string> GetSatelliteImage((double minLon, double minLat, double maxLon, double maxLat) bbox, DateTime time)
        {
            try
            {
                long timestamp = new DateTimeOffset(time).ToUnixTimeMilliseconds();

                string url = $"https://wvs.earthdata.nasa.gov/api/v1/snapshot" +
                             $"?REQUEST=GetSnapshot" +
                             $"&LAYERS=VIIRS_SNPP_CorrectedReflectance_BandsM11-I2-I1,VIIRS_SNPP_Thermal_Anomalies_375m_Day,Reference_Features_15m" +
                             $"&CRS=EPSG:4326" +
                             $"&TIME={time:yyyy-MM-dd}" +
                             $"&WRAP=DAY,DAY,X" +
                             $"&BBOX={bbox.minLon},{bbox.minLat},{bbox.maxLon},{bbox.maxLat}" +
                             $"&FORMAT=image/jpeg" +
                             $"&WIDTH=512&HEIGHT=512" +
                             $"&AUTOSCALE=TRUE" +
                             $"&ts={timestamp}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                return Convert.ToBase64String(imageBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving satellite image.");
                return null;
            }
        }

        private async Task UpdateFireDataWithImage(FireData fireData, string imageBase64)
        {
            var filter = Builders<FireData>.Filter.Eq(f => f.Id, fireData.Id);
            var update = Builders<FireData>.Update.Set(f => f.Photo, imageBase64);

            await _fireCollection.UpdateOneAsync(filter, update);
        }
    }
}

