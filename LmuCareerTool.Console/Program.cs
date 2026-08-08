using LmuCareerTool.Career;
using LmuCareerTool.Content;
using LmuCareerTool.Watching;

namespace LmuCareerTool.ConsoleApp;

public static class Program
{
    // --- Konfigurasjon: juster for din maskin ---
    private const string ResultsFolder =
        @"C:\Program Files (x86)\Steam\steamapps\common\Le Mans Ultimate\UserData\Log\Results";

    private const string PlayerName = "Terje Hognestad"; // ditt LMU-visningsnavn
    private const string CareerFilePath = "career.json";
    // -------------------------------------------------------

    private static readonly HashSet<string> ProcessedFiles = new(StringComparer.OrdinalIgnoreCase);
    private static CareerEngine _engine = null!;

    public static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        if (args.Length >= 1 && args[0].Equals("validate", StringComparison.OrdinalIgnoreCase))
        {
            var folder = args.Length >= 2 ? args[1] : ResultsFolder;
            RunValidate(folder);
            return;
        }

        var contentPath = Path.Combine(AppContext.BaseDirectory, "Content", "game-content.json");
        _engine = new CareerEngine(PlayerName, CareerFilePath, contentPath);
        _engine.SessionIgnored += session =>
            Console.WriteLine($"↪ {session.TrackVenue} ({session.SettingMode}) - teller ikke mot karrieren (kun 'Race Weekend' telles).");

        if (args.Length >= 2 && args[0].Equals("replay", StringComparison.OrdinalIgnoreCase))
        {
            RunReplay(args[1]);
            return;
        }

        if (args.Length >= 1 && args[0].Equals("season", StringComparison.OrdinalIgnoreCase))
        {
            PrintSeasonTable();
            return;
        }

        if (args.Length >= 2 && args[0].Equals("manufacturers", StringComparison.OrdinalIgnoreCase))
        {
            PrintManufacturers(args[1]);
            return;
        }

        if (args.Length >= 2 && args[0].Equals("newseason", StringComparison.OrdinalIgnoreCase))
        {
            var manufacturer = args.Length >= 3 ? args[2] : null;
            _engine.StartNewSeason(args[1], manufacturer);
            Console.WriteLine($"Ny sesong startet for klasse {args[1]}{(manufacturer != null ? $" hos {manufacturer}" : "")}.");
            PrintSeasonTable();
            return;
        }

        if (args.Length >= 3 && args[0].Equals("buy", StringComparison.OrdinalIgnoreCase))
        {
            var success = _engine.TryBuyManufacturer(args[1], args[2]);
            Console.WriteLine(success
                ? $"Kjøpte deg inn hos {args[2]} i {args[1]}! Credits igjen: {_engine.Career.Credits}"
                : "Kjøp feilet - sjekk at merket finnes og at du har nok credits.");
            return;
        }

