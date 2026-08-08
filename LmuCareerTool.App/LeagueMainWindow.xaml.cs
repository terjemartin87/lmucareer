using System.IO;
using System.Windows;
using System.Windows.Controls;
using LmuCareerTool.League;
using LmuCareerTool.Models;
using LmuCareerTool.Settings;
using LmuCareerTool.Watching;
using Microsoft.Win32;

namespace LmuCareerTool.App;

public class CalendarRowVm
{
    public int Round { get; set; }
    public string Track { get; set; } = "";
    public string Format { get; set; } = "";
    public string Status { get; set; } = "";
    public string Winner { get; set; } = "";
}

public class PenaltyRowVm
{
    public int Round { get; set; }
    public string Driver { get; set; } = "";
    public string Consequence { get; set; } = "";
    public string Reason { get; set; } = "";
}

public class LeagueDriverStandingRowVm
{
    public int Position { get; set; }
    public string Name { get; set; } = "";
    public string Team { get; set; } = "";
    public int Points { get; set; }
    public int Wins { get; set; }
    public int Podiums { get; set; }
    public int Top5 { get; set; }
    public int Top10 { get; set; }
    public string PenaltyPoints { get; set; } = "-";
}

public class LeagueManufacturerStandingRowVm
{
    public int Position { get; set; }
    public string Manufacturer { get; set; } = "";
    public int Points { get; set; }
    public int Wins { get; set; }
}

/// <summary>
/// Hovedvinduet for Liga modus - helt uavhengig av karrierens MainWindow (ingen delt state).
/// Samme overvåkings-mønster (ResultsWatcher + indeksering av eksisterende filer), men mot
/// LeagueEngine i stedet for CareerEngine, og uten XP/Rating/Credits/kontrakter.
/// </summary>
public partial class LeagueMainWindow : Window
{
    private readonly AppSettingsStore _settingsStore = new(AppPaths.LeagueSettingsFilePath);
    private readonly HashSet<string> _processedFiles = new(StringComparer.OrdinalIgnoreCase);

    private LeagueEngine? _engine;
    private ResultsWatcher? _watcher;
    private string _leagueName = "";
    private string _hostName = "";

    public LeagueMainWindow()
    {
        InitializeComponent();
        DarkTitleBarHelper.Apply(this);

        FormatBox.ItemsSource = Enum.GetValues<LeagueFormatPreference>();
        FormatBox.SelectedIndex = 0;
    }

    /// <summary>Brukes av LeagueWelcomeWindow - fyller ut oppsettet og starter overvåking automatisk.</summary>
    public LeagueMainWindow(string resultsFolder, string leagueName, string hostName) : this()
    {
        ResultsFolderBox.Text = resultsFolder;
        _leagueName = leagueName;
        _hostName = hostName;

        Loaded += LeagueMainWindow_AutoStart;
    }

    private void LeagueMainWindow_AutoStart(object sender, RoutedEventArgs e)
    {
        Loaded -= LeagueMainWindow_AutoStart;
        StartWatching();
    }

    private void StartStopButton_Click(object sender, RoutedEventArgs e)
    {
        if (_watcher == null)
            StartWatching();
        else
            StopWatching();
    }

    private void BrowseResultsFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Velg LMU Results-mappe" };
        if (Directory.Exists(ResultsFolderBox.Text))
            dialog.InitialDirectory = ResultsFolderBox.Text;

