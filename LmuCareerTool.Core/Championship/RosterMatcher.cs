namespace LmuCareerTool.Championship;

/// <summary>
/// LMU legger til "#1234" som suffiks når to sjåfører (spillere eller AI) har samme navn.
/// Dette suffikset kan variere fra runde til runde selv for samme sjåfør, så vi normaliserer
/// det bort for å kunne følge samme sjåfør gjennom en hel sesong.
/// </summary>
public static class RosterMatcher
{
    public static string Normalize(string rawName) => rawName.Split('#')[0].Trim();

    public static bool AreSameDriver(string a, string b) =>
        string.Equals(Normalize(a), Normalize(b), StringComparison.OrdinalIgnoreCase);
}
