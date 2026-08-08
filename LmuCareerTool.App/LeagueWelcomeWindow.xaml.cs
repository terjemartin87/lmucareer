using System.IO;
using System.Windows;
using System.Windows.Controls;
using LmuCareerTool.Settings;
using Microsoft.Win32;

namespace LmuCareerTool.App;

/// <summary>Inngangsskjerm for Liga modus - samler inn liganavn, vertens visningsnavn og
/// Results-mappe, og gir stafettpinnen videre til LeagueMainWindow. Helt separat flyt fra
/// WelcomeWindow/MainWindow (karriere) - ingen delt lagring.</summary>
public partial class LeagueWelcomeWindow : Window
{
    private readonly AppSettingsStore _settingsStore = new(AppPaths.LeagueSettingsFilePath);

    public LeagueWelcomeWindow()
    {
        InitializeComponent();
        DarkTitleBarHelper.Apply(this);

        var settings = _settingsStore.LoadOrDefault();
        ResultsFolderBox.Text = string.IsNullOrWhiteSpace(settings.ResultsFolder)
            ? (LmuPathLocator.TryFindResultsFolder()
               ?? @"C:\Program Files (x86)\Steam\steamapps\common\Le Mans Ultimate\UserData\Log\Results")
            : settings.ResultsFolder;
        LeagueNameBox.Text = settings.PlayerName; // gjenbruker feltet - inneholder liganavn her

        UpdatePrimaryButtonState();
    }

    private void LeagueNameBox_TextChanged(object sender, TextChangedEventArgs e) => UpdatePrimaryButtonState();

    private void UpdatePrimaryButtonState()
    {
        var name = LeagueNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            PrimaryButton.Content = "Start liga";
            WelcomeBackText.Visibility = Visibility.Collapsed;
            return;
        }

        if (File.Exists(AppPaths.LeagueFilePath(name)))
        {
            PrimaryButton.Content = "Fortsett liga";
            WelcomeBackText.Text = $"👋 Velkommen tilbake til {name}!";
            WelcomeBackText.Visibility = Visibility.Visible;
        }
        else
        {
            PrimaryButton.Content = "Opprett liga";
            WelcomeBackText.Visibility = Visibility.Collapsed;
        }
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Velg LMU Results-mappe" };
        if (Directory.Exists(ResultsFolderBox.Text))
            dialog.InitialDirectory = ResultsFolderBox.Text;

        if (dialog.ShowDialog(this) == true)
            ResultsFolderBox.Text = dialog.FolderName;
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        new ModeSelectWindow().Show();
        Close();
    }

    private void PrimaryButton_Click(object sender, RoutedEventArgs e)
    {
        var resultsFolder = ResultsFolderBox.Text.Trim();
        var leagueName = LeagueNameBox.Text.Trim();
        var hostName = HostNameBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(leagueName))
        {
            ShowError("Skriv inn et liganavn først.");
            return;
        }

        var isReturning = File.Exists(AppPaths.LeagueFilePath(leagueName));
        if (!isReturning && string.IsNullOrWhiteSpace(hostName))
        {
            ShowError("Skriv inn visningsnavnet ditt som vert.");
            return;
        }

        if (string.IsNullOrWhiteSpace(resultsFolder) || !Directory.Exists(resultsFolder))
        {
            ShowError("Fant ikke Results-mappen. Velg riktig mappe med \"Bla gjennom...\".");
            return;
        }

        ErrorText.Visibility = Visibility.Collapsed;
        _settingsStore.Save(new AppSettings { ResultsFolder = resultsFolder, PlayerName = leagueName });

        var main = new LeagueMainWindow(resultsFolder, leagueName, hostName);
        main.Show();
        Close();
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
