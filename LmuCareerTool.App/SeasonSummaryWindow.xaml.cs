using System.Windows;
using System.Windows.Controls;
using LmuCareerTool.Career;
using LmuCareerTool.Season;

namespace LmuCareerTool.App;

public partial class SeasonSummaryWindow : Window
{
    private readonly CareerEngine _engine;
    private string? _selectedManufacturer;

    public string SelectedClass { get; private set; } = "";
    public string? SelectedManufacturer => _selectedManufacturer;

    public SeasonSummaryWindow(SeasonModel? completedSeason, CareerEngine engine)
    {
        InitializeComponent();
        _engine = engine;

        if (completedSeason != null)
        {
            TitleText.Text = $"🏆 Sesong {completedSeason.SeasonNumber} fullført!";
            SubTitleText.Text = $"{completedSeason.CarClass}   ·   {completedSeason.TotalPoints} poeng sammenlagt   ·   " +
                                 $"{completedSeason.TotalXp} XP   ·   Beste resultat: P{completedSeason.BestFinish}";
            ResultsGrid.ItemsSource = completedSeason.Events.Select(e => new SeasonResultRow(e)).ToList();
        }
        else
        {
            TitleText.Text = "Velkommen!";
            SubTitleText.Text = "Velg klasse og merke for å starte din første sesong.";
            ResultsBorder.Visibility = Visibility.Collapsed;
        }

        ClassComboBox.ItemsSource = engine.Career.UnlockedClasses;
        ClassComboBox.SelectedItem = engine.Career.UnlockedClasses.Contains(engine.Career.CurrentClass)
            ? engine.Career.CurrentClass
            : engine.Career.UnlockedClasses.FirstOrDefault();

        // SelectionChanged trigges av linjen over, som fyller merkelisten for valgt klasse.
    }

    private void ClassComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ClassComboBox.SelectedItem is not string selectedClass) return;
        RefreshManufacturerList(selectedClass);
    }

    private void RefreshManufacturerList(string carClass)
    {
        var manufacturers = _engine.GetManufacturersForClass(carClass);

        if (manufacturers.Count == 0)
        {
            ManufacturerList.Visibility = Visibility.Collapsed;
            NoManufacturersText.Visibility = Visibility.Visible;
            _selectedManufacturer = null;
            return;
        }

        ManufacturerList.Visibility = Visibility.Visible;
        NoManufacturersText.Visibility = Visibility.Collapsed;

        var rows = manufacturers
            .Select(m => new ManufacturerRowVm(m, canAfford: _engine.Career.Credits >= m.UnlockCost))
            .ToList();
        ManufacturerList.ItemsSource = rows;

        // Behold valgt merke hvis fortsatt gyldig/opplåst, ellers velg det du allerede kjører for, ellers første opplåste.
        var stillValid = rows.FirstOrDefault(r => r.Name == _selectedManufacturer && r.Unlocked);
        if (stillValid == null)
        {
            var preferred = rows.FirstOrDefault(r => r.Name == _engine.Career.CurrentManufacturer && r.Unlocked)
                             ?? rows.FirstOrDefault(r => r.Unlocked);
            _selectedManufacturer = preferred?.Name;
        }

        // Merk riktig RadioButton visuelt ved å bygge listen på nytt (databinding trigger IsEnabled korrekt;
        // faktisk "checked"-tilstand styres av brukerklikk via ManufacturerRadio_Checked - førstegang settes ingen
        // radioknapp automatisk merket, så be brukeren klikke hvis feltet er tomt).
    }

    private void ManufacturerRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string name })
        {
            _selectedManufacturer = name;
        }
    }

    private void BuyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string manufacturerName } || ClassComboBox.SelectedItem is not string carClass) return;

        var success = _engine.TryBuyManufacturer(carClass, manufacturerName);
        if (success)
        {
            MessageBox.Show(this, $"Du er nå signert med {manufacturerName}!", "Kjøp fullført",
                MessageBoxButton.OK, MessageBoxImage.Information);
            _selectedManufacturer = manufacturerName;
            RefreshManufacturerList(carClass);
        }
        else
        {
            MessageBox.Show(this, "Ikke nok credits, eller merket finnes ikke.", "Kjøp feilet",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (ClassComboBox.SelectedItem is not string selectedClass)
        {
            MessageBox.Show(this, "Velg en klasse først.", "Mangler valg", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var manufacturers = _engine.GetManufacturersForClass(selectedClass);
        if (manufacturers.Count > 0 && string.IsNullOrEmpty(_selectedManufacturer))
        {
            MessageBox.Show(this, "Velg et merke (klikk radioknappen ved siden av et opplåst merke) først.",
                "Mangler valg", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SelectedClass = selectedClass;
        DialogResult = true;
    }
}
