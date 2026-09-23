using Atad.Domain.Models;

namespace Atad.UI.Navigation;

/// <summary>
/// Maps each <see cref="ApplicationState"/> to the keyboard shortcuts that
/// should be shown for it in the <see cref="Widgets.ShortcutBarView"/>.
/// </summary>
public static class KeyboardShortcutProvider
{
    public static List<KeyboardShortcutViewModel> For(ApplicationState state) => state switch
    {
        ApplicationState.ListOverview =>
        [
            new KeyboardShortcutViewModel("[Ctrl+Enter]", "Create new"),
            new KeyboardShortcutViewModel("[F2]", "Edit title"),
            new KeyboardShortcutViewModel("[Del]", "Remove"),
            new KeyboardShortcutViewModel("[Esc]", "Exit")
        ],

        ApplicationState.TodoListDetail =>
        [
            new KeyboardShortcutViewModel("[Ctrl+Enter]", "Create new"),
            new KeyboardShortcutViewModel("[F2]", "Edit title"),
            new KeyboardShortcutViewModel("[Space]", "Toggle done"),
            new KeyboardShortcutViewModel("[Del]", "Remove"),
            new KeyboardShortcutViewModel("[Esc]", "Back")
        ],

        // ApplicationState.TodoEditor and any future default
        _ =>
        [
            new KeyboardShortcutViewModel("[Esc]", "Save"),
            new KeyboardShortcutViewModel("[Ctrl+Esc]", "Exit")
        ]
    };
}