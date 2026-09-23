using Terminal.Gui;
using TgApplication = Terminal.Gui.Application;

namespace Atad.UI.Dialogs;

/// <summary>
/// A small modal dialog with a single text field, used for both the "create"
/// and "rename" flows for lists and todos. The dialog is shown modally;
/// <paramref name="onConfirm"/> is invoked with the trimmed-non-empty text
/// when the user confirms via the button or Enter, and is never invoked for
/// blank input.
/// </summary>
public static class TextInputDialog
{
    public static void Show(
        string title,
        string confirmLabel,
        Action<string> onConfirm,
        bool includeCancelButton = false)
    {
        var dialog = new Dialog(title, 50, 7);
        var input = new TextField("") { X = 1, Y = 1, Width = Dim.Fill() - 2 };
        var okBtn = new Button(confirmLabel);

        void Confirm()
        {
            var text = input.Text?.ToString();
            if (string.IsNullOrWhiteSpace(text)) return;

            TgApplication.RequestStop();
            onConfirm(text);
        }

        okBtn.Clicked += Confirm;
        input.KeyDown += e =>
        {
            if (e.KeyEvent.Key != Key.Enter) return;
            e.Handled = true;
            Confirm();
        };

        if (includeCancelButton)
        {
            var cancelBtn = new Button("Cancel");
            cancelBtn.Clicked += () => TgApplication.RequestStop();

            dialog.Add(input);
            dialog.AddButton(okBtn);
            dialog.AddButton(cancelBtn);
        }
        else
        {
            okBtn.X = 1;
            okBtn.Y = 3;
            dialog.Add(input, okBtn);
        }

        TgApplication.Run(dialog);
    }
}