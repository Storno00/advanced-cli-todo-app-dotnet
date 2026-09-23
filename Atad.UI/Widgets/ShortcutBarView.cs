using Atad.Domain.Models;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace Atad.UI.Widgets;

/// <summary>
/// A single-line bar of keyboard shortcut hints, anchored to the bottom-right
/// corner of whatever it's added to. Add one instance permanently to a
/// window/view and call <see cref="SetShortcuts"/> whenever the active set of
/// shortcuts changes (e.g. after navigating between screens).
/// </summary>
public class ShortcutBarView : View
{
    private static readonly ColorScheme HighlightScheme = new()
    {
        Normal = new Attribute(Color.BrightYellow, Color.Black),
        Focus = new Attribute(Color.BrightYellow, Color.Black)
    };

    private static readonly ColorScheme NormalScheme = new()
    {
        Normal = new Attribute(Color.White, Color.Black),
        Focus = new Attribute(Color.White, Color.Black)
    };

    public void SetShortcuts(IReadOnlyList<KeyboardShortcutViewModel> shortcuts)
    {
        RemoveAll();

        if (shortcuts.Count == 0)
        {
            X = Pos.AnchorEnd(0);
            Width = 0;
            Height = 0;
            return;
        }

        var totalWidth = shortcuts
            .Sum(s => s.KeyboardShortcut.Length + s.Description.Length + 3) + 1;

        X = Pos.AnchorEnd(totalWidth);
        Y = Pos.AnchorEnd(1);
        Width = totalWidth;
        Height = 1;

        Pos currentX = 0;

        for (var i = 0; i < shortcuts.Count; i++)
        {
            var shortcut = shortcuts[i];

            var keyText = $"{shortcut.KeyboardShortcut} ";
            var descText = i == shortcuts.Count - 1
                ? $"{shortcut.Description}"
                : $"{shortcut.Description} | ";

            var keyLabel = new Label(keyText)
            {
                X = currentX,
                Y = 0,
                ColorScheme = HighlightScheme
            };

            var descLabel = new Label(descText)
            {
                X = Pos.Right(keyLabel),
                Y = 0,
                ColorScheme = NormalScheme
            };

            Add(keyLabel, descLabel);
            currentX = Pos.Right(descLabel);
        }
    }
}
