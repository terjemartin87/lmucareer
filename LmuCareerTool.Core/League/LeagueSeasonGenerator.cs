using LmuCareerTool.Content;
using LmuCareerTool.Season;

namespace LmuCareerTool.League;

public enum LeagueFormatPreference
{
    Sprint,
    Endurance,
    Mixed
}

/// <summary>Genererer en ny ligasesongs runder (bane + format). Ingen bil tildeles - i
/// ligamodus velger hver sjåfør selv hva de kjører, i motsetning til karrieremodus.</summary>
public static class LeagueSeasonGenerator
{
    public static LeagueSeason Generate(
        GameContent content,
        string carClass,
        int seasonNumber,
        int roundCount,
        LeagueFormatPreference formatPreference,
        string? avoidOpeningTrack = null,
        int? randomSeed = null)
    {
        if (content.Tracks.Count == 0)
            throw new InvalidOperationException("Ingen baner definert i game-content.json.");

        var rng = randomSeed.HasValue ? new Random(randomSeed.Value) : new Random();
        var tracks = CalendarBuilder.BuildTrackList(content.Tracks, roundCount, rng, avoidOpeningTrack);
        var formats = BuildFormatList(roundCount, formatPreference, rng);

        var season = new LeagueSeason { SeasonNumber = seasonNumber, CarClass = carClass };
        for (var i = 0; i < roundCount; i++)
        {
            season.Rounds.Add(new LeagueRound
            {
                RoundNumber = i + 1,
                TrackVenue = tracks[i],
                Format = formats[i],
            });
        }

        return season;
    }

    private static List<RaceFormat> BuildFormatList(int roundCount, LeagueFormatPreference preference, Random rng)
    {
        return preference switch
        {
            LeagueFormatPreference.Sprint => Enumerable.Repeat(RaceFormat.Sprint, roundCount).ToList(),
            LeagueFormatPreference.Endurance => Enumerable.Repeat(RaceFormat.Endurance, roundCount).ToList(),
            _ => BuildMixedFormatList(roundCount, rng),
        };
    }

    private static List<RaceFormat> BuildMixedFormatList(int roundCount, Random rng)
    {
        var enduranceCount = Math.Max(1, (int)Math.Round(roundCount * 0.35));
        return Enumerable.Repeat(RaceFormat.Endurance, enduranceCount)
            .Concat(Enumerable.Repeat(RaceFormat.Sprint, roundCount - enduranceCount))
            .OrderBy(_ => rng.Next())
            .ToList();
    }
}
