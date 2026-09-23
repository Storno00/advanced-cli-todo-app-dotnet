using Atad.Application.Interfaces;
using Atad.Domain.Models;
using Atad.UI.Dialogs;
using Terminal.Gui;

namespace Atad.UI.Screens;

/// <summary>
/// Renders the top-level "all todo lists" screen and owns the create /
/// rename / delete dialogs for todo lists. Raises <see cref="ListOpened"/>
/// when the user opens a list, letting the caller decide what happens next.
/// </summary>
public class ListOverviewScreen
{
    private readonly ITodoListRepository _todoListRepo;
    private readonly ITodoRepository _todoRepo;

    private View? _container;
    private List<TodoList> _todoLists = [];
    private int _lastKnownIndex;

    public event Action<TodoList>? ListOpened;

    public ListOverviewScreen(ITodoListRepository todoListRepo, ITodoRepository todoRepo)
    {
        _todoListRepo = todoListRepo;
        _todoRepo = todoRepo;
    }

    public async void Render(View container)
    {
        _container = container;
        container.RemoveAll();

        _todoLists = (await _todoListRepo.GetAllTodoListsAsync()).ToList();
        var todoListNames = _todoLists.Select(todoList => $"- {todoList.Name}").ToList();

        var listView = new ListView(todoListNames)
        {
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        listView.SelectedItem = _lastKnownIndex >= _todoLists.Count
            ? _todoLists.Count - 1
            : _lastKnownIndex;

        listView.SelectedItemChanged += e => _lastKnownIndex = e.Item;

        listView.OpenSelectedItem += args =>
        {
            if (args.Item >= 0 && args.Item < _todoLists.Count)
            {
                ListOpened?.Invoke(_todoLists[args.Item]);
            }
        };

        listView.KeyDown += args =>
        {
            // Create a new list (F1)
            if (args.KeyEvent.Key is Key.F1)
            {
                ShowCreateDialog();
                args.Handled = true;
            }

            // Rename list (F3)
            if (args.KeyEvent.Key is Key.F3)
            {
                ShowRenameDialog(_todoLists[listView.SelectedItem]);
                args.Handled = true;
            }

            // Delete a list (Del)
            if (args.KeyEvent.Key is Key.DeleteChar)
            {
                ShowDeleteDialog(_todoLists[listView.SelectedItem]);
                args.Handled = true;
            }
        };

        container.Add(listView);
        listView.SetFocus();
    }

    private void ShowCreateDialog()
    {
        TextInputDialog.Show("Create new TODO List", "Create", async text =>
        {
            var newList = new TodoList { Name = text };
            await _todoListRepo.UpsertTodoListAsync(newList);
            Render(_container!);
        });
    }

    private void ShowRenameDialog(TodoList listToRename)
    {
        TextInputDialog.Show($"Rename '{listToRename.Name}'", "Rename", async text =>
        {
            listToRename.Name = text;
            await _todoListRepo.UpsertTodoListAsync(listToRename);
            Render(_container!);
        }, initialInputValue: listToRename.Name, includeCancelButton: true);
    }

    private void ShowDeleteDialog(TodoList listToDelete)
    {
        ConfirmationDialog.Show(
            $"Delete '{listToDelete.Name}'?",
            "Are you sure you want to delete this list?\nThis will also delete all todos in the list.",
            async () =>
            {
                await _todoRepo.DeleteTodosByTodoListId(listToDelete.Id);
                await _todoListRepo.DeleteTodoListByIdAsync(listToDelete.Id);
                Render(_container!);
            });
    }
}
