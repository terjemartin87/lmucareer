using System.Windows;
using System.Windows.Input;

namespace LmuCareerTool.App;

/// <summary>
/// Aller første skjermbilde. Skiller de to modusene helt fra hverandre allerede her - Karriere
/// modus og Liga modus deler verken lagringsfiler, UI-vinduer eller fremgangssystem, kun
/// XML-parseren av LMUs resultatfiler nede i Core.
/// </summary>
public partial class ModeSelectWindow : Window
{
    public ModeSelectWindow()
    {
        InitializeComponent();
        DarkTitleBarHelper.Apply(this);
    }

    private void CareerCard_Click(object sender, MouseButtonEventArgs e) => GoToCareer();
    private void CareerCard_Click(object sender, RoutedEventArgs e) => GoToCareer();

    private void LeagueCard_Click(object sender, MouseButtonEventArgs e) => GoToLeague();
    private void LeagueCard_Click(object sender, RoutedEventArgs e) => GoToLeague();

    private void GoToCareer()
    {
        new WelcomeWindow().Show();
        Close();
    }

    private void GoToLeague()
    {
        new LeagueWelcomeWindow().Show();
        Close();
    }
}
