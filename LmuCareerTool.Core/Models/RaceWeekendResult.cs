namespace LmuCareerTool.Models;

/// <summary>
/// Samlet resultat for spilleren for én løpshelg (bane), satt sammen fra
/// separate Practice/Qualifying/Race-XML-filer.
/// </summary>
public class RaceWeekendResult
{
    public string TrackVenue { get; set; } = "";
    public DateTime CompletedAtUtc { get; set; }

    public DriverResult? RaceResult { get; set; }
    public DriverResult? QualifyingResult { get; set; }
    public DriverResult? BestPracticeResult { get; set; }

    public int TotalParticipants { get; set; }

    public int PositionsGainedFromGrid =>
        RaceResult == null ? 0 : RaceResult.GridPos - RaceResult.Position;

    public bool Finished =>
        RaceResult?.FinishStatus.Equals("Finished Normally", StringComparison.OrdinalIgnoreCase) == true;
}
