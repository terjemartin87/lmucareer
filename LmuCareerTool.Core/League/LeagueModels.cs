using LmuCareerTool.Models;
using LmuCareerTool.Season;

namespace LmuCareerTool.League;

/// <summary>
/// En manuell straff gitt av verten etter et løp - poengtrekk og/eller diskvalifisering,
/// med en begrunnelse. Ligamodus har ingen automatisk straffe-deteksjon (incidents/track
/// limits leses fra resultatfilen til informasjon, men fører aldri til poengtrekk av seg
/// selv) - verten avgjør alt manuelt, akkurat slik en ekte liga-steward ville gjort det.
/// </summary>
public class LeaguePenalty
{
    public string DriverName { get; set; } = "";
    public int PointsDeducted { get; set; }
    public bool Disqualified { get; set; }
    public string Reason { get; set; } = "";
    public DateTime AppliedAtUtc { get; set; }
}

public class LeagueRound
{
    public int RoundNumber { get; set; }
    public string TrackVenue { get; set; } = "";
    public RaceFormat Format { get; set; }

    public bool Completed { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>Hele feltets resultat for denne runden - grunnlaget for standings. Hver
    /// sjåfør kan ha en annen bil enn de andre (ligamedlemmer velger selv), i motsetning til
    /// karrieremodus der hele sesongen kjøres med én fast bil.</summary>
    public List<FieldResultEntry> FieldResults { get; set; } = new();

    public List<LeaguePenalty> Penalties { get; set; } = new();
}

public class LeagueSeason
{
    public int SeasonNumber { get; set; }
    public string CarClass { get; set; } = "";
    public List<LeagueRound> Rounds { get; set; } = new();

    /// <summary>Navnene (normalisert) på feltet slik det så ut i den første fullførte runden -
    /// samme prinsipp som karrieremodusens mesterskap, se Championship/FieldRoster.</summary>
    public List<string> LockedRosterNames { get; set; } = new();

    public bool IsComplete => Rounds.Count > 0 && Rounds.All(r => r.Completed);
    public LeagueRound? NextRound => Rounds.FirstOrDefault(r => !r.Completed);
    public int CompletedCount => Rounds.Count(r => r.Completed);
}

/// <summary>
/// Rot-objektet for én liga - helt separat fra CareerProfile. Ingen XP, Rating, Credits eller
/// kontrakter - kun et poengsystem for ekte sjåfører i et hostet felt.
/// </summary>
public class LeagueProfile
{
    public string LeagueName { get; set; } = "";
    public string HostDisplayName { get; set; } = "";

    public LeagueSeason? CurrentSeason { get; set; }
    public List<LeagueSeason> SeasonHistory { get; set; } = new();
}
