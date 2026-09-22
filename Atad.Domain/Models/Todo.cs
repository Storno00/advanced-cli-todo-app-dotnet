namespace Atad.Domain.Models;

public class Todo : EntityBase
{
    public Guid TodoListId { get; init; } = Guid.NewGuid();
    
    public string LongDescription { get; set; } = string.Empty;
    
    public bool IsDone { get; private set; } = false;
    
    public void ToggleIsDone()
    {
        IsDone = !IsDone;
    }
}