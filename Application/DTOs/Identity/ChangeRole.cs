using Domain.Entities.Identity.Enums;

namespace Application.DTOs.Identity
{
    public class ChangeRole
    {
        public string Username { get; set; }
        public Roles Role { get; set; }
    }
}
