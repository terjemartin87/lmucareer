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
        SeasonModel? lastSeason)
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

        // Renhet - Safety Rating direkte - middels faktor.
        score += Math.Clamp(career.SafetyRating, 0, 100) * 0.15;

        // Lojalitet: liten bonus hvis du allerede kjører for merket.
        if (string.Equals(career.CurrentContract?.Manufacturer, manufacturer.Name, StringComparison.OrdinalIgnoreCase))
            score += 10;

        // Manager: forhandler bedre på dine vegne uansett merke.
        if (career.HasManager)
            score += 8;

        return (int)Math.Clamp(Math.Round(score), 0, 100);
    }
}
