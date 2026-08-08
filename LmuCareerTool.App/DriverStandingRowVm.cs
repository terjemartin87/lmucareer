using LmuCareerTool.Models;

namespace LmuCareerTool.App;

public class DriverStandingRowVm
{
    public DriverStandingRowVm(int position, DriverStandingEntry entry, int? previousPosition)
    {
        Position = position;
        Trend = previousPosition == null ? "•"
            : previousPosition > position ? "▲"
            : previousPosition < position ? "▼"
            : "•";

        Name = entry.IsPlayer ? $"★ {entry.Name}" : entry.Name + (entry.IsReserve ? " (reserve)" : "");
        Team = entry.TeamName;
        Car = entry.CarType;
        Points = entry.Points;
        Wins = entry.Wins;
        Podiums = entry.Podiums;
        Rounds = entry.RoundsCompleted;
        IsPlayer = entry.IsPlayer;
    }

    public int Position { get; }
    public string Trend { get; }
    public string Name { get; }
    public string Team { get; }
    public string Car { get; }
    public int Points { get; }
    public int Wins { get; }
    public int Podiums { get; }
    public int Rounds { get; }
    public bool IsPlayer { get; }
}

public class ManufacturerStandingRowVm
{
    public ManufacturerStandingRowVm(int position, ManufacturerStandingEntry entry)
    {
        Position = position;
        Manufacturer = entry.Manufacturer;
        Points = entry.Points;
        Wins = entry.Wins;
    }

    public int Position { get; }
    public string Manufacturer { get; }
    public int Points { get; }
    public int Wins { get; }
}
