using LmuCareerTool.Season;

namespace LmuCareerTool.App;

public class SeasonEventRow
{
    public SeasonEventRow(SeasonEvent ev, bool isNext)
    {
        Round = ev.RoundNumber;
        Track = ev.TrackVenue;
        Format = ev.Format.ToString();
        Weather = ev.AssignedWeather;
        Car = ev.AssignedCar;

        Status = ev.Completed ? "Fullført"
            : isNext ? "Neste"
            : "Ikke kjørt";

        Result = ev.Completed
            ? $"P{ev.FinishPos} (+{ev.XpEarned} XP)"
            : "-";

        Points = ev.Completed ? (ev.PointsEarned ?? 0).ToString() : "-";
    }

    public int Round { get; }
    public string Track { get; }
    public string Format { get; }
    public string Weather { get; }
    public string Car { get; }
    public string Status { get; }
    public string Result { get; }
    public string Points { get; }
}
