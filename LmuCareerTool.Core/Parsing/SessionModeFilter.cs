using LmuCareerTool.Models;

namespace LmuCareerTool.Parsing;

/// <summary>
/// LMUs resultatfiler skiller på &lt;Setting&gt;: "Race Weekend" er en offline økt mot
/// spillets faste AI-startliste (WEC-feltet), mens "Multiplayer" er et hostet løp med ekte
/// mennesker - enten en tilfeldig offentlig lobby, eller en avtalt liga-kveld. Karrieremodus
/// og ligamodus teller derfor stikk motsatte modus: karrieren trenger Race Weekends faste
/// AI-startliste for at et mesterskap skal gi mening, mens ligaen KUN gir mening for et ekte
/// hostet felt av mennesker.
/// </summary>
public static class SessionModeFilter
{
    private const string CareerEligibleMode = "Race Weekend";
    private const string LeagueEligibleMode = "Multiplayer";

    public static bool IsCareerEligible(SessionResult session) =>
        string.Equals(session.SettingMode?.Trim(), CareerEligibleMode, StringComparison.OrdinalIgnoreCase);

    public static bool IsLeagueEligible(SessionResult session) =>
        string.Equals(session.SettingMode?.Trim(), LeagueEligibleMode, StringComparison.OrdinalIgnoreCase);
}
