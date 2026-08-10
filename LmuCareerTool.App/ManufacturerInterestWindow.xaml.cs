using System.Windows;
using LmuCareerTool.Career;

namespace LmuCareerTool.App;

/// <summary>
/// Read-only forhåndsvisning av hvilke merker som er interessert i deg akkurat nå, basert på
/// din nåværende Rating og forrige sesongs resultater - samme beregning som brukes ved faktisk
/// signering, men uten å kunne signere herfra. Svarer på "hvor kommer tilbud fra?".
/// </summary>
public partial class ManufacturerInterestWindow : Window
{
    public ManufacturerInterestWindow(CareerEngine engine)
    {
        InitializeComponent();
        DarkTitleBarHelper.Apply(this);

        var carClass = engine.Career.CurrentClass;
        SubTitleText.Text = $"Klasse: {carClass}   ·   Driver: {engine.Career.DriverRating}   ·   Safety: {engine.Career.SafetyRating}   ·   Credits: {engine.Career.Credits}";

        var offers = engine.GetContractOffers(carClass);
        if (offers.Count == 0)
        {
            OfferList.Visibility = Visibility.Collapsed;
            NoOffersText.Visibility = Visibility.Visible;
            return;
        }

        OfferList.ItemsSource = offers
            .Select(o => new ContractOfferRowVm(o, canAfford: engine.Career.Credits >= o.SigningCost))
            .ToList();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
