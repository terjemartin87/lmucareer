using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using LmuCareerTool.Models;

namespace LmuCareerTool.Parsing;

public static class ResultXmlParser
{
    // Eksempel filnavn: 2026_08_03_21_57_04-25R1.xml  ->  suffiks "25R1" betyr sesjons-id 25, type Race, heat 1
    private static readonly Regex FileNamePattern = new(
        @"(?<num>\d+)(?<type>[PQR])(?<session>\d+)\.xml$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static SessionResult Parse(string filePath)
    {
        // Prøv å åpne filen med delt lesetilgang og noen retries, i tilfelle
        // LMU fortsatt holder filen låst rett etter at den ble skrevet.
        var doc = LoadWithRetry(filePath);

        var root = doc.Root ?? throw new InvalidOperationException("Tom XML-fil.");
        var raceResults = root.Element("RaceResults")
            ?? throw new InvalidOperationException("Fant ikke <RaceResults>-elementet i resultatfilen.");
        var settingNode = raceResults.Element("Setting");

        // Sesjonens container heter ulike ting avhengig av type: <Practice1>, <Qualify1>, <Race> osv.
        // I stedet for å hardkode alle mulige navn, finner vi elementet som faktisk inneholder
        // sesjonsdata (kjennetegnet ved at det har en <Stream>- eller <Driver>-child).
        var raceNode = raceResults.Elements()
            .FirstOrDefault(e => e.Element("Stream") != null || e.Elements("Driver").Any())
            ?? throw new InvalidOperationException("Fant ingen sesjons-container (Practice/Qualify/Race) i resultatfilen.");

        var result = new SessionResult
        {
            SourceFile = filePath,
            TrackVenue = GetString(raceResults, "TrackVenue"),
            TrackCourse = GetString(raceResults, "TrackCourse"),
            TrackLength = GetDouble(raceResults, "TrackLength") ?? 0,
            SettingMode = settingNode?.Value ?? "",
            RaceTimeMinutes = (int)(GetDouble(raceResults, "RaceTime") ?? 0),
            RaceLaps = (int)(GetDouble(raceResults, "RaceLaps") ?? 0),
            VehiclesAllowed = GetString(raceResults, "VehiclesAllowed"),
        };

        var match = FileNamePattern.Match(Path.GetFileName(filePath));
        if (match.Success)
        {
            result.SessionType = match.Groups["type"].Value.ToUpperInvariant() switch
            {
                "P" => SessionType.Practice,
                "Q" => SessionType.Qualifying,
                "R" => SessionType.Race,
                _ => SessionType.Unknown
            };
            result.SessionNumber = int.Parse(match.Groups["session"].Value);
        }

        // Tidsstempel fra filnavnet (mer robust enn <TimeString>, som kan mangle)
        result.SessionTimeUtc = ParseTimestampFromFileName(filePath);

        foreach (var driverNode in raceNode.Elements("Driver"))
        {
            result.Drivers.Add(ParseDriver(driverNode));
        }

        // Tell hendelser fra <Stream> per sjåfør (matcher på "Navn(ID)"-mønster i tekst/attributter)
        var streamNode = raceNode.Element("Stream");
        if (streamNode != null)
        {
            CountStreamEvents(streamNode, result.Drivers);
        }

        return result;
    }

    private static XDocument LoadWithRetry(string filePath, int maxAttempts = 8, int delayMs = 300)
    {
        Exception? last = null;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                return XDocument.Load(stream);
            }
            catch (IOException ex)
            {
                last = ex;
                Thread.Sleep(delayMs);
            }
        }
        throw new IOException($"Klarte ikke å åpne {filePath} etter {maxAttempts} forsøk.", last);
    }

    private static DriverResult ParseDriver(XElement d)
    {
        var driver = new DriverResult
        {
            Name = GetString(d, "Name"),
            TeamName = GetString(d, "TeamName"),
            CarClass = GetString(d, "CarClass"),
            CarType = GetString(d, "CarType"),
            CarNumber = (int)(GetDouble(d, "CarNumber") ?? 0),
            IsPlayer = GetString(d, "isPlayer") == "1",
            GridPos = (int)(GetDouble(d, "GridPos") ?? 0),
            Position = (int)(GetDouble(d, "Position") ?? 0),
            ClassGridPos = (int)(GetDouble(d, "ClassGridPos") ?? 0),
            ClassPosition = (int)(GetDouble(d, "ClassPosition") ?? 0),
            BestLapTime = GetDouble(d, "BestLapTime"),
            FinishTime = GetDouble(d, "FinishTime"),
            Laps = (int)(GetDouble(d, "Laps") ?? 0),
            Pitstops = (int)(GetDouble(d, "Pitstops") ?? 0),
            FinishStatus = GetString(d, "FinishStatus"),
            DnfReason = d.Element("DNFReason")?.Value,
        };

        foreach (var lapNode in d.Elements("Lap"))
        {
            var lapTimeRaw = lapNode.Value;
            driver.LapData.Add(new LapResult
            {
                LapNumber = (int)(GetAttrDouble(lapNode, "num") ?? 0),
                Position = (int)(GetAttrDouble(lapNode, "p") ?? 0),
                ElapsedTime = GetAttrDouble(lapNode, "et") ?? 0,
                Sector1 = GetAttrDouble(lapNode, "s1"),
                Sector2 = GetAttrDouble(lapNode, "s2"),
                Sector3 = GetAttrDouble(lapNode, "s3"),
                TopSpeed = GetAttrDouble(lapNode, "topspeed"),
                PitThisLap = lapNode.Attribute("pit") != null,
                LapTime = ParseInvariantDoubleOrNull(lapTimeRaw)
            });
        }

        return driver;
    }

    private static void CountStreamEvents(XElement stream, List<DriverResult> drivers)
    {
        // Hendelser i <Stream> refererer til sjåfører som "Navn(ID)" i tekst eller
        // som Driver="Navn"-attributt (TrackLimits, Penalty). Vi matcher på navnet.
        foreach (var driver in drivers)
        {
            var shortName = driver.Name.Split('#')[0].Trim();

            driver.IncidentCount = stream.Elements("Incident")
                .Count(e => e.Value.StartsWith(shortName, StringComparison.OrdinalIgnoreCase));

            driver.PenaltyCount = stream.Elements("Penalty")
                .Count(e => (string?)e.Attribute("Driver") == shortName
                            || e.Value.StartsWith(shortName, StringComparison.OrdinalIgnoreCase));

            driver.TrackLimitWarnings = stream.Elements("TrackLimits")
                .Count(e => (string?)e.Attribute("Driver") == shortName
                            && (string?)e.Attribute("Resolution") != "7"); // "7" = No Further Action
        }
    }

    private static DateTime ParseTimestampFromFileName(string filePath)
    {
        var name = Path.GetFileNameWithoutExtension(filePath);
        // 2026_08_03_21_57_04-25R1  -> ta de 6 første "_"-separerte delene
        var parts = name.Split('-')[0].Split('_');
        if (parts.Length >= 6 &&
            int.TryParse(parts[0], out var y) && int.TryParse(parts[1], out var mo) &&
            int.TryParse(parts[2], out var da) && int.TryParse(parts[3], out var h) &&
            int.TryParse(parts[4], out var mi) && int.TryParse(parts[5], out var se))
        {
            return new DateTime(y, mo, da, h, mi, se, DateTimeKind.Local).ToUniversalTime();
        }
        return File.GetLastWriteTimeUtc(filePath);
    }

    private static string GetString(XElement parent, string name) => parent.Element(name)?.Value ?? "";

    private static double? GetDouble(XElement parent, string name)
        => ParseInvariantDoubleOrNull(parent.Element(name)?.Value);

    private static double? GetAttrDouble(XElement node, string attr)
        => ParseInvariantDoubleOrNull((string?)node.Attribute(attr));

    private static double? ParseInvariantDoubleOrNull(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (raw.Contains('-') && raw.Contains('.') && raw.All(c => c == '-' || c == '.')) return null; // "--.----"
        return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;
    }
}
