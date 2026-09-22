using Atad.Application.Interfaces;
using Atad.Domain.Models;
using Atad.Infrastructure.Persistence;
using MongoDB.Driver;

namespace Atad.Infrastructure.Repositories;

public class TodoRepository(IMongoDatabase db) : ITodoRepository
{
    private readonly IMongoCollection<Todo> _collection =
        db.GetCollection<Todo>(MongoCollections.Todos);

    public async Task<bool> UpsertTodoAsync(Todo todo)
    {
        try
        {
            var result = await _collection.ReplaceOneAsync(
                filter: x => x.Id == todo.Id,
                replacement: todo,
                options: new ReplaceOptions { IsUpsert = true }
            );

            return result.IsAcknowledged;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<Todo>> GetAllTodosAsync(Guid todoListId)
    {
        try
        {
            return await _collection.Find(todo => todo.TodoListId == todoListId).ToListAsync();
        }
        catch
        {
            return [];
        }
    }

    public async Task<bool> UpdateTodoAsync(Todo newTodo)
    {
        try
        {
            var result = await _collection.ReplaceOneAsync(
                filter: x => x.Id == newTodo.Id, 
                replacement: newTodo
            );

            return result.IsAcknowledged && result.ModifiedCount > 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteTodoByIdAsync(Guid id)
    {
        try
        {
            var result = await _collection.DeleteOneAsync(todo => todo.Id == id);
            
            return result.IsAcknowledged && result.DeletedCount > 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteTodosByTodoListId(Guid todoListId)
    {
        try
        {
            var result = await _collection.DeleteManyAsync(todo => todo.TodoListId == todoListId);
            
            return result.IsAcknowledged && result.DeletedCount > 0;
        }
        catch
        {
            return false;
        }
    }
}