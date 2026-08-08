using System.Text.Json;

namespace LmuCareerTool.Settings;

public class AppSettingsStore
{
    private readonly string _filePath;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public AppSettingsStore(string filePath)
    {
        _filePath = filePath;
    }

    public AppSettings LoadOrDefault()
    {
        if (!File.Exists(_filePath)) return new AppSettings();
        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        File.WriteAllText(_filePath, JsonSerializer.Serialize(settings, JsonOptions));
    }
}
