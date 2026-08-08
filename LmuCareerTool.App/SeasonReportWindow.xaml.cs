using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LmuCareerTool.Career;
using LmuCareerTool.Models;
using Microsoft.Win32;

namespace LmuCareerTool.App;

public partial class SeasonReportWindow : Window
{
    private readonly SeasonReport _report;
    private readonly string _driverName;

    public SeasonReportWindow(SeasonReport report, string driverName)
    {
        InitializeComponent();
        DarkTitleBarHelper.Apply(this);
        _report = report;
        _driverName = driverName;

        TitleText.Text = $"🏆 Sesong {report.SeasonNumber} fullført!";
        SubTitleText.Text = $"{report.CarClass}" + (report.Manufacturer != null ? $"   ·   {report.Manufacturer}" : "");
        ContractSummaryText.Text = report.ContractSummary;

        TileList.ItemsSource = new List<StatTileVm>
        {
            new("PLASSERING", report.ChampionshipPosition > 0 ? $"P{report.ChampionshipPosition} / {report.TotalDrivers}" : "-"),
            new("POENG", report.Points.ToString()),
            new("SEIRE", report.Wins.ToString()),
            new("PODIER", report.Podiums.ToString()),
            new("POLE POSITIONS", report.PolePositions.ToString()),
            new("DNF", report.Dnfs.ToString()),
            new("SNITTPLASSERING", report.AverageFinish > 0 ? $"P{report.AverageFinish:0.#}" : "-"),
            new("DISTANSE", $"{report.TotalDistanceKm:0} km"),
        };

        if (report.Awards.Count > 0)
        {
            AwardsList.ItemsSource = report.Awards.Select(a => new AwardRowVm(a)).ToList();
        }
        else
        {
            AwardsSection.Visibility = Visibility.Collapsed;
        }

        RoundsGrid.ItemsSource = report.Rounds.Select(r => new RoundReportRowVm(r)).ToList();

        DriverGrid.ItemsSource = report.DriverStandings
            .Select((d, i) => new DriverStandingRowVm(i + 1, d, previousPosition: null, driverName))
            .ToList();

        ManufacturerGrid.ItemsSource = report.ManufacturerStandings
            .Select((m, i) => new ManufacturerStandingRowVm(i + 1, m))
            .ToList();

        if (report.TrackRecords.Count > 0)
        {
            TrackRecordsGrid.ItemsSource = report.TrackRecords.Select(r => new TrackRecordRowVm(r)).ToList();
        }
        else
        {
            TrackRecordsSection.Visibility = Visibility.Collapsed;
        }
    }

    private void ContinueButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void SaveImageButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PNG-bilde (*.png)|*.png",
            FileName = $"Sesong_{_report.SeasonNumber}_rapport.png"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            var width = (int)Math.Ceiling(ReportContent.ActualWidth);
            var height = (int)Math.Ceiling(ReportContent.ActualHeight);
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(ReportContent);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = File.Create(dialog.FileName);
            encoder.Save(stream);

            MessageBox.Show(this, "Rapporten er lagret som bilde.", "Lagret", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Klarte ikke å lagre bildet: {ex.Message}", "Feil", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveHtmlButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "HTML-fil (*.html)|*.html",
            FileName = $"Sesong_{_report.SeasonNumber}_rapport.html"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            File.WriteAllText(dialog.FileName, SeasonReportHtmlBuilder.Build(_report, _driverName));
            MessageBox.Show(this, "Rapporten er lagret som HTML.", "Lagret", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Klarte ikke å lagre HTML-filen: {ex.Message}", "Feil", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
