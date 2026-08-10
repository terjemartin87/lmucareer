using LmuCareerTool.Models;

namespace LmuCareerTool.App;

public class SpecialEventHistoryRowVm
{
    public SpecialEventHistoryRowVm(SpecialEventResult result)
    {
        Summary = $"{result.EventName} - P{result.FinishPos} av {result.TotalParticipants}";
        Detail = $"{result.TrackVenue}   ·   {result.CompletedAtUtc.ToLocalTime():dd.MM.yyyy HH:mm}   ·   " +
                 $"{result.FinishStatus}   ·   +{result.XpEarned} XP   ·   +{result.CreditsEarned} cr";
    }

    public string Summary { get; }
    public string Detail { get; }
}