        if (dialog.ShowDialog(this) == true)
            ResultsFolderBox.Text = dialog.FolderName;
    }

    private void StartWatching()
    {
        var resultsFolder = ResultsFolderBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(resultsFolder) || !Directory.Exists(resultsFolder))
        {
            MessageBox.Show(this, $"Fant ikke mappen:\n{resultsFolder}", "Feil mappe",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        _settingsStore.Save(new AppSettings { ResultsFolder = resultsFolder, PlayerName = _leagueName });

        try
        {
            var leaguePath = AppPaths.LeagueFilePath(_leagueName);
            _engine = new LeagueEngine(_leagueName, _hostName, leaguePath, AppPaths.ContentFilePath);
            _engine.SessionIgnored += OnSessionIgnored;
            _engine.RoundCompleted += OnRoundCompleted;

            _hostName = _engine.League.HostDisplayName;
            CarClassBox.ItemsSource = _engine.Content.Classes.Select(c => c.Name).ToList();
            if (CarClassBox.Items.Count > 0) CarClassBox.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Klarte ikke å starte: {ex.Message}", "Feil",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        RefreshAll();

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
        StartStopButton.Content = "Stopp overvåking";
        StatusText.Text = $"Overvåker: {resultsFolder}";
        Log("Venter på nye løpsresultater fra Multiplayer-økter...");
        SetDashboardStatus("🟢", "Overvåker Results-mappen. Kjør et hostet løp (Multiplayer) i LMU - resultatet dukker opp her automatisk når det er ferdig.");

        NavDashboard.IsChecked = true;
    }

    private void StopWatching()
    {
        _watcher?.Dispose();
        _watcher = null;
        SetDashboardStatus("⏸", "Overvåking stoppet. Trykk Start overvåking i Innstillinger for å fortsette.");

        ResultsFolderBox.IsEnabled = true;
        StartStopButton.Content = "Start overvåking";
        StatusText.Text = "Stoppet.";
        Log("Sluttet å overvåke.");
    }

    private void OnSessionIgnored(SessionResult session)
    {
        Log($"↪ {session.TrackVenue} ({session.SettingMode}) - teller ikke mot ligaen (kun 'Multiplayer' telles).");
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

    private void OnRoundCompleted(LeagueRoundOutcome outcome) { /* HandleOutcome dekker UI-oppdateringen */ }

    private void HandleOutcome(LeagueRoundOutcome outcome)
    {
        var weekend = outcome.Weekend;

        if (outcome.MatchedRound == null && outcome.CandidateRound != null)
        {
            Log($"⚠ {weekend.TrackVenue} matchet ikke neste runde ({outcome.CandidateRound.TrackVenue}).");

            var result = MessageBox.Show(this,
                $"Løpet ble kjørt på {weekend.TrackVenue}, men neste runde i kalenderen er {outcome.CandidateRound.TrackVenue}.\n\n" +
                "Godkjenne runden likevel med dette resultatet?",
                "Bane stemte ikke", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                var approved = _engine!.ApproveDespiteMismatch(outcome);
                Log($"✅ Godkjent manuelt: runde {approved.MatchedRound?.RoundNumber} - {weekend.TrackVenue}, {weekend.TotalParticipants} deltakere.");
                RefreshAll();
                if (approved.SeasonJustCompleted) AnnounceSeasonComplete();
            }
            return;
        }

        if (outcome.MatchedRound == null)
        {
            Log($"↪ {weekend.TrackVenue}: fullført løp registrert, men ingen aktiv sesong å knytte det til.");
            return;
        }

        Log($"🏁 Runde {outcome.MatchedRound.RoundNumber} fullført: {weekend.TrackVenue} - {weekend.TotalParticipants} deltakere.");
        RefreshAll();

        if (outcome.SeasonJustCompleted) AnnounceSeasonComplete();
    }

    private void AnnounceSeasonComplete()
    {
        Log("🏆 Sesongen er fullført! Generer en ny sesong fra Innstillinger når dere er klare.");
    }

    private void GenerateSeasonButton_Click(object sender, RoutedEventArgs e)
    {
        if (_engine == null)
        {
            MessageBox.Show(this, "Start overvåking først.", "Ingen aktiv liga", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_engine.League.CurrentSeason is { IsComplete: false })
        {
            var confirm = MessageBox.Show(this,
                "Det finnes allerede en pågående sesong som ikke er fullført. Generere en ny sesong vil erstatte den (den fullførte historikken beholdes ikke).\n\nFortsette?",
                "Pågående sesong", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;
        }

        if (CarClassBox.SelectedItem is not string carClass)
        {
            MessageBox.Show(this, "Velg en klasse først.", "Mangler klasse", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(RoundCountBox.Text, out var roundCount) || roundCount < 1)
        {
            MessageBox.Show(this, "Antall løp må være et positivt tall.", "Ugyldig antall", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var format = (LeagueFormatPreference)FormatBox.SelectedItem;

        _engine.GenerateNewSeason(carClass, roundCount, format);
        Log($"📅 Ny sesong generert: {carClass}, {roundCount} runder, {format}.");
        RefreshAll();
        NavCalendar.IsChecked = true;
    }

    private void PublishButton_Click(object sender, RoutedEventArgs e)
    {
        if (_engine == null)
        {
            MessageBox.Show(this, "Start overvåking først.", "Ingen aktiv liga", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Publiser ligastilling",
            Filter = "HTML-fil (*.html)|*.html",
            FileName = $"{_leagueName}-stilling.html",
        };

        if (dialog.ShowDialog(this) != true) return;

        try
        {
            _engine.PublishSnapshot(dialog.FileName);
            Log($"📤 Publisert: {dialog.FileName}");
            MessageBox.Show(this, "Ligastillingen er publisert. Del filen med hvem du vil - den er statisk og read-only.",
                "Publisert", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Klarte ikke å publisere: {ex.Message}", "Feil", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyPenaltyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_engine == null) return;

        if (PenaltyRoundBox.SelectedItem is not int round)
        {
            MessageBox.Show(this, "Velg en runde.", "Mangler runde", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var driver = (PenaltyDriverBox.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(driver))
        {
            MessageBox.Show(this, "Velg eller skriv inn en fører.", "Mangler fører", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(PenaltyPointsBox.Text, out var pointsDeducted) || pointsDeducted < 0)
            pointsDeducted = 0;

        var reason = (PenaltyReasonBox.Text ?? "").Trim();

        _engine.ApplyPenalty(round, driver, pointsDeducted, PenaltyDsqBox.IsChecked == true, reason);
        Log($"🚩 Straff gitt: {driver} (runde {round}) - " +
            (PenaltyDsqBox.IsChecked == true ? "diskvalifisert" : $"-{pointsDeducted} poeng"));

        PenaltyReasonBox.Text = "";
        PenaltyPointsBox.Text = "0";
        PenaltyDsqBox.IsChecked = false;

        RefreshAll();
    }

    private void DriverGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_engine == null || DriverGrid.SelectedItem is not LeagueDriverStandingRowVm row) return;

        var history = LeagueStandingsCalculator.BuildDriverHistory(_engine.League, row.Name);
        MessageBox.Show(this,
            $"Løp: {history.TotalRaces}\nPoeng totalt: {history.TotalPoints}\nSeire: {history.Wins}\n" +
            $"Podier: {history.Podiums}\nTop 5: {history.Top5}\nTop 10: {history.Top10}\n" +
            $"Beste plassering: {(history.BestFinish > 0 ? $"P{history.BestFinish}" : "-")}",
            $"Historikk - {history.Name}", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Nav_Checked(object sender, RoutedEventArgs e)
    {
        if (DashboardPanel == null) return;

        DashboardPanel.Visibility = ReferenceEquals(sender, NavDashboard) ? Visibility.Visible : Visibility.Collapsed;
        StandingsPanel.Visibility = ReferenceEquals(sender, NavStandings) ? Visibility.Visible : Visibility.Collapsed;
        CalendarPanel.Visibility = ReferenceEquals(sender, NavCalendar) ? Visibility.Visible : Visibility.Collapsed;
        PenaltiesPanel.Visibility = ReferenceEquals(sender, NavPenalties) ? Visibility.Visible : Visibility.Collapsed;
        SettingsPanel.Visibility = ReferenceEquals(sender, NavSettings) ? Visibility.Visible : Visibility.Collapsed;

        if (ReferenceEquals(sender, NavStandings)) RefreshStandings();
        if (ReferenceEquals(sender, NavCalendar)) RefreshCalendar();
        if (ReferenceEquals(sender, NavPenalties)) RefreshPenalties();
    }

    private void RefreshAll()
    {
        RefreshHeader();
        RefreshStandings();
        RefreshCalendar();
        RefreshPenalties();
    }

    private void RefreshHeader()
    {
        if (_engine == null) return;

        LeagueNameNavText.Text = _engine.League.LeagueName.ToUpperInvariant();
        HeaderLeagueNameText.Text = $"Liga: {_engine.League.LeagueName}";

        var season = _engine.League.CurrentSeason;
        if (season == null)
        {
            HeaderSeasonText.Text = "Ingen aktiv sesong - generer en fra Innstillinger.";
            RoundsText.Text = "0 / 0";
            LeaderText.Text = "-";
            return;
        }

        HeaderSeasonText.Text = $"Sesong {season.SeasonNumber} - {season.CarClass}";
        RoundsText.Text = $"{season.CompletedCount} / {season.Rounds.Count}";

        var leader = LeagueStandingsCalculator.ComputeDriverStandings(season).FirstOrDefault();
        LeaderText.Text = leader != null ? $"{leader.Name} ({leader.Points}p)" : "-";
    }

    private void RefreshStandings()
    {
        var season = _engine?.League.CurrentSeason ?? _engine?.League.SeasonHistory.LastOrDefault();
        if (season == null || _engine == null)
        {
            DriverGrid.ItemsSource = null;
            ManufacturerGrid.ItemsSource = null;
            return;
        }

        var driverStandings = LeagueStandingsCalculator.ComputeDriverStandings(season);
        DriverGrid.ItemsSource = driverStandings.Select((d, i) => new LeagueDriverStandingRowVm
        {
            Position = i + 1,
            Name = d.Name,
            Team = d.TeamName,
            Points = d.Points,
            Wins = d.Wins,
            Podiums = d.Podiums,
            Top5 = d.Top5,
            Top10 = d.Top10,
            PenaltyPoints = d.PenaltyPointsTotal > 0 ? $"-{d.PenaltyPointsTotal}" : "-",
        }).ToList();

        var makeStandings = LeagueStandingsCalculator.ComputeManufacturerStandings(season, _engine.Content);
        ManufacturerGrid.ItemsSource = makeStandings.Select((m, i) => new LeagueManufacturerStandingRowVm
        {
            Position = i + 1,
            Manufacturer = m.Manufacturer,
            Points = m.Points,
            Wins = m.Wins,
        }).ToList();
    }

    private void RefreshCalendar()
    {
        var season = _engine?.League.CurrentSeason ?? _engine?.League.SeasonHistory.LastOrDefault();
        if (season == null)
        {
            CalendarGrid.ItemsSource = null;
            PenaltyRoundBox.ItemsSource = null;
            return;
        }

        CalendarGrid.ItemsSource = season.Rounds.Select(r => new CalendarRowVm
        {
            Round = r.RoundNumber,
            Track = r.TrackVenue,
            Format = r.Format.ToString(),
            Status = r.Completed ? "Fullført" : "Ikke kjørt",
            Winner = r.FieldResults.FirstOrDefault(f => f.Position == 1)?.Name ?? "-",
        }).ToList();

        PenaltyRoundBox.ItemsSource = season.Rounds.Where(r => r.Completed).Select(r => r.RoundNumber).ToList();
        PenaltyDriverBox.ItemsSource = season.LockedRosterNames;
    }

    private void RefreshPenalties()
    {
        var season = _engine?.League.CurrentSeason ?? _engine?.League.SeasonHistory.LastOrDefault();
        if (season == null)
        {
            PenaltyGrid.ItemsSource = null;
            return;
        }

        PenaltyGrid.ItemsSource = season.Rounds
            .SelectMany(r => r.Penalties.Select(p => new PenaltyRowVm
            {
                Round = r.RoundNumber,
                Driver = p.DriverName,
                Consequence = p.Disqualified ? "Diskvalifisert" : $"-{p.PointsDeducted} poeng",
                Reason = p.Reason,
            }))
            .OrderByDescending(p => p.Round)
            .ToList();
    }

    private void SetDashboardStatus(string icon, string message)
    {
        StatusIcon.Text = icon;
        DashboardStatusText.Text = message;
    }

    private void Log(string message)
    {
        LogListBox.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
        while (LogListBox.Items.Count > 200) LogListBox.Items.RemoveAt(LogListBox.Items.Count - 1);
    }
}
