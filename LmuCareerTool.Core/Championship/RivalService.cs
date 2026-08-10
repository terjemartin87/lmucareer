using LmuCareerTool.Models;

namespace LmuCareerTool.Championship;

/// <summary>
/// Peker ut en rival fra mesterskapstabellen: føreren rett foran deg (den du jager), eller -
/// hvis du leder - føreren rett bak (den som jager deg). Regnes helt på nytt fra gjeldende
/// stilling hver gang, i stedet for å låses fast ved sesongstart, siden det ikke finnes noen
/// reell stilling å plukke en rival fra før første runde er kjørt.
/// </summary>
public static class RivalService
{
    public static DriverStandingEntry? FindRival(List<DriverStandingEntry> standings)
    {
        var playerIndex = standings.FindIndex(s => s.IsPlayer);
        if (playerIndex < 0) return null;

        if (playerIndex > 0) return standings[playerIndex - 1];
        return standings.Count > 1 ? standings[1] : null;
    }
}
