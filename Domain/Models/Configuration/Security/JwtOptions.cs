namespace Domain.Models.Configuration.Security
{
    public class JwtOptions
    {
        public string? Secret { get; set; }
        public TimeSpan TokenLifetime { get; set; }
    }
}
