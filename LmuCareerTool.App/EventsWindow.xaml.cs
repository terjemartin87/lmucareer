using System.Windows;
using System.Windows.Controls;
using LmuCareerTool.Career;

namespace LmuCareerTool.App;

/// <summary>Oversikt over spesialarrangementer (Fase 7) og din eventuelle aktive påmelding.</summary>
public partial class EventsWindow : Window
{
    private readonly CareerEngine _engine;

    public EventsWindow(CareerEngine engine)
    {
        InitializeComponent();
        DarkTitleBarHelper.Apply(this);
        _engine = engine;

        Refresh();
    }

    private void Refresh()
    {
        var career = _engine.Career;
        var active = career.ActiveSpecialEvent;

        if (active == null)
        {
            ActiveEventCard.Visibility = Visibility.Collapsed;
        }
        else
        {
            ActiveEventCard.Visibility = Visibility.Visible;
            var hours = active.DurationMinutes / 60.0;
            ActiveEventText.Text = $"{active.EventName} - {active.TrackVenue}   ·   {active.CarClass} ({active.AssignedCar})   ·   " +
                                    $"{hours:0.#} timer   ·   Vær: {active.AssignedWeather}   ·   Betalt: {active.EntryFeePaid} cr";
        }

        EventList.ItemsSource = _engine.GetSpecialEvents()
            .Select(def => new SpecialEventRowVm(def, canEnroll: LmuCareerTool.Career.SpecialEventService.CanEnroll(career, def)))
            .ToList();

        var history = career.SpecialEventHistory.AsEnumerable().Reverse()
            .Select(r => new SpecialEventHistoryRowVm(r))
            .ToList();
        HistoryHeaderText.Visibility = history.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        HistoryList.ItemsSource = history;
    }

    private void EnrollButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: SpecialEventRowVm row }) return;

        var window = new EventEnrollWindow(_engine, row.Definition) { Owner = this };
        window.ShowDialog();

        if (window.DidEnroll) Refresh();
    }

    private void WithdrawButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(this,
            "Dette avslutter påmeldingen din uten å refundere inngangspengene. Vil du fortsette?",
            "Trekk deg", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        _engine.WithdrawSpecialEvent();
        Refresh();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
