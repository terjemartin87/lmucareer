using LmuCareerTool.Championship;
using LmuCareerTool.Content;
using LmuCareerTool.Scoring;

namespace LmuCareerTool.League;

public class LeagueDriverStandingEntry
{
    public string Name { get; set; } = "";
    public string TeamName { get; set; } = "";
    public string CarType { get; set; } = "";
    public string CarClass { get; set; } = "";
    public int Points { get; set; }
    public int Wins { get; set; }
    public int Podiums { get; set; }
    public int Top5 { get; set; }
    public int Top10 { get; set; }
    public int RoundsCompleted { get; set; }
    public int PenaltyPointsTotal { get; set; }
}

public class LeagueManufacturerStandingEntry
{
    public string Manufacturer { get; set; } = "";
    public int Points { get; set; }
    public int Wins { get; set; }
}

/// <summary>Aggregert historikk for én sjåfør på tvers av ALLE ligaens sesonger (ikke bare
/// den pågående) - grunnlaget for "klikk på fører for å se historikk".</summary>
public class LeagueDriverHistory
{
    public string Name { get; set; } = "";
    public int TotalRaces { get; set; }
    public int TotalPoints { get; set; }
    public int Wins { get; set; }
    public int Podiums { get; set; }
    public int Top5 { get; set; }
    public int Top10 { get; set; }
    public int BestFinish { get; set; }
}

/// <summary>
/// Fører- og merkemesterskap for en ligasesong, inkludert manuelle straffer (poengtrekk og
/// diskvalifisering) fra verten. Samme grunnprinsipp som Championship/ChampionshipTable for
/// karrieremodus, men helt uavhengig implementasjon siden ligamodus har straffer og ikke har
/// et fast "spiller"-perspektiv.
///
/// VIKTIG: et hostet ligaløp kan fritt blande klasser i samme race (GT3 sammen med LMP2, LMP3,
/// Hypercar osv - akkurat som ekte WEC). Poeng regnes derfor ALLTID ut fra sjåførens plassering
/// INNENFOR egen klasse (ClassPosition), aldri den overordnede løpsplasseringen - ellers ville en
/// GT3-vinner som krysset mål bak alle Hypercar-ene fått poeng som om han var sist. Alle
/// standings-metoder tar en valgfri klassefilter; kall GetClassesInSeason for å finne ut hvilke
/// klasser som faktisk er kjørt i sesongen (én klasse i en enkeltklasse-liga, flere i en
/// multiklasse-liga).
/// </summary>
public static class LeagueStandingsCalculator
{
    /// <summary>Alle distinkte klasser som faktisk har kjørt i sesongen, sortert. Tom liste hvis
    /// resultatfilene ikke inneholder klasseinfo i det hele tatt.</summary>
    public static List<string> GetClassesInSeason(LeagueSeason? season)
    {
        if (season == null) return new List<string>();

        return season.Rounds
            .SelectMany(r => r.FieldResults)
            .Select(f => f.CarClass)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static List<LeagueDriverStandingEntry> ComputeDriverStandings(
        LeagueSeason? season, string? carClass = null, int? throughRound = null)
    {
        var result = new List<LeagueDriverStandingEntry>();
        if (season == null) return result;

        var byName = new Dictionary<string, LeagueDriverStandingEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var round in RelevantRounds(season, throughRound))
        {
            foreach (var fieldResult in FilterByClass(round.FieldResults, carClass))
            {
                if (!byName.TryGetValue(fieldResult.Name, out var entry))
                {
                    entry = new LeagueDriverStandingEntry { Name = fieldResult.Name };
                    byName[fieldResult.Name] = entry;
                }

                entry.TeamName = fieldResult.TeamName;
                entry.CarClass = fieldResult.CarClass;
                entry.RoundsCompleted++;

                var (points, penaltyPoints, disqualified) = ScoreRound(round, fieldResult.Name, fieldResult.ClassPosition);
                entry.Points += points;
                entry.PenaltyPointsTotal += penaltyPoints;

                if (!disqualified)
                {
                    if (fieldResult.ClassPosition == 1) entry.Wins++;
                    if (fieldResult.ClassPosition is >= 1 and <= 3) entry.Podiums++;
                    if (fieldResult.ClassPosition is >= 1 and <= 5) entry.Top5++;
                    if (fieldResult.ClassPosition is >= 1 and <= 10) entry.Top10++;
                }
            }
        }

        return byName.Values
            .OrderByDescending(e => e.Points)
            .ThenByDescending(e => e.Wins)
            .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static List<LeagueManufacturerStandingEntry> ComputeManufacturerStandings(
        LeagueSeason? season, GameContent content, string? carClass = null, int? throughRound = null)
    {
        var result = new List<LeagueManufacturerStandingEntry>();
        if (season == null) return result;

        var carToManufacturer = content.Classes
            .SelectMany(c => c.Manufacturers.SelectMany(m => m.Cars.Select(car => (car, m.Name))))
            .GroupBy(x => x.car, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Name, StringComparer.OrdinalIgnoreCase);

        var byMake = new Dictionary<string, LeagueManufacturerStandingEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var round in RelevantRounds(season, throughRound))
        {
            foreach (var fieldResult in FilterByClass(round.FieldResults, carClass))
            {
                var make = carToManufacturer.TryGetValue(fieldResult.CarType, out var mapped) ? mapped : fieldResult.CarType;
                if (!byMake.TryGetValue(make, out var entry))
                {
                    entry = new LeagueManufacturerStandingEntry { Manufacturer = make };
                    byMake[make] = entry;
                }

                var (points, _, disqualified) = ScoreRound(round, fieldResult.Name, fieldResult.ClassPosition);
                entry.Points += points;
                if (!disqualified && fieldResult.ClassPosition == 1) entry.Wins++;
            }
        }

        return byMake.Values.OrderByDescending(e => e.Points).ToList();
    }

