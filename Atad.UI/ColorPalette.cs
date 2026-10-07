using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace Atad.UI;

public static class ColorPalette
{
    private static readonly Dictionary<string, ColorScheme> Schemes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["green"] = Build(Color.Green),
        ["red"] = Build(Color.BrightRed),
        ["blue"] = Build(Color.BrightBlue),
        ["pink"] = Build(Color.BrightMagenta),
        ["yellow"] = Build(Color.BrightYellow),
        ["orange"] = Build(Color.Brown)
    };

    public static readonly IReadOnlyList<(string Label, string? Value)> Options =
    [
        ("Not colorized", null),
        ("Green", "green"),
        ("Red", "red"),
        ("Blue", "blue"),
        ("Pink", "pink"),
        ("Yellow", "yellow"),
        ("Orange", "orange")
    ];

    public static bool TryGetColorScheme(string? colorName, out ColorScheme scheme)
    {
        if (!string.IsNullOrWhiteSpace(colorName) && Schemes.TryGetValue(colorName, out scheme!))
        {
            return true;
        }

        scheme = null!;
        return false;
    }

    public static bool TryGetAttribute(string? colorName, bool isFocused, out Attribute attribute)
    {
        if (TryGetColorScheme(colorName, out var scheme))
        {
            attribute = isFocused ? scheme.Focus : scheme.Normal;
            return true;
        }

        attribute = default;
        return false;
    }

    private static ColorScheme Build(Color foreground)
    {
        var normal = Attribute.Make(foreground, Color.Black);
        var focused = Attribute.Make(Color.Black, foreground);

        return new ColorScheme
        {
            Normal = normal,
            Focus = focused,
            HotNormal = normal,
            HotFocus = focused,
            Disabled = Attribute.Make(Color.Gray, Color.Black)
        };
    }
}
