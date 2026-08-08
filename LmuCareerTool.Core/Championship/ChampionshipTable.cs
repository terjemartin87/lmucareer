using LmuCareerTool.Career;
using LmuCareerTool.Content;
using LmuCareerTool.Models;
using LmuCareerTool.Season;

namespace LmuCareerTool.Championship;

/// <summary>
/// Regner ut fører- og merkemesterskap fra sesongens lagrede rundeforløp
/// (SeasonEvent.FieldResults). Bruker samme poengskala som spillerens egen sesongpoeng,
/// slik at tabellen er sammenlignbar.
/// </summary>
public static class ChampionshipTable
{
    public static List<DriverStandingEntry> ComputeDriverStandings(
        SeasonModel? season, string playerName, int? throughRound = null)
    {
        var result = new List<DriverStandingEntry>();
        if (season == null) return result;

        var byName = new Dictionary<string, DriverStandingEntry>(StringComparer.OrdinalIgnoreCase);
        var normalizedPlayer = RosterMatcher.Normalize(playerName);

        foreach (var ev in RelevantEvents(season, throughRound))
        {
            var presentNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var fieldResult in ev.FieldResults)
            {
                presentNames.Add(fieldResult.Name);

                if (!byName.TryGetValue(fieldResult.Name, out var entry))
                {
                    entry = new DriverStandingEntry
                    {
                        Name = fieldResult.Name,
                        IsPlayer = string.Equals(fieldResult.Name, normalizedPlayer, StringComparison.OrdinalIgnoreCase),
                        IsReserve = FieldRoster.IsReserve(season, fieldResult.Name)
                    };
                    byName[fieldResult.Name] = entry;
                }

                entry.TeamName = fieldResult.TeamName;
                entry.CarType = fieldResult.CarType;
                entry.RoundsCompleted++;

                var points = PointsCalculator.PointsForPosition(fieldResult.Position);
                entry.Points += points;
                if (fieldResult.Position == 1) entry.Wins++;
                if (fieldResult.Position is >= 1 and <= 3) entry.Podiums++;
                if (fieldResult.Position > 0 && (entry.BestFinish == null || fieldResult.Position < entry.BestFinish))
                    entry.BestFinish = fieldResult.Position;
            }

            // Sjåfører som var i det låste feltet, men mangler i denne rundens resultat: DNS, 0 poeng.
            foreach (var rosterName in season.LockedRosterNames.Where(n => !presentNames.Contains(n)))
            {
                if (byName.TryGetValue(rosterName, out var dnsEntry))
                    dnsEntry.RoundsCompleted++;
            }
        }

        return byName.Values
            .OrderByDescending(e => e.Points)
            .ThenBy(e => e.BestFinish ?? int.MaxValue)
            .ToList();
    }

    public static List<ManufacturerStandingEntry> ComputeManufacturerStandings(
        SeasonModel? season, GameContent content, int? throughRound = null)
    {
        var result = new List<ManufacturerStandingEntry>();
        if (season == null) return result;

        var carToManufacturer = content.Classes
            .SelectMany(c => c.Manufacturers.SelectMany(m => m.Cars.Select(car => (car, m.Name))))
            .GroupBy(x => x.car, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Name, StringComparer.OrdinalIgnoreCase);

        var byMake = new Dictionary<string, ManufacturerStandingEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var ev in RelevantEvents(season, throughRound))
        {
            foreach (var fieldResult in ev.FieldResults)
            {
                var make = carToManufacturer.TryGetValue(fieldResult.CarType, out var mapped)
                    ? mapped
                    : fieldResult.CarType;

                if (!byMake.TryGetValue(make, out var entry))
                {
                    entry = new ManufacturerStandingEntry { Manufacturer = make };
                    byMake[make] = entry;
                }

                entry.Points += PointsCalculator.PointsForPosition(fieldResult.Position);
                if (fieldResult.Position == 1) entry.Wins++;
            }
        }

        return byMake.Values.OrderByDescending(e => e.Points).ToList();
    }

    private static IEnumerable<SeasonEvent> RelevantEvents(SeasonModel season, int? throughRound) =>
        season.Events.Where(e => e.Completed && (throughRound == null || e.RoundNumber <= throughRound));
}
