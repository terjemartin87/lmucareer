using LmuCareerTool.Models;

namespace LmuCareerTool.App;

/// <summary>Én rad per bane - Practice og Qualifying for samme bane slås sammen til én rad,
/// siden de fjernes sammen (ClearPendingSession fjerner begge for banen).</summary>
public class PendingSessionRowVm
{
    public PendingSessionRowVm(string trackVenue, List<SessionResult> sessions)
    {
        TrackVenue = trackVenue;
        var types = sessions.Select(s => s.SessionType == SessionType.Practice ? "Practice" : "Qualifying");
        var newest = sessions.Max(s => s.SessionTimeUtc);
        Summary = $"{trackVenue} · {string.Join(" + ", types)} · {newest.ToLocalTime():dd.MM.yyyy HH:mm}";
    }

    public string TrackVenue { get; }
    public string Summary { get; }
}
