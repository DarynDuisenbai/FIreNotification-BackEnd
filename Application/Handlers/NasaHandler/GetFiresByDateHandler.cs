using Application.DTOs.NasaDto;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Application.Handlers.NasaHandler
{
    public class GetFiresByDateCommand : IRequest<List<GetFireByDateDto>>
    {
        public DateTime Date { get; }

        public GetFiresByDateCommand(DateTime date)
        {
            Date = date;
        }
    }

    public class GetFiresByDateHandler : IRequestHandler<GetFiresByDateCommand, List<GetFireByDateDto>>
    {
        private readonly IMongoCollection<BsonDocument> _firesCollection;

        public GetFiresByDateHandler(IMongoDatabase database)
        {
            _firesCollection = database.GetCollection<BsonDocument>("FireData");
        }

        public async Task<List<GetFireByDateDto>> Handle(GetFiresByDateCommand request, CancellationToken cancellationToken)
        {
            var startDate = request.Date.Date;
            var endDate = startDate.AddDays(1).AddSeconds(-1);

            var filter = Builders<BsonDocument>.Filter.Gte("Time_fire", startDate) &
                         Builders<BsonDocument>.Filter.Lt("Time_fire", endDate);

            var fires = await _firesCollection.Find(filter).ToListAsync(cancellationToken);

            return fires.Select(f => new GetFireByDateDto
            {
                Latitude = f["Latitude"].AsDouble,
                Longitude = f["Longitude"].AsDouble,
                Address = f.Contains("Address") ? f["Address"].AsString : null,
                Time_fire = f["Time_fire"].ToUniversalTime(),
                Photo = f.Contains("Photo") ? f["Photo"].AsString : null,
            }).ToList();
        }
    }
}
