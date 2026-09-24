using Atad.Application.Interfaces;
using Atad.Domain.Models;
using Atad.UI.Dialogs;
using Terminal.Gui;

namespace Atad.UI.Screens;

/// <summary>
/// Renders the flattened parent/child todo list for a single
/// <see cref="TodoList"/> and owns the create / rename / delete /
/// toggle-done interactions for todos. Raises <see cref="TodoOpened"/> when
/// the user opens a todo, letting the caller decide what happens next.
/// </summary>
public class TodoListDetailScreen(ITodoRepository todoRepo)
{
    private readonly Dictionary<Guid, int> _lastKnownIndexes = [];

    private View? _container;
    private TodoList? _activeList;
    private List<Todo> _orderedTodos = [];

    public event Action<Todo>? TodoOpened;

    public async void Render(View container, TodoList list)
    {
        _container = container;
        _activeList = list;
        container.RemoveAll();

        var todos = await todoRepo.GetAllTodosAsync(list.Id);
        _orderedTodos = FlattenTodos(todos, parentId: null);

        if (_orderedTodos.Count == 0)
        {
            ShowEmptyView(_container);
            return;
        }

        var todoStrings = _orderedTodos.Select(todo =>
        {
            var checkbox = "";

            if (todo.ParentId is null)
            {
                var children = _orderedTodos
                    .Where(orderedTodo => orderedTodo.ParentId == todo.Id)
                    .ToList();
                var hasChildren = children.Count > 0;

                if (hasChildren && children.All(child => !child.IsDone)) checkbox = "[ ]";
                if (hasChildren && children.All(child => child.IsDone)) checkbox = "[X]";
                if (hasChildren && checkbox.Length == 0) checkbox = "[-]";
            }

            if (checkbox.Length == 0) checkbox = todo.IsDone ? "[X]" : "[ ]";

            var indent = todo.ParentId is not null ? "    → " : "↓";

            return $"{indent}{checkbox} {todo.Name}";
        }).ToList();

        var listView = new ListView(todoStrings)
        {
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        _lastKnownIndexes.TryGetValue(list.Id, out var lastKnownIndex);
        if (_orderedTodos.Count > 0)
        {
            listView.SelectedItem = Math.Clamp(lastKnownIndex, 0, _orderedTodos.Count - 1);
        }

        listView.OpenSelectedItem += args =>
        {
            if (args.Item >= 0 && args.Item < _orderedTodos.Count)
            {
                TodoOpened?.Invoke(_orderedTodos[args.Item]);
            }
        };

        listView.SelectedItemChanged += e => _lastKnownIndexes[list.Id] = e.Item;

        listView.KeyPress += async args =>
        {
            var selectedTodo = GetSelectedTodo(listView);

            // Toggle ([Space])
            if (args.KeyEvent.Key == Key.Space)
            {
                args.Handled = true;

                if (selectedTodo is null) return;

                // If parent has no children
                if (selectedTodo.ParentId is null)
                {
                    var children = _orderedTodos
                        .Where(todo => todo.ParentId == selectedTodo.Id)
                        .ToList();

                    if (children.Count > 0) return;

                    selectedTodo.ToggleIsDone();
                    await todoRepo.UpsertTodoAsync(selectedTodo);
                    Render(container, list);

                    return;
                }

                // If parent has children
                selectedTodo.ToggleIsDone();
                await todoRepo.UpsertTodoAsync(selectedTodo);

                // If all children are done, mark the parent as done as well
                var parent = _orderedTodos.First(t => t.Id == selectedTodo.ParentId);
                var allSiblingsAreDone = _orderedTodos
                    .Where(t => t.ParentId == selectedTodo.ParentId)
                    .All(t => t.IsDone);

                parent.IsDone = allSiblingsAreDone;
                await todoRepo.UpsertTodoAsync(parent);

                Render(container, list);
            }

            // Create parent (F1)
            if (args.KeyEvent.Key is Key.F1)
            {
                args.Handled = true;
                ShowCreateDialog(list.Id);
            }

            // Create a child (F2)
            if (args.KeyEvent.Key is Key.F2)
            {
                args.Handled = true;

                if (selectedTodo is null) return;

                var parentId = selectedTodo.ParentId ?? selectedTodo.Id;

                ShowCreateDialog(list.Id, parentId);
            }

            // Rename (F3)
            if (args.KeyEvent.Key == Key.F3)
            {
                args.Handled = true;

                if (selectedTodo is null) return;

                ShowRenameDialog(selectedTodo);
            }

            // Delete (Del)
            if (args.KeyEvent.Key is Key.DeleteChar)
            {
                args.Handled = true;

                if (selectedTodo is null) return;

                ShowDeleteDialog(selectedTodo);
            }
        };

        container.Add(listView);
        listView.SetFocus();
    }

    private void ShowEmptyView(View container)
    {
        container.CanFocus = true;

        var emptyLabel = new Label("Currently there are no TODOs in this list. Press F1 to create a new one!")
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
            if (args.KeyEvent.Key is Key.F1 && _activeList is not null)
            {
                ShowCreateDialog(_activeList.Id);
                args.Handled = true;
            }
        };

        container.Add(emptyLabel);
        emptyLabel.SetFocus();
    }

