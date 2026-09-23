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
            new KeyboardShortcutViewModel("[F1]", "Create new"),
            new KeyboardShortcutViewModel("[F3]", "Rename"),
            new KeyboardShortcutViewModel("[Del]", "Remove"),
        ],

        ApplicationState.TodoListDetail =>
        [
            new KeyboardShortcutViewModel("[F1]", "Create new"),
            new KeyboardShortcutViewModel("[F2]", "Create new sub"),
            new KeyboardShortcutViewModel("[F3]", "Rename"),
            new KeyboardShortcutViewModel("[Space]", "Toggle done"),
            new KeyboardShortcutViewModel("[Del]", "Remove"),
            new KeyboardShortcutViewModel("[Esc]", "Back")
        ],

        // ApplicationState.TodoEditor and any future default
        _ =>
        [
            new KeyboardShortcutViewModel("[Esc]", "Save and back"),
            new KeyboardShortcutViewModel("[Ctrl+Esc]", "Exit without save")
        ]
    };
}