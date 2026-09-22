using Atad.Application.Interfaces;
using Atad.Domain.Models;
using Atad.UI.Navigation;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;
using TgApplication = Terminal.Gui.Application;

namespace Atad.UI;

public class MainWindow : Window
{
    private readonly ITodoListRepository _todoListRepo;
    private readonly ITodoRepository _todoRepo;
    
    private readonly NavigationContext _navContext;

    private readonly View _contentArea;
    
    private readonly Label _breadcrumbLabel;
    
    private readonly List<KeyboardShortcutViewModel> _keyBindings = [];
    private View? _shortcutBarView;

    private int _todoListsLastKnownIndex;
    private readonly Dictionary<Guid, int> _todoLastKnownIndexes = [];

    public MainWindow(
        ITodoListRepository todoListRepo,
        ITodoRepository todoRepo,
        NavigationContext navContext)
    {
        _todoListRepo = todoListRepo;
        _todoRepo = todoRepo;
        _navContext = navContext;

        Title = "CLI TODO App";

        _breadcrumbLabel = new Label()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 1,
            ColorScheme = new ColorScheme()
            {
                Normal = Attribute.Make(Color.White, Color.DarkGray)
            }
        };

        _contentArea = new View()
        {
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1
        };
        
        Add(_breadcrumbLabel, _contentArea);
        
        KeyDown += (args) =>
        {
            if (args.KeyEvent.Key != Key.Esc) return;
            
            if (HandleBackNavigation())
            {
                args.Handled = true;
            }
        };

