using Domain.Entities.Identity;
using MongoDB.Entities;

namespace Domain.Entities.FireData
{
    [Collection(nameof(User))]
    public class FireData
    {
        public string Id { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Daynight { get; set; }
        public string? Address { get; set; }
        public DateTime Time_fire { get; set; }
        public DateTime Request_Time { get; set; }
    }
}
