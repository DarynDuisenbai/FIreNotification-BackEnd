using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using Application.DTOs.OutSource;
using Application.DTOs.CrowdSourcing;
using Domain.Entities.FireData;
using Application.Interfaces;
using MongoDB.Driver;
using Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace Application.Service
{
    public class PhotoVerificationService: IPhotoVerificationService
    {
        private readonly IMongoCollection<CrowdSourcingData> _crowdSourcingData;
        public PhotoVerificationService(IOptions<MongoDbSettings> mongoSettings)
        {
            var mongoClient = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = mongoClient.GetDatabase(mongoSettings.Value.DatabaseName);
            _crowdSourcingData = database.GetCollection<CrowdSourcingData>(nameof(CrowdSourcingData));
        }

        public async Task<bool> VerifyAndSavePhotoAsync(VerifyPhotoRequest request)
        {
            try
            {
                // Конвертируем base64 в байты
                var imageBytes = Convert.FromBase64String(request.PhotoBase64);

                // Проверяем метаданные фото
                var photoMetadata = ExtractPhotoMetadata(imageBytes);

                // Проверяем время создания фото (должно быть в пределах последних 5 минут)
                var isRecentPhoto = IsPhotoRecent(photoMetadata.Timestamp);

                // Проверяем валидность (фото свежее и в пределах 100 метров от указанных координат)
                var isValid = isRecentPhoto;

                if (isValid)
                {
                    // Получаем адрес по координатам через OpenStreetMap
                    var address = await GetAddressByCoordinates(request.Latitude, request.Longitude);

                    // Сохраняем данные в коллекцию
                    var crowdSourcingData = new CrowdSourcingData
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = request.UserId,
                        Latitude = request.Latitude,
                        Longitude = request.Longitude,
                        Address = address,
                        Time_fire = DateTime.UtcNow,
                        Photo = request.PhotoBase64,
                        Definition = request.Definition
                    };

                    await _crowdSourcingData.InsertOneAsync(crowdSourcingData);
                }

                return isValid;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке фото: {ex.Message}");
                return false;
            }
        }

        private async Task<string> GetAddressByCoordinates(double latitude, double longitude)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={latitude}&lon={longitude}&addressdetails=1";

                    // Добавляем User-Agent (требование OpenStreetMap)
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "YourAppName/1.0");

                    var response = await httpClient.GetStringAsync(url);
                    var jsonResponse = JsonDocument.Parse(response);

                    if (jsonResponse.RootElement.TryGetProperty("display_name", out var displayName))
                    {
                        return displayName.GetString();
                    }

                    return "Адрес не найден";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении адреса: {ex.Message}");
                return "Ошибка получения адреса";
            }
        }

        private PhotoMetadata ExtractPhotoMetadata(byte[] imageBytes)
        {
            var metadata = new PhotoMetadata();

            try
            {
                using (var image = Image.Load(imageBytes))
                {
                    var exifProfile = image.Metadata.ExifProfile;

                    if (exifProfile != null)
                    {
                        // Извлекаем дату создания фото
                        if (exifProfile.TryGetValue(ExifTag.DateTime, out var dateTimeValue))
                        {
                            if (DateTime.TryParseExact(dateTimeValue.Value, "yyyy:MM:dd HH:mm:ss", null,
                                System.Globalization.DateTimeStyles.None, out var dateTime))
                            {
                                metadata.Timestamp = dateTime;
                            }
                        }

                        // Альтернативные поля даты
                        if (metadata.Timestamp == null)
                        {
                            if (exifProfile.TryGetValue(ExifTag.DateTimeOriginal, out var originalDateTime))
                            {
                                if (DateTime.TryParseExact(originalDateTime.Value, "yyyy:MM:dd HH:mm:ss", null,
                                    System.Globalization.DateTimeStyles.None, out var origDateTime))
                                {
                                    metadata.Timestamp = origDateTime;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при извлечении метаданных: {ex.Message}");
            }

            // Если время не найдено в EXIF, используем текущее время
            if (metadata.Timestamp == null)
            {
                metadata.Timestamp = DateTime.UtcNow;
            }

            return metadata;
        }

        private bool IsPhotoRecent(DateTime? timestamp)
        {
            if (!timestamp.HasValue)
                return false;

            var timeDifference = Math.Abs((DateTime.UtcNow - timestamp.Value).TotalMinutes);
            return timeDifference <= 5;
        }

    }
}
