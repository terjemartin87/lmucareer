using System.Text.Json;
using System.Text.Json.Serialization;

namespace LmuCareerTool.League;

public class LeagueStore
{
    private readonly string _filePath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public LeagueStore(string filePath)
    {
        _filePath = filePath;
    }

    public LeagueProfile? Load()
    {
        if (!File.Exists(_filePath)) return null;
        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<LeagueProfile>(json, JsonOptions);
    }

    public LeagueProfile LoadOrCreate(string leagueName, string hostDisplayName)
    {
        var loaded = Load();
        if (loaded != null) return loaded;

        var fresh = new LeagueProfile { LeagueName = leagueName, HostDisplayName = hostDisplayName };
        Save(fresh);
        return fresh;
    }

    public void Save(LeagueProfile league)
    {
        var json = JsonSerializer.Serialize(league, JsonOptions);
        File.WriteAllText(_filePath, json);
    }
}
