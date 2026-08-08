using System.Text.Json;
using System.Text.Json.Serialization;
using LmuCareerTool.Models;

namespace LmuCareerTool.Career;

public class CareerStore
{
    private readonly string _filePath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public CareerStore(string filePath)
    {
        _filePath = filePath;
    }

    public CareerProfile LoadOrCreate(string driverName)
    {
        if (File.Exists(_filePath))
        {
            var json = File.ReadAllText(_filePath);
            var loaded = JsonSerializer.Deserialize<CareerProfile>(json);
            if (loaded != null) return loaded;
        }

        var fresh = new CareerProfile { DriverName = driverName };
        Save(fresh);
        return fresh;
    }

    public void Save(CareerProfile profile)
    {
        var json = JsonSerializer.Serialize(profile, JsonOptions);
        File.WriteAllText(_filePath, json);
    }
}
