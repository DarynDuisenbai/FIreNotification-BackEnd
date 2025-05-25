using Application.DTOs.NasaDto;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Application.Handlers.NasaHandler
{
    public class GetFiresByDateCommand : IRequest<List<List<GetFireByDateDto>>>
    {
        public DateTime Date { get; }
        public int GroupSize { get; }

        public GetFiresByDateCommand(DateTime date, int groupSize = 12)
        {
            Date = date;
            GroupSize = groupSize;
        }
    }

    public class GetFiresByDateHandler : IRequestHandler<GetFiresByDateCommand, List<List<GetFireByDateDto>>>
    {
        private readonly IMongoCollection<BsonDocument> _firesCollection;

        public GetFiresByDateHandler(IMongoDatabase database)
        {
            _firesCollection = database.GetCollection<BsonDocument>("FireData");
        }

        public async Task<List<List<GetFireByDateDto>>> Handle(GetFiresByDateCommand request, CancellationToken cancellationToken)
        {
            var startDate = request.Date.Date;
            var endDate = startDate.AddDays(1).AddMinutes(-1);

            var filter = Builders<BsonDocument>.Filter.Gte("Time_fire", startDate) &
                        Builders<BsonDocument>.Filter.Lt("Time_fire", endDate);

            var fires = await _firesCollection.Find(filter).ToListAsync(cancellationToken);

            var fireDtos = fires.Select(f => new GetFireByDateDto
            {
                Latitude = f.GetValue("Latitude", BsonNull.Value).IsBsonNull ? 0 : f["Latitude"].AsDouble,
                Longitude = f.GetValue("Longitude", BsonNull.Value).IsBsonNull ? 0 : f["Longitude"].AsDouble,
                Address = f.GetValue("Address", BsonNull.Value).IsBsonNull ? null : f["Address"].AsString,
                Time_fire = f.GetValue("Time_fire", BsonNull.Value).IsBsonNull ? (DateTime?)null : f["Time_fire"].ToUniversalTime(),
                Photo = f.GetValue("Photo", BsonNull.Value).IsBsonNull ? null : f["Photo"].AsString
            });

            return fireDtos.Chunk(request.GroupSize).Select(chunk => chunk.ToList()).ToList();
        }
    }
}
