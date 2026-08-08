using System.Windows;
using System.Windows.Controls;
using LmuCareerTool.Career;
using LmuCareerTool.Season;

namespace LmuCareerTool.App;

public partial class SeasonSummaryWindow : Window
{
    private readonly CareerEngine _engine;

    public SeasonSummaryWindow(
        SeasonModel? completedSeason, CareerEngine engine,
        bool droppedByManufacturer = false, bool contractExpired = false)
    {
        InitializeComponent();
        DarkTitleBarHelper.Apply(this);
        _engine = engine;

        if (completedSeason != null)
        {
            TitleText.Text = $"🏆 Sesong {completedSeason.SeasonNumber} fullført!";
            SubTitleText.Text = $"{completedSeason.CarClass}   ·   {completedSeason.TotalPoints} poeng sammenlagt   ·   " +
                                 $"{completedSeason.TotalXp} XP   ·   Beste resultat: P{completedSeason.BestFinish}";
            ResultsGrid.ItemsSource = completedSeason.Events.Select(e => new SeasonResultRow(e)).ToList();

            if (droppedByManufacturer)
            {
                ContractStatusText.Text = "📉 Merket var ikke fornøyd med resultatene og har sagt opp kontrakten din.";
                ContractStatusText.Visibility = Visibility.Visible;
            }
            else if (contractExpired)
            {
                ContractStatusText.Text = "📄 Kontrakten din har løpt ut. Tid for et nytt tilbud.";
                ContractStatusText.Visibility = Visibility.Visible;
            }
        }
        else
        {
            TitleText.Text = "Velkommen!";
            SubTitleText.Text = "Velg klasse og signer en kontrakt for å starte din første sesong.";
            ResultsBorder.Visibility = Visibility.Collapsed;
        }

        ClassComboBox.ItemsSource = engine.Career.UnlockedClasses;
        ClassComboBox.SelectedItem = engine.Career.UnlockedClasses.Contains(engine.Career.CurrentClass)
            ? engine.Career.CurrentClass
            : engine.Career.UnlockedClasses.FirstOrDefault();

        // SelectionChanged trigges av linjen over, som fyller tilbudslisten for valgt klasse.
    }

    private void ClassComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ClassComboBox.SelectedItem is not string selectedClass) return;
        RefreshOffers(selectedClass);
    }

    private void RefreshOffers(string carClass)
    {
        var offers = _engine.GetContractOffers(carClass);

        if (offers.Count == 0)
        {
            OfferList.Visibility = Visibility.Collapsed;
            NoOffersText.Visibility = Visibility.Visible;
            return;
        }

        OfferList.Visibility = Visibility.Visible;
        NoOffersText.Visibility = Visibility.Collapsed;

        OfferList.ItemsSource = offers
            .Select(o => new ContractOfferRowVm(o, canAfford: _engine.Career.Credits >= o.SigningCost))
            .ToList();
    }

    private void SignOfferButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ContractOfferRowVm row }) return;

        if (_engine.SignContract(row.Offer))
        {
            DialogResult = true;
        }
        else
        {
            MessageBox.Show(this, "Ikke nok credits til å signere dette tilbudet.", "Signering feilet",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
