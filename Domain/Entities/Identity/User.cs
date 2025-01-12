using MongoDB.Entities;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.Identity
{
    [Collection(nameof(User) + "s")]
    public class User : IEntity
    {
        [MaxLength(50)]
        public string? Firstname { get; set; }
        [MaxLength(50)]
        public string? Lastname { get; set; }
        [MaxLength(50)]
        public string? Patronymic { get; set; }
        public string? Password { get; set; }
        public string? Email { get; set; }
        public string? PasswordHash { get; set; }

        public object GenerateNewID()
        {
            throw new NotImplementedException();
        }

        public bool HasDefaultID()
        {
            throw new NotImplementedException();
        }
    }
}
