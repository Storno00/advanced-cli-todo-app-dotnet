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
public class TodoListDetailScreen
{
    private readonly ITodoRepository _todoRepo;
    private readonly Dictionary<Guid, int> _lastKnownIndexes = [];

    private View? _container;
    private TodoList? _activeList;
    private List<Todo> _orderedTodos = [];

    public event Action<Todo>? TodoOpened;

    public TodoListDetailScreen(ITodoRepository todoRepo)
    {
        _todoRepo = todoRepo;
    }

    public async void Render(View container, TodoList list)
    {
        _container = container;
        _activeList = list;
        container.RemoveAll();

        var todos = await _todoRepo.GetAllTodosAsync(list.Id);
        _orderedTodos = FlattenTodos(todos, parentId: null);

        var todoStrings = _orderedTodos.Select(todo =>
        {
            var checkbox = "";

            if (todo.ParentId is null)
            {
                var children = _orderedTodos
                    .Where(orderedTodo => orderedTodo.ParentId == todo.Id)
                    .ToList();

                if (children.All(child => !child.IsDone)) checkbox = "[ ]";
                if (children.All(child => child.IsDone)) checkbox = "[X]";
                if (checkbox.Length == 0) checkbox = "[-]";
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
            if (listView.SelectedItem < 0 || listView.SelectedItem >= _orderedTodos.Count)
                return;

            var selectedTodo = _orderedTodos[listView.SelectedItem];

            // Toggle ([Space])
            if (args.KeyEvent.Key == Key.Space)
            {
                args.Handled = true;
                
                // Parents cannot be toggled manually
                if (selectedTodo.ParentId is null) return;

                selectedTodo.ToggleIsDone();
                await _todoRepo.UpsertTodoAsync(selectedTodo);

                // If all children are done, mark the parent as done as well
                var parent = _orderedTodos.First(t => t.Id == selectedTodo.ParentId);
                var allSiblingsAreDone = _orderedTodos
                    .Where(t => t.ParentId == selectedTodo.ParentId)
                    .All(t => t.IsDone);
                    
                parent.IsDone = allSiblingsAreDone;
                await _todoRepo.UpsertTodoAsync(parent);

                Render(container, list);
            }

            // Create (Ctrl+Enter, Ctrl+N)
            if (args.KeyEvent.Key is (Key.CtrlMask | Key.Enter) or (Key.CtrlMask | Key.n) or (Key.CtrlMask | Key.N))
            {
                args.Handled = true;
                ShowCreateDialog(list.Id);
            }

            // Rename (F2)
            if (args.KeyEvent.Key == Key.F2)
            {
                args.Handled = true;
                ShowRenameDialog(selectedTodo);
            }

            // Delete (Delete / Backspace)
            if (args.KeyEvent.Key is Key.Delete or Key.Backspace)
            {
                args.Handled = true;
                ShowDeleteDialog(selectedTodo);
            }
        };

        container.Add(listView);
        listView.SetFocus();
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

    private void ShowCreateDialog(Guid listId)
    {
        TextInputDialog.Show("New TODO", "Save", async text =>
        {
            var newTodo = new Todo { Name = text, TodoListId = listId };
            await _todoRepo.UpsertTodoAsync(newTodo);
            Render(_container!, _activeList!);
        });
    }

    private void ShowRenameDialog(Todo todoToRename)
    {
        TextInputDialog.Show($"Rename '{todoToRename.Name}'", "Rename", async text =>
        {
            todoToRename.Name = text;
            await _todoRepo.UpsertTodoAsync(todoToRename);
            Render(_container!, _activeList!);
        }, includeCancelButton: true);
    }

    private void ShowDeleteDialog(Todo todoToDelete)
    {
        ConfirmationDialog.Show(
            $"Delete '{todoToDelete.Name}'?",
            "Are you sure you want to delete this todo?",
            async () =>
            {
                await _todoRepo.DeleteTodoByIdAsync(todoToDelete.Id);
                Render(_container!, _activeList!);
            });
    }
}
