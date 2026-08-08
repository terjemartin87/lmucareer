using System.Windows.Media;

namespace LmuCareerTool.App;

public class ToastVm
{
    public ToastVm(string icon, string message, Brush accentBrush)
    {
        Icon = icon;
        Message = message;
        AccentBrush = accentBrush;
    }

    public string Icon { get; }
    public string Message { get; }
    public Brush AccentBrush { get; }
}
