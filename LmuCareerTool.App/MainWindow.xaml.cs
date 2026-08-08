using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using LmuCareerTool.Settings;
using LmuCareerTool.Career;
using LmuCareerTool.Models;
using LmuCareerTool.Validation;
using LmuCareerTool.Watching;

namespace LmuCareerTool.App;

public partial class MainWindow : Window
{
    private readonly AppSettingsStore _settingsStore = new(GetSettingsPath());
    private readonly HashSet<string> _processedFiles = new(StringComparer.OrdinalIgnoreCase);

    private CareerEngine? _engine;
    private ResultsWatcher? _watcher;

    private readonly ObservableCollection<SeasonEventRow> _seasonRows = new();
    private readonly ObservableCollection<RaceHistoryRow> _historyRows = new();

    public MainWindow()
    {
        InitializeComponent();
        SeasonGrid.ItemsSource = _seasonRows;
        HistoryListBox.ItemsSource = _historyRows;

        var settings = _settingsStore.LoadOrDefault();
        ResultsFolderBox.Text = string.IsNullOrWhiteSpace(settings.ResultsFolder)
            ? (LmuPathLocator.TryFindResultsFolder()
               ?? @"C:\Program Files (x86)\Steam\steamapps\common\Le Mans Ultimate\UserData\Log\Results")
            : settings.ResultsFolder;
        PlayerNameBox.Text = settings.PlayerName;
    }

    private static string GetSettingsPath() =>
        Path.Combine(AppContext.BaseDirectory, "settings.json");

    private void StartStopButton_Click(object sender, RoutedEventArgs e)
    {
        if (_watcher == null)
            StartWatching();
        else
            StopWatching();
    }

