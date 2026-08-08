using System.IO;
using System.Text.Json;
using LmuCareerTool.Settings;

namespace LmuCareerTool.App.Localization;

/// <summary>Leser/skriver det ENE globale språkvalget for hele appen (ikke tilknyttet
/// karriere- eller liga-profiler - de har sine egne separate innstillingsfiler).
/// Mangler filen (nytt/eksisterende installer), standard er norsk - eksisterende
/// installasjoner endrer altså ikke oppførsel med mindre brukeren aktivt bytter.</summary>
public static class LanguageStore
{
    private static string FilePath => Path.Combine(AppPaths.DataRoot, "language.json");

    private class LanguageFile
    {
        public string Language { get; set; } = "nb";
    }

    public static AppLanguage Load()
    {
        if (!File.Exists(FilePath)) return AppLanguage.Norwegian;
        try
        {
            var json = File.ReadAllText(FilePath);
            var file = JsonSerializer.Deserialize<LanguageFile>(json);
            return string.Equals(file?.Language, "en", StringComparison.OrdinalIgnoreCase)
                ? AppLanguage.English
                : AppLanguage.Norwegian;
        }
        catch
        {
            return AppLanguage.Norwegian;
        }
    }

    public static void Save(AppLanguage language)
    {
        var file = new LanguageFile { Language = language == AppLanguage.English ? "en" : "nb" };
        File.WriteAllText(FilePath, JsonSerializer.Serialize(file, new JsonSerializerOptions { WriteIndented = true }));
    }
}
