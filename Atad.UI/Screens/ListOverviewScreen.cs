using System.Data;
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

    private readonly ITodoListStatService _todoListStatService;

    private View? _container;
    private List<TodoList> _todoLists = [];
    private int _lastKnownIndex;

    public event Action<TodoList>? ListOpened;

    public ListOverviewScreen(ITodoListRepository todoListRepo, ITodoRepository todoRepo, ITodoListStatService todoListStatService)
    {
        _todoListRepo = todoListRepo;
        _todoRepo = todoRepo;
        _todoListStatService = todoListStatService;
    }

    public async void Render(View container)
    {
        _container = container;
        container.RemoveAll();

        _todoLists = (await _todoListRepo.GetAllTodoListsAsync()).ToList();

        var todoListStats = await _todoListStatService.GetAllTodoListStatsAsync();

        var table = new DataTable();

        table.Columns.Add("Name");
        table.Columns.Add("Compleation");
        table.Columns.Add("Total number of TODOs");

        foreach (var todoList in _todoLists)
        {
            todoListStats.TryGetValue(todoList.Id, out var todoStat);

            table.Rows.Add(todoList.Name);
            table.Rows.Add(todoStat?.ComplitionPercentage.ToString());
            table.Rows.Add(todoStat?.TotalNumberOfTodos.ToString());
        }

        var tableView = new TableView(table)
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            FullRowSelect = true
        };

        //tableView.Style.ShowHorizontalHeaderOverline = false;
        //tableView.Style.ShowHorizontalHeaderUnderline = false;
        //tableView.Style.ShowHorizontalBottomline = false;
        //tableView.Style.ShowVerticalCellLines = false;
        //tableView.Style.ShowVerticalHeaderLines = false;

        tableView.SelectedRow = _lastKnownIndex >= _todoLists.Count
            ? Math.Max(_todoLists.Count - 1, 0)
            : _lastKnownIndex;

        tableView.SelectedCellChanged += args => _lastKnownIndex = args.NewRow;

        tableView.CellActivated += args =>
        {
            if (args.Row >= 0 && args.Row < _todoLists.Count)
            {
                ListOpened?.Invoke(_todoLists[args.Row]);
            }
        };

        tableView.KeyDown += args =>
        {
            // Create a new list (F1)
            if (args.KeyEvent.Key is Key.F1)
            {
                ShowCreateDialog();
                args.Handled = true;
            }

            // Rename list (F3)
            if (args.KeyEvent.Key is Key.F3 && _todoLists.Count > 0)
            {
                ShowRenameDialog(_todoLists[tableView.SelectedRow]);
                args.Handled = true;
            }

            // Delete a list (Del)
            if (args.KeyEvent.Key is Key.DeleteChar && _todoLists.Count > 0)
            {
                ShowDeleteDialog(_todoLists[tableView.SelectedRow]);
                args.Handled = true;
            }
        };

        container.Add(tableView);
        tableView.SetFocus();
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