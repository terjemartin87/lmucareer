using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace LmuCareerTool.App;

/// <summary>Ber Windows tegne vindusrammen (tittellinje) i mørk modus, slik at den ikke lyser
/// opp som en hvit stripe over en ellers gjennomført mørk app. Ingen effekt på Windows-versjoner
/// uten støtte (før 1809) - da beholdes bare den vanlige lyse rammen.</summary>
public static class DarkTitleBarHelper
{
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaUseImmersiveDarkModeOld = 19;

    public static void Apply(Window window)
    {
        window.SourceInitialized += (_, _) =>
        {
            try
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero) return;

                var enabled = 1;
                if (DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref enabled, sizeof(int)) != 0)
                    DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkModeOld, ref enabled, sizeof(int));
            }
            catch
            {
                // Eldre Windows-versjon uten støtte - behold standard tittellinje.
            }
        };
    }
}