    private Todo? GetSelectedTodo(ListView listView)
    {
        if (listView.SelectedItem < 0 || listView.SelectedItem >= _orderedTodos.Count) return null;

        return _orderedTodos[listView.SelectedItem];
    }

    private static List<Todo> FlattenTodos(IEnumerable<Todo> allTodos, Guid? parentId)
    {
        var result = new List<Todo>();
        var enumerable = allTodos as Todo[] ?? allTodos.ToArray();
        var children = enumerable.Where(t => t.ParentId == parentId);

        foreach (var child in children)
        {
            result.Add(child);
            result.AddRange(FlattenTodos(enumerable, child.Id));
        }

        return result;
    }

    private void ShowCreateDialog(Guid listId, Guid? parentId = null)
    {
        var dialogTitle = parentId is not null
            ? "Create new SUB_TODO"
            : "Create new TODO";

        TextInputDialog.Show(dialogTitle, "Save", async text =>
        {
            var newTodo = parentId is not null
                ? new Todo { Name = text, TodoListId = listId, ParentId = parentId }
                : new Todo { Name = text, TodoListId = listId };

            // If parent doesn't have children and parent is compleated, then set to incompleate
            if (newTodo.ParentId is not null && !_orderedTodos.Any(todo => todo.ParentId == parentId))
            {
                var parent = _orderedTodos.Find(todo => todo.Id == parentId);

                if (parent is null) return;

                if (parent.IsDone)
                {
                    parent.IsDone = false;
                    await todoRepo.UpsertTodoAsync(parent);
                }
            }

            await todoRepo.UpsertTodoAsync(newTodo);
            Render(_container!, _activeList!);
        });
    }

    private void ShowRenameDialog(Todo todoToRename)
    {
        TextInputDialog.Show($"Rename '{todoToRename.Name}'", "Rename", async text =>
        {
            todoToRename.Name = text;
            await todoRepo.UpsertTodoAsync(todoToRename);
            Render(_container!, _activeList!);
        }, initialInputValue: todoToRename.Name, includeCancelButton: true);
    }

    private void ShowDeleteDialog(Todo todoToDelete)
    {
        ConfirmationDialog.Show(
            $"Delete '{todoToDelete.Name}'?",
            "Are you sure you want to delete this todo?",
            async () =>
            {
                if (todoToDelete.ParentId is null)
                {
                    var childrenToDelete = _orderedTodos
                        .Where(todo => todo.ParentId == todoToDelete.Id)
                        .Select(todo => todo.Id)
                        .ToList();

                    await todoRepo.DeleteTodosByIdsAsync(childrenToDelete);
                }

                await todoRepo.DeleteTodoByIdAsync(todoToDelete.Id);
                Render(_container!, _activeList!);
            });
    }
}
