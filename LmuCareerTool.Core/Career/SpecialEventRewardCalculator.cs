using LmuCareerTool.Models;

namespace LmuCareerTool.Career;

/// <summary>
/// Egen belønningsformel for spesialarrangementer (Fase 7) - bevisst IKKE den vanlige
/// XpCalculator/CreditsCalculator (de er kalibrert for 25-60 min sesongrunder). Her skalerer
/// credits og XP direkte med arrangementets varighet: jo lengre løp, jo mer - et 24-timersløp
/// skal føles som en helt annen prestasjon (og utbetaling) enn en vanlig sesongrunde.
/// Driver/Safety Rating er bevisst IKKE skalert opp her - de har et fast tak på 0-100 og
/// ±10 per løp, og ville blitt meningsløse hvis ett løp kunne gi +60.
/// </summary>
public static class SpecialEventRewardCalculator
{
    public static int CalculateCredits(RaceWeekendResult weekend, SpecialEventEntry activeEvent)
    {
        var race = weekend.RaceResult;
        if (race == null) return 0;

        var hours = activeEvent.DurationMinutes / 60.0;
        var percentile = GetPercentile(weekend, race);

        var perHour = 500 + percentile * 700; // 500-1200 cr/time avhengig av plassering
        var credits = hours * perHour;

        if (!weekend.Finished) credits *= 0.4; // fullførte du ikke - fortsatt noe for forsøket, mye mindre

        credits -= race.IncidentCount * 30;
        credits -= race.PenaltyCount * 75;

        // Aldri under halve inngangsprisen tilbake - et fullført forsøk skal aldri føles som ren tapsbutikk.
        var minimumRefund = activeEvent.EntryFeePaid / 2;
        return Math.Max((int)Math.Round(credits), minimumRefund);
    }

    public static int CalculateXp(RaceWeekendResult weekend, SpecialEventEntry activeEvent)
    {
        var race = weekend.RaceResult;
        if (race == null) return 0;

        var hours = activeEvent.DurationMinutes / 60.0;
        var percentile = GetPercentile(weekend, race);

        var perHour = 15 + percentile * 45; // 15-60 XP/time avhengig av plassering
        var xp = hours * perHour;

        if (!weekend.Finished) xp *= 0.4;

        xp -= race.IncidentCount * 2;
        xp -= race.PenaltyCount * 5;

        return Math.Max((int)Math.Round(xp), 0);
    }

    private static double GetPercentile(RaceWeekendResult weekend, DriverResult race) =>
        weekend.TotalParticipants > 0 && race.Position > 0
            ? 1.0 - (double)(race.Position - 1) / weekend.TotalParticipants
            : 0.3;
}
