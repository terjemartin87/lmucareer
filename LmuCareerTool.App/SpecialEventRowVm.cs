using LmuCareerTool.Content;

namespace LmuCareerTool.App;

public class SpecialEventRowVm
{
    public SpecialEventRowVm(SpecialEventDefinition definition, bool canEnroll)
    {
        Definition = definition;
        Name = definition.Name;
        var hours = definition.DurationMinutes / 60.0;
        var minCredits = (int)Math.Round(hours * 500);
        var maxCredits = (int)Math.Round(hours * 1200);
        DetailText = $"{definition.TrackVenue}   ·   {hours:0.#} timer   ·   Inngang: {definition.EntryFeeCredits} cr   ·   " +
                     $"Belønning: {minCredits}-{maxCredits} cr avhengig av resultat";
        CanEnroll = canEnroll;
    }

    public SpecialEventDefinition Definition { get; }
    public string Name { get; }
    public string DetailText { get; }
    public bool CanEnroll { get; }
}
