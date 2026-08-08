using LmuCareerTool.Models;

namespace LmuCareerTool.Career;

/// <summary>
/// Enkel, lett-å-justere XP-formel. Tanken er at du finjusterer tallene
/// selv etter hvert som du ser hva som føles rettferdig.
/// </summary>
public static class XpCalculator
{
    public static int Calculate(RaceWeekendResult weekend)
    {
        var race = weekend.RaceResult;
        if (race == null) return 0; // Spilleren ble ikke funnet i resultatet

        var xp = 0;

        // Grunnpoeng for å fullføre løpet i det hele tatt
        if (weekend.Finished)
            xp += 20;
        else
            xp += 5; // litt trøstepoeng for å ha deltatt, selv ved DNF

        // Plassering: flere poeng jo høyere oppe, skalert mot feltstørrelsen
        if (weekend.TotalParticipants > 0 && race.Position > 0)
        {
            var scorePct = 1.0 - (double)(race.Position - 1) / weekend.TotalParticipants;
            xp += (int)Math.Round(scorePct * 60); // opptil 60 poeng for seier i et stort felt
        }

        // Bonus for posisjoner vunnet fra start
        if (weekend.PositionsGainedFromGrid > 0)
            xp += weekend.PositionsGainedFromGrid * 2;

        // Trekk for rot (incidents/penalties funnet i <Stream>)
        xp -= race.IncidentCount * 2;
        xp -= race.PenaltyCount * 5;

        return Math.Max(xp, 0);
    }

    /// <summary>Enkel nivåkurve: 100 XP per nivå, øker litt for hvert nivå.</summary>
    public static int LevelFromXp(int totalXp)
    {
        var level = 1;
        var threshold = 100;
        var remaining = totalXp;
        while (remaining >= threshold)
        {
            remaining -= threshold;
            level++;
            threshold += 25; // hvert nivå krever litt mer enn forrige
        }
        return level;
    }
}
