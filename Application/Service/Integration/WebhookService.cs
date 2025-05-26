using Application.DTOs.Integration;
using Application.Interfaces;
using System.Text.Json;
using System.Text;
using MongoDB.Driver;
using Domain.Entities.Integration;
using Infrastructure.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Bson;

namespace Application.Service.Integration
{
    public class WebhookService : IWebhookService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WebhookService> _logger;
        private readonly IMongoCollection<WebhookData> _webhookData;

        public WebhookService(
            HttpClient httpClient,
            ILogger<WebhookService> logger,
            IOptions<MongoDbSettings> mongoSettings)
        {
            _httpClient = httpClient;
            _logger = logger;
            var mongoClient = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = mongoClient.GetDatabase(mongoSettings.Value.DatabaseName);
            _webhookData = database.GetCollection<WebhookData>(nameof(WebhookData));
        }

        public async Task SendWebhookNotificationAsync(double latitude, double longitude, string address, string photoBase64)
        {
            try
            {
                // Получаем все активные веб-хуки из MongoDB
                var activeWebhooks = await _webhookData
                    .Find(w => w.Active == true)
                    .ToListAsync();

                if (!activeWebhooks.Any())
                {
                    _logger.LogInformation("Нет активных веб-хуков для отправки");
                    return;
                }

                // Создаем payload для отправки
                var payload = new WebhookPayload
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    Address = address,
                    PhotoBase64 = photoBase64
                };

                var jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                // Отправляем данные по всем активным веб-хукам параллельно
                var tasks = activeWebhooks.Select(webhook => SendToWebhookAsync(webhook, jsonPayload));
                await Task.WhenAll(tasks);

                _logger.LogInformation($"Отправлено уведомление по {activeWebhooks.Count} веб-хукам");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке веб-хук уведомлений");
                throw;
            }
        }

        private async Task SendToWebhookAsync(WebhookData webhook, string jsonPayload)
        {
            try
            {
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // Устанавливаем таймаут для запроса
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                var response = await _httpClient.PostAsync(webhook.Url, content, cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Успешно отправлено в веб-хук: {webhook.DisplayName} ({webhook.Url})");

                    // Обновляем время последнего срабатывания в MongoDB
                    var filter = Builders<WebhookData>.Filter.Eq(x => x.Id, webhook.Id);
                    var update = Builders<WebhookData>.Update.Set(x => x.LastTriggeredAt, DateTime.UtcNow);

                    await _webhookData.UpdateOneAsync(filter, update);
                }
                else
                {
                    _logger.LogWarning($"Веб-хук {webhook.DisplayName} вернул статус: {response.StatusCode}");
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning($"Таймаут при отправке в веб-хук: {webhook.DisplayName} ({webhook.Url})");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при отправке в веб-хук: {webhook.DisplayName} ({webhook.Url})");
            }
        }

        public async Task<WebhookData> AddWebhookAsync(string url, string displayName)
        {
            var webhook = new WebhookData
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Url = url,
                DisplayName = displayName,
                Active = true,
                CreatedAt = DateTime.UtcNow
            };

            await _webhookData.InsertOneAsync(webhook);
            _logger.LogInformation($"Добавлен новый веб-хук: {displayName} ({url})");

            return webhook;
        }

        public async Task<WebhookData> UpdateWebhookStatusAsync(string id, bool active)
        {
            var filter = Builders<WebhookData>.Filter.Eq(x => x.Id, id);
            var update = Builders<WebhookData>.Update.Set(x => x.Active, active);

            var result = await _webhookData.UpdateOneAsync(filter, update);

            if (result.ModifiedCount > 0)
            {
                _logger.LogInformation($"Статус веб-хука с ID {id} изменен на {active}");

                return await _webhookData.Find(filter).FirstOrDefaultAsync();
            }

            return null;
        }
        public async Task<WebhookData> UpdateWebhookAsync(WebhookData model)
        {
            var filter = Builders<WebhookData>.Filter.Eq(x => x.Id, model.Id);

            var result = await _webhookData.ReplaceOneAsync(filter, model);

            if (result.ModifiedCount > 0)
            {
                _logger.LogInformation($"Веб-хук с ID {model.Id} успешно обновлён.");
                return model;
            }

            _logger.LogWarning($"Не удалось обновить веб-хук с ID {model.Id} — возможно, такого ID не существует или данные не изменились.");
            return null;
        }

        public async Task<List<WebhookData>> GetAllWebhooksAsync()
        {
            return await _webhookData
                .Find(_ => true)
                .SortByDescending(x => x.CreatedAt)
                .ToListAsync();
        }
    }
}
