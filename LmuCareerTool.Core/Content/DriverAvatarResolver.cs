using System.Text;

namespace LmuCareerTool.Content;

/// <summary>
/// Knytter et førernavn til et portrettbilde i images/Drivers - deg selv til "myself.png",
/// alle andre til en av driver1.png..driver47.png. Mappingen er deterministisk (en stabil
/// hash av navnet, IKKE string.GetHashCode() som randomiseres per prosess i .NET), så samme
/// sjåfør alltid får samme portrett igjen, uten at vi trenger å lagre noe.
/// </summary>
public static class DriverAvatarResolver
{
    public const string SelfPortraitFile = "myself.png";
    private const int DriverPortraitCount = 47;

    /// <summary>Filnavnet (ikke full sti) for førerens portrett, f.eks. "driver12.png".</summary>
    public static string GetAvatarFileName(string driverName, string playerName)
    {
        var normalizedDriver = Normalize(driverName);
        var normalizedPlayer = Normalize(playerName);

        if (string.Equals(normalizedDriver, normalizedPlayer, StringComparison.OrdinalIgnoreCase))
            return SelfPortraitFile;

        var index = (int)(StableHash(normalizedDriver) % DriverPortraitCount) + 1;
        return $"driver{index}.png";
    }

    /// <summary>Full sti til portrettet, gitt mappa der images/Drivers ligger (typisk AppContext.BaseDirectory).</summary>
    public static string GetAvatarPath(string imagesRootFolder, string driverName, string playerName) =>
        Path.Combine(imagesRootFolder, "images", "Drivers", GetAvatarFileName(driverName, playerName));

    private static string Normalize(string rawName) => rawName.Split('#')[0].Trim();

    /// <summary>FNV-1a - samme resultat hver gang for samme streng, på tvers av prosesser og maskiner.</summary>
    private static uint StableHash(string value)
    {
        unchecked
        {
            const uint fnvOffsetBasis = 2166136261;
            const uint fnvPrime = 16777619;

            var hash = fnvOffsetBasis;
            foreach (var b in Encoding.UTF8.GetBytes(value.ToLowerInvariant()))
            {
                hash ^= b;
                hash *= fnvPrime;
            }
            return hash;
        }
    }
}
