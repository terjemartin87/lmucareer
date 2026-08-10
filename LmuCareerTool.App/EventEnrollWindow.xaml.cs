using System.Windows;
using System.Windows.Controls;
using LmuCareerTool.Career;
using LmuCareerTool.Content;

namespace LmuCareerTool.App;

/// <summary>Velg klasse og bil for et spesialarrangement. Været tildeles av systemet ved
/// bekreftet påmelding - ikke noe spilleren velger.</summary>
public partial class EventEnrollWindow : Window
{
    private readonly CareerEngine _engine;
    private readonly SpecialEventDefinition _definition;

    public bool DidEnroll { get; private set; }

    public EventEnrollWindow(CareerEngine engine, SpecialEventDefinition definition)
    {
        InitializeComponent();
        DarkTitleBarHelper.Apply(this);
        _engine = engine;
        _definition = definition;

        TitleText.Text = definition.Name;
        var hours = definition.DurationMinutes / 60.0;
        SubTitleText.Text = $"{definition.TrackVenue}   ·   {hours:0.#} timer   ·   Inngang: {definition.EntryFeeCredits} cr   ·   Credits: {engine.Career.Credits}";

        ClassComboBox.ItemsSource = engine.Career.UnlockedClasses;
        ClassComboBox.SelectedItem = engine.Career.UnlockedClasses.Contains(engine.Career.CurrentClass)
            ? engine.Career.CurrentClass
            : engine.Career.UnlockedClasses.FirstOrDefault();
    }

    private void ClassComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ClassComboBox.SelectedItem is not string carClass) return;
        CarComboBox.ItemsSource = _engine.GetCarsForClass(carClass);
        CarComboBox.SelectedIndex = 0;
    }

    private void EnrollButton_Click(object sender, RoutedEventArgs e)
    {
        if (ClassComboBox.SelectedItem is not string carClass || CarComboBox.SelectedItem is not string car)
        {
            ShowError("Velg klasse og bil først.");
            return;
        }

        if (!_engine.TryEnrollSpecialEvent(_definition, carClass, car))
        {
            ShowError("Ikke nok credits, eller du har allerede en aktiv påmelding.");
            return;
        }

        DidEnroll = true;
        Close();
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}
