using System.IO;
using System.Windows;
using System.Windows.Controls;
using LmuCareerTool.Settings;
using Microsoft.Win32;

namespace LmuCareerTool.App;

/// <summary>
/// Første skjermbilde brukeren møter. Samler inn Results-mappe + LMU-visningsnavn, kjenner
/// igjen en returnerende fører (endrer hovedknappen til "Fortsett karriere"), og gir stafett-
/// pinnen videre til MainWindow - som deretter starter overvåking automatisk, uten et ekstra
/// klikk i Innstillinger-fanen.
/// </summary>
public partial class WelcomeWindow : Window
{
    private readonly AppSettingsStore _settingsStore = new(AppPaths.SettingsFilePath);

    public WelcomeWindow()
    {
        InitializeComponent();
        DarkTitleBarHelper.Apply(this);

        var settings = _settingsStore.LoadOrDefault();
        ResultsFolderBox.Text = string.IsNullOrWhiteSpace(settings.ResultsFolder)
            ? (LmuPathLocator.TryFindResultsFolder()
               ?? @"C:\Program Files (x86)\Steam\steamapps\common\Le Mans Ultimate\UserData\Log\Results")
            : settings.ResultsFolder;
        PlayerNameBox.Text = settings.PlayerName;

        UpdatePrimaryButtonState();
    }

    private void PlayerNameBox_TextChanged(object sender, TextChangedEventArgs e) => UpdatePrimaryButtonState();

    private void UpdatePrimaryButtonState()
    {
        var name = PlayerNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            PrimaryButton.Content = "Start";
            WelcomeBackText.Visibility = Visibility.Collapsed;
            return;
        }

        if (File.Exists(AppPaths.CareerFilePath(name)))
        {
            PrimaryButton.Content = "Fortsett karriere";
            WelcomeBackText.Text = $"👋 Velkommen tilbake, {name}!";
            WelcomeBackText.Visibility = Visibility.Visible;
        }
        else
        {
            PrimaryButton.Content = "Start karriere";
            WelcomeBackText.Visibility = Visibility.Collapsed;
        }
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Velg LMU Results-mappe",
        };
        if (Directory.Exists(ResultsFolderBox.Text))
            dialog.InitialDirectory = ResultsFolderBox.Text;

        if (dialog.ShowDialog(this) == true)
            ResultsFolderBox.Text = dialog.FolderName;
    }

    private void PrimaryButton_Click(object sender, RoutedEventArgs e)
    {
        var resultsFolder = ResultsFolderBox.Text.Trim();
        var playerName = PlayerNameBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(playerName))
        {
            ShowError("Skriv inn LMU-visningsnavnet ditt først.");
            return;
        }

        if (string.IsNullOrWhiteSpace(resultsFolder) || !Directory.Exists(resultsFolder))
        {
            ShowError("Fant ikke Results-mappen. Velg riktig mappe med \"Bla gjennom...\".");
            return;
        }

        ErrorText.Visibility = Visibility.Collapsed;

        var main = new MainWindow(resultsFolder, playerName);
        main.Show();
        Close();
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
