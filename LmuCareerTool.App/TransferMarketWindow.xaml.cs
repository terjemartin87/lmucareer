using System.Windows;
using LmuCareerTool.Career;

namespace LmuCareerTool.App;

/// <summary>
/// Viser hele transfer-markedet for spillerens klasse: alle merker, ikke bare de som allerede
/// sender tilbud, slik at spilleren ser hva som mangler for å bli attraktiv for et bestemt merke.
/// </summary>
public partial class TransferMarketWindow : Window
{
    public TransferMarketWindow(CareerEngine engine)
    {
        InitializeComponent();
        DarkTitleBarHelper.Apply(this);

        var carClass = engine.Career.CurrentClass;
        SubTitleText.Text = $"Klasse: {carClass}   ·   Driver: {engine.Career.DriverRating}   ·   Safety: {engine.Career.SafetyRating}";

        var entries = engine.GetManufacturerInterest(carClass);
        if (entries.Count == 0)
        {
            ManufacturerList.Visibility = Visibility.Collapsed;
            NoManufacturersText.Visibility = Visibility.Visible;
            return;
        }

        ManufacturerList.ItemsSource = entries.Select(e => new TransferMarketRowVm(e)).ToList();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
