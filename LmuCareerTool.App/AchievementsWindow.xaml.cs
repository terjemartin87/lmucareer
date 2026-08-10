using System.Windows;
using LmuCareerTool.Career;

namespace LmuCareerTool.App;

/// <summary>Viser alle prestasjoner i spillet, låste og opplåste, til bruk som et mål å bygge
/// karrieren mot i tillegg til rating/credits/kontrakter.</summary>
public partial class AchievementsWindow : Window
{
    public AchievementsWindow(CareerEngine engine)
    {
        InitializeComponent();
        DarkTitleBarHelper.Apply(this);

        var status = engine.GetAchievementStatus();
        var unlockedCount = status.Count(s => s.Unlocked);
        SubTitleText.Text = $"{unlockedCount} av {status.Count} låst opp";

        AchievementList.ItemsSource = status
            .Select(s => new AchievementRowVm(s.Definition, s.Unlocked))
            .ToList();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
