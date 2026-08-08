using LmuCareerTool.Models;
using LmuCareerTool.Season;

namespace LmuCareerTool.Validation;

/// <summary>
/// Sammenligner det du faktisk kjørte mot det sesongen ba deg sette opp, og produserer
/// menneskelesbare avviksmeldinger. Verktøyet styrer ikke LMU, så dette er den eneste
/// måten å oppdage at oppsettet ikke stemte - i stedet for at runden bare stille ikke teller.
/// </summary>
public static class SetupValidator
{
    public static List<string> Validate(
        SeasonEvent? matchedEvent,
        bool trackMismatch,
        string? expectedNextTrack,
        RaceWeekendResult weekend)
    {
        var issues = new List<string>();
        var race = weekend.RaceResult;
        if (race == null) return issues;

        if (trackMismatch && expectedNextTrack != null)
        {
            issues.Add(
                $"Feil bane! Neste runde i sesongen er {expectedNextTrack}, men du kjørte {weekend.TrackVenue}. " +
                "Ingen sesongfremgang registrert - kjør riktig bane, eller godkjenn denne likevel.");
        }

        if (matchedEvent != null)
        {
            var carOk = string.Equals(
                race.CarType.Trim(), matchedEvent.AssignedCar.Trim(), StringComparison.OrdinalIgnoreCase);

            if (!carOk)
            {
                issues.Add(
                    $"Feil bil! Sesongen krever {matchedEvent.AssignedCar}, du kjørte {race.CarType}. " +
                    "Ingen XP/poeng/rating/credits gitt - kjør runden på nytt med riktig bil, eller godkjenn likevel.");
            }
        }

        return issues;
    }
}
