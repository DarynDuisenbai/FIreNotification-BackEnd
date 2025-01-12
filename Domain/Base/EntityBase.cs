using MongoDB.Entities;

namespace Domain.Base
{
    public abstract class EntityBase : Entity
    {
        public DateTime? DateOfCreation { get; set; } = DateTime.UtcNow;

    }
}
