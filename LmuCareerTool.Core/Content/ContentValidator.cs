using LmuCareerTool.Parsing;

namespace LmuCareerTool.Content;

public class ContentValidationReport
{
    public int FilesScanned { get; set; }
    public int FilesFailed { get; set; }

    public List<string> KnownCarTypes { get; set; } = new();
    public List<string> UnknownCarTypes { get; set; } = new();
    public List<string> KnownTrackVenues { get; set; } = new();
    public List<string> UnknownTrackVenues { get; set; } = new();
}

/// <summary>
/// Skanner en Results-mappe og sjekker hvert &lt;CarType&gt;/&lt;TrackVenue&gt; som faktisk
/// dukker opp i spillet mot det som er definert i game-content.json - slik at bil- og
/// banenavn aldri må gjettes på, kun leses av fra dine egne resultatfiler.
/// </summary>
public static class ContentValidator
{
    public static ContentValidationReport Validate(GameContent content, string resultsFolder)
    {
        if (!Directory.Exists(resultsFolder))
            throw new DirectoryNotFoundException($"Fant ikke mappen: {resultsFolder}");

        var knownCars = new HashSet<string>(
            content.Classes.SelectMany(c => c.Cars.Concat(c.Manufacturers.SelectMany(m => m.Cars))),
            StringComparer.OrdinalIgnoreCase);
        var knownTracks = new HashSet<string>(content.Tracks, StringComparer.OrdinalIgnoreCase);

        var seenCars = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenTracks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var report = new ContentValidationReport();

        foreach (var file in Directory.GetFiles(resultsFolder, "*.xml"))
        {
            report.FilesScanned++;
            try
            {
                var session = ResultXmlParser.Parse(file);

                if (!string.IsNullOrWhiteSpace(session.TrackVenue))
                    seenTracks.Add(session.TrackVenue);

                foreach (var driver in session.Drivers)
                {
                    if (!string.IsNullOrWhiteSpace(driver.CarType))
                        seenCars.Add(driver.CarType);
                }
            }
            catch
            {
                report.FilesFailed++;
            }
        }

        foreach (var car in seenCars.OrderBy(c => c, StringComparer.OrdinalIgnoreCase))
            (knownCars.Contains(car) ? report.KnownCarTypes : report.UnknownCarTypes).Add(car);

        foreach (var track in seenTracks.OrderBy(t => t, StringComparer.OrdinalIgnoreCase))
            (knownTracks.Contains(track) ? report.KnownTrackVenues : report.UnknownTrackVenues).Add(track);

        return report;
    }
}
