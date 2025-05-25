using Application.DTOs.CrowdSourcing;
using Application.DTOs.Identity;
using Application.Interfaces;
using Domain.Entities.FireData;
using Domain.Entities.Identity;
using Infrastructure.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;


namespace Application.Service.CrowdService
{
    public class CrowdService : ICrowdService
    {
        private readonly IMongoCollection<CrowdSourcingData> _crowdSourcingData;

        public CrowdService(IOptions<MongoDbSettings> mongoSettings)
        {
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoSettings.Value.DatabaseName);
            _crowdSourcingData = database.GetCollection<CrowdSourcingData>("CrowdSourcingData");
        }

        public async Task<List<CrowdSourcingData>> GetDataByUserId(string userId)
        {
            var filter = Builders<CrowdSourcingData>.Filter.Eq(x => x.UserId, userId);
            var result = await _crowdSourcingData.Find(filter).ToListAsync();
            return result;
        }

    }
}

