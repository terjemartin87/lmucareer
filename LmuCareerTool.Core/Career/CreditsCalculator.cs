using LmuCareerTool.Models;

namespace LmuCareerTool.Career;

/// <summary>Credits er en ren spillvaluta - ikke koblet til noe ekte, kun for å kjøpe deg inn hos merker.</summary>
public static class CreditsCalculator
{
    public static int Calculate(RaceWeekendResult weekend)
    {
        var race = weekend.RaceResult;
        if (race == null) return 0;

        var credits = 200; // "startpenger" for å delta

        if (weekend.Finished) credits += 100;

        if (weekend.TotalParticipants > 0 && race.Position > 0)
        {
            var percentile = 1.0 - (double)(race.Position - 1) / weekend.TotalParticipants;
            credits += (int)Math.Round(percentile * 500);
        }

        credits -= race.IncidentCount * 20;
        credits -= race.PenaltyCount * 50;

        return Math.Max(credits, 0);
    }
}
