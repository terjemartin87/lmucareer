using LmuCareerTool.Season;

namespace LmuCareerTool.Models;

public class CareerRaceEntry
{
    public DateTime CompletedAtUtc { get; set; }
    public string TrackVenue { get; set; } = "";
    public string CarType { get; set; } = "";
    public int? RoundNumber { get; set; }
    public int? SeasonNumber { get; set; }

    public int GridPos { get; set; }
    public int FinishPos { get; set; }
    public int TotalParticipants { get; set; }
    public string FinishStatus { get; set; } = "";
    public int IncidentCount { get; set; }
    public int XpEarned { get; set; }
    public int PointsEarned { get; set; }

    public double? PracticeBestLap { get; set; }
    public double? PracticeS1 { get; set; }
    public double? PracticeS2 { get; set; }
    public double? PracticeS3 { get; set; }

    public int? QualifyingPos { get; set; }
    public double? QualifyingBestLap { get; set; }
    public double? QualifyingS1 { get; set; }
    public double? QualifyingS2 { get; set; }
    public double? QualifyingS3 { get; set; }

    public double? RaceBestLap { get; set; }
    public double? RaceS1 { get; set; }
    public double? RaceS2 { get; set; }
    public double? RaceS3 { get; set; }
}

public class CareerProfile
{
    public string DriverName { get; set; } = "";
    public string CurrentClass { get; set; } = "GT3";
    public string CurrentSeries { get; set; } = "WEC"; // eller "ELMS"
    public int TotalXp { get; set; }
    public int Level { get; set; } = 1;

    public int DriverRating { get; set; } = 50; // 0-100, styrer hvilke merker som vil ha deg
    public int Credits { get; set; } = 0;       // "penger" - kan brukes til å kjøpe seg inn hos et merke
    public string? CurrentManufacturer { get; set; }

    /// <summary>Hvilke merker som er opplåst, per klasse (nøkkel = klassenavn, f.eks. "GT3").</summary>
    public Dictionary<string, List<string>> UnlockedManufacturers { get; set; } = new();

    public List<string> UnlockedClasses { get; set; } = new() { "GT3" };
    public SeasonModel? CurrentSeason { get; set; }
    public List<SeasonModel> SeasonHistory { get; set; } = new();

    public List<CareerRaceEntry> RaceHistory { get; set; } = new();
}
