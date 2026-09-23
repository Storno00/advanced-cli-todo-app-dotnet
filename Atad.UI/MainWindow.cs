using Atad.Application.Interfaces;
using Atad.Domain.Models;
using Atad.UI.Navigation;
using Atad.UI.Screens;
using Atad.UI.Widgets;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace Atad.UI;

/// <summary>
/// Application shell: owns the breadcrumb and shortcut-bar chrome, and
/// switches the content area between the three screens based on
/// <see cref="NavigationContext"/>. All the actual screen rendering and
/// dialog logic lives in <see cref="Screens"/>.
/// </summary>
public class MainWindow : Window
{
    private readonly NavigationContext _navContext;

    private readonly View _contentArea;
    private readonly Label _breadcrumbLabel;
    private readonly ShortcutBarView _shortcutBar;

    private readonly ListOverviewScreen _listOverviewScreen;
    private readonly TodoListDetailScreen _todoListDetailScreen;
    private readonly TodoEditorScreen _todoEditorScreen;

    public MainWindow(
        ITodoListRepository todoListRepo,
        ITodoRepository todoRepo,
        ITodoListStatService todoStatService,
        NavigationContext navContext)
    {
        _navContext = navContext;

        Title = "CLI TODO App";

        _breadcrumbLabel = new Label
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 1,
            ColorScheme = new ColorScheme
            {
                Normal = Attribute.Make(Color.White, Color.DarkGray)
            }
        };

        _contentArea = new View
        {
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1
        };

        _shortcutBar = new ShortcutBarView();

        Add(_breadcrumbLabel, _contentArea, _shortcutBar);

        _listOverviewScreen = new ListOverviewScreen(todoListRepo, todoRepo, todoStatService);
        _listOverviewScreen.ListOpened += ShowTodoListDetail;

        _todoListDetailScreen = new TodoListDetailScreen(todoRepo);
        _todoListDetailScreen.TodoOpened += ShowTodoEditor;

        _todoEditorScreen = new TodoEditorScreen(todoRepo);
        _todoEditorScreen.Closed += () => ShowTodoListDetail(_navContext.ActiveList!);

        KeyDown += args =>
        {
            if (args.KeyEvent.Key != Key.Esc) return;

            if (HandleBackNavigation())
            {
                args.Handled = true;
            }
        };

        ShowListOverview();
    }

    private void UpdateChrome()
    {
        _breadcrumbLabel.Text = _navContext.CurrentView switch
        {
            ApplicationState.ListOverview => "Todo Lists",
            ApplicationState.TodoListDetail => $"Todo Lists > {_navContext.ActiveList?.Name}",
            ApplicationState.TodoEditor => $"Todo Lists > {_navContext.ActiveList?.Name} > {_navContext.ActiveTodo?.Name}",
            _ => ""
        };

        _shortcutBar.SetShortcuts(KeyboardShortcutProvider.For(_navContext.CurrentView));
    }

    private void ShowListOverview()
    {
        _navContext.NavigateToOverview();
        UpdateChrome();
        _listOverviewScreen.Render(_contentArea);
    }

    private void ShowTodoListDetail(TodoList list)
    {
        _navContext.NavigateToDetail(list);
        UpdateChrome();
        _todoListDetailScreen.Render(_contentArea, list);
    }

    private void ShowTodoEditor(Todo todo)
    {
        _navContext.NavigateToEditor(todo);
        UpdateChrome();
        _todoEditorScreen.Render(_contentArea, todo);
    }

    private bool HandleBackNavigation()
    {
        if (_navContext.CurrentView != ApplicationState.TodoListDetail) return false;

        ShowListOverview();
        return true;
    }
}
