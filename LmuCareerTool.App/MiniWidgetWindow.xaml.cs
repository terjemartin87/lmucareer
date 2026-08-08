using System.Windows;
using LmuCareerTool.Career;

namespace LmuCareerTool.App;

/// <summary>Liten alltid-øverst-widget som viser neste løp, til å ha synlig mens du er inne i
/// LMUs egne menyer og setter opp Race Weekend.</summary>
public partial class MiniWidgetWindow : Window
{
    private readonly CareerEngine _engine;

    public MiniWidgetWindow(CareerEngine engine)
    {
        InitializeComponent();
        DarkTitleBarHelper.Apply(this);
        _engine = engine;

        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 24;
        Top = workArea.Top + 24;

        Refresh();
    }

    public void Refresh()
    {
        var career = _engine.Career;
        var next = career.CurrentSeason?.NextEvent;

        if (next == null)
        {
            TitleText.Text = "Ingen aktiv sesong";
            DetailText.Text = "-";
        }
        else
        {
            TitleText.Text = $"Runde {next.RoundNumber}: {next.TrackVenue}";
            DetailText.Text = $"{career.CurrentClass} - {next.AssignedCar}   ·   {next.Format}   ·   " +
                               $"~{next.SuggestedRaceMinutes} min   ·   Vær: {next.AssignedWeather}";
        }

        RatingText.Text = career.DriverRating.ToString();
        CreditsText.Text = $"{career.Credits} cr";
    }
}
