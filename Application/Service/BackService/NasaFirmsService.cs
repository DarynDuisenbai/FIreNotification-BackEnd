using Newtonsoft.Json.Linq;
using System.Globalization;
using CsvHelper;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Domain.Entities.FireData;
using Infrastructure.Settings;
using Application.DTOs.NasaDto;

namespace Application.Service.BackService
{
    public class NasaFirmsService : BackgroundService
    {
        private readonly ILogger<NasaFirmsService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly IMongoCollection<FireData> _fireCollection;
        private readonly SatelliteImageService _satelliteImageService;

        public NasaFirmsService(
            ILogger<NasaFirmsService> logger,
            IConfiguration configuration,
            HttpClient httpClient,
            IOptions<MongoDbSettings> mongoSettings,
            SatelliteImageService satelliteImageService)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;
            _satelliteImageService = satelliteImageService;

            var mongoClient = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = mongoClient.GetDatabase(mongoSettings.Value.DatabaseName);
            _fireCollection = database.GetCollection<FireData>(nameof(FireData));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessFireData();
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing NASA FIRMS data");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }

        public async Task ProcessFireData()
        {
            var apiKey = _configuration["NasaFirms:ApiKey"];
            var country = "KAZ";
            var days = 1;
            var date = DateTime.Now.ToString("yyyy-MM-dd");
            var url = $"https://firms.modaps.eosdis.nasa.gov/api/country/csv/{apiKey}/VIIRS_NOAA20_NRT/{country}/{days}/{date}";

            var response = await _httpClient.GetStringAsync(url);
            var fireDtos = ParseCsvResponse(response);

            foreach (var dto in fireDtos)
            {
                var fireData = await MapToFireData(dto);
                await SaveToDatabase(fireData);
            }
        }

        private IEnumerable<FireDto> ParseCsvResponse(string csvContent)
        {
            using var reader = new StringReader(csvContent);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            return csv.GetRecords<FireDto>().ToList();
        }

        private async Task<FireData> MapToFireData(FireDto dto)
        {
            var timeString = dto.AcqTime.ToString().PadLeft(4, '0');
            var hours = int.Parse(timeString.Substring(0, 2));
            var minutes = int.Parse(timeString.Substring(2, 2));
            var fireTime = dto.AcqDate.Date.AddHours(hours).AddMinutes(minutes);

            var address = await GetAddressFromCoordinates(dto.Latitude, dto.Longitude);

            var fireData = new FireData
            {
                Id = Guid.NewGuid().ToString(),
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Daynight = dto.Daynight,
                Address = address,
                Time_fire = fireTime,
                Request_Time = DateTime.UtcNow
            };

            return fireData;
        }

        private async Task SaveToDatabase(FireData fireData)
        {
            try
            {
                var filter = Builders<FireData>.Filter.And(
                    Builders<FireData>.Filter.Eq(x => x.Latitude, fireData.Latitude),
                    Builders<FireData>.Filter.Eq(x => x.Longitude, fireData.Longitude),
                    Builders<FireData>.Filter.Eq(x => x.Time_fire, fireData.Time_fire)
                );

                var existingRecord = await _fireCollection.Find(filter).FirstOrDefaultAsync();

                if (existingRecord == null)
                {
                    await _fireCollection.InsertOneAsync(fireData);
                    _logger.LogInformation($"Saved new fire data: Lat={fireData.Latitude}, Lon={fireData.Longitude}, Time={fireData.Time_fire}");

                    // Получаем изображение только после сохранения записи в базу данных
                    await _satelliteImageService.ProcessSatelliteImage(fireData);
                }
                else
                {
                    _logger.LogInformation($"Fire data already exists: Lat={fireData.Latitude}, Lon={fireData.Longitude}, Time={fireData.Time_fire}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving fire data: Lat={fireData.Latitude}, Lon={fireData.Longitude}");
                throw;
            }
        }

        private async Task<string?> GetAddressFromCoordinates(double latitude, double longitude)
        {
            try
            {
                var url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={latitude}&lon={longitude}&zoom=18&addressdetails=1";
                var request = new HttpRequestMessage(HttpMethod.Get, url);

                request.Headers.Add("User-Agent", "NasaFirmsService/1.0");
                request.Headers.Add("Referer", "https://yourappwebsitehere.com");

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Ошибка при получении адреса. Код: {StatusCode}, Ответ: {ErrorBody}", response.StatusCode, errorBody);
                    return null;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(responseBody);

                if (!json.TryGetValue("display_name", out var addressToken))
                {
                    _logger.LogWarning("Адрес не найден для координат: {Lat}, {Lon}. Ответ API: {ResponseBody}", latitude, longitude, responseBody);
                    return null;
                }

                return addressToken.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении адреса через OpenStreetMap");
                return null;
            }
        }
    }
}
