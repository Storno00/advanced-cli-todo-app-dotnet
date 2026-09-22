using Atad.Application.Interfaces;
using Atad.Domain.Models;
using Atad.Infrastructure.Persistence;
using MongoDB.Driver;

namespace Atad.Infrastructure.Repositories;

public class TodoListRepository(IMongoDatabase db) : ITodoListRepository
{
    private readonly IMongoCollection<TodoList> _collection =
        db.GetCollection<TodoList>(MongoCollections.TodoLists);

    public async Task<List<TodoList>> GetAllTodoListsAsync()
    {
        try
        {
            return await _collection.Find(_ => true).ToListAsync();
        }
        catch
        {
            return [];
        }
    }

    public async Task<TodoList?> GetTodoListByIdAsync(Guid id)
    {
        try
        {
            return await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> UpsertTodoListAsync(TodoList todoList)
    {
        try
        {
            var result = await _collection.ReplaceOneAsync(
                filter: x => x.Id == todoList.Id,
                replacement: todoList,
                options: new ReplaceOptions { IsUpsert = true }
            );

            return result.IsAcknowledged;
        }
        catch (Exception ec)
        {
            Console.WriteLine(ec);
            return false;
        }
    }

    public async Task<bool> DeleteTodoListByIdAsync(Guid id)
    {
        try
        {
            var result = await _collection.DeleteOneAsync(x => x.Id == id);
            return result.IsAcknowledged && result.DeletedCount > 0;
        }
        catch
        {
            return false;
        }
    }
}
