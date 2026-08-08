using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace LmuCareerTool.Settings;

/// <summary>
/// Prøver å finne Le Mans Ultimates installasjonsmappe automatisk via Steam, slik at
/// spilleren slipper å lete etter Results-mappen selv ved førstegangsoppsett.
/// Returnerer null i alle "kunne ikke finne" -tilfeller i stedet for å kaste - dette skal
/// aldri være en hard avhengighet, kun en bekvemmelighet.
/// </summary>
public static class LmuPathLocator
{
    private const string GameFolderName = "Le Mans Ultimate";

    public static string? TryFindResultsFolder()
    {
        var installPath = TryFindGameInstallPath();
        if (installPath == null) return null;

        var resultsPath = Path.Combine(installPath, "UserData", "Log", "Results");
        return Directory.Exists(resultsPath) ? resultsPath : null;
    }

    public static string? TryFindGameInstallPath()
    {
        if (!OperatingSystem.IsWindows()) return null;

        foreach (var library in GetSteamLibraryFolders())
        {
            var candidate = Path.Combine(library, "steamapps", "common", GameFolderName);
            if (Directory.Exists(candidate)) return candidate;
        }

        return null;
    }

    [SupportedOSPlatform("windows")]
    private static IEnumerable<string> GetSteamLibraryFolders()
    {
        var steamPath = GetSteamInstallPath();
        if (steamPath == null) yield break;

        yield return steamPath; // hovedbiblioteket ligger direkte under Steam-mappa

        var vdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(vdfPath)) yield break;

        // libraryfolders.vdf er Valves enkle KeyValues-format. Vi trenger bare "path"-verdiene,
        // så et regex-søk er nok til å slippe en full VDF-parser.
        string text;
        try { text = File.ReadAllText(vdfPath); }
        catch { yield break; }

        foreach (Match m in Regex.Matches(text, "\"path\"\\s*\"([^\"]+)\""))
        {
            var path = m.Groups[1].Value.Replace("\\\\", "\\");
            if (Directory.Exists(path)) yield return path;
        }
    }

    [SupportedOSPlatform("windows")]
    private static string? GetSteamInstallPath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            var path = key?.GetValue("SteamPath") as string;
            if (!string.IsNullOrWhiteSpace(path)) return path.Replace('/', '\\');
        }
        catch { /* ingen tilgang / ikke Windows - ignorer og prøv neste kilde */ }

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam")
                             ?? Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\Steam");
            var path = key?.GetValue("InstallPath") as string;
            if (!string.IsNullOrWhiteSpace(path)) return path;
        }
        catch { /* ignorer */ }

        return null;
    }
}
