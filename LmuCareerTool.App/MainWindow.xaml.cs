using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using LmuCareerTool.Settings;
using LmuCareerTool.Career;
using LmuCareerTool.Models;
using LmuCareerTool.Validation;
using LmuCareerTool.Watching;

namespace LmuCareerTool.App;

public partial class MainWindow : Window
{
    private readonly AppSettingsStore _settingsStore = new(AppPaths.SettingsFilePath);
    private readonly HashSet<string> _processedFiles = new(StringComparer.OrdinalIgnoreCase);

    private CareerEngine? _engine;
    private ResultsWatcher? _watcher;
    private MiniWidgetWindow? _miniWidget;

    private readonly ObservableCollection<SeasonEventRow> _seasonRows = new();
    private readonly ObservableCollection<RaceHistoryRow> _historyRows = new();
    private readonly ObservableCollection<ToastVm> _toasts = new();

    public MainWindow()
    {
        InitializeComponent();
        DarkTitleBarHelper.Apply(this);

        SeasonGrid.ItemsSource = _seasonRows;
        HistoryListBox.ItemsSource = _historyRows;
        RecentHistoryListBox.ItemsSource = _historyRows;
        ToastList.ItemsSource = _toasts;

        var settings = _settingsStore.LoadOrDefault();
        ResultsFolderBox.Text = string.IsNullOrWhiteSpace(settings.ResultsFolder)
            ? (LmuPathLocator.TryFindResultsFolder()
               ?? @"C:\Program Files (x86)\Steam\steamapps\common\Le Mans Ultimate\UserData\Log\Results")
            : settings.ResultsFolder;
        PlayerNameBox.Text = settings.PlayerName;

        if (string.IsNullOrWhiteSpace(settings.PlayerName))
            NavSettings.IsChecked = true; // ingen tidligere oppsett - start på Innstillinger i stedet for et tomt Dashboard
    }

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
            var careerPath = AppPaths.CareerFilePath(playerName);
            _engine = new CareerEngine(playerName, careerPath, AppPaths.ContentFilePath);
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
        RefreshChampionship();

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
        ShowToast("🏁", "Overvåking startet - klar for løp!", "AccentColor");

        NavDashboard.IsChecked = true;
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

