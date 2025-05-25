namespace Application.DTOs.NasaDto
{
    public class GetFireByDateDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Daynight { get; set; }
        public string? Address { get; set; }
        public DateTime? Time_fire { get; set; }
        public string? Photo { get; set; }
    }
}
