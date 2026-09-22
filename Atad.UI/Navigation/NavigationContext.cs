using Atad.Domain.Models;

namespace Atad.UI.Navigation;

public class NavigationContext
{
    public ApplicationState CurrentView { get; set; } = ApplicationState.ListOverview;
    public TodoList? ActiveList { get; set; }
    public Todo? ActiveTodo { get; set; }

    public void NavigateToOverview()
    {
        CurrentView = ApplicationState.ListOverview;
        ActiveList = null;
        ActiveTodo = null;
    }

    public void NavigateToDetail(TodoList list)
    {
        CurrentView = ApplicationState.TodoListDetail;
        ActiveList = list;
        ActiveTodo = null;
    }

    public void NavigateToEditor(Todo todo)
    {
        CurrentView = ApplicationState.TodoEditor;
        ActiveTodo = todo;
    }
}