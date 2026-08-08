using LmuCareerTool.Models;

namespace LmuCareerTool.App;

public class RaceHistoryRow
{
    public RaceHistoryRow(CareerRaceEntry entry)
    {
        Entry = entry;
        Summary = $"{entry.CompletedAtUtc.ToLocalTime():dd.MM HH:mm}  {entry.TrackVenue}";
        Detail = $"P{entry.FinishPos} av {entry.TotalParticipants} (start P{entry.GridPos})  ·  {entry.FinishStatus}  ·  +{entry.XpEarned} XP  ·  +{entry.PointsEarned} poeng";
    }

    public CareerRaceEntry Entry { get; }
    public string Summary { get; }
    public string Detail { get; }
}