    private void StartWatching()
    {
        var resultsFolder = ResultsFolderBox.Text.Trim();
        var playerName = PlayerNameBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(resultsFolder) || string.IsNullOrWhiteSpace(playerName))
        {
            MessageBox.Show(this, "Fyll ut både Results-mappe og visningsnavn først.", "Mangler info",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!Directory.Exists(resultsFolder))
        {
            MessageBox.Show(this, $"Fant ikke mappen:\n{resultsFolder}", "Feil mappe",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        _settingsStore.Save(new AppSettings { ResultsFolder = resultsFolder, PlayerName = playerName });

        try
        {
            var contentPath = Path.Combine(AppContext.BaseDirectory, "Content", "game-content.json");
            var careerPath = Path.Combine(AppContext.BaseDirectory, $"career_{Sanitize(playerName)}.json");
            _engine = new CareerEngine(playerName, careerPath, contentPath);
            _engine.SessionIgnored += OnSessionIgnored;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Klarte ikke å starte: {ex.Message}", "Feil",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        RefreshHeader();
        RefreshSeason();
        RefreshHistory();

        // Edge case: appen ble kanskje lukket rett etter forrige sesong ble fullført,
        // før du fikk valgt klasse for neste. Be om det nå før vi starter overvåking.
        if (_engine.Career.CurrentSeason == null)
        {
            ShowSeasonSummaryAndPickNext(null);
        }

        // Indekser eksisterende filer først (fyller Practice/Qualifying-cache)
        var existing = Directory.GetFiles(resultsFolder, "*.xml").OrderBy(f => f).ToList();
        Log($"Fant {existing.Count} eksisterende fil(er), indekserer...");
        foreach (var file in existing)
        {
            if (!_processedFiles.Add(file)) continue;
            try { _engine.IndexExistingFile(file); }
            catch { /* ignorer filer som ikke er sesjonsresultater */ }
        }

        _watcher = new ResultsWatcher(resultsFolder);
        _watcher.NewResultFile += OnNewResultFile;
        _watcher.Start();

        ResultsFolderBox.IsEnabled = false;
        PlayerNameBox.IsEnabled = false;
        StartStopButton.Content = "Stopp overvåking";
        StatusText.Text = $"Overvåker: {resultsFolder}";
        Log("Venter på nye løpsresultater...");
    }

    private void StopWatching()
    {
        _watcher?.Dispose();
        _watcher = null;

        ResultsFolderBox.IsEnabled = true;
        PlayerNameBox.IsEnabled = true;
        StartStopButton.Content = "Start overvåking";
        StatusText.Text = "Stoppet.";
        Log("Sluttet å overvåke.");
    }

    private void OnSessionIgnored(SessionResult session)
    {
        Log($"↪ {session.TrackVenue} ({session.SettingMode}) - teller ikke mot karrieren (kun 'Race Weekend' telles).");
    }

    private void OnNewResultFile(string path)
    {
        if (!_processedFiles.Add(path)) return;

        Dispatcher.Invoke(() =>
        {
            Log($"Ny fil: {Path.GetFileName(path)}");

            try
            {
                var outcome = _engine!.ProcessFile(path);
                if (outcome != null) HandleOutcome(outcome);
            }
            catch (Exception ex)
            {
                Log($"[FEIL] Klarte ikke å lese filen: {ex.Message}");
            }
        });
    }

    private void HandleOutcome(WeekendProcessingOutcome outcome)
    {
        var weekend = outcome.Weekend;
        if (weekend.RaceResult == null)
        {
            Log($"⚠ Fant ikke deg i resultatet for {weekend.TrackVenue}. Sjekk visningsnavnet.");
            return;
        }

        var race = weekend.RaceResult;

        if (outcome.Issues.Count > 0)
        {
            foreach (var issue in outcome.Issues)
                Log($"⚠ {issue}");

            if (outcome.CanApproveAnyway)
            {
                var result = MessageBox.Show(this,
                    $"{string.Join("\n\n", outcome.Issues)}\n\nGodkjenne runden likevel med dette resultatet (P{race.Position})?",
                    "Oppsett stemte ikke", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    var approved = _engine!.ApproveDespiteMismatch(outcome);
                    Log($"✅ Godkjent manuelt: {weekend.TrackVenue} - +{approved.XpEarned} XP, +{approved.PointsEarned} poeng, " +
                        $"Rating {approved.RatingDelta:+0;-0;0}, +{approved.CreditsEarned} cr");
                    HandlePostOutcomeEffects(approved);
                    return;
                }
            }
        }
        else
        {
            Log($"🏁 {weekend.TrackVenue}: P{race.Position} av {weekend.TotalParticipants} - " +
                $"+{outcome.XpEarned} XP, +{outcome.PointsEarned} poeng, Rating {outcome.RatingDelta:+0;-0;0}, +{outcome.CreditsEarned} cr");
        }

        HandlePostOutcomeEffects(outcome);
    }

    private void HandlePostOutcomeEffects(WeekendProcessingOutcome outcome)
    {
        if (outcome.ContractSalaryEarned > 0)
            Log($"💰 Kontraktlønn: +{outcome.ContractSalaryEarned} cr");

        foreach (var unlocked in outcome.NewUnlocks)
            Log($"🔓 Ny klasse låst opp: {unlocked}!");

        RefreshHeader();
        RefreshSeason();
        RefreshHistory();

        if (outcome.SeasonJustCompleted)
        {
            Log($"🏆 Sesong fullført! {outcome.CompletedSeason?.TotalPoints} poeng sammenlagt.");

            if (outcome.DroppedByManufacturer)
                Log("📉 Merket var ikke fornøyd med resultatene og har sagt opp kontrakten din.");
            else if (outcome.ContractExpired)
                Log("📄 Kontrakten din har løpt ut. Tid for et nytt tilbud.");

            ShowSeasonSummaryAndPickNext(outcome.CompletedSeason, outcome.DroppedByManufacturer, outcome.ContractExpired);
        }
    }

    private void ShowSeasonSummaryAndPickNext(
        LmuCareerTool.Season.SeasonModel? completedSeason,
        bool droppedByManufacturer = false, bool contractExpired = false)
    {
        if (_engine == null) return;

        var dialog = new SeasonSummaryWindow(completedSeason, _engine, droppedByManufacturer, contractExpired) { Owner = this };

        if (dialog.ShowDialog() == true)
        {
            var contract = _engine.Career.CurrentContract;
            Log(contract == null
                ? $"Ny sesong startet: {_engine.Career.CurrentClass}"
                : $"Ny sesong startet: {_engine.Career.CurrentClass}" +
                  (contract.IsPrivateerSeat ? " (privatlag)"
                   : contract.IsFreeAgent ? ""
                   : $" hos {contract.Manufacturer} ({contract.SeasonsRemaining} sesong(er) igjen, {contract.SalaryPerRound} cr/runde)"));
            RefreshHeader();
            RefreshSeason();
        }
    }

    private void ChampionshipButton_Click(object sender, RoutedEventArgs e)
    {
        if (_engine == null)
        {
            MessageBox.Show(this, "Start overvåking først.", "Ingen aktiv karriere", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var window = new ChampionshipWindow(_engine) { Owner = this };
        window.ShowDialog();
    }

    private void CopyRecipeButton_Click(object sender, RoutedEventArgs e)
    {
        var next = _engine?.Career.CurrentSeason?.NextEvent;
        if (_engine == null || next == null) return;

        var text = PreRaceChecklist.BuildRecipeText(next, _engine.Career.CurrentClass, _engine.Career.CurrentManufacturer);
        Clipboard.SetText(text);
        Log("📋 Oppskrift for neste løp kopiert til utklippstavlen.");
    }

    private void RefreshHeader()
    {
        if (_engine == null) return;
        var career = _engine.Career;
        DriverNameText.Text = $"Fører: {career.DriverName}";
        ClassText.Text = $"Klasse: {career.CurrentClass}   ·   Opplåst: {string.Join(", ", career.UnlockedClasses)}";

        var contract = career.CurrentContract;
        ManufacturerText.Text = contract == null
            ? "Merke: -"
            : contract.IsPrivateerSeat
                ? $"Merke: Privatlag (betalt sete)"
                : contract.IsFreeAgent
                    ? "Merke: Fri kjøring (ingen merke-oppsett i klassen)"
                    : $"Merke: {contract.Manufacturer}   ·   {contract.SeasonsRemaining} sesong(er) igjen   ·   " +
                      $"{contract.SalaryPerRound} cr/runde   ·   Mål: {contract.GoalDescription}";
        LevelText.Text = career.Level.ToString();
        XpText.Text = career.TotalXp.ToString();
        SeasonPointsText.Text = (career.CurrentSeason?.TotalPoints ?? 0).ToString();
        RatingText.Text = career.DriverRating.ToString();
        CreditsText.Text = $"{career.Credits} cr";
    }

    private void RefreshSeason()
    {
        var season = _engine?.Career.CurrentSeason;
        if (season == null)
        {
            _seasonRows.Clear();
            NextRaceTitle.Text = "Venter på valg av klasse...";
            NextRaceDetail.Text = "-";
            CopyRecipeButton.IsEnabled = false;
            return;
        }

        _seasonRows.Clear();
        foreach (var ev in season.Events)
        {
            _seasonRows.Add(new SeasonEventRow(ev, isNext: season.NextEvent == ev));
        }

        if (season.NextEvent is { } next)
        {
            NextRaceTitle.Text = $"Runde {next.RoundNumber}: {next.TrackVenue} ({next.Format})";
            NextRaceDetail.Text = $"Sett opp i LMU: {_engine!.Career.CurrentClass} - {next.AssignedCar}   ·   " +
                                   $"~{next.SuggestedRaceMinutes} min race   ·   Vær: {next.AssignedWeather}";
            CopyRecipeButton.IsEnabled = true;
        }
        else
        {
            CopyRecipeButton.IsEnabled = false;
        }
    }

    private void RefreshHistory()
    {
        if (_engine == null) return;
        _historyRows.Clear();
        foreach (var entry in _engine.Career.RaceHistory.AsEnumerable().Reverse().Take(20))
        {
            _historyRows.Add(new RaceHistoryRow(entry));
        }
    }

    private void HistoryListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (HistoryListBox.SelectedItem is RaceHistoryRow row)
        {
            var detailWindow = new RaceDetailWindow(row.Entry) { Owner = this };
            detailWindow.ShowDialog();
        }
    }

    private void Log(string message)
    {
        LogListBox.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
        while (LogListBox.Items.Count > 200) LogListBox.Items.RemoveAt(LogListBox.Items.Count - 1);
    }

    private static string Sanitize(string name) =>
        string.Concat(name.Split(Path.GetInvalidFileNameChars())).Replace(' ', '_');
}
