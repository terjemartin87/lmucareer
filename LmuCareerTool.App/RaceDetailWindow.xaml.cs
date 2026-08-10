using System.Windows;
using LmuCareerTool.Career;
using LmuCareerTool.Models;

namespace LmuCareerTool.App;

public partial class RaceDetailWindow : Window
{
    private readonly CareerEngine _engine;
    private readonly CareerRaceEntry _entry;

    /// <summary>Satt til true hvis brukeren slettet løpet - MainWindow bruker dette til å
    /// oppdatere header/sesong/mesterskap etter at vinduet lukkes.</summary>
    public bool WasDeleted { get; private set; }

    public RaceDetailWindow(CareerEngine engine, CareerRaceEntry entry)
    {
        InitializeComponent();
        DarkTitleBarHelper.Apply(this);
        _engine = engine;
        _entry = entry;

        TrackText.Text = entry.TrackVenue;
        var roundText = entry.RoundNumber.HasValue ? $"Runde {entry.RoundNumber} - " : "";
        SubText.Text = $"{roundText}{entry.CarType}   ·   {entry.CompletedAtUtc.ToLocalTime():dd.MM.yyyy HH:mm}";

        PracticeText.Text = entry.PracticeBestLap.HasValue
            ? $"Beste runde: {FormatTime(entry.PracticeBestLap)}\n" +
              $"Sektorer: S1 {FormatTime(entry.PracticeS1)}  S2 {FormatTime(entry.PracticeS2)}  S3 {FormatTime(entry.PracticeS3)}"
            : "Ingen Practice-data registrert for dette løpet.";

        QualifyingText.Text = entry.QualifyingBestLap.HasValue
            ? $"Plassering: P{entry.QualifyingPos}\n" +
              $"Beste runde: {FormatTime(entry.QualifyingBestLap)}\n" +
              $"Sektorer: S1 {FormatTime(entry.QualifyingS1)}  S2 {FormatTime(entry.QualifyingS2)}  S3 {FormatTime(entry.QualifyingS3)}"
            : "Ingen Qualifying-data registrert for dette løpet.";

        RaceText.Text =
            $"Plassering: P{entry.FinishPos} av {entry.TotalParticipants} (startet P{entry.GridPos})\n" +
            $"Status: {entry.FinishStatus}\n" +
            $"Beste runde: {FormatTime(entry.RaceBestLap)}\n" +
            $"Sektorer: S1 {FormatTime(entry.RaceS1)}  S2 {FormatTime(entry.RaceS2)}  S3 {FormatTime(entry.RaceS3)}\n" +
            $"Hendelser: {entry.IncidentCount}\n" +
            $"XP: +{entry.XpEarned}   Poeng: +{entry.PointsEarned}";
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(this,
            "Dette fjerner løpet fra historikken og reverserer XP, poeng, Driver/Safety Rating og credits det ga. " +
            "Hvis dette var en runde i den pågående sesongen, nullstilles runden slik at du kan kjøre den på nytt. " +
            "Prestasjoner og klasser du allerede har låst opp beholdes. Vil du fortsette?",
            "Slett løp", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        if (_engine.DeleteRaceHistoryEntry(_entry))
        {
            WasDeleted = true;
            Close();
        }
    }

    private static string FormatTime(double? seconds)
    {
        if (seconds is null or <= 0) return "-";
        var ts = TimeSpan.FromSeconds(seconds.Value);
        return $"{(int)ts.TotalMinutes}:{ts.Seconds:00}.{ts.Milliseconds:000}";
    }
}
