using System.Windows;
using System.Windows.Threading;

namespace LmuCareerTool.App;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(e.Exception.ToString(), "Uventet feil (UI-tråd)", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        MessageBox.Show(e.ExceptionObject?.ToString() ?? "Ukjent feil", "Uventet feil (bakgrunn)", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
