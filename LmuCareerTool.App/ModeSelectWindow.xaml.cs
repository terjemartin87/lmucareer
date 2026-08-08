using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using LmuCareerTool.App.Localization;

namespace LmuCareerTool.App;

/// <summary>
/// Aller første skjermbilde. Skiller de to modusene helt fra hverandre allerede her - Karriere
/// modus og Liga modus deler verken lagringsfiler, UI-vinduer eller fremgangssystem, kun
/// XML-parseren av LMUs resultatfiler nede i Core.
/// </summary>
public partial class ModeSelectWindow : Window
{
    private bool _suppressLanguageChange = true;

    public ModeSelectWindow()
    {
        InitializeComponent();
        DarkTitleBarHelper.Apply(this);

        if (Strings.Current == AppLanguage.English) LangEnButton.IsChecked = true;
        else LangNoButton.IsChecked = true;
        _suppressLanguageChange = false;
    }

    private void CareerCard_Click(object sender, MouseButtonEventArgs e) => GoToCareer();
    private void CareerCard_Click(object sender, RoutedEventArgs e) => GoToCareer();

    private void LeagueCard_Click(object sender, MouseButtonEventArgs e) => GoToLeague();
    private void LeagueCard_Click(object sender, RoutedEventArgs e) => GoToLeague();

    private void LangNoButton_Checked(object sender, RoutedEventArgs e) => SwitchLanguage(AppLanguage.Norwegian);
    private void LangEnButton_Checked(object sender, RoutedEventArgs e) => SwitchLanguage(AppLanguage.English);

    private void SwitchLanguage(AppLanguage language)
    {
        if (_suppressLanguageChange || language == Strings.Current) return;

        LanguageStore.Save(language);

        // Enkleste vei til en fullstendig "oversatt" UI: restart appen i stedet for å bygge et
        // live-rebinding-system for noe som uansett skjer sjelden (én gang, kanskje to).
        Process.Start(Environment.ProcessPath!);
        Application.Current.Shutdown();
    }

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
