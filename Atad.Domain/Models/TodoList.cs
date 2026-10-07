namespace Atad.Domain.Models;

public class TodoList : EntityBase
{
    public string? Color { get; set; }

    [MongoDB.Bson.Serialization.Attributes.BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}