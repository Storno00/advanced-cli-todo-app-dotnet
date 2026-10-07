namespace Atad.Domain.Models;

public class TodoList : EntityBase
{
    [MongoDB.Bson.Serialization.Attributes.BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}