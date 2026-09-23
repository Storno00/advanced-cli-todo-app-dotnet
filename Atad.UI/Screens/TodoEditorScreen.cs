using Atad.Application.Interfaces;
using Atad.Domain.Models;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace Atad.UI.Screens;

/// <summary>
/// Renders the free-text description editor for a single <see cref="Todo"/>.
/// Raises <see cref="Closed"/> once the user leaves the editor, whether they
/// saved (Esc) or discarded (Ctrl+Esc) — the caller decides where to
/// navigate next.
/// </summary>
public class TodoEditorScreen
{
    private readonly ITodoRepository _todoRepo;

    public event Action? Closed;

    public TodoEditorScreen(ITodoRepository todoRepo)
    {
        _todoRepo = todoRepo;
    }

    public void Render(View container, Todo todo)
    {
        container.RemoveAll();

        var frame = new FrameView("Description")
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ColorScheme = new ColorScheme
            {
                Focus = Attribute.Make(Color.White, Color.Black)
            }
        };

        var textView = new TextView
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Text = todo.LongDescription,
            ColorScheme = new ColorScheme
            {
                Focus = new Attribute(Color.White, Color.DarkGray)
            }
        };

        textView.KeyDown += async args =>
        {
            // Discard and exit editor (F12)
            if (args.KeyEvent.Key == Key.F12)
            {
                args.Handled = true;
                Closed?.Invoke();
                return;
            }

            // Save and exit editor (Esc)
            if (args.KeyEvent.Key == Key.Esc)
            {
                args.Handled = true;
                todo.LongDescription = textView.Text.ToString() ?? string.Empty;
                await _todoRepo.UpsertTodoAsync(todo);
                Closed?.Invoke();
            }
        };

        frame.Add(textView);
        container.Add(frame);
        textView.SetFocus();
    }
}
