using LmuCareerTool.Season;
using LmuCareerTool.Transfers;

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
    public int PenaltyCount { get; set; }
    public int XpEarned { get; set; }
    public int PointsEarned { get; set; }

    /// <summary>Lagret for å kunne reversere effekten av dette løpet nøyaktig hvis det slettes
    /// fra historikken (Fase 3). Gamle løp lagret før dette feltet fantes står på 0.</summary>
    public int RatingDelta { get; set; }
    public int SafetyRatingDelta { get; set; }
    public int CreditsEarned { get; set; }
    public int ContractSalaryEarned { get; set; }

    /// <summary>Fullførte runder og banelengde i meter (Fase 4: kjørt distanse i sesongrapporten).</summary>
    public int Laps { get; set; }
    public double TrackLength { get; set; }

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

/// <summary>Din aktive påmelding til et spesialarrangement (Fase 7) - ryddes når løpet
/// fullføres eller du trekker deg. Snapshot av bane/varighet/inngangspenger fra definisjonen
/// på påmeldingstidspunktet, slik at senere endringer i innholdsfilen ikke påvirker en aktiv påmelding.</summary>
public class SpecialEventEntry
{
    public string EventId { get; set; } = "";
    public string EventName { get; set; } = "";
    public string TrackVenue { get; set; } = "";
    public string CarClass { get; set; } = "";
    public string AssignedCar { get; set; } = "";
    public string AssignedWeather { get; set; } = "";
    public int DurationMinutes { get; set; }
    public int EntryFeePaid { get; set; }
    public DateTime EnrolledAtUtc { get; set; }
}

/// <summary>Et fullført spesialarrangement - holdt helt separat fra CareerRaceEntry/RaceHistory
/// slik at det aldri teller mot mesterskapstabellen.</summary>
public class SpecialEventResult
{
    public string EventId { get; set; } = "";
    public string EventName { get; set; } = "";
    public string TrackVenue { get; set; } = "";
    public DateTime CompletedAtUtc { get; set; }
    public int FinishPos { get; set; }
    public int TotalParticipants { get; set; }
    public string FinishStatus { get; set; } = "";
    public int XpEarned { get; set; }
    public int CreditsEarned { get; set; }
    public int RatingDelta { get; set; }
    public int SafetyRatingDelta { get; set; }
}

public class CareerProfile
{
    public string DriverName { get; set; } = "";
    public string CurrentClass { get; set; } = "GT3";
    public string CurrentSeries { get; set; } = "WEC"; // eller "ELMS"
    public int TotalXp { get; set; }
    public int Level { get; set; } = 1;

    public int DriverRating { get; set; } = 50; // 0-100, prestasjon relativt til feltet - styrer hvor attraktiv du er for merker
    public int SafetyRating { get; set; } = 70; // 0-100, renhet (incidents/straffer) - styrer kontrakters "renhet"-krav
    public int OverallRating => (DriverRating + SafetyRating) / 2;
    public int Credits { get; set; } = 0;       // "penger" - lønn, bruddsummer, privatlag-seter
    public string? CurrentManufacturer { get; set; }

    /// <summary>Din aktive kontrakt (merke/privatlag/fri klasse) - null hvis du står uten kontrakt.</summary>
    public Contract? CurrentContract { get; set; }
    public List<Contract> ContractHistory { get; set; } = new();

    /// <summary>Merkets "hukommelse" av deg (0-100) - driver sakte mot faktisk beregnet interesse
    /// hver gang transfer-markedet vises, i stedet for å hoppe rundt momentant.</summary>
    public Dictionary<string, int> ManufacturerReputation { get; set; } = new();

    public List<string> UnlockedClasses { get; set; } = new() { "GT3" };
    public List<string> UnlockedAchievements { get; set; } = new();

    /// <summary>Ikke-gjenkjøpbare butikkvarer du allerede har kjøpt (f.eks. manager).</summary>
    public List<string> PurchasedShopItems { get; set; } = new();

    /// <summary>Har du ansatt en manager - gir passiv bonus på merke-interesse og billigere kontraktsforhandlinger.</summary>
    public bool HasManager { get; set; }
    public SeasonModel? CurrentSeason { get; set; }
    public List<SeasonModel> SeasonHistory { get; set; } = new();

    public List<CareerRaceEntry> RaceHistory { get; set; } = new();

    /// <summary>Din nåværende påmelding til et spesialarrangement (24h Le Mans osv) - null hvis ingen.</summary>
    public SpecialEventEntry? ActiveSpecialEvent { get; set; }
    public List<SpecialEventResult> SpecialEventHistory { get; set; } = new();
}
