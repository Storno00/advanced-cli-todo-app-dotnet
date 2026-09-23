using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;
using TgApplication = Terminal.Gui.Application;

namespace Atad.UI.Dialogs;

/// <summary>
/// A modal, red-themed "are you sure?" confirmation dialog used for
/// destructive actions (deleting lists and todos).
/// </summary>
public static class ConfirmationDialog
{
    private static readonly ColorScheme DialogScheme = new()
    {
        Normal = new Attribute(Color.White, Color.Red),
        Focus = new Attribute(Color.White, Color.Red),
        HotNormal = new Attribute(Color.BrightYellow, Color.Red),
        HotFocus = new Attribute(Color.BrightYellow, Color.Red),
        Disabled = new Attribute(Color.Gray, Color.Red)
    };

    private static readonly ColorScheme ButtonScheme = new()
    {
        Normal = new Attribute(Color.White, Color.Red),
        Focus = new Attribute(Color.Red, Color.White),
        HotNormal = new Attribute(Color.BrightYellow, Color.Red),
        HotFocus = new Attribute(Color.Red, Color.White),
        Disabled = new Attribute(Color.Gray, Color.Red)
    };

    public static void Show(string title, string message, Action onConfirm)
    {
        var dialog = new Dialog(title, 50, 7)
        {
            ColorScheme = DialogScheme
        };

        var okBtn = new Button("Delete") { ColorScheme = ButtonScheme };
        var cancelBtn = new Button("Cancel") { ColorScheme = ButtonScheme };

        okBtn.Clicked += () =>
        {
            TgApplication.RequestStop();
            onConfirm();
        };
        cancelBtn.Clicked += () => TgApplication.RequestStop();

        var messageLabel = new Label(message)
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Y = 1
        };

        dialog.Add(messageLabel);
        dialog.AddButton(okBtn);
        dialog.AddButton(cancelBtn);

        TgApplication.Run(dialog);
    }
}