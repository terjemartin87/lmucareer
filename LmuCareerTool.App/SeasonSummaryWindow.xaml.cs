using System.Windows;
using System.Windows.Controls;
using LmuCareerTool.Career;
using LmuCareerTool.Season;

namespace LmuCareerTool.App;

public partial class SeasonSummaryWindow : Window
{
    private readonly CareerEngine _engine;
    private List<ManufacturerRowVm> _manufacturerRows = new();
    private string? _preferredManufacturer;

    public string SelectedClass { get; private set; } = "";
    public string? SelectedManufacturer => _manufacturerRows.FirstOrDefault(r => r.IsSelected)?.Name;

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

        _preferredManufacturer = engine.Career.CurrentManufacturer;

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
        // Husk hva som var valgt før vi bygger listen på nytt (f.eks. etter et kjøp).
        var previouslySelected = SelectedManufacturer ?? _preferredManufacturer;

        var manufacturers = _engine.GetManufacturersForClass(carClass);

        if (manufacturers.Count == 0)
        {
            ManufacturerList.Visibility = Visibility.Collapsed;
            NoManufacturersText.Visibility = Visibility.Visible;
            _manufacturerRows = new List<ManufacturerRowVm>();
            ManufacturerList.ItemsSource = null;
            return;
        }

        ManufacturerList.Visibility = Visibility.Visible;
        NoManufacturersText.Visibility = Visibility.Collapsed;

        _manufacturerRows = manufacturers
            .Select(m => new ManufacturerRowVm(m, canAfford: _engine.Career.Credits >= m.UnlockCost))
            .ToList();
        ManufacturerList.ItemsSource = _manufacturerRows;

        // Behold valgt merke hvis fortsatt gyldig/opplåst, ellers det du allerede kjører for, ellers første opplåste.
        var toSelect = _manufacturerRows.FirstOrDefault(r => r.Name == previouslySelected && r.Unlocked)
                       ?? _manufacturerRows.FirstOrDefault(r => r.Unlocked);
        if (toSelect != null) toSelect.IsSelected = true;
    }

    private void BuyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string manufacturerName } || ClassComboBox.SelectedItem is not string carClass) return;

        var success = _engine.TryBuyManufacturer(carClass, manufacturerName);
        if (success)
        {
            MessageBox.Show(this, $"Du er nå signert med {manufacturerName}!", "Kjøp fullført",
                MessageBoxButton.OK, MessageBoxImage.Information);
            _preferredManufacturer = manufacturerName;
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
        if (manufacturers.Count > 0 && string.IsNullOrEmpty(SelectedManufacturer))
        {
            MessageBox.Show(this, "Velg et merke (klikk radioknappen ved siden av et opplåst merke) først.",
                "Mangler valg", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SelectedClass = selectedClass;
        DialogResult = true;
    }
}
