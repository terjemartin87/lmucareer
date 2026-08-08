namespace LmuCareerTool.Models;

public class DriverStandingEntry
{
    public string Name { get; set; } = "";
    public string TeamName { get; set; } = "";
    public string CarType { get; set; } = "";
    public bool IsPlayer { get; set; }
    public bool IsReserve { get; set; }

    public int Points { get; set; }
    public int Wins { get; set; }
    public int Podiums { get; set; }
    public int RoundsCompleted { get; set; }
    public int? BestFinish { get; set; }
}

public class ManufacturerStandingEntry
{
    public string Manufacturer { get; set; } = "";
    public int Points { get; set; }
    public int Wins { get; set; }
}
