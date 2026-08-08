namespace LmuCareerTool.Models;

public class SeasonReportRound
{
    public int RoundNumber { get; set; }
    public string TrackVenue { get; set; } = "";
    public int? FinishPos { get; set; }
    public int PointsEarned { get; set; }

    /// <summary>Din posisjon i førermesterskapet etter denne runden. 0 = ukjent.</summary>
    public int ChampionshipPositionAfter { get; set; }
}

public class PersonalTrackRecord
{
    public string TrackVenue { get; set; } = "";
    public double BestLapSeconds { get; set; }

    /// <summary>True hvis dette er din raskeste runde på banen noensinne, ikke bare denne sesongen.</summary>
    public bool IsCareerBest { get; set; }
}

public class SeasonAward
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
}

/// <summary>
/// Full sesongrapport bygget av SeasonSummaryBuilder når en sesong er fullført. Merk at
/// "raskeste runde" og "pole position" her kun måles mot din EGEN historikk - resultatfilene
/// gir oss ikke rundetider for AI-motstandernes fulle felt, kun sluttplassering, så et
/// "raskeste runde i feltet"-sammenligning er ikke mulig med dagens data.
/// </summary>
public class SeasonReport
{
    public int SeasonNumber { get; set; }
    public string CarClass { get; set; } = "";
    public string? Manufacturer { get; set; }

    public int ChampionshipPosition { get; set; } // 0 = ikke funnet i tabellen
    public int TotalDrivers { get; set; }
    public int Points { get; set; }
    public int Wins { get; set; }
    public int Podiums { get; set; }
    public int PolePositions { get; set; }
    public int Dnfs { get; set; }
    public double AverageFinish { get; set; }
    public double TotalDistanceKm { get; set; }
    public int RoundsCompleted { get; set; }

    public List<SeasonReportRound> Rounds { get; set; } = new();
    public List<DriverStandingEntry> DriverStandings { get; set; } = new();
    public List<ManufacturerStandingEntry> ManufacturerStandings { get; set; } = new();
    public List<PersonalTrackRecord> TrackRecords { get; set; } = new();
    public List<SeasonAward> Awards { get; set; } = new();

    public string ContractSummary { get; set; } = "";
}
