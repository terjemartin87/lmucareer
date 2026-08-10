using LmuCareerTool.Content;
using LmuCareerTool.Models;

namespace LmuCareerTool.Career;

/// <summary>
/// Håndterer påmelding til og uttrekking fra spesialarrangementer (Fase 7). Kun én aktiv
/// påmelding om gangen. Inngangspenger refunderes ikke ved uttrekking - det er ekte penger
/// brukt på en ekte startplass, akkurat som i virkeligheten.
/// </summary>
public static class SpecialEventService
{
    public static bool CanEnroll(CareerProfile career, SpecialEventDefinition definition) =>
        career.ActiveSpecialEvent == null && career.Credits >= definition.EntryFeeCredits;

    public static bool TryEnroll(
        CareerProfile career, SpecialEventDefinition definition, string carClass, string car, string weather)
    {
        if (!CanEnroll(career, definition)) return false;

        career.Credits -= definition.EntryFeeCredits;
        career.ActiveSpecialEvent = new SpecialEventEntry
        {
            EventId = definition.Id,
            EventName = definition.Name,
            TrackVenue = definition.TrackVenue,
            CarClass = carClass,
            AssignedCar = car,
            AssignedWeather = weather,
            DurationMinutes = definition.DurationMinutes,
            EntryFeePaid = definition.EntryFeeCredits,
            EnrolledAtUtc = DateTime.UtcNow,
        };
        return true;
    }

    public static void Withdraw(CareerProfile career)
    {
        career.ActiveSpecialEvent = null;
    }
}
