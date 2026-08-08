using LmuCareerTool.Models;

namespace LmuCareerTool.Season;

public enum RaceFormat
{
    Sprint,
    Endurance
}

public class SeasonEvent
{
    public int RoundNumber { get; set; }
    public string TrackVenue { get; set; } = "";
    public RaceFormat Format { get; set; }
    public string AssignedCar { get; set; } = "";
    public string AssignedWeather { get; set; } = "";

    public bool Completed { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int? FinishPos { get; set; }
    public int? XpEarned { get; set; }
    public int? PointsEarned { get; set; }
    public bool? CarMatched { get; set; }

    /// <summary>Hele feltets resultat for denne runden (Fase 2: mesterskapstabell) - tomt til runden er fullført.</summary>
    public List<FieldResultEntry> FieldResults { get; set; } = new();

    /// <summary>Anbefalt varighet, kun til visning/planlegging - du setter dette opp selv i LMU.</summary>
    public int SuggestedRaceMinutes => Format == RaceFormat.Endurance ? 60 : 25;
}

public class SeasonModel
{
    public int SeasonNumber { get; set; }
    public string CarClass { get; set; } = "";
    public List<SeasonEvent> Events { get; set; } = new();

    /// <summary>Navnene (normalisert, uten "#id"-suffiks) på feltet slik det så ut i den første
    /// fullførte runden. Låses én gang per sesong, slik at samme AI-motstandere følges gjennom
    /// hele sesongen for mesterskapstabellen. Nye navn som dukker opp senere telles som reserver.</summary>
    public List<string> LockedRosterNames { get; set; } = new();

    public bool IsComplete => Events.Count > 0 && Events.All(e => e.Completed);
    public SeasonEvent? NextEvent => Events.FirstOrDefault(e => !e.Completed);
    public int CompletedCount => Events.Count(e => e.Completed);
    public int TotalPoints => Events.Sum(e => e.PointsEarned ?? 0);
    public int TotalXp => Events.Sum(e => e.XpEarned ?? 0);
    public int BestFinish => Events.Where(e => e.FinishPos.HasValue).Select(e => e.FinishPos!.Value).DefaultIfEmpty(0).Min();
}
