namespace LmuCareerTool.Season;

/// <summary>
/// Velger banerekkefølgen for en sesong. Med færre baner enn runder (typisk i dag: 7 baner,
/// 9 runder) er noen repetisjoner uunngåelig, men denne sprer dem jevnere enn et fast
/// modulo-mønster (som alltid ville gitt runde 1 og runde 8 samme bane), og unngår at to
/// runder på rad havner på samme bane eller at sesongen åpner der forrige sesong sluttet.
/// </summary>
public static class CalendarBuilder
{
    public static List<string> BuildTrackList(
        IReadOnlyList<string> trackPool, int raceCount, Random rng, string? avoidOpeningTrack = null)
    {
        if (trackPool.Count == 0)
            throw new InvalidOperationException("Ingen baner definert i game-content.json.");

        var result = new List<string>();
        while (result.Count < raceCount)
        {
            result.AddRange(trackPool.OrderBy(_ => rng.Next()));
        }
        result = result.Take(raceCount).ToList();

        if (trackPool.Count > 1)
        {
            // Unngå at runde 1 er samme bane som forrige sesongs siste runde.
            if (avoidOpeningTrack != null &&
                result[0].Equals(avoidOpeningTrack, StringComparison.OrdinalIgnoreCase))
            {
                var swapIndex = result.FindIndex(1, t => !t.Equals(avoidOpeningTrack, StringComparison.OrdinalIgnoreCase));
                if (swapIndex > 0) (result[0], result[swapIndex]) = (result[swapIndex], result[0]);
            }

            // Unngå at samme bane kommer to runder på rad (kan skje i overgangen mellom to sykluser av poolen).
            for (var i = 1; i < result.Count; i++)
            {
                if (!result[i].Equals(result[i - 1], StringComparison.OrdinalIgnoreCase)) continue;

                var swapIndex = result.FindIndex(i + 1, t => !t.Equals(result[i - 1], StringComparison.OrdinalIgnoreCase));
                if (swapIndex > 0) (result[i], result[swapIndex]) = (result[swapIndex], result[i]);
            }
        }

        return result;
    }
}
