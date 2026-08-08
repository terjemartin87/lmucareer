using LmuCareerTool.Models;
using LmuCareerTool.Season;

namespace LmuCareerTool.Championship;

/// <summary>
/// Låser sesongens startfelt basert på den første fullførte runden, slik at
/// mesterskapstabellen følger de samme motstanderne gjennom hele sesongen i stedet for
/// å bytte ut feltet hver runde.
/// </summary>
public static class FieldRoster
{
    public static void LockIfNeeded(SeasonModel season, List<FieldResultEntry> fieldResults)
    {
        if (season.LockedRosterNames.Count > 0) return;

        season.LockedRosterNames = fieldResults
            .Select(r => r.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>True hvis navnet dukket opp etter at feltet ble låst - regnes som en reserve.</summary>
    public static bool IsReserve(SeasonModel season, string normalizedName) =>
        season.LockedRosterNames.Count > 0 &&
        !season.LockedRosterNames.Contains(normalizedName, StringComparer.OrdinalIgnoreCase);
}
