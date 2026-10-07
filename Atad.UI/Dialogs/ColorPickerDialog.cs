using Terminal.Gui;
using TgApplication = Terminal.Gui.Application;

namespace Atad.UI.Dialogs;

public static class ColorPickerDialog
{
    public static void Show(string title, string? currentColor, Action<string?> onConfirm)
    {
        var dialog = new Dialog(title, 38, 16);

        var options = ColorPalette.Options;
        var items = options.Select(option => option.Label).ToList();

        var listView = new ListView(items)
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill() - 2,
            Height = 9,
            CanFocus = true
        };

        var selectedIndex = 0;
        if (!string.IsNullOrWhiteSpace(currentColor))
        {
            var existingIndex = options
                .Select((option, index) => new { option, index })
                .FirstOrDefault(x => string.Equals(x.option.Value, currentColor, StringComparison.OrdinalIgnoreCase));

            if (existingIndex is not null)
            {
                selectedIndex = existingIndex.index;
            }
        }

        listView.SelectedItem = selectedIndex;

        void Confirm()
        {
            var index = Math.Clamp(listView.SelectedItem, 0, options.Count - 1);
            TgApplication.RequestStop();
            onConfirm(options[index].Value);
        }

        var saveButton = new Button("Save");
        var cancelButton = new Button("Cancel");

        saveButton.Clicked += Confirm;
        cancelButton.Clicked += () => TgApplication.RequestStop();

        dialog.KeyDown += e =>
        {
            if (e.KeyEvent.Key == Key.Esc)
            {
                e.Handled = true;
                TgApplication.RequestStop();
                return;
            }

            if (e.KeyEvent.Key == Key.Enter)
            {
                e.Handled = true;
                Confirm();
            }
        };

        dialog.Add(listView);
        dialog.AddButton(saveButton);
        dialog.AddButton(cancelButton);

        TgApplication.Run(dialog);
    }
}
