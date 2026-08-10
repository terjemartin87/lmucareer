using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using LmuCareerTool.App.Localization;
using LmuCareerTool.Settings;
using LmuCareerTool.Career;
using LmuCareerTool.Models;
using LmuCareerTool.Transfers;
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
    private bool _suppressLanguageChange = true;

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

        LanguageBox.ItemsSource = new[] { Strings.T("Common_LanguageNorwegian"), Strings.T("Common_LanguageEnglish") };
        LanguageBox.SelectedIndex = Strings.Current == AppLanguage.English ? 1 : 0;
        _suppressLanguageChange = false;

        if (string.IsNullOrWhiteSpace(settings.PlayerName))
            NavSettings.IsChecked = true; // ingen tidligere oppsett - start på Innstillinger i stedet for et tomt Dashboard
    }

    /// <summary>Brukes av WelcomeWindow - fyller ut oppsettet og starter overvåking med det
    /// samme, slik at man ikke må gjenta seg selv med et ekstra klikk i Innstillinger-fanen.</summary>
    public MainWindow(string resultsFolder, string playerName) : this()
    {
        ResultsFolderBox.Text = resultsFolder;
        PlayerNameBox.Text = playerName;

        // StartWatching() kan åpne dialogvinduer (f.eks. garasjen hvis ingen sesong er valgt)
        // som setter Owner = this - det krever at dette vinduet allerede er vist. Konstruktøren
        // kjører derimot FØR WelcomeWindow rekker å kalle Show() på oss, så vent til Loaded.
        Loaded += MainWindow_AutoStart;
    }

    private void MainWindow_AutoStart(object sender, RoutedEventArgs e)
    {
        Loaded -= MainWindow_AutoStart;
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
        var dialog = new Microsoft.Win32.OpenFolderDialog { Title = Strings.T("Common_BrowseResultsFolderTitle") };
        if (Directory.Exists(ResultsFolderBox.Text))
            dialog.InitialDirectory = ResultsFolderBox.Text;

        if (dialog.ShowDialog(this) == true)
            ResultsFolderBox.Text = dialog.FolderName;
    }

    private void StartWatching()
    {
        var resultsFolder = ResultsFolderBox.Text.Trim();
        var playerName = PlayerNameBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(resultsFolder) || string.IsNullOrWhiteSpace(playerName))
        {
            MessageBox.Show(this, Strings.T("Main_Msg_MissingSetup"), Strings.T("Main_Msg_MissingSetupTitle"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!Directory.Exists(resultsFolder))
        {
            MessageBox.Show(this, string.Format(Strings.T("Main_Msg_FolderNotFound"), resultsFolder), Strings.T("Main_Msg_FolderNotFoundTitle"),
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
            MessageBox.Show(this, string.Format(Strings.T("Main_Msg_StartFailed"), ex.Message), Strings.T("Common_ErrorTitle"),
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
        Log(string.Format(Strings.T("Main_Log_IndexingFiles"), existing.Count));
        foreach (var file in existing)
        {
            if (!_processedFiles.Add(file)) continue;
            try { _engine.IndexExistingFile(file); }
            catch { /* ignorer filer som ikke er sesjonsresultater */ }
        }
        RefreshPendingSessions();

        _watcher = new ResultsWatcher(resultsFolder);
        _watcher.NewResultFile += OnNewResultFile;
        _watcher.Start();

        ResultsFolderBox.IsEnabled = false;
        PlayerNameBox.IsEnabled = false;
        StartStopButton.Content = Strings.T("Main_Settings_StopWatching");
        StatusText.Text = string.Format(Strings.T("Main_Settings_StatusWatching"), resultsFolder);
        Log(Strings.T("Main_Log_Waiting"));
        ShowToast("🏁", Strings.T("Main_Toast_WatchingStarted"), "AccentColor");
        SetDashboardStatus("🟢", Strings.T("Main_DashboardStatus_Watching"));

        NavDashboard.IsChecked = true;
    }

    private void StopWatching()
    {
        _watcher?.Dispose();
        _watcher = null;
        SetDashboardStatus("⏸", Strings.T("Main_DashboardStatus_Stopped"));

        ResultsFolderBox.IsEnabled = true;
        PlayerNameBox.IsEnabled = true;
        StartStopButton.Content = Strings.T("Main_Settings_StartWatching");
        StatusText.Text = Strings.T("Main_Settings_StatusStopped");
        Log(Strings.T("Main_Log_Stopped"));
    }

    private void OnSessionIgnored(SessionResult session)
    {
        Log(string.Format(Strings.T("Main_Log_SessionIgnored"), session.TrackVenue, session.SettingMode));
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
                else RefreshPendingSessions();
            }
            catch (Exception ex)
            {
                Log(string.Format(Strings.T("Main_Log_FileReadError"), ex.Message));
            }
        });
    }

    private void HandleOutcome(WeekendProcessingOutcome outcome)
    {
        var weekend = outcome.Weekend;
        if (weekend.RaceResult == null)
        {
            Log(string.Format(Strings.T("Main_Log_PlayerNotFound"), weekend.TrackVenue));
            return;
        }

        var race = weekend.RaceResult;

        if (outcome.CompletedSpecialEventName != null)
        {
            Log($"🏆 {outcome.CompletedSpecialEventName} fullført: P{race.Position} av {weekend.TotalParticipants} - " +
                $"+{outcome.XpEarned} XP, Driver {FormatDelta(outcome.RatingDelta)}, Safety {FormatDelta(outcome.SafetyRatingDelta)}, +{outcome.CreditsEarned} cr");
            ShowToast("🏆", $"{outcome.CompletedSpecialEventName} fullført! P{race.Position}", "AccentGold");
            HandlePostOutcomeEffects(outcome);
            return;
        }

        if (outcome.Issues.Count > 0)
        {
            foreach (var issue in outcome.Issues)
                Log($"⚠ {issue}");

            ShowToast("⚠", Strings.T("Main_Toast_SetupMismatch"), "WarnColor");

            if (outcome.CanApproveAnyway)
            {
                var result = MessageBox.Show(this,
                    string.Format(Strings.T("Main_Msg_SetupMismatchBody"), string.Join("\n\n", outcome.Issues), race.Position),
                    Strings.T("Main_Msg_SetupMismatchTitle"), MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    var approved = _engine!.ApproveDespiteMismatch(outcome);
                    Log(string.Format(Strings.T("Main_Log_ApprovedManually"), weekend.TrackVenue, approved.XpEarned, approved.PointsEarned,
                        FormatDelta(approved.RatingDelta), FormatDelta(approved.SafetyRatingDelta), approved.CreditsEarned));
                    HandlePostOutcomeEffects(approved);
                    return;
                }
            }
        }
        else
        {
            Log(string.Format(Strings.T("Main_Log_RaceResult"), weekend.TrackVenue, race.Position, weekend.TotalParticipants,
                outcome.XpEarned, outcome.PointsEarned, FormatDelta(outcome.RatingDelta), FormatDelta(outcome.SafetyRatingDelta), outcome.CreditsEarned));
            ShowToast("🏁", string.Format(Strings.T("Main_Toast_RaceResult"), weekend.TrackVenue, race.Position, outcome.XpEarned), "AccentColor");
        }

        HandlePostOutcomeEffects(outcome);
    }

    private static string FormatDelta(int delta) => delta > 0 ? $"+{delta}" : delta < 0 ? delta.ToString() : "0";

    private void HandlePostOutcomeEffects(WeekendProcessingOutcome outcome)
    {
        if (outcome.ContractSalaryEarned > 0)
            Log(string.Format(Strings.T("Main_Log_ContractSalary"), outcome.ContractSalaryEarned));

        foreach (var unlocked in outcome.NewUnlocks)
        {
            Log(string.Format(Strings.T("Main_Log_ClassUnlocked"), unlocked));
            ShowToast("🔓", string.Format(Strings.T("Main_Toast_ClassUnlocked"), unlocked), "AccentGold");
        }

        foreach (var achievement in outcome.NewAchievements)
        {
            Log(string.Format(Strings.T("Main_Log_AchievementUnlocked"), achievement.Name));
            ShowToast(achievement.Icon, string.Format(Strings.T("Main_Toast_AchievementUnlocked"), achievement.Name), "AccentGold");
        }

        RefreshHeader();
        RefreshSeason();
        RefreshHistory();
        RefreshChampionship();

        if (outcome.SeasonJustCompleted)
        {
            Log(string.Format(Strings.T("Main_Log_SeasonComplete"), outcome.CompletedSeason?.TotalPoints));
            ShowToast("🏆", Strings.T("Main_Toast_SeasonComplete"), "AccentGold");

            if (outcome.DroppedByManufacturer)
                Log(Strings.T("Main_Log_DroppedByManufacturer"));
            else if (outcome.ContractExpired)
                Log(Strings.T("Main_Log_ContractExpired"));

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
            var suffix = contract == null ? ""
                : contract.IsPrivateerSeat ? Strings.T("Main_Log_NewSeasonPrivateer")
                : contract.IsFreeAgent ? ""
                : string.Format(Strings.T("Main_Log_NewSeasonManufacturer"), contract.Manufacturer, contract.SeasonsRemaining, contract.SalaryPerRound);
            Log(string.Format(Strings.T("Main_Log_NewSeasonStarted"), _engine.Career.CurrentClass) + suffix);
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

    private void HelpLink_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        NavSettings.IsChecked = true;
    }

    private void CheckInterestButton_Click(object sender, RoutedEventArgs e)
    {
        if (_engine == null)
        {
            MessageBox.Show(this, Strings.T("Main_Msg_NoActiveCareer"), Strings.T("Main_Msg_NoActiveCareerTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var window = new ManufacturerInterestWindow(_engine) { Owner = this };
        window.ShowDialog();
    }

    private void TransferMarketButton_Click(object sender, RoutedEventArgs e)
    {
        if (_engine == null)
        {
            MessageBox.Show(this, Strings.T("Main_Msg_NoActiveCareer"), Strings.T("Main_Msg_NoActiveCareerTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var window = new TransferMarketWindow(_engine) { Owner = this };
        window.ShowDialog();
    }

    private void AchievementsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_engine == null)
        {
            MessageBox.Show(this, Strings.T("Main_Msg_NoActiveCareer"), Strings.T("Main_Msg_NoActiveCareerTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var window = new AchievementsWindow(_engine) { Owner = this };
        window.ShowDialog();
    }

    private void ShopButton_Click(object sender, RoutedEventArgs e)
    {
        if (_engine == null)
        {
            MessageBox.Show(this, Strings.T("Main_Msg_NoActiveCareer"), Strings.T("Main_Msg_NoActiveCareerTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var window = new ShopWindow(_engine) { Owner = this };
        window.ShowDialog();
        RefreshHeader();
    }

    private void EventsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_engine == null)
        {
            MessageBox.Show(this, Strings.T("Main_Msg_NoActiveCareer"), Strings.T("Main_Msg_NoActiveCareerTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var window = new EventsWindow(_engine) { Owner = this };
        window.ShowDialog();
        RefreshHeader();
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
        Log(Strings.T("Main_Log_CopiedRecipe"));
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
        DriverNameText.Text = string.Format(Strings.T("Main_Header_Driver"), career.DriverName);
        ClassText.Text = string.Format(Strings.T("Main_Header_Class"), career.CurrentClass, string.Join(", ", career.UnlockedClasses));

        var contract = career.CurrentContract;
        ManufacturerText.Text = contract == null
            ? Strings.T("Main_Header_MakeEmpty")
            : contract.IsPrivateerSeat
                ? Strings.T("Main_Header_MakePrivateer")
                : contract.IsFreeAgent
                    ? Strings.T("Main_Header_MakeFreeAgent")
                    : string.Format(Strings.T("Main_Header_MakeContract"), contract.Manufacturer, contract.SeasonsRemaining,
                        contract.SalaryPerRound, ContractGoalFormatter.Describe(contract.Goals));
        LevelText.Text = career.Level.ToString();
        XpText.Text = career.TotalXp.ToString();
        SeasonPointsText.Text = (career.CurrentSeason?.TotalPoints ?? 0).ToString();
        RatingText.Text = career.DriverRating.ToString();
        SafetyRatingText.Text = career.SafetyRating.ToString();
        CreditsText.Text = $"{career.Credits} cr";
    }

    private void RefreshSeason()
    {
        var season = _engine?.Career.CurrentSeason;
        if (season == null)
        {
            _seasonRows.Clear();
            NextRaceTitle.Text = Strings.T("Main_NextRaceWaiting");
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
            NextRaceTitle.Text = string.Format(Strings.T("Main_NextRaceTitle"), next.RoundNumber, next.TrackVenue, next.Format);
            NextRaceDetail.Text = string.Format(Strings.T("Main_NextRaceDetail"), _engine!.Career.CurrentClass, next.AssignedCar,
                next.SuggestedRaceMinutes, next.AssignedWeather);
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

        RefreshPendingSessions();
    }

    private void RefreshPendingSessions()
    {
        if (_engine == null) return;

        var rows = _engine.GetPendingSessions()
            .GroupBy(s => s.TrackVenue)
            .Select(g => new PendingSessionRowVm(g.Key, g.ToList()))
            .ToList();

        PendingSessionsCard.Visibility = rows.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        PendingSessionsList.ItemsSource = rows;
    }

    private void ClearPendingButton_Click(object sender, RoutedEventArgs e)
    {
        if (_engine == null || sender is not Button { Tag: PendingSessionRowVm row }) return;

        _engine.ClearPendingSession(row.TrackVenue);
        RefreshPendingSessions();
    }

    private void RefreshChampionship()
    {
        if (_engine == null) return;
        var season = _engine.Career.CurrentSeason;

        if (season == null)
        {
            ChampionshipSubTitleText.Text = Strings.T("Main_ChampionshipNoSeason");
            DriverGrid.ItemsSource = null;
            ManufacturerGrid.ItemsSource = null;
            RivalCard.Visibility = Visibility.Collapsed;
            return;
        }

        var fieldStatus = season.LockedRosterNames.Count > 0
            ? string.Format(Strings.T("Main_Championship_FieldLocked"), season.LockedRosterNames.Count)
            : Strings.T("Main_Championship_FieldLocksNext");

        ChampionshipSubTitleText.Text =
            string.Format(Strings.T("Main_Championship_Header"), season.SeasonNumber, season.CarClass) + "   ·   " +
            string.Format(Strings.T("Main_Championship_RoundsDone"), season.CompletedCount, season.Events.Count) + "   ·   " + fieldStatus;

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

        var rivalComparison = _engine.GetRivalComparison(season);
        if (rivalComparison == null)
        {
            RivalCard.Visibility = Visibility.Collapsed;
        }
        else
        {
            var (player, rival) = rivalComparison.Value;
            var playerPos = driverStandings.FindIndex(s => s.IsPlayer) + 1;
            var rivalPos = driverStandings.FindIndex(s => ReferenceEquals(s, rival)) + 1;
            var gap = Math.Abs(player.Points - rival.Points);
            var chasing = rivalPos < playerPos;

            RivalCard.Visibility = Visibility.Visible;
            RivalText.Text = chasing
                ? $"Du jager {rival.Name} (P{rivalPos}) - {gap} poeng bak. De har {rival.Wins} seire og {rival.Podiums} pallplasser mot dine {player.Wins}/{player.Podiums}."
                : $"{rival.Name} (P{rivalPos}) jager deg - {gap} poeng bak. De har {rival.Wins} seire og {rival.Podiums} pallplasser mot dine {player.Wins}/{player.Podiums}.";
        }
    }

    private void HistoryListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_engine != null && sender is ListBox { SelectedItem: RaceHistoryRow row })
        {
            var detailWindow = new RaceDetailWindow(_engine, row.Entry) { Owner = this };
            detailWindow.ShowDialog();

            if (detailWindow.WasDeleted)
            {
                RefreshHeader();
                RefreshSeason();
                RefreshHistory();
                RefreshChampionship();
            }
        }
    }

    private void SetDashboardStatus(string icon, string message)
    {
        StatusIcon.Text = icon;
        DashboardStatusText.Text = message;
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