            ShowToast("⚠", "Oppsettet stemte ikke - se loggen for detaljer.", "WarnColor");

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
            ShowToast("🏁", $"{weekend.TrackVenue}: P{race.Position} - +{outcome.XpEarned} XP", "AccentColor");
        }

        HandlePostOutcomeEffects(outcome);
    }

    private void HandlePostOutcomeEffects(WeekendProcessingOutcome outcome)
    {
        if (outcome.ContractSalaryEarned > 0)
            Log($"💰 Kontraktlønn: +{outcome.ContractSalaryEarned} cr");

        foreach (var unlocked in outcome.NewUnlocks)
        {
            Log($"🔓 Ny klasse låst opp: {unlocked}!");
            ShowToast("🔓", $"Ny klasse låst opp: {unlocked}!", "AccentGold");
        }

        RefreshHeader();
        RefreshSeason();
        RefreshHistory();
        RefreshChampionship();

        if (outcome.SeasonJustCompleted)
        {
            Log($"🏆 Sesong fullført! {outcome.CompletedSeason?.TotalPoints} poeng sammenlagt.");
            ShowToast("🏆", "Sesong fullført!", "AccentGold");

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

        if (completedSeason != null)
        {
            var report = _engine.BuildSeasonReport(completedSeason, droppedByManufacturer, contractExpired);
            var reportWindow = new SeasonReportWindow(report, _engine.Career.DriverName) { Owner = this };
            reportWindow.ShowDialog();
        }

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
            RefreshChampionship();
        }
    }

    private void Nav_Checked(object sender, RoutedEventArgs e)
    {
        if (DashboardPanel == null) return; // fyres under InitializeComponent før alle paneler finnes

        DashboardPanel.Visibility = ReferenceEquals(sender, NavDashboard) ? Visibility.Visible : Visibility.Collapsed;
        SeasonPanel.Visibility = ReferenceEquals(sender, NavSeason) ? Visibility.Visible : Visibility.Collapsed;
        ChampionshipPanel.Visibility = ReferenceEquals(sender, NavChampionship) ? Visibility.Visible : Visibility.Collapsed;
        HistoryPanel.Visibility = ReferenceEquals(sender, NavHistory) ? Visibility.Visible : Visibility.Collapsed;
        SettingsPanel.Visibility = ReferenceEquals(sender, NavSettings) ? Visibility.Visible : Visibility.Collapsed;

        if (ReferenceEquals(sender, NavChampionship)) RefreshChampionship();
    }

    private void AvatarBorder_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_engine == null) return;
        var window = new DriverProfileWindow(_engine) { Owner = this };
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

    private void MiniWidgetButton_Click(object sender, RoutedEventArgs e)
    {
        if (_engine == null) return;

        if (_miniWidget != null)
        {
            _miniWidget.Close();
            return;
        }

        _miniWidget = new MiniWidgetWindow(_engine);
        _miniWidget.Closed += (_, _) => _miniWidget = null;
        _miniWidget.Show();
    }

    private void RefreshHeader()
    {
        if (_engine == null) return;
        var career = _engine.Career;
        AvatarBrush.ImageSource = AvatarImageCache.GetForDriver(career.DriverName, career.DriverName);
        DriverNameText.Text = $"Fører: {career.DriverName}";
        ClassText.Text = $"Klasse: {career.CurrentClass}   ·   Opplåst: {string.Join(", ", career.UnlockedClasses)}";

        var contract = career.CurrentContract;
        ManufacturerText.Text = contract == null
            ? "Merke: -"
            : contract.IsPrivateerSeat
                ? "Merke: Privatlag (betalt sete)"
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
            _miniWidget?.Refresh();
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

        _miniWidget?.Refresh();
    }

    private void RefreshHistory()
    {
        if (_engine == null) return;
        _historyRows.Clear();
        foreach (var entry in _engine.Career.RaceHistory.AsEnumerable().Reverse())
        {
            _historyRows.Add(new RaceHistoryRow(entry));
        }
    }

    private void RefreshChampionship()
    {
        if (_engine == null) return;
        var season = _engine.Career.CurrentSeason;

        if (season == null)
        {
            ChampionshipSubTitleText.Text = "Ingen aktiv sesong ennå - velg klasse og signer en kontrakt.";
            DriverGrid.ItemsSource = null;
            ManufacturerGrid.ItemsSource = null;
            return;
        }

        ChampionshipSubTitleText.Text = $"Sesong {season.SeasonNumber} ({season.CarClass})   ·   " +
                                         $"{season.CompletedCount} av {season.Events.Count} runder kjørt" +
                                         (season.LockedRosterNames.Count > 0
                                             ? $"   ·   Feltet ble låst med {season.LockedRosterNames.Count} sjåfører etter runde 1"
                                             : "   ·   Feltet låses når runde 1 er fullført");

        var lastRound = season.Events.Where(e => e.Completed).Select(e => e.RoundNumber).DefaultIfEmpty(0).Max();
        var driverStandings = _engine.GetDriverStandings(season);
        var previousDriverStandings = lastRound > 1 ? _engine.GetDriverStandings(season, lastRound - 1) : null;

        var driverRows = new List<DriverStandingRowVm>();
        for (var i = 0; i < driverStandings.Count; i++)
        {
            var entry = driverStandings[i];
            int? previousPosition = null;
            if (previousDriverStandings != null)
            {
                var idx = previousDriverStandings.FindIndex(e => e.Name.Equals(entry.Name, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0) previousPosition = idx + 1;
            }
            driverRows.Add(new DriverStandingRowVm(i + 1, entry, previousPosition, _engine.Career.DriverName));
        }
        DriverGrid.ItemsSource = driverRows;

        var makeStandings = _engine.GetManufacturerStandings(season);
        ManufacturerGrid.ItemsSource = makeStandings.Select((entry, i) => new ManufacturerStandingRowVm(i + 1, entry)).ToList();
    }

    private void HistoryListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is ListBox { SelectedItem: RaceHistoryRow row })
        {
            var detailWindow = new RaceDetailWindow(row.Entry) { Owner = this };
            detailWindow.ShowDialog();
        }
    }

    private void ShowToast(string icon, string message, string accentBrushKey)
    {
        var brush = (Brush)FindResource(accentBrushKey);
        var toast = new ToastVm(icon, message, brush);
        _toasts.Add(toast);

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            _toasts.Remove(toast);
        };
        timer.Start();
    }

    private void Log(string message)
    {
        LogListBox.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
        while (LogListBox.Items.Count > 200) LogListBox.Items.RemoveAt(LogListBox.Items.Count - 1);
    }
}
