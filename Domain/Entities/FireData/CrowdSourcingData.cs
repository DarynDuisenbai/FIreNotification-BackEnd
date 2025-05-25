using Domain.Entities.Identity;
using MongoDB.Entities;

namespace Domain.Entities.FireData
{
    [Collection(nameof(User))]
    public class CrowdSourcingData
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Address { get; set; }
        public DateTime Time_fire { get; set; }
        public string Photo { get; set; }
        public string Definition { get; set; }
    }
}

