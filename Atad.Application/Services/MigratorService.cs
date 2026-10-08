using Atad.Application.Interfaces;

namespace Atad.Application.Services;

public class MigratorService(ITodoListRepository todoListRepository) : IMigratorService
{
    private readonly ITodoListRepository _todoListRepository = todoListRepository;

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
}
