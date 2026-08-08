using System.IO;
using System.Windows.Media.Imaging;
using LmuCareerTool.Content;

namespace LmuCareerTool.App;

/// <summary>Laster og gjenbruker BitmapImage-instanser for førerportretter, slik at vi ikke
/// dekoder samme PNG på nytt for hver rad i en tabell.</summary>
public static class AvatarImageCache
{
    private static readonly Dictionary<string, BitmapImage> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static BitmapImage GetForDriver(string driverName, string playerName, int decodePixelWidth = 150)
    {
        var fileName = DriverAvatarResolver.GetAvatarFileName(driverName, playerName);
        return GetByFileName(fileName, decodePixelWidth);
    }

    public static BitmapImage GetByFileName(string fileName, int decodePixelWidth = 150)
    {
        var key = $"{fileName}|{decodePixelWidth}";
        if (Cache.TryGetValue(key, out var cached)) return cached;

        var path = Path.Combine(AppContext.BaseDirectory, "images", "Drivers", fileName);

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.DecodePixelWidth = decodePixelWidth;
        image.UriSource = new Uri(path, UriKind.Absolute);
        image.EndInit();
        image.Freeze();

        Cache[key] = image;
        return image;
    }
}
