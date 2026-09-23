using Atad.Application.Interfaces;
using Atad.Domain.Models;

namespace Atad.Application.Services;

public class TodoListStatService(ITodoListRepository todoListRepository, ITodoRepository todoRepository) : ITodoListStatService
{
    private readonly ITodoListRepository _todoListRepository = todoListRepository;

    private readonly ITodoRepository _todoRepository = todoRepository;

    public async Task<Dictionary<Guid, TodoListStat>> GetAllTodoListStatsAsync()
    {
        var allTodoListIds = (await _todoListRepository
            .GetAllTodoListsAsync())
            .Select(todoList => todoList.Id)
            .ToList();

        var output = new Dictionary<Guid, TodoListStat>();

        foreach (var todoListId in allTodoListIds)
        {
            var allTodosOfCurrentList = await _todoRepository.GetAllTodosAsync(todoListId);
            var allCompleatedTodoCountOfCurrentList = allTodosOfCurrentList
                .Where(todo => todo.IsDone)
                .ToList()
                .Count;
            var complitionPercentage = allTodosOfCurrentList.Count == 0
                ? 0
                : allCompleatedTodoCountOfCurrentList / allTodosOfCurrentList.Count * 100;

            var todoListStat = new TodoListStat(allTodosOfCurrentList.Count,
                complitionPercentage);

            output.Add(todoListId, todoListStat);
        }

        return output;
    }
}
