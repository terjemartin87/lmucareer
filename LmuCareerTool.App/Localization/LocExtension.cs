using System.Windows.Markup;

namespace LmuCareerTool.App.Localization;

/// <summary>XAML-utvidelse for oversatt tekst: {loc:Loc NøkkelNavn}. Løses opp én gang når
/// vinduet konstrueres (InitializeComponent) - språket er fast for hele prosessens levetid
/// (se LanguageStore/ModeSelectWindow-bytteren, som restarter appen ved språkbytte i stedet
/// for å bygge et helt live-rebinding-system for noe som uansett skjer sjelden).</summary>
[MarkupExtensionReturnType(typeof(string))]
public class LocExtension : MarkupExtension
{
    public string Key { get; set; } = "";

    public LocExtension() { }

    public LocExtension(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => Strings.T(Key);
}
