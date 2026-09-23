using Atad.Domain.Models;

namespace Atad.Application.Interfaces;

public interface ITodoRepository
{
    public Task<bool> UpsertTodoAsync(Todo todo);
    
    public Task<List<Todo>> GetAllTodosAsync(Guid todoListId);

    public Task<bool> UpdateTodoAsync(Todo newTodo);
    
    public Task<bool> DeleteTodoByIdAsync(Guid id);

    public Task<bool> DeleteTodosByIdsAsync(List<Guid> todoIdsToDelete);

    public Task<bool> DeleteTodosByTodoListId(Guid todoListId);
}