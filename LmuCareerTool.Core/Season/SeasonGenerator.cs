using LmuCareerTool.Content;

namespace LmuCareerTool.Season;

public static class SeasonGenerator
{
    /// <summary>
    /// Genererer en ny sesong for gitt bilklasse. Antall løp og fordelingen mellom
    /// Sprint/Endurance bestemmes én gang ved generering (ikke valgfritt per løp).
    /// Hvis klassen har merker definert (f.eks. GT3), brukes bilen fra det valgte
    /// merket for hele sesongen. Ellers trekkes en tilfeldig bil fra klassens flate billiste.
    /// </summary>
    public static SeasonModel Generate(
        GameContent content,
        string carClass,
        string? manufacturer,
        int seasonNumber,
        int raceCount = 9,
        double enduranceRatio = 0.35,
        int? randomSeed = null,
        string? explicitCar = null,
        string? avoidOpeningTrack = null)
    {
        if (content.Tracks.Count == 0)
            throw new InvalidOperationException("Ingen baner definert i game-content.json.");

        var classDef = content.Classes.FirstOrDefault(c => c.Name.Equals(carClass, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Fant ikke klassen '{carClass}' i game-content.json.");

        var rng = randomSeed.HasValue ? new Random(randomSeed.Value) : new Random();

        List<string> availableCars;
        if (!string.IsNullOrWhiteSpace(explicitCar))
        {
            // Privatlag/fri-agent: bilen er allerede bestemt av tilbudet, ikke av et merke.
            availableCars = new List<string> { explicitCar };
        }
        else if (classDef.Manufacturers.Count > 0)
        {
            var manufacturerDef = classDef.Manufacturers.FirstOrDefault(m =>
                m.Name.Equals(manufacturer, StringComparison.OrdinalIgnoreCase));
            if (manufacturerDef == null || manufacturerDef.Cars.Count == 0)
                throw new InvalidOperationException($"Fant ikke merket '{manufacturer}' (eller det har ingen biler) for klassen '{carClass}'.");
            availableCars = manufacturerDef.Cars;
        }
        else
        {
            if (classDef.Cars.Count == 0)
                throw new InvalidOperationException($"Ingen biler definert for klassen '{carClass}' i game-content.json.");
            availableCars = classDef.Cars;
        }

        var selectedTracks = CalendarBuilder.BuildTrackList(content.Tracks, raceCount, rng, avoidOpeningTrack);

        var enduranceCount = (int)Math.Round(raceCount * enduranceRatio);
        var formats = Enumerable.Repeat(RaceFormat.Endurance, enduranceCount)
            .Concat(Enumerable.Repeat(RaceFormat.Sprint, raceCount - enduranceCount))
            .OrderBy(_ => rng.Next())
            .ToList();

        // Én bil for HELE sesongen (fra valgt merke) - mer "karriere-aktig" enn å bytte hvert løp.
        var seasonCar = availableCars[rng.Next(availableCars.Count)];

        var season = new SeasonModel
        {
            SeasonNumber = seasonNumber,
            CarClass = carClass
        };

        for (var i = 0; i < raceCount; i++)
        {
            var weather = content.WeatherOptions.Count > 0
                ? content.WeatherOptions[rng.Next(content.WeatherOptions.Count)]
                : "Clear";

            season.Events.Add(new SeasonEvent
            {
                RoundNumber = i + 1,
                TrackVenue = selectedTracks[i],
                Format = formats[i],
                AssignedCar = seasonCar,
                AssignedWeather = weather
            });
        }

        return season;
    }
}
