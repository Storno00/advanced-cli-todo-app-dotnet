namespace Atad.Domain.Models;

public class Todo : EntityBase
{
    public Guid TodoListId { get; init; } = Guid.NewGuid();

    public int OrderNumber { get; set; }

    public string? Color { get; set; }
    
    public Guid? ParentId { get; set; }
    
    public string LongDescription { get; set; } = string.Empty;
    
    public bool IsDone { get; set; } = false;
    
    public void ToggleIsDone()
    {
        IsDone = !IsDone;
    }
}