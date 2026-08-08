using System.Windows;
using System.Windows.Threading;
using LmuCareerTool.App.Localization;

namespace LmuCareerTool.App;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Strings.Initialize(LanguageStore.Load());
        new ModeSelectWindow().Show();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(e.Exception.ToString(), Strings.T("Common_UnexpectedErrorUiThread"), MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        MessageBox.Show(e.ExceptionObject?.ToString() ?? Strings.T("Common_UnknownError"), Strings.T("Common_UnexpectedErrorBackground"), MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
