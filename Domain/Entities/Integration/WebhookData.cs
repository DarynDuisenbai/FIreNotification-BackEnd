using MongoDB.Entities;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.Integration
{
    [Collection(nameof(WebhookData))]
    public class WebhookData
    {
        public string Id { get; set; }

        public string Url { get; set; }

        public bool Active { get; set; } = true;

        public string DisplayName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastTriggeredAt { get; set; }
    }
}
