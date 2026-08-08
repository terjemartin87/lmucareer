namespace LmuCareerTool.Models;

public enum SessionType
{
    Practice,
    Qualifying,
    Race,
    Unknown
}

/// <summary>
/// Ett løp/sesjon på et rundetidsnivå (én <Lap> i XML-en).
/// </summary>
public class LapResult
{
    public int LapNumber { get; set; }
    public int Position { get; set; }
    public double ElapsedTime { get; set; }
    public double? Sector1 { get; set; }
    public double? Sector2 { get; set; }
    public double? Sector3 { get; set; }
    public double? TopSpeed { get; set; }
    public bool PitThisLap { get; set; }
    public double? LapTime { get; set; } // null hvis "--.----" (runde ikke fullført rent)
}

/// <summary>
/// Én sjåførs resultat i en gitt sesjon (Practice/Qualifying/Race).
/// </summary>
public class DriverResult
{
    public string Name { get; set; } = "";
    public string TeamName { get; set; } = "";
    public string CarClass { get; set; } = "";
    public string CarType { get; set; } = "";
    public int CarNumber { get; set; }
    public bool IsPlayer { get; set; }

    public int GridPos { get; set; }
    public int Position { get; set; }
    public int ClassGridPos { get; set; }
    public int ClassPosition { get; set; }

    public double? BestLapTime { get; set; }
    public double? FinishTime { get; set; }
    public int Laps { get; set; }
    public int Pitstops { get; set; }
    public string FinishStatus { get; set; } = "";
    public string? DnfReason { get; set; }

    public List<LapResult> LapData { get; set; } = new();

    /// <summary>Beste (laveste) sektortider på tvers av alle registrerte runder.</summary>
    public double? BestSector1 => LapData.Select(l => l.Sector1).Where(v => v is > 0).Min();
    public double? BestSector2 => LapData.Select(l => l.Sector2).Where(v => v is > 0).Min();
    public double? BestSector3 => LapData.Select(l => l.Sector3).Where(v => v is > 0).Min();

    // Hendelser fra <Stream> som nevner denne sjåføren (fylles inn separat)
    public int IncidentCount { get; set; }
    public int PenaltyCount { get; set; }
    public int TrackLimitWarnings { get; set; }

    /// <summary>
    /// Sjekker om et LMU-visningsnavn tilhører denne sjåføren.
    /// LMU legger til "#1234" som suffiks når to spillere har samme navn på en server,
    /// så vi matcher på at navnet starter likt i stedet for eksakt likhet.
    /// </summary>
    public bool MatchesPlayerName(string playerName)
    {
        var normalized = Name.Split('#')[0].Trim();
        return string.Equals(normalized, playerName.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Hele innholdet i én resultat-XML-fil (én sesjon: enten Practice, Qualifying eller Race).
/// </summary>
public class SessionResult
{
    public string SourceFile { get; set; } = "";
    public SessionType SessionType { get; set; }
    public int SessionNumber { get; set; } = 1;

    public string TrackVenue { get; set; } = "";
    public string TrackCourse { get; set; } = "";
    public double TrackLength { get; set; }

    public string SettingMode { get; set; } = ""; // "Multiplayer" / "Offline" osv.
    public DateTime SessionTimeUtc { get; set; }
    public int RaceTimeMinutes { get; set; }
    public int RaceLaps { get; set; }

    public string VehiclesAllowed { get; set; } = "";

    public List<DriverResult> Drivers { get; set; } = new();

    public DriverResult? FindPlayer(string playerName) =>
        Drivers.FirstOrDefault(d => d.MatchesPlayerName(playerName));
}
