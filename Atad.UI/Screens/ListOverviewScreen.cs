using Atad.Application.Interfaces;
using Atad.Domain.Models;
using Atad.UI;
using Atad.UI.Dialogs;
using System.Data;
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

        if (_todoLists.Count == 0)
        {
            ShowEmptyView(container);
            return;
        }

        var todoListStats = await _todoListStatService.GetAllTodoListStatsAsync();

        var table = new DataTable();

        table.Columns.Add("Name");
        table.Columns.Add("Progress");
        table.Columns.Add("TODOs");
        table.Columns.Add("Last updated");
        table.Columns.Add("Creation date");

        foreach (var todoList in _todoLists)
        {
            todoListStats.TryGetValue(todoList.Id, out var todoStat);

            table.Rows.Add(
                todoList.Name,
                $"{todoStat?.ComplitionPercentage}%",
                todoStat?.TotalNumberOfTodos.ToString(),
                todoList.LastUpdatedAt.ToString("yyyy-MM-dd HH:mm"),
                todoList.CreatedAt.ToString("yyyy-MM-dd"));
        }

        var tableView = new TableView(table)
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            FullRowSelect = true,
        };

        tableView.Style.ShowHorizontalHeaderOverline = false;
        tableView.Style.ShowVerticalCellLines = false;
        tableView.Style.ShowVerticalHeaderLines = false;
        tableView.Style.RowColorGetter = args =>
        {
            if (args.RowIndex < 0 || args.RowIndex >= _todoLists.Count)
            {
                return null;
            }

            var todoList = _todoLists[args.RowIndex];

            if (ColorPalette.TryGetColorScheme(todoList.Color, out var scheme))
            {
                return scheme;
            }

            return null;
        };
        
        const int columnGap = 10;
        foreach (DataColumn column in table.Columns)
        {
            var longestCellLength = table.Rows.Cast<DataRow>()
                .Select(row => row[column]?.ToString()?.Length ?? 0)
                .DefaultIfEmpty(0)
                .Max();
 
            tableView.Style.ColumnStyles[column] = new TableView.ColumnStyle
            {
                MinWidth = Math.Max(column.ColumnName.Length, longestCellLength) + columnGap
            };
        }

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
            var selectedRow = tableView.SelectedRow;
            var normalizedKey = args.KeyEvent.Key & ~Key.AltMask & ~Key.CtrlMask & ~Key.ShiftMask;

            // Move list up/down (Alt+Up / Alt+Down)
            if (args.KeyEvent.IsAlt && _todoLists.Count > 1)
            {
                if (normalizedKey == Key.CursorUp)
                {
                    args.Handled = true;

                    if (selectedRow > 0)
                    {
                        MoveTodoList(selectedRow, selectedRow - 1);
                    }
                }

                if (normalizedKey == Key.CursorDown)
                {
                    args.Handled = true;

                    if (selectedRow >= 0 && selectedRow < _todoLists.Count - 1)
                    {
                        MoveTodoList(selectedRow, selectedRow + 1);
                    }
                }
            }

            // Create a new list (F1)
            if (args.KeyEvent.Key is Key.F1)
            {
                ShowCreateDialog();
                args.Handled = true;
            }

            // Rename list (F2)
            if (args.KeyEvent.Key is Key.F2 && _todoLists.Count > 0)
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

            // Colorize list (F4)
            if (args.KeyEvent.Key is Key.F4 && _todoLists.Count > 0)
            {
                ShowColorizeDialog(_todoLists[tableView.SelectedRow]);
                args.Handled = true;
            }
        };

        container.Add(tableView);
        tableView.SetFocus();
    }

    private void ShowEmptyView(View container)
    {
        container.CanFocus = true;

        var emptyLabel = new Label("Currently there are no TODO lists. Press F1 to create a new one!")
        {
            X = Pos.Center(),
            Y = Pos.Center(),
            TextAlignment = TextAlignment.Centered,
            CanFocus = true,
            ColorScheme = new ColorScheme
            {
                Normal = Colors.Base.Normal,
                Focus = new Terminal.Gui.Attribute(Color.White, Color.Black)
            }
        };

        emptyLabel.KeyDown += (args) =>
        {
            if (args.KeyEvent.Key is Key.F1)
            {
                ShowCreateDialog();
                args.Handled = true;
            }
        };

        container.Add(emptyLabel);
        emptyLabel.SetFocus();
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

    private void ShowColorizeDialog(TodoList listToColorize)
    {
        ColorPickerDialog.Show($"Colorize '{listToColorize.Name}'", listToColorize.Color, async selectedColor =>
        {
            listToColorize.Color = selectedColor;
            await _todoListRepo.UpsertTodoListAsync(listToColorize);
            Render(_container!);
        });
    }

    private async void MoveTodoList(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= _todoLists.Count) return;
        if (toIndex < 0 || toIndex >= _todoLists.Count) return;
        if (fromIndex == toIndex) return;

        var movedList = _todoLists[fromIndex];
        _todoLists.RemoveAt(fromIndex);
        _todoLists.Insert(toIndex, movedList);

        for (var i = 0; i < _todoLists.Count; i++)
        {
            _todoLists[i].OrderNumber = i;
        }

        var isSuccess = await _todoListRepo.UpsertMenyTodoListsAsync(_todoLists);
        if (!isSuccess) return;

        _lastKnownIndex = toIndex;
        Render(_container!);
    }
}