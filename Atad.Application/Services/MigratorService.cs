using Atad.Application.Interfaces;

namespace Atad.Application.Services;

public class MigratorService(ITodoListRepository todoListRepository, ITodoRepository todoRepository) : IMigratorService
{
    private readonly ITodoListRepository _todoListRepository = todoListRepository;
    private readonly ITodoRepository _todoRepository = todoRepository;

    public async Task AddOrderNumbersIfTheyDoesntExistAsync()
    {
        var todoLists = await _todoListRepository.GetAllTodoListsAsync();

        if (todoLists.Count == 0) return;

        var orderNumbers = todoLists
            .Select(todoList => todoList.OrderNumber)
            .ToList();

        var isAllUnique = orderNumbers.Count == orderNumbers
            .Distinct()
            .Count();

        if (isAllUnique) return;

        var orderedLists = todoLists
            .OrderBy(todoList => todoList.OrderNumber)
            .ToList();

        for (var i = 0; i < orderedLists.Count; i++) {
            orderedLists[i].OrderNumber = i;
        }

        await _todoListRepository.UpsertMenyTodoListsAsync(orderedLists);
    }

    public async Task AddTodoOrderNumbersIfTheyDoesntExistAsync()
    {
        var todoLists = await _todoListRepository.GetAllTodoListsAsync();

        foreach (var todoList in todoLists)
        {
            var todos = await _todoRepository.GetAllTodosAsync(todoList.Id);
            if (todos.Count == 0) continue;

            var orderNumbers = todos
                .Select(todo => todo.OrderNumber)
                .ToList();

            var isAllUnique = orderNumbers.Count == orderNumbers
                .Distinct()
                .Count();

            if (isAllUnique) continue;

            var orderedTodos = todos
                .OrderBy(todo => todo.OrderNumber)
                .ThenBy(todo => todo.CreatedAt)
                .ToList();

            for (var i = 0; i < orderedTodos.Count; i++)
            {
                orderedTodos[i].OrderNumber = i;
            }

            await _todoRepository.UpsertManyTodosAsync(orderedTodos);
        }
    }
}
