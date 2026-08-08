using LmuCareerTool.Models;

namespace LmuCareerTool.Career;

/// <summary>
/// Regner ut noen personlige sesongpriser fra dine egne rundeforløp - ikke sammenlignet mot
/// AI-feltet (vi har ikke deres hendelses-/gridata), men mot din egen sesong og historikk.
/// </summary>
public static class AwardService
{
    public static List<SeasonAward> ComputeAwards(List<CareerRaceEntry> seasonRaces)
    {
        var awards = new List<SeasonAward>();
        if (seasonRaces.Count < 2) return awards;

        AddImprovementAward(awards, seasonRaces);
        AddCleanDrivingAward(awards, seasonRaces);
        AddComebackAward(awards, seasonRaces);

        return awards;
    }

    private static void AddImprovementAward(List<SeasonAward> awards, List<CareerRaceEntry> seasonRaces)
    {
        var half = Math.Max(1, seasonRaces.Count / 2);
        var firstHalf = seasonRaces.Take(half).Where(r => r.FinishPos > 0).Select(r => (double)r.FinishPos).ToList();
        var secondHalf = seasonRaces.Skip(half).Where(r => r.FinishPos > 0).Select(r => (double)r.FinishPos).ToList();
        if (firstHalf.Count == 0 || secondHalf.Count == 0) return;

        var improvement = firstHalf.Average() - secondHalf.Average();
        if (improvement < 1.0) return;

        awards.Add(new SeasonAward
        {
            Title = "Mest forbedret",
            Description = $"Snittplassering gikk fra P{firstHalf.Average():0.#} til P{secondHalf.Average():0.#} gjennom sesongen."
        });
    }

    private static void AddCleanDrivingAward(List<SeasonAward> awards, List<CareerRaceEntry> seasonRaces)
    {
        var avgTrouble = seasonRaces.Average(r => r.IncidentCount + r.PenaltyCount * 2);
        if (avgTrouble > 2.0) return;

        awards.Add(new SeasonAward
        {
            Title = "Renest kjørestil",
            Description = $"Snitt {avgTrouble:0.#} hendelser/straffer per løp - en ren sesong."
        });
    }

    private static void AddComebackAward(List<SeasonAward> awards, List<CareerRaceEntry> seasonRaces)
    {
        var best = seasonRaces
            .Where(r => r.GridPos > 0 && r.FinishPos > 0)
            .OrderByDescending(r => r.GridPos - r.FinishPos)
            .FirstOrDefault();

        if (best == null || best.GridPos - best.FinishPos < 5) return;

        awards.Add(new SeasonAward
        {
            Title = "Comeback of the Season",
            Description = $"{best.TrackVenue}: fra P{best.GridPos} til P{best.FinishPos} - " +
                          $"{best.GridPos - best.FinishPos} plasser vunnet i ett løp."
        });
    }
}
