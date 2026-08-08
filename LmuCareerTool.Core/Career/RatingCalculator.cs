using LmuCareerTool.Models;

namespace LmuCareerTool.Career;

/// <summary>
/// Driver Rating (0-100) er en langsiktig prestisje-score, separat fra XP.
/// XP styrer hvilke KLASSER du kan kjøre i; Rating styrer hvilke MERKER som
/// legger merke til deg innad i klassen du allerede kjører.
/// </summary>
public static class RatingCalculator
{
    public static int CalculateDelta(RaceWeekendResult weekend)
    {
        var race = weekend.RaceResult;
        if (race == null) return 0;

        var delta = 0;

        delta += weekend.Finished ? 2 : -5;

        if (weekend.TotalParticipants > 0 && race.Position > 0)
        {
            var percentile = 1.0 - (double)(race.Position - 1) / weekend.TotalParticipants;
            delta += (int)Math.Round(percentile * 6) - 3; // ca -3 til +3
        }

        if (weekend.PositionsGainedFromGrid > 0)
            delta += Math.Min(weekend.PositionsGainedFromGrid, 5);

        delta -= race.IncidentCount;
        delta -= race.PenaltyCount * 2;

        return Math.Clamp(delta, -10, 10);
    }

    public static int ApplyDelta(int currentRating, int delta) => Math.Clamp(currentRating + delta, 0, 100);
}
