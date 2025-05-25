namespace Application.DTOs.CrowdSourcing
{
    public class VerifyPhotoRequest
    {
        public string UserId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string PhotoBase64 { get; set; }
        public string Definition { get; set; }
    }
}
