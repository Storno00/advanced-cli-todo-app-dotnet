using MongoDB.Bson.Serialization.Attributes;

namespace Atad.Domain.Models;

public class EntityBase
{
    [BsonId]
    public Guid Id { get; init; } =  Guid.NewGuid();
    
    public required string Name { get; set; }
    
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}