using Domain.Entities.Identity.Enums;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Identity
{
    public class ChangeRole
    {
        public string UserId { get; set; }

        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } 

        public bool IsValidRole()
        {
            return Domain.Entities.Identity.Enums.Roles.IsValidRole(Role);
        }
    }
}
