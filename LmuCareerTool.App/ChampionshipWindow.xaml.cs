using System.Windows;
using LmuCareerTool.Career;

namespace LmuCareerTool.App;

public partial class ChampionshipWindow : Window
{
    public ChampionshipWindow(CareerEngine engine)
    {
        InitializeComponent();

        var season = engine.Career.CurrentSeason;
        if (season == null)
        {
            TitleText.Text = "Mesterskap";
            SubTitleText.Text = "Ingen aktiv sesong ennå - velg klasse og merke for å starte en.";
            return;
        }

        TitleText.Text = $"Mesterskap - Sesong {season.SeasonNumber} ({season.CarClass})";
        SubTitleText.Text = $"{season.CompletedCount} av {season.Events.Count} runder kjørt" +
                             (season.LockedRosterNames.Count > 0
                                 ? $"   ·   Feltet ble låst med {season.LockedRosterNames.Count} sjåfører etter runde 1"
                                 : "   ·   Feltet låses når runde 1 er fullført");

        var lastRound = season.Events.Where(e => e.Completed).Select(e => e.RoundNumber).DefaultIfEmpty(0).Max();

        var driverStandings = engine.GetDriverStandings(season);
        var previousDriverStandings = lastRound > 1 ? engine.GetDriverStandings(season, lastRound - 1) : null;

        var driverRows = new List<DriverStandingRowVm>();
        for (var i = 0; i < driverStandings.Count; i++)
        {
            var entry = driverStandings[i];
            int? previousPosition = null;
            if (previousDriverStandings != null)
            {
                var idx = previousDriverStandings.FindIndex(e =>
                    e.Name.Equals(entry.Name, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0) previousPosition = idx + 1;
            }

            driverRows.Add(new DriverStandingRowVm(i + 1, entry, previousPosition, engine.Career.DriverName));
        }
        DriverGrid.ItemsSource = driverRows;

        var makeStandings = engine.GetManufacturerStandings(season);
        ManufacturerGrid.ItemsSource = makeStandings
            .Select((entry, i) => new ManufacturerStandingRowVm(i + 1, entry))
            .ToList();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