        ShowListOverview();
    }

    private void UpdateBreadcrumb()
    {
        _breadcrumbLabel.Text = _navContext.CurrentView switch
        {
            ApplicationState.ListOverview => "Todo Lists",
            ApplicationState.TodoListDetail => $"Todo Lists > {_navContext.ActiveList?.Name}",
            ApplicationState.TodoEditor => $"Todo Lists > {_navContext.ActiveList?.Name} > {_navContext.ActiveTodo?.Name}",
            _ => ""
        };
    }

    private void UpdateKeyboardShortcuts()
    {
        _keyBindings.RemoveAll(_ => true);
        
        switch (_navContext.CurrentView)
        {
            case ApplicationState.ListOverview:
            {
                _keyBindings.Add(new KeyboardShortcutViewModel("[Ctrl+Enter]", "Create new"));
                _keyBindings.Add(new KeyboardShortcutViewModel("[F2]", "Edit title"));
                _keyBindings.Add(new KeyboardShortcutViewModel("[Del]", "Remove"));
                _keyBindings.Add(new KeyboardShortcutViewModel("[Esc]", "Exit"));
                break;
            }
            case ApplicationState.TodoListDetail:
            {
                _keyBindings.Add(new KeyboardShortcutViewModel("[Ctrl+Enter]", "Create new"));
                _keyBindings.Add(new KeyboardShortcutViewModel("[F2]", "Edit title"));
                _keyBindings.Add(new KeyboardShortcutViewModel("[Del]", "Remove"));
                _keyBindings.Add(new KeyboardShortcutViewModel("[Esc]", "Back"));
                break;
            }
            
            case ApplicationState.TodoEditor:
            default:
            {
                _keyBindings.Add(new KeyboardShortcutViewModel("[Esc]", "Save"));
                _keyBindings.Add(new KeyboardShortcutViewModel("[Ctrl+Esc]", "Exit"));
                break;
            }
        }
    }
    
    private void AddShortcutHint(View parentContainer)
    {
        UpdateKeyboardShortcuts();

        if (_shortcutBarView != null)
        {
            parentContainer.Remove(_shortcutBarView);
            _shortcutBarView = null;
        }

        if (!_keyBindings.Any()) return;

        var highlightScheme = new ColorScheme {
            Normal = new Attribute(Color.BrightYellow, Color.Black),
            Focus = new Attribute(Color.BrightYellow, Color.Black)
        };
    
        var normalScheme = new ColorScheme {
            Normal = new Attribute(Color.White, Color.Black),
            Focus = new Attribute(Color.White, Color.Black)
        };

        var totalWidth = _keyBindings
            .Sum(s => s.KeyboardShortcut.Length + s.Description.Length + 3) + 1;

        _shortcutBarView = new View() {
            X = Pos.AnchorEnd(totalWidth),
            Y = Pos.AnchorEnd(1),
            Width = totalWidth,
            Height = 1
        };

        Pos currentX = 0;

        for (var i = 0; i < _keyBindings.Count; i++)
        {
            var shortcut = _keyBindings[i];
            
            var keyText = $"{shortcut.KeyboardShortcut} ";
            var descText = i == _keyBindings.Count - 1
                ? $"{shortcut.Description}"
                : $"{shortcut.Description} | ";

            var keyLabel = new Label(keyText)
            {
                X = currentX,
                Y = 0,
                ColorScheme = highlightScheme
            };

            var descLabel = new Label(descText)
            {
                X = Pos.Right(keyLabel),
                Y = 0,
                ColorScheme = normalScheme
            };

            _shortcutBarView.Add(keyLabel, descLabel);
            currentX = Pos.Right(descLabel);
        }

        parentContainer.Add(_shortcutBarView);
    }
    
    private async void ShowListOverview()
    {
        _navContext.NavigateToOverview();
        UpdateBreadcrumb();
        AddShortcutHint(this);
        _contentArea.RemoveAll();

        var todoLists = await _todoListRepo.GetAllTodoListsAsync();
        var todoListNames = todoLists.Select(todoList => $"- {todoList.Name}").ToList();

        var listView = new ListView(todoListNames)
        {
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        
        listView.SelectedItem = _todoListsLastKnownIndex >= todoLists.Count
            ? todoLists.Count - 1
            : _todoListsLastKnownIndex;

        listView.SelectedItemChanged += (e) =>
        {
            _todoListsLastKnownIndex = e.Item;
        };
        
        listView.OpenSelectedItem += (args) =>
        {
            if (args.Item >= 0 && args.Item < todoLists.Count)
            {
                ShowTodoListDetail(todoLists[args.Item]);
            }
        };

        listView.KeyDown += (args) =>
        {
            // Create a new list
            if (args.KeyEvent.Key is (Key.CtrlMask | Key.Enter) or (Key.CtrlMask | Key.n) or (Key.CtrlMask | Key.N))
            {
                CreateNewTodoListDialog();
                args.Handled = true;
            }
            
            // Rename list
            if (args.KeyEvent.Key is Key.F2)
            {
                RenameTodoListDialog(todoLists[listView.SelectedItem]);
                args.Handled = true;
            }

            // Delete a list
            if (args.KeyEvent.Key is Key.Delete or Key.Backspace)
            {
                DeleteTodoListDialog(todoLists[listView.SelectedItem]);
                args.Handled = true;
            }
        };

        _contentArea.Add(listView);
        listView.SetFocus();
    }
    
    private async void ShowTodoListDetail(TodoList list)
    {
        _navContext.NavigateToDetail(list);
        UpdateBreadcrumb();
        AddShortcutHint(this);
        _contentArea.RemoveAll();

        var todos = await _todoRepo.GetAllTodosAsync(list.Id);

        var listView = new ListView(todos.Select(t => $"- [{(t.IsDone ? "X" : " ")}] {t.Name}").ToList())
        {
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        
        _todoLastKnownIndexes.TryGetValue(list.Id, out var lastKnownIndex);
        listView.SelectedItem = lastKnownIndex >= todos.Count
            ? todos.Count - 1
            : lastKnownIndex;

        listView.OpenSelectedItem += (args) =>
        {
            if (args.Item >= 0 && args.Item < todos.Count)
            {
                ShowTodoEditor(todos[args.Item]);
            }
        };

        listView.SelectedItemChanged += (e) =>
        {
            _todoLastKnownIndexes[list.Id] = e.Item;
        };
        
        listView.KeyDown += async (args) =>
        {
            // Toggle
            if (args.KeyEvent.Key == Key.Space)
            {
                args.Handled = true;
                var todo = todos[listView.SelectedItem];
                todo.ToggleIsDone();
                await _todoRepo.UpsertTodoAsync(todo);
                ShowTodoListDetail(list);
            }
            
            // Create
            if (args.KeyEvent.Key is (Key.CtrlMask | Key.Enter) or (Key.CtrlMask | Key.n) or (Key.CtrlMask | Key.N))
            {
                args.Handled = true;
                CreateNewTodoDialog(list.Id);
            }
            
            // Rename
            if (args.KeyEvent.Key is Key.F2)
            {
                args.Handled = true;
                RenameTodoDialog(todos[listView.SelectedItem]);
            }

            // Delete
            if (args.KeyEvent.Key is Key.Delete or Key.Backspace)
            {
                args.Handled = true;
                DeleteTodoDialog(todos[listView.SelectedItem]);
            }
        };

        _contentArea.Add(listView);
        listView.SetFocus();
    }
    
    private void ShowTodoEditor(Todo todo)
    {
        _navContext.NavigateToEditor(todo);
        UpdateBreadcrumb();
        AddShortcutHint(this);
        _contentArea.RemoveAll();
        
        var frame = new FrameView("Description")
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ColorScheme = new ColorScheme()
            {
                Focus = Attribute.Make(Color.White, Color.Black)
            }
        };

        var textView = new TextView()
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Text = todo.LongDescription,
            ColorScheme = new ColorScheme
            {
                Focus = new Attribute(Color.White, Color.DarkGray),
            }
        };
        
        textView.KeyDown += async (args) =>
        {
            if (args.KeyEvent.Key == (Key.CtrlMask | Key.Esc))
            {
                ShowTodoListDetail(_navContext.ActiveList!);
                args.Handled = true;
                return;
            }
            
            if (args.KeyEvent.Key == (Key.Esc))
            {
                todo.LongDescription = textView.Text.ToString() ?? string.Empty;
                await _todoRepo.UpsertTodoAsync(todo);
                ShowTodoListDetail(_navContext.ActiveList!);
                args.Handled = true;
            }
        };

        frame.Add(textView);
        _contentArea.Add(frame);
        textView.SetFocus();
    }

    private bool HandleBackNavigation()
    {
        if (_navContext.CurrentView != ApplicationState.TodoListDetail) return false;
        
        ShowListOverview();
        return true;
    }

    private void CreateNewTodoListDialog()
    {
        var dialog = new Dialog("Create new TODO List", 50, 7);
        var input = new TextField("") { X = 1, Y = 1, Width = Dim.Fill() - 2 };
        var okBtn = new Button("Create") { X = 1, Y = 3 };

        okBtn.Clicked += HandleCreateNewTodoList;
        input.KeyDown += (e) =>
        {
            if (e.KeyEvent.Key != Key.Enter) return;
            HandleCreateNewTodoList();
            e.Handled = true; 
        };

        dialog.Add(input, okBtn);
        TgApplication.Run(dialog);
        
        return;

        async void HandleCreateNewTodoList()
        {
            var text = input.Text?.ToString();
            
            if (string.IsNullOrWhiteSpace(text)) return;
            
            var newList = new TodoList
            {
                Name = text,
            };
            await _todoListRepo.UpsertTodoListAsync(newList);
            TgApplication.RequestStop();
            ShowListOverview();
        }
    }
    
    private void RenameTodoListDialog(TodoList listToRename)
    {
        var dialog = new Dialog($"Rename '{listToRename.Name}'", 50, 7);
        var input = new TextField("") { X = 1, Y = 1, Width = Dim.Fill() - 2 };
        var okBtn = new Button("Rename");
        var cancelBtn = new Button("Cancel");
        
        okBtn.Clicked += HandleRenameTodoList;
        cancelBtn.Clicked += () => TgApplication.RequestStop();
        input.KeyDown += (e) =>
        {
            if (e.KeyEvent.Key != Key.Enter) return;
            
            e.Handled = true;
            HandleRenameTodoList();
            ShowListOverview();
        };
        
        dialog.Add(input);
        dialog.AddButton(okBtn);
        dialog.AddButton(cancelBtn);
        
        TgApplication.Run(dialog);

        async void HandleRenameTodoList()
        {
            var text = input.Text?.ToString();

            if (string.IsNullOrWhiteSpace(text)) return;

            listToRename.Name = text;
            
            await _todoListRepo.UpsertTodoListAsync(listToRename);
            
            TgApplication.RequestStop();
            ShowListOverview();
        }
    }
    
    private void DeleteTodoListDialog(TodoList listToDelete)
    {
        var dialog = new Dialog($"Delete '{listToDelete.Name}'?", 50, 7);
        var okBtn = new Button("Delete");
        var cancelBtn = new Button("Cancel");
        
        dialog.ColorScheme = new ColorScheme
        {
            Normal    = new Attribute(Color.White, Color.Red),
            Focus     = new Attribute(Color.White, Color.Red),
            HotNormal = new Attribute(Color.BrightYellow, Color.Red),
            HotFocus  = new Attribute(Color.BrightYellow, Color.Red),
            Disabled  = new Attribute(Color.Gray, Color.Red),
        };
        
        var buttonScheme = new ColorScheme
        {
            Normal    = new Attribute(Color.White, Color.Red),
            Focus     = new Attribute(Color.Red, Color.White),
            HotNormal = new Attribute(Color.BrightYellow, Color.Red),
            HotFocus  = new Attribute(Color.Red, Color.White),
            Disabled  = new Attribute(Color.Gray, Color.Red)
        };
        okBtn.ColorScheme = buttonScheme;
        cancelBtn.ColorScheme = buttonScheme;

        okBtn.Clicked += HandleDeleteTodoList;
        cancelBtn.Clicked += () => TgApplication.RequestStop();

        var dialogLabel = new Label(
            $"Are you sure you want to delete this list?" + "\n" +
            $"This will also delete all todos in the list.")
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Y = 1,
        };

        dialog.Add(dialogLabel);
        dialog.AddButton(okBtn);
        dialog.AddButton(cancelBtn);

        TgApplication.Run(dialog);

        async void HandleDeleteTodoList()
        {
            await _todoRepo.DeleteTodosByTodoListId(listToDelete.Id);
            await _todoListRepo.DeleteTodoListByIdAsync(listToDelete.Id);

            TgApplication.RequestStop();
            ShowListOverview();
        }
    }

    private void CreateNewTodoDialog(Guid listId)
    {
        var dialog = new Dialog("New TODO", 50, 7);
        var input = new TextField("") { X = 1, Y = 1, Width = Dim.Fill() - 2 };
        var okBtn = new Button("Save") { X = 1, Y = 3 };
        
        okBtn.Clicked += HandleCreateNewTodo;
        input.KeyDown += (e) =>
        {
            if (e.KeyEvent.Key != Key.Enter) return;
            HandleCreateNewTodo();
            e.Handled = true; 
        };

        async void HandleCreateNewTodo()
        {
            var text = input.Text?.ToString();

            if (string.IsNullOrWhiteSpace(text)) return;

            var newTodo = new Todo
            {
                Name = text,
                TodoListId = listId
            };
            await _todoRepo.UpsertTodoAsync(newTodo);
            TgApplication.RequestStop();
            ShowTodoListDetail(_navContext.ActiveList!);
        }

        dialog.Add(input, okBtn);
        TgApplication.Run(dialog);
    }
    
    private void RenameTodoDialog(Todo todoToRename)
    {
        var dialog = new Dialog($"Rename '{todoToRename.Name}'", 50, 7);
        var input = new TextField("") { X = 1, Y = 1, Width = Dim.Fill() - 2 };
        var okBtn = new Button("Rename");
        var cancelBtn = new Button("Cancel");
        
        okBtn.Clicked += HandleRenameTodo;
        cancelBtn.Clicked += () => TgApplication.RequestStop();
        input.KeyDown += (e) =>
        {
            if (e.KeyEvent.Key != Key.Enter) return;
            
            e.Handled = true;
            HandleRenameTodo();
            ShowTodoListDetail(_navContext.ActiveList!);
        };
        
        dialog.Add(input);
        dialog.AddButton(okBtn);
        dialog.AddButton(cancelBtn);
        
        TgApplication.Run(dialog);

        async void HandleRenameTodo()
        {
            var text = input.Text?.ToString();

            if (string.IsNullOrWhiteSpace(text)) return;

            todoToRename.Name = text;
            
            await _todoRepo.UpsertTodoAsync(todoToRename);
            
            TgApplication.RequestStop();
            ShowTodoListDetail(_navContext.ActiveList!);
        }
    }
    
    private void DeleteTodoDialog(Todo todoToDelete)
    {
        var dialog = new Dialog($"Delete '{todoToDelete.Name}'?", 50, 7);
        var okBtn = new Button("Delete");
        var cancelBtn = new Button("Cancel");
        
        dialog.ColorScheme = new ColorScheme
        {
            Normal    = new Attribute(Color.White, Color.Red),
            Focus     = new Attribute(Color.White, Color.Red),
            HotNormal = new Attribute(Color.BrightYellow, Color.Red),
            HotFocus  = new Attribute(Color.BrightYellow, Color.Red),
            Disabled  = new Attribute(Color.Gray, Color.Red),
        };
        
        var buttonScheme = new ColorScheme
        {
            Normal    = new Attribute(Color.White, Color.Red),
            Focus     = new Attribute(Color.Red, Color.White),
            HotNormal = new Attribute(Color.BrightYellow, Color.Red),
            HotFocus  = new Attribute(Color.Red, Color.White),
            Disabled  = new Attribute(Color.Gray, Color.Red)
        };
        okBtn.ColorScheme = buttonScheme;
        cancelBtn.ColorScheme = buttonScheme;

        okBtn.Clicked += HandleDeleteTodoList;
        cancelBtn.Clicked += () => TgApplication.RequestStop();

        var dialogLabel = new Label($"Are you sure you want to delete this todo?")
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Y = 1,
        };

        dialog.Add(dialogLabel);
        dialog.AddButton(okBtn);
        dialog.AddButton(cancelBtn);

        TgApplication.Run(dialog);

        async void HandleDeleteTodoList()
        {
            await _todoRepo.DeleteTodoByIdAsync(todoToDelete.Id);

            TgApplication.RequestStop();
            ShowTodoListDetail(_navContext.ActiveList!);
        }
    }
}
