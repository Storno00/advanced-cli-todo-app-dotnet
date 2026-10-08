using Atad.Application.Interfaces;
using Atad.Domain.Models;
using Atad.Infrastructure.Persistence;
using MongoDB.Driver;

namespace Atad.Infrastructure.Repositories;

public class TodoRepository(IMongoDatabase db) : ITodoRepository
{
    private readonly IMongoCollection<Todo> _collection =
        db.GetCollection<Todo>(MongoCollections.Todos);

    private readonly ITodoListRepository _todoListRepository = new TodoListRepository(db);

    private async Task TouchTodoListIfSuccessfulAsync(bool operationSucceeded, Guid todoListId)
    {
        if (!operationSucceeded)
        {
            return;
        }

        await _todoListRepository.TouchTodoListAsync(todoListId);
    }

    public async Task<bool> UpsertTodoAsync(Todo todo)
    {
        try
        {
            var result = await _collection.ReplaceOneAsync(
                filter: x => x.Id == todo.Id,
                replacement: todo,
                options: new ReplaceOptions { IsUpsert = true }
            );

            var isSuccess = result.IsAcknowledged;
            await TouchTodoListIfSuccessfulAsync(isSuccess, todo.TodoListId);
            return isSuccess;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpsertManyTodosAsync(List<Todo> todos)
    {
        try
        {
            var models = todos.Select(todo =>
                new ReplaceOneModel<Todo>(
                    filter: Builders<Todo>.Filter.Eq(x => x.Id, todo.Id),
                    replacement: todo)
                {
                    IsUpsert = true
                }
            ).ToList();

            if (models.Count == 0) return true;

            var result = await _collection.BulkWriteAsync(models);
            var isSuccess = result.IsAcknowledged;

            if (isSuccess)
            {
                foreach (var todoListId in todos.Select(todo => todo.TodoListId).Distinct())
                {
                    await _todoListRepository.TouchTodoListAsync(todoListId);
                }
            }

            return isSuccess;
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
            return await _collection
                .Find(todo => todo.TodoListId == todoListId)
                .SortBy(todo => todo.OrderNumber)
                .ToListAsync();
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

            var isSuccess = result.IsAcknowledged && result.ModifiedCount > 0;
            await TouchTodoListIfSuccessfulAsync(isSuccess, newTodo.TodoListId);
            return isSuccess;
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
            var todo = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (todo is null)
            {
                return false;
            }

            var result = await _collection.DeleteOneAsync(todo => todo.Id == id);

            var isSuccess = result.IsAcknowledged && result.DeletedCount > 0;
            await TouchTodoListIfSuccessfulAsync(isSuccess, todo.TodoListId);
            return isSuccess;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteTodosByIdsAsync(List<Guid> todoIdsToDelete)
    {
        try
        {
            var todoListIds = await _collection
                .Find(todo => todoIdsToDelete.Contains(todo.Id))
                .Project(todo => todo.TodoListId)
                .ToListAsync();

            var result = await _collection.DeleteManyAsync(todo => todoIdsToDelete.Contains(todo.Id));

            var isSuccess = result.IsAcknowledged && result.DeletedCount > 0;
            if (isSuccess)
            {
                foreach (var todoListId in todoListIds.Distinct())
                {
                    await _todoListRepository.TouchTodoListAsync(todoListId);
                }
            }

            return isSuccess;
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

            var isSuccess = result.IsAcknowledged && result.DeletedCount > 0;
            await TouchTodoListIfSuccessfulAsync(isSuccess, todoListId);
            return isSuccess;
        }
        catch
        {
            return false;
        }
    }
}