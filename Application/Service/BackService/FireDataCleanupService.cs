using Domain.Entities.FireData;
using Infrastructure.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Application.Service.BackService
{
    public class FireDataCleanupService : BackgroundService
    {
        private readonly IMongoCollection<FireData> _fireCollection;
        private readonly ILogger<FireDataCleanupService> _logger;

        public FireDataCleanupService(IOptions<MongoDbSettings> mongoSettings, ILogger<FireDataCleanupService> logger)
        {
            _logger = logger;
            var mongoClient = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = mongoClient.GetDatabase(mongoSettings.Value.DatabaseName);
            _fireCollection = database.GetCollection<FireData>(nameof(FireData));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("FireDataCleanupService запущен.");

            while (!stoppingToken.IsCancellationRequested)
            {
                await RemoveDuplicates();
                _logger.LogInformation("Очистка дубликатов завершена. Следующий запуск через 1 час.");

                // Запуск очистки раз в 1 час
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        public async Task RemoveDuplicates()
        {
            var pipeline = new[]
            {
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", new BsonDocument {
                    { "Latitude", "$Latitude" },
                    { "Longitude", "$Longitude" },
                    { "Time_fire", "$Time_fire" }
                }},
                { "dups", new BsonDocument("$push", "$_id") },
                { "count", new BsonDocument("$sum", 1) }
            }),
            new BsonDocument("$match", new BsonDocument("count", new BsonDocument("$gt", 1)))
        };

            var duplicateGroups = await _fireCollection.Aggregate<BsonDocument>(pipeline).ToListAsync();

            foreach (var group in duplicateGroups)
            {
                var duplicateIds = group["dups"].AsBsonArray.Skip(1).Select(id => id.AsObjectId).ToList();

                var filter = Builders<FireData>.Filter.In("_id", duplicateIds);
                await _fireCollection.DeleteManyAsync(filter);
                _logger.LogInformation($"Удалено {duplicateIds.Count} дубликатов пожаров.");
            }
        }
    }
}
