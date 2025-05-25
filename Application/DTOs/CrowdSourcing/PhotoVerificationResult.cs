namespace Application.DTOs.CrowdSourcing
{
    public class PhotoVerificationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public DateTime? PhotoTimestamp { get; set; }
        public double? PhotoLatitude { get; set; }
        public double? PhotoLongitude { get; set; }
        public double ProvidedLatitude { get; set; }
        public double ProvidedLongitude { get; set; }
        public double? DistanceInMeters { get; set; }
    }
}
