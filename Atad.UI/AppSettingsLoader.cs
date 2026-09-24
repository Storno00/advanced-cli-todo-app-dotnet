using System.Text.Json;

namespace Atad.UI;

internal static class AppSettingsLoader
{
    public static AppSettings Load(string path, string defaultConnectionString, string defaultDatabaseName)
    {
        var defaults = new AppSettings(defaultConnectionString, defaultDatabaseName);

        if (!File.Exists(path))
        {
            Write(path, defaults);
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(path);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var normalized = new AppSettings(
                string.IsNullOrWhiteSpace(loaded?.ConnectionString)
                    ? defaults.ConnectionString
                    : loaded.ConnectionString.Trim(),
                string.IsNullOrWhiteSpace(loaded?.DatabaseName)
                    ? defaults.DatabaseName
                    : loaded.DatabaseName.Trim()
            );

            if (normalized != loaded)
            {
                Write(path, normalized);
            }

            return normalized;
        }
        catch
        {
            Write(path, defaults);
            return defaults;
        }
    }

    private static void Write(string path, AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(path, json);
    }
}
