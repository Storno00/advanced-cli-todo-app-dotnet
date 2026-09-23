using Atad.Domain.Models;

namespace Atad.Application.Interfaces;

public interface ITodoListStatService
{
    public Task<Dictionary<Guid, TodoListStat>> GetAllTodoListStatsAsync();
}
