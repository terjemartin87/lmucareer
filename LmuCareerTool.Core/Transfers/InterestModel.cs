using LmuCareerTool.Content;
using LmuCareerTool.Models;
using LmuCareerTool.Season;

namespace LmuCareerTool.Transfers;

/// <summary>
/// Regner ut hvor interessert et merke er i deg (0-100), som styrer om de sender et
/// kontraktstilbud ved sesongslutt. Ingen ting "låses opp" permanent lenger - interessen
/// regnes ut på nytt hver gang, ut fra hvem du er akkurat nå.
/// </summary>
public static class InterestModel
{
    /// <summary>Rating mer enn dette under kravet betyr at merket ikke engang vurderer deg.</summary>
    private const int PrestigeGapCutoff = 10;

    public static int ComputeInterest(
        CareerProfile career,
        ManufacturerDefinition manufacturer,
        SeasonModel? lastSeason,
        List<CareerRaceEntry> lastSeasonRaces)
    {
        if (career.DriverRating < manufacturer.RatingRequired - PrestigeGapCutoff)
            return 0; // prestisjegap - de vet du ikke er i nærheten ennå

        double score = 0;

        // Driver Rating mot merkets krav - tyngst faktor.
        var ratingGap = career.DriverRating - manufacturer.RatingRequired;
        score += Math.Clamp(50 + ratingGap * 2.0, 0, 100) * 0.45;

        // Form siste sesong (snittplassering) - tung faktor.
        if (lastSeason is { CompletedCount: > 0 })
        {
            var avgFinish = lastSeason.Events
                .Where(e => e.FinishPos.HasValue)
                .Select(e => (double)e.FinishPos!.Value)
                .DefaultIfEmpty(20)
                .Average();
            score += Math.Clamp(100 - avgFinish * 4, 0, 100) * 0.30;
        }
        else
        {
            score += 45 * 0.30; // ingen sesong å vurdere ennå - nøytralt
        }

        // Renhet siste sesong (incidents + straffer per løp) - middels faktor.
        if (lastSeasonRaces.Count > 0)
        {
            var avgTrouble = lastSeasonRaces.Average(r => r.IncidentCount + r.PenaltyCount * 2);
            score += Math.Clamp(100 - avgTrouble * 12, 0, 100) * 0.15;
        }
        else
        {
            score += 60 * 0.15;
        }

        // Lojalitet: liten bonus hvis du allerede kjører for merket.
        if (string.Equals(career.CurrentContract?.Manufacturer, manufacturer.Name, StringComparison.OrdinalIgnoreCase))
            score += 10;

        return (int)Math.Clamp(Math.Round(score), 0, 100);
    }
}
