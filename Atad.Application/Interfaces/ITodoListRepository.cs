using Atad.Domain.Models;

namespace Atad.Application.Interfaces;

public interface ITodoListRepository
{
    Task<List<TodoList>> GetAllTodoListsAsync();
    
    Task<TodoList?> GetTodoListByIdAsync(Guid id);
    
    Task<bool> UpsertTodoListAsync(TodoList todoList);
    
    Task<bool> DeleteTodoListByIdAsync(Guid id);
}