using System.Text.Json;
using System.Text.Json.Serialization;
using LmuCareerTool.Models;

namespace LmuCareerTool.Parsing;

public class PendingWeekendState
{
    public Dictionary<string, SessionResult> Practice { get; set; } = new();
    public Dictionary<string, SessionResult> Qualifying { get; set; } = new();
}

/// <summary>
/// WeekendGrouper holder Practice/Qualifying-resultater i minnet til Race-filen kommer inn.
/// Uten dette mistes den kvalifiseringsdataen hvis appen restartes midt i en løpshelg - denne
/// klassen lagrer og gjenoppretter den tilstanden til/fra disk.
/// </summary>
public class PendingWeekendStore
{
    private readonly string _filePath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public PendingWeekendStore(string filePath)
    {
        _filePath = filePath;
    }

    public PendingWeekendState LoadOrEmpty()
    {
        if (!File.Exists(_filePath)) return new PendingWeekendState();
        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<PendingWeekendState>(json, JsonOptions) ?? new PendingWeekendState();
        }
        catch
        {
            return new PendingWeekendState();
        }
    }

    public void Save(PendingWeekendState state)
    {
        File.WriteAllText(_filePath, JsonSerializer.Serialize(state, JsonOptions));
    }
}
