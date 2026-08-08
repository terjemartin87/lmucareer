using LmuCareerTool.Models;

namespace LmuCareerTool.Parsing;

/// <summary>
/// LMUs resultatfiler skiller på &lt;Setting&gt;: "Race Weekend" er en offline økt mot
/// spillets faste AI-startliste (WEC-feltet), mens "Multiplayer" er en tilfeldig lobby med
/// andre mennesker som aldri kommer igjen. Et ekte mesterskap med faste rivaler er kun
/// meningsfullt for Race Weekend, så kun den modusen skal telle mot karrieren.
/// </summary>
public static class SessionModeFilter
{
    private const string CareerEligibleMode = "Race Weekend";

    public static bool IsCareerEligible(SessionResult session) =>
        string.Equals(session.SettingMode?.Trim(), CareerEligibleMode, StringComparison.OrdinalIgnoreCase);
}
