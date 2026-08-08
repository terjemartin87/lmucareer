using LmuCareerTool.Season;

namespace LmuCareerTool.Validation;

/// <summary>Bygger en kopierbar "oppskrift" for hva du skal sette opp i LMU for en sesongrunde.</summary>
public static class PreRaceChecklist
{
    public static string BuildRecipeText(SeasonEvent ev, string carClass, string? manufacturer)
    {
        var lines = new List<string>
        {
            $"Runde {ev.RoundNumber}: {ev.TrackVenue}",
            $"Klasse: {carClass}{(manufacturer != null ? $" ({manufacturer})" : "")}",
            $"Bil: {ev.AssignedCar}",
            $"Format: {ev.Format} (~{ev.SuggestedRaceMinutes} min race)",
            $"Vær: {ev.AssignedWeather}",
        };
        return string.Join(Environment.NewLine, lines);
    }
}
