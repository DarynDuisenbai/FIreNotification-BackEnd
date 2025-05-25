using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.CrowdSourcing
{
    public class UploadPhotoDto
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public string PhotoBase64 { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }
    }
}
