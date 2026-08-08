using System.Windows;
using LmuCareerTool.Career;

namespace LmuCareerTool.App;

public partial class DriverProfileWindow : Window
{
    public DriverProfileWindow(CareerEngine engine)
    {
        InitializeComponent();

        var career = engine.Career;
        AvatarBrush.ImageSource = AvatarImageCache.GetForDriver(career.DriverName, career.DriverName, decodePixelWidth: 400);
        DriverNameText.Text = career.DriverName;

        var contract = career.CurrentContract;
        ContractText.Text = contract == null
            ? $"{career.CurrentClass} - ingen aktiv kontrakt"
            : contract.IsPrivateerSeat
                ? $"{career.CurrentClass} - privatlag (betalt sete)"
                : contract.IsFreeAgent
                    ? $"{career.CurrentClass} - fri kjøring"
                    : $"{career.CurrentClass} hos {contract.Manufacturer}   ·   {contract.SeasonsRemaining} sesong(er) igjen   ·   Mål: {contract.GoalDescription}";

        var totalSeasons = career.SeasonHistory.Count + (career.CurrentSeason != null ? 1 : 0);
        var totalRaces = career.RaceHistory.Count;
        var wins = career.RaceHistory.Count(r => r.FinishPos == 1);
        var podiums = career.RaceHistory.Count(r => r.FinishPos is >= 1 and <= 3);
        var bestFinish = career.RaceHistory.Where(r => r.FinishPos > 0).Select(r => r.FinishPos).DefaultIfEmpty(0).Min();
        var totalPoints = career.SeasonHistory.Sum(s => s.TotalPoints) + (career.CurrentSeason?.TotalPoints ?? 0);
        var totalDistanceKm = career.RaceHistory.Sum(r => r.Laps * r.TrackLength) / 1000.0;

        StatList.ItemsSource = new List<StatTileVm>
        {
            new("NIVÅ", career.Level.ToString()),
            new("TOTAL XP", career.TotalXp.ToString()),
            new("RATING", career.DriverRating.ToString()),
            new("CREDITS", $"{career.Credits} cr"),
            new("SESONGER", totalSeasons.ToString()),
            new("LØP", totalRaces.ToString()),
            new("SEIRE", wins.ToString()),
            new("PODIER", podiums.ToString()),
            new("BESTE RESULTAT", bestFinish > 0 ? $"P{bestFinish}" : "-"),
            new("TOTALE POENG", totalPoints.ToString()),
            new("DISTANSE KJØRT", $"{totalDistanceKm:0} km"),
            new("OPPLÅSTE KLASSER", string.Join(", ", career.UnlockedClasses)),
        };
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
