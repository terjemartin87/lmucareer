namespace LmuCareerTool.Settings;

/// <summary>
/// Alle steder appen lagrer sine egne data. Alt havner i %LOCALAPPDATA%\LmuCareerTool - IKKE
/// ved siden av .exe-filen, som var den forrige oppførselen. Det fungerte fint under
/// `dotnet run`, men en installert app under Program Files (eller til og med under
/// %LOCALAPPDATA%\Programs) er ofte skrivebeskyttet, og appen ville feilet stille med
/// "kan ikke lagre karrieren" for enhver som faktisk installerte den.
/// </summary>
public static class AppPaths
{
    private const string AppFolderName = "LmuCareerTool";

    public static string DataRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppFolderName);

    public static string SettingsFilePath => Path.Combine(DataRoot, "settings.json");

    public static string CareerFilePath(string playerName) =>
        Path.Combine(DataRoot, $"career_{Sanitize(playerName)}.json");

    public static string ContentFilePath => Path.Combine(AppContext.BaseDirectory, "Content", "game-content.json");

    static AppPaths()
    {
        Directory.CreateDirectory(DataRoot);
    }

    private static string Sanitize(string name) =>
        string.Concat(name.Split(Path.GetInvalidFileNameChars())).Replace(' ', '_');
}
