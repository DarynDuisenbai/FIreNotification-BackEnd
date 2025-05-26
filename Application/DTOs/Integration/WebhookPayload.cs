namespace Application.DTOs.Integration
{
    public class WebhookPayload
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Address { get; set; }
        public string PhotoBase64 { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
