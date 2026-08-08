using LmuCareerTool.Models;

namespace LmuCareerTool.App;

public class RoundReportRowVm
{
    public RoundReportRowVm(SeasonReportRound round)
    {
        Round = round.RoundNumber;
        Track = round.TrackVenue;
        Result = round.FinishPos is > 0 ? $"P{round.FinishPos}" : "-";
        Points = round.PointsEarned;
        TablePosition = round.ChampionshipPositionAfter > 0 ? $"P{round.ChampionshipPositionAfter}" : "-";
    }

    public int Round { get; }
    public string Track { get; }
    public string Result { get; }
    public int Points { get; }
    public string TablePosition { get; }
}

public class TrackRecordRowVm
{
    public TrackRecordRowVm(PersonalTrackRecord record)
    {
        Track = record.TrackVenue;
        BestLap = FormatTime(record.BestLapSeconds);
        Badge = record.IsCareerBest ? "🏆 Karriererekord" : "";
    }

    public string Track { get; }
    public string BestLap { get; }
    public string Badge { get; }

    private static string FormatTime(double seconds)
    {
        if (seconds <= 0) return "-";
        var ts = TimeSpan.FromSeconds(seconds);
        return $"{(int)ts.TotalMinutes}:{ts.Seconds:00}.{ts.Milliseconds:000}";
    }
}

public class StatTileVm
{
    public StatTileVm(string label, string value)
    {
        Label = label;
        Value = value;
    }

    public string Label { get; }
    public string Value { get; }
}

public class AwardRowVm
{
    public AwardRowVm(SeasonAward award)
    {
        Title = award.Title;
        Description = award.Description;
    }

    public string Title { get; }
    public string Description { get; }
}