    /// <summary>Bygger karriere-lang historikk (alle sesonger, alle klasser) for én navngitt sjåfør.</summary>
    public static LeagueDriverHistory BuildDriverHistory(LeagueProfile league, string driverName)
    {
        var normalized = RosterMatcher.Normalize(driverName);
        var history = new LeagueDriverHistory { Name = normalized };

        var allSeasons = league.SeasonHistory
            .Concat(league.CurrentSeason != null ? new[] { league.CurrentSeason } : Array.Empty<LeagueSeason>());

        foreach (var round in allSeasons.SelectMany(s => s.Rounds).Where(r => r.Completed))
        {
            var fieldResult = round.FieldResults.FirstOrDefault(f => RosterMatcher.AreSameDriver(f.Name, normalized));
            if (fieldResult == null) continue;

            history.TotalRaces++;

            var (points, _, disqualified) = ScoreRound(round, fieldResult.Name, fieldResult.ClassPosition);
            history.TotalPoints += points;

            if (disqualified) continue;

            if (fieldResult.ClassPosition == 1) history.Wins++;
            if (fieldResult.ClassPosition is >= 1 and <= 3) history.Podiums++;
            if (fieldResult.ClassPosition is >= 1 and <= 5) history.Top5++;
            if (fieldResult.ClassPosition is >= 1 and <= 10) history.Top10++;
            if (history.BestFinish == 0 || fieldResult.ClassPosition < history.BestFinish)
                history.BestFinish = fieldResult.ClassPosition;
        }

        return history;
    }

    /// <summary>Alle sjåførnavn ligaen noensinne har sett, på tvers av alle sesonger - til en fører-velger i UI-en.</summary>
    public static List<string> GetAllKnownDriverNames(LeagueProfile league)
    {
        var allSeasons = league.SeasonHistory
            .Concat(league.CurrentSeason != null ? new[] { league.CurrentSeason } : Array.Empty<LeagueSeason>());

        return allSeasons
            .SelectMany(s => s.Rounds)
            .SelectMany(r => r.FieldResults)
            .Select(f => f.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<LmuCareerTool.Models.FieldResultEntry> FilterByClass(
        IEnumerable<LmuCareerTool.Models.FieldResultEntry> fieldResults, string? carClass) =>
        string.IsNullOrWhiteSpace(carClass)
            ? fieldResults
            : fieldResults.Where(f => string.Equals(f.CarClass, carClass, StringComparison.OrdinalIgnoreCase));

    private static (int points, int penaltyPoints, bool disqualified) ScoreRound(LeagueRound round, string driverName, int classPosition)
    {
        var penalties = round.Penalties.Where(p => RosterMatcher.AreSameDriver(p.DriverName, driverName)).ToList();
        var disqualified = penalties.Any(p => p.Disqualified);
        var penaltyPoints = penalties.Sum(p => p.PointsDeducted);

        var basePoints = disqualified ? 0 : PointsCalculator.PointsForPosition(classPosition);
        var finalPoints = Math.Max(0, basePoints - penaltyPoints);

        return (finalPoints, penaltyPoints, disqualified);
    }

    private static IEnumerable<LeagueRound> RelevantRounds(LeagueSeason season, int? throughRound) =>
        season.Rounds.Where(r => r.Completed && (throughRound == null || r.RoundNumber <= throughRound));
}
