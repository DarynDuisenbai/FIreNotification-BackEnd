namespace Application.DTOs.Identity
{
    public class UserProfileDto
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Photo { get; set; }
        public string Roles { get; set; } 
        public DateTime CreatedAt { get; set; }
    }
}
