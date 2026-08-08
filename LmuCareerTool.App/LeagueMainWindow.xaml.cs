using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using LmuCareerTool.App.Localization;
using LmuCareerTool.League;
using LmuCareerTool.Models;
using LmuCareerTool.Settings;
using LmuCareerTool.Watching;
using Microsoft.Win32;

namespace LmuCareerTool.App;

/// <summary>Avkryssbar rad i klassevelgeren ved sesonggenerering - en liga kan kjøre én klasse
/// (f.eks. kun GT3) eller flere sammen i samme hostede løp (f.eks. GT3 + LMP2 + LMP3 + Hypercar,
/// som ekte WEC), akkurat som et vanlig hostet lobby-løp i LMU tillater.</summary>
public class CarClassCheckItem : INotifyPropertyChanged
{
    private bool _isSelected;

    public CarClassCheckItem(string name) => Name = name;

    public string Name { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected))); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

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
    private bool _suppressLanguageChange = true;

    public LeagueMainWindow()
    {
        InitializeComponent();
        DarkTitleBarHelper.Apply(this);

        FormatBox.ItemsSource = Enum.GetValues<LeagueFormatPreference>();
        FormatBox.SelectedIndex = 0;

        LanguageBox.ItemsSource = new[] { Strings.T("Common_LanguageNorwegian"), Strings.T("Common_LanguageEnglish") };
        LanguageBox.SelectedIndex = Strings.Current == AppLanguage.English ? 1 : 0;
        _suppressLanguageChange = false;
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

    private void LanguageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressLanguageChange) return;

        var language = LanguageBox.SelectedIndex == 1 ? AppLanguage.English : AppLanguage.Norwegian;
        if (language == Strings.Current) return;

        LanguageStore.Save(language);
        System.Diagnostics.Process.Start(Environment.ProcessPath!);
        Application.Current.Shutdown();
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
        var dialog = new OpenFolderDialog { Title = Strings.T("Common_BrowseResultsFolderTitle") };
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
            MessageBox.Show(this, string.Format(Strings.T("Main_Msg_FolderNotFound"), resultsFolder), Strings.T("Main_Msg_FolderNotFoundTitle"),
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
            var classItems = _engine.Content.Classes.Select(c => new CarClassCheckItem(c.Name)).ToList();
            if (classItems.Count > 0) classItems[0].IsSelected = true;
            CarClassList.ItemsSource = classItems;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, string.Format(Strings.T("Main_Msg_StartFailed"), ex.Message), Strings.T("Common_ErrorTitle"),
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        RefreshAll();

        var existing = Directory.GetFiles(resultsFolder, "*.xml").OrderBy(f => f).ToList();
        Log(string.Format(Strings.T("Main_Log_IndexingFiles"), existing.Count));
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
        StartStopButton.Content = Strings.T("Main_Settings_StopWatching");
        StatusText.Text = string.Format(Strings.T("Main_Settings_StatusWatching"), resultsFolder);
        Log(Strings.T("League_Log_Waiting"));
        SetDashboardStatus("🟢", Strings.T("League_DashboardStatusWatching"));

        NavDashboard.IsChecked = true;
    }

    private void StopWatching()
    {
        _watcher?.Dispose();
        _watcher = null;
        SetDashboardStatus("⏸", Strings.T("Main_DashboardStatus_Stopped"));

        ResultsFolderBox.IsEnabled = true;
        StartStopButton.Content = Strings.T("Main_Settings_StartWatching");
        StatusText.Text = Strings.T("Main_Settings_StatusStopped");
        Log(Strings.T("Main_Log_Stopped"));
    }

    private void OnSessionIgnored(SessionResult session)
    {
        Log(string.Format(Strings.T("League_Log_SessionIgnored"), session.TrackVenue, session.SettingMode));
    }

    private void OnNewResultFile(string path)
    {
        if (!_processedFiles.Add(path)) return;

        Dispatcher.Invoke(() =>
        {
            Log(string.Format(Strings.T("Main_Log_NewFile"), Path.GetFileName(path)));

            try
            {
                var outcome = _engine!.ProcessFile(path);
                if (outcome != null) HandleOutcome(outcome);
            }
            catch (Exception ex)
            {
                Log(string.Format(Strings.T("Main_Log_FileReadError"), ex.Message));
            }
        });
    }

    private void OnRoundCompleted(LeagueRoundOutcome outcome) { /* HandleOutcome dekker UI-oppdateringen */ }

    private void HandleOutcome(LeagueRoundOutcome outcome)
    {
        var weekend = outcome.Weekend;

        if (outcome.MatchedRound == null && outcome.CandidateRound != null)
        {
            Log(string.Format(Strings.T("League_Log_TrackMismatch"), weekend.TrackVenue, outcome.CandidateRound.TrackVenue));

            var result = MessageBox.Show(this,
                string.Format(Strings.T("League_Msg_TrackMismatchBody"), weekend.TrackVenue, outcome.CandidateRound.TrackVenue),
                Strings.T("League_Msg_TrackMismatchTitle"), MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                var approved = _engine!.ApproveDespiteMismatch(outcome);
                Log(string.Format(Strings.T("League_Log_ApprovedManually"), approved.MatchedRound?.RoundNumber, weekend.TrackVenue, weekend.TotalParticipants));
                RefreshAll();
                if (approved.SeasonJustCompleted) AnnounceSeasonComplete();
            }
            return;
        }

        if (outcome.MatchedRound == null)
        {
            Log(string.Format(Strings.T("League_Log_NoActiveSeason"), weekend.TrackVenue));
            return;
        }

        Log(string.Format(Strings.T("League_Log_RoundCompleted"), outcome.MatchedRound.RoundNumber, weekend.TrackVenue, weekend.TotalParticipants));
        RefreshAll();

        if (outcome.SeasonJustCompleted) AnnounceSeasonComplete();
    }

    private void AnnounceSeasonComplete()
    {
        Log(Strings.T("League_Log_SeasonComplete"));
    }

    private void GenerateSeasonButton_Click(object sender, RoutedEventArgs e)
    {
        if (_engine == null)
        {
            MessageBox.Show(this, Strings.T("League_Msg_NoActiveLeague"), Strings.T("League_Msg_NoActiveLeagueTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_engine.League.CurrentSeason is { IsComplete: false })
        {
            var confirm = MessageBox.Show(this,
                Strings.T("League_Msg_OngoingSeasonBody"),
                Strings.T("League_Msg_OngoingSeasonTitle"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;
        }

        var selectedClasses = (CarClassList.ItemsSource as List<CarClassCheckItem> ?? new())
            .Where(c => c.IsSelected).Select(c => c.Name).ToList();

        if (selectedClasses.Count == 0)
        {
            MessageBox.Show(this, Strings.T("League_Msg_MissingClass"), Strings.T("League_Msg_MissingClassTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(RoundCountBox.Text, out var roundCount) || roundCount < 1)
        {
            MessageBox.Show(this, Strings.T("League_Msg_InvalidRoundCount"), Strings.T("League_Msg_InvalidRoundCountTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var format = (LeagueFormatPreference)FormatBox.SelectedItem;
        var carClass = string.Join(" + ", selectedClasses);

        _engine.GenerateNewSeason(carClass, roundCount, format);
        Log(string.Format(Strings.T("League_Log_SeasonGenerated"), carClass, roundCount, format) +
            (selectedClasses.Count > 1 ? Strings.T("League_Log_MulticlassNote") : ""));
        RefreshAll();
        NavCalendar.IsChecked = true;
    }

    private void PublishButton_Click(object sender, RoutedEventArgs e)
    {
        if (_engine == null)
        {
            MessageBox.Show(this, Strings.T("League_Msg_NoActiveLeague"), Strings.T("League_Msg_NoActiveLeagueTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = Strings.T("League_PublishDialogTitle"),
            Filter = Strings.T("Common_HtmlFileFilter"),
            FileName = $"{_leagueName}-standings.html",
        };

        if (dialog.ShowDialog(this) != true) return;

        try
        {
            _engine.PublishSnapshot(dialog.FileName);
            Log(string.Format(Strings.T("League_Log_Published"), dialog.FileName));
            MessageBox.Show(this, Strings.T("League_Msg_PublishSuccess"),
                Strings.T("League_Msg_PublishedTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, string.Format(Strings.T("League_Msg_PublishFailed"), ex.Message), Strings.T("Common_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyPenaltyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_engine == null) return;

        if (PenaltyRoundBox.SelectedItem is not int round)
        {
            MessageBox.Show(this, Strings.T("League_Msg_MissingRound"), Strings.T("League_Msg_MissingRoundTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var driver = (PenaltyDriverBox.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(driver))
        {
            MessageBox.Show(this, Strings.T("League_Msg_MissingDriver"), Strings.T("League_Msg_MissingDriverTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(PenaltyPointsBox.Text, out var pointsDeducted) || pointsDeducted < 0)
            pointsDeducted = 0;

        var reason = (PenaltyReasonBox.Text ?? "").Trim();

        _engine.ApplyPenalty(round, driver, pointsDeducted, PenaltyDsqBox.IsChecked == true, reason);
        var consequence = PenaltyDsqBox.IsChecked == true
            ? Strings.T("League_Log_PenaltyDisqualified")
            : string.Format(Strings.T("League_Log_PenaltyPointsDeducted"), pointsDeducted);
        Log(string.Format(Strings.T("League_Log_PenaltyGiven"), driver, round, consequence));

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
            string.Format(Strings.T("League_DriverHistory_Body"), history.TotalRaces, history.TotalPoints, history.Wins,
                history.Podiums, history.Top5, history.Top10, history.BestFinish > 0 ? $"P{history.BestFinish}" : "-"),
            string.Format(Strings.T("League_DriverHistory_Title"), history.Name), MessageBoxButton.OK, MessageBoxImage.Information);
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
        HeaderLeagueNameText.Text = string.Format(Strings.T("League_HeaderLeagueName"), _engine.League.LeagueName);

        var season = _engine.League.CurrentSeason;
        if (season == null)
        {
            HeaderSeasonText.Text = Strings.T("League_HeaderNoSeasonGenerate");
            RoundsText.Text = "0 / 0";
            LeaderText.Text = "-";
            return;
        }

        HeaderSeasonText.Text = string.Format(Strings.T("League_HeaderSeason"), season.SeasonNumber, season.CarClass);
        RoundsText.Text = $"{season.CompletedCount} / {season.Rounds.Count}";

        var leader = LeagueStandingsCalculator.ComputeDriverStandings(season).FirstOrDefault();
        LeaderText.Text = leader != null ? $"{leader.Name} ({leader.Points}p)" : "-";
    }

    private void RefreshStandings()
    {
        var season = _engine?.League.CurrentSeason ?? _engine?.League.SeasonHistory.LastOrDefault();
        if (season == null || _engine == null)
        {
            ClassFilterCard.Visibility = Visibility.Collapsed;
            DriverGrid.ItemsSource = null;
            ManufacturerGrid.ItemsSource = null;
            return;
        }

        // Et hostet løp kan blande flere klasser samtidig (GT3 + LMP2 + LMP3 + Hypercar osv,
        // akkurat som ekte WEC) - vis en klassevelger kun når sesongen faktisk har mer enn én.
        var classes = LeagueStandingsCalculator.GetClassesInSeason(season);
        if (classes.Count > 1)
        {
            ClassFilterCard.Visibility = Visibility.Visible;
            if (ClassFilterBox.ItemsSource is not List<string> currentClasses || !currentClasses.SequenceEqual(classes))
            {
                ClassFilterBox.ItemsSource = classes;
                ClassFilterBox.SelectedIndex = 0;
            }
            RefreshStandingsTables(season, ClassFilterBox.SelectedItem as string);
        }
        else
        {
            ClassFilterCard.Visibility = Visibility.Collapsed;
            RefreshStandingsTables(season, null);
        }
    }

    private void ClassFilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var season = _engine?.League.CurrentSeason ?? _engine?.League.SeasonHistory.LastOrDefault();
        if (season == null) return;
        RefreshStandingsTables(season, ClassFilterBox.SelectedItem as string);
    }

    private void RefreshStandingsTables(LeagueSeason season, string? carClass)
    {
        if (_engine == null) return;

        var driverStandings = LeagueStandingsCalculator.ComputeDriverStandings(season, carClass);
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

        var makeStandings = LeagueStandingsCalculator.ComputeManufacturerStandings(season, _engine.Content, carClass);
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
            Status = r.Completed ? Strings.T("League_Status_Completed") : Strings.T("League_Status_NotRun"),
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
                Consequence = p.Disqualified ? Strings.T("League_Consequence_Disqualified") : string.Format(Strings.T("League_Consequence_PointsDeducted"), p.PointsDeducted),
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
