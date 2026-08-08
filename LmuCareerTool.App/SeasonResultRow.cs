using LmuCareerTool.Season;

namespace LmuCareerTool.App;

public class SeasonResultRow
{
    public SeasonResultRow(SeasonEvent ev)
    {
        Round = ev.RoundNumber;
        Track = ev.TrackVenue;
        Result = ev.Completed ? $"P{ev.FinishPos}" : "-";
        Points = ev.PointsEarned?.ToString() ?? "0";
    }

    public int Round { get; }
    public string Track { get; }
    public string Result { get; }
    public string Points { get; }
}
