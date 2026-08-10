namespace LmuCareerTool.Content;

public class ManufacturerDefinition
{
    public string Name { get; set; } = "";
    public List<string> Cars { get; set; } = new();
    public bool StartUnlocked { get; set; }
    public int RatingRequired { get; set; }
    public int UnlockCost { get; set; }
}

public class ClassDefinition
{
    public string Name { get; set; } = "";
    public int Order { get; set; }
    public int XpThreshold { get; set; }
    public List<string> Cars { get; set; } = new();
    public List<ManufacturerDefinition> Manufacturers { get; set; } = new();
}

public enum AchievementMetric
{
    TotalWins,
    TotalPodiums,
    TotalPoles,
    TotalRacesFinished,
    TotalCleanRaces,
    TotalXp,
    DriverRating,
    SafetyRating,
    SeasonsCompleted,
    ClassesUnlocked,
    TotalDistanceKm,
    SpecialEventsCompleted,

    /// <summary>Krever at et bestemt spesialarrangement (RequiredEventId) er fullført minst én
    /// gang - Threshold brukes ikke for denne metrikken.</summary>
    SpecificSpecialEvent,
}

public class AchievementDefinition
{
    /// <summary>Stabil nøkkel lagret på karrieren - ikke endre etter at spillere kan ha låst den opp.</summary>
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "🏆";
    public AchievementMetric Metric { get; set; }
    public int Threshold { get; set; }

    /// <summary>Kun brukt når Metric er SpecificSpecialEvent - id-en til SpecialEventDefinition
    /// som må ha en fullført SpecialEventResult i historikken.</summary>
    public string? RequiredEventId { get; set; }
}

public enum ShopItemEffect
{
    DriverRatingBoost,
    SafetyRatingBoost,
    XpBoost,
    HireManager,
}

public class ShopItemDefinition
{
    /// <summary>Stabil nøkkel lagret på karrieren for ikke-gjenkjøpbare varer (f.eks. manager).</summary>
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "🛒";
    public int Cost { get; set; }
    public ShopItemEffect Effect { get; set; }
    public int EffectValue { get; set; }

    /// <summary>Coaching kan kjøpes flere ganger. Manager er en engangs-ansettelse.</summary>
    public bool Repeatable { get; set; } = true;
}

/// <summary>Et ekte/ekte-inspirert enduranceklassiker (Fase 7) - bane og varighet er faste,
/// spilleren velger selv klasse og bil, systemet tildeler vær ved påmelding.</summary>
public class SpecialEventDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string TrackVenue { get; set; } = "";
    public int DurationMinutes { get; set; }
    public int EntryFeeCredits { get; set; }
}

public class GameContent
{
    public List<ClassDefinition> Classes { get; set; } = new();
    public List<string> Tracks { get; set; } = new();
    public List<string> WeatherOptions { get; set; } = new();
    public List<AchievementDefinition> Achievements { get; set; } = new();
    public List<ShopItemDefinition> ShopItems { get; set; } = new();
    public List<SpecialEventDefinition> SpecialEvents { get; set; } = new();
}
