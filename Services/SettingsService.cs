using System.IO;
using System.Text.Json;
using PrintBatcher.Models;

namespace PrintBatcher.Services;

public class PersistedState
{
    public AppSettings Settings { get; set; } = new();
    public string? LastPrinter { get; set; }
    public List<string> LastSessionFiles { get; set; } = new();
}

public static class SettingsService
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PrintBatcher", "settings.json");

    public static PersistedState Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var state = JsonSerializer.Deserialize<PersistedState>(json);
                if (state != null) return state;
            }
        }
        catch
        {
            // Corrupt or unreadable settings file: fall back to defaults.
        }

        return new PersistedState();
    }

    public static void Save(PersistedState state)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch
        {
            // Best effort: failing to persist settings should not crash the app.
        }
    }
}