        RunLive();
    }

    private static void RunValidate(string folder)
    {
        Console.WriteLine($"Validerer game-content.json mot resultatfiler i:\n  {folder}\n");

        if (!Directory.Exists(folder))
        {
            Console.WriteLine("Fant ikke mappen. Sjekk stien, eller oppgi en annen: dotnet run -- validate <mappe>");
            return;
        }

        var contentPath = Path.Combine(AppContext.BaseDirectory, "Content", "game-content.json");
        var content = ContentLoader.Load(contentPath);
        var report = ContentValidator.Validate(content, folder);

        Console.WriteLine($"Skannet {report.FilesScanned} fil(er) ({report.FilesFailed} kunne ikke leses).\n");

        Console.WriteLine($"--- Baner: {report.KnownTrackVenues.Count} kjente, {report.UnknownTrackVenues.Count} ukjente ---");
        foreach (var t in report.UnknownTrackVenues) Console.WriteLine($"  ⚠ UKJENT BANE: \"{t}\"");

        Console.WriteLine();
        Console.WriteLine($"--- Biler: {report.KnownCarTypes.Count} kjente, {report.UnknownCarTypes.Count} ukjente ---");
        foreach (var c in report.UnknownCarTypes) Console.WriteLine($"  ⚠ UKJENT BIL: \"{c}\"");

        Console.WriteLine();
        if (report.UnknownCarTypes.Count == 0 && report.UnknownTrackVenues.Count == 0)
        {
            Console.WriteLine("✅ Alt som ble funnet i resultatfilene dine matcher game-content.json.");
        }
        else
        {
            Console.WriteLine("Legg de ukjente navnene til i game-content.json - kopier skrivemåten EKSAKT som over.");
        }
    }

    private static void RunReplay(string path)
    {
        try
        {
            var outcome = _engine.ProcessFile(path);
            if (outcome != null) PrintOutcome(outcome);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FEIL] {ex.Message}");
        }

        Console.WriteLine();
        Console.WriteLine($"Karriere lagret til: {Path.GetFullPath(CareerFilePath)}");
        Console.WriteLine($"Total XP: {_engine.Career.TotalXp}  (Nivå {_engine.Career.Level})  Rating: {_engine.Career.DriverRating}  Credits: {_engine.Career.Credits}");
    }

    private static void RunLive()
    {
        var career = _engine.Career;
        Console.WriteLine("=== LMU Karriere-tool ===");
        Console.WriteLine($"Fører: {career.DriverName}  (Nivå {career.Level}, {career.TotalXp} XP, Rating {career.DriverRating}, {career.Credits} credits)");
        Console.WriteLine($"Klasse: {career.CurrentClass}   Merke: {career.CurrentManufacturer ?? "-"}   Opplåste klasser: {string.Join(", ", career.UnlockedClasses)}");
        Console.WriteLine($"Overvåker: {ResultsFolder}");

        if (career.CurrentSeason == null)
        {
            Console.WriteLine();
            Console.WriteLine("Ingen aktiv sesong. Start en med:");
            Console.WriteLine($"  dotnet run -- manufacturers {career.CurrentClass}   (se tilgjengelige merker)");
            Console.WriteLine($"  dotnet run -- newseason {career.CurrentClass} <Merke>");
            return;
        }

        PrintSeasonTable();

        if (Directory.Exists(ResultsFolder))
        {
            var existing = Directory.GetFiles(ResultsFolder, "*.xml").OrderBy(f => f).ToList();
            Console.WriteLine();
            Console.WriteLine($"Fant {existing.Count} eksisterende fil(er) (indekseres, trigger ikke XP).");
            foreach (var file in existing)
            {
                if (!ProcessedFiles.Add(file)) continue;
                try { _engine.IndexExistingFile(file); }
                catch (Exception ex) { Console.WriteLine($"  (kunne ikke lese {Path.GetFileName(file)}: {ex.Message})"); }
            }
        }

        using var watcher = new ResultsWatcher(ResultsFolder);
        watcher.NewResultFile += path =>
        {
            if (!ProcessedFiles.Add(path)) return;
            try
            {
                var outcome = _engine.ProcessFile(path);
                if (outcome != null) PrintOutcome(outcome);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FEIL] Klarte ikke å parse {Path.GetFileName(path)}: {ex.Message}");
            }
        };
        watcher.Start();

        Console.WriteLine();
        Console.WriteLine("Venter på nye løpsresultater... (kjør et Race Weekend i LMU)");
        Console.WriteLine("Trykk Enter for å avslutte.");
        Console.ReadLine();
    }

    private static void PrintManufacturers(string carClass)
    {
        var manufacturers = _engine.GetManufacturersForClass(carClass);
        if (manufacturers.Count == 0)
        {
            Console.WriteLine($"Klassen '{carClass}' har ingen merke-oppsett (bruker flat billiste i stedet).");
            return;
        }

        Console.WriteLine($"--- Merker i {carClass} (din Rating: {_engine.Career.DriverRating}, credits: {_engine.Career.Credits}) ---");
        foreach (var m in manufacturers)
        {
            var status = m.Unlocked ? "OPPLÅST" : $"LÅST (krever Rating {m.RatingRequired}, eller kjøp for {m.UnlockCost} credits)";
            Console.WriteLine($"  {m.Name,-15} {status}");
        }
    }

    private static void PrintOutcome(WeekendProcessingOutcome outcome)
    {
        var weekend = outcome.Weekend;
        if (weekend.RaceResult == null)
        {
            Console.WriteLine($"[Advarsel] Fant ikke '{PlayerName}' i raceresultatet for {weekend.TrackVenue}. Sjekk PlayerName.");
            return;
        }

        var race = weekend.RaceResult;
        Console.WriteLine();
        Console.WriteLine($"=== RACE WEEKEND: {weekend.TrackVenue} ===");

        if (weekend.BestPracticeResult != null)
        {
            var p = weekend.BestPracticeResult;
            Console.WriteLine();
            Console.WriteLine("--- Practice ---");
            Console.WriteLine($"Beste runde: {FormatTime(p.BestLapTime)}");
            Console.WriteLine($"Beste sektorer: S1 {FormatTime(p.BestSector1)}  S2 {FormatTime(p.BestSector2)}  S3 {FormatTime(p.BestSector3)}");
        }

        if (weekend.QualifyingResult != null)
        {
            var q = weekend.QualifyingResult;
            Console.WriteLine();
            Console.WriteLine("--- Qualifying ---");
            Console.WriteLine($"Plassering:  P{q.Position}  (klasse: P{q.ClassPosition})");
            Console.WriteLine($"Beste runde: {FormatTime(q.BestLapTime)}");
            Console.WriteLine($"Beste sektorer: S1 {FormatTime(q.BestSector1)}  S2 {FormatTime(q.BestSector2)}  S3 {FormatTime(q.BestSector3)}");
        }

        Console.WriteLine();
        Console.WriteLine("--- Race ---");
        Console.WriteLine($"Bil brukt:   {race.CarType}");
        Console.WriteLine($"Plassering:  P{race.Position} av {weekend.TotalParticipants} (startet P{race.GridPos})");
        Console.WriteLine($"Status:      {race.FinishStatus}{(race.DnfReason != null ? $" ({race.DnfReason})" : "")}");
        Console.WriteLine($"Beste runde: {FormatTime(race.BestLapTime)}");
        Console.WriteLine($"Beste sektorer: S1 {FormatTime(race.BestSector1)}  S2 {FormatTime(race.BestSector2)}  S3 {FormatTime(race.BestSector3)}");
        Console.WriteLine($"Hendelser:   {race.IncidentCount} incidents, {race.PenaltyCount} straffer");

        if (outcome.Issues.Count > 0)
        {
            Console.WriteLine();
            foreach (var issue in outcome.Issues)
                Console.WriteLine($"⚠️  {issue}");

            if (outcome.CanApproveAnyway)
            {
                Console.WriteLine("    (Kan godkjennes manuelt i desktop-appen - ikke støttet i konsollverktøyet ennå.)");
            }
        }
        else
        {
            Console.WriteLine($"XP: +{outcome.XpEarned}   Poeng: +{outcome.PointsEarned}   Rating: {outcome.RatingDelta:+0;-0;0}   Credits: +{outcome.CreditsEarned}");
        }

        Console.WriteLine($"Total XP: {_engine.Career.TotalXp} (Nivå {_engine.Career.Level})   Rating: {_engine.Career.DriverRating}   Credits: {_engine.Career.Credits}");

        if (outcome.MatchedEvent != null && !outcome.CarMismatch)
            Console.WriteLine($"Sesong:      Runde {outcome.MatchedEvent.RoundNumber} markert som fullført ✔");
        else if (outcome.MatchedEvent == null)
            Console.WriteLine("Sesong:      Dette løpet var utenfor gjeldende sesongkalender (frikjøring) - telles fortsatt som XP.");

        foreach (var unlocked in outcome.NewUnlocks)
            Console.WriteLine($"🔓 NY KLASSE LÅST OPP: {unlocked}!");

        foreach (var offer in outcome.NewManufacturerOffers)
            Console.WriteLine($"📩 {offer} har lagt merke til deg og tilbyr deg kontrakt! (bruk 'newseason' neste sesong for å bytte)");

        if (outcome.SeasonJustCompleted && outcome.CompletedSeason is { } completed)
        {
            Console.WriteLine();
            Console.WriteLine($"🏆 SESONG {completed.SeasonNumber} FULLFØRT! Sammenlagt: {completed.TotalPoints} poeng, {completed.TotalXp} XP, beste resultat P{completed.BestFinish}.");
            Console.WriteLine($"    Kjør 'dotnet run -- manufacturers <Klasse>' for å se merker, deretter 'dotnet run -- newseason <Klasse> <Merke>'.");
        }

        Console.WriteLine("=========================================");
        PrintSeasonTable();
        Console.WriteLine();
    }

    private static void PrintSeasonTable()
    {
        var season = _engine.Career.CurrentSeason;
        if (season == null) return;

        Console.WriteLine();
        Console.WriteLine($"--- Sesong {season.SeasonNumber} ({season.CarClass}, {_engine.Career.CurrentManufacturer}: {season.Events.FirstOrDefault()?.AssignedCar}) - {season.CompletedCount}/{season.Events.Count} løp - {season.TotalPoints} poeng sammenlagt ---");
        foreach (var ev in season.Events)
        {
            var marker = ev.Completed ? "✔" : (season.NextEvent == ev ? "→" : " ");
            var resultText = ev.Completed ? $"  (P{ev.FinishPos}, +{ev.XpEarned} XP, +{ev.PointsEarned} poeng)" : "";
            Console.WriteLine($" {marker} Runde {ev.RoundNumber,2}: {ev.TrackVenue,-35} [{ev.Format,-9}] {ev.AssignedWeather,-30}{resultText}");
        }

        if (season.NextEvent is { } next)
        {
            Console.WriteLine();
            Console.WriteLine($"Neste løp: Runde {next.RoundNumber} - {next.TrackVenue} - {next.Format}");
            Console.WriteLine($"  Sett opp i LMU: {_engine.Career.CurrentClass} ({next.AssignedCar}), ~{next.SuggestedRaceMinutes} min race, vær: {next.AssignedWeather}");
        }
    }

    private static string FormatTime(double? seconds)
    {
        if (seconds is null or <= 0) return "-";
        var ts = TimeSpan.FromSeconds(seconds.Value);
        return $"{(int)ts.TotalMinutes}:{ts.Seconds:00}.{ts.Milliseconds:000}";
    }
}
