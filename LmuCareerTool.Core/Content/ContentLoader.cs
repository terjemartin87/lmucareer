using System.Text.Json;

namespace LmuCareerTool.Content;

public static class ContentLoader
{
    public static GameContent Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Fant ikke innholdsfil: {path}");

        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var content = JsonSerializer.Deserialize<GameContent>(json, options);
        return content ?? throw new InvalidOperationException("Klarte ikke å lese innholdsfilen.");
    }
}
