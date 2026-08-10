using System.Windows;
using System.Windows.Media;
using LmuCareerTool.Transfers;

namespace LmuCareerTool.App;

public class TransferMarketRowVm
{
    public TransferMarketRowVm(ManufacturerInterestEntry entry)
    {
        Name = entry.Name;
        Interest = entry.Interest;

        BadgeText = entry.IsCurrentManufacturer ? "DIN KONTRAKT"
            : entry.WouldSendOffer ? "SENDER TILBUD"
            : "BYGGER RELASJON";

        var badgeColorKey = entry.IsCurrentManufacturer ? "AccentGold"
            : entry.WouldSendOffer ? "GoodColor"
            : "MutedTextColor";
        BadgeColor = (Brush)Application.Current.FindResource(badgeColorKey);

        GapText = entry.RatingGap >= 0
            ? $"Driver Rating {entry.RatingRequired} kreves - du har {entry.RatingRequired + entry.RatingGap} (innfridd, +{entry.RatingGap})"
            : $"Driver Rating {entry.RatingRequired} kreves - du har {entry.RatingRequired + entry.RatingGap} (mangler {-entry.RatingGap})";

        InterestText = $"Interesse: {entry.Interest}/100   ·   Tilbud sendes ved ≥ {OfferGenerator.MinInterestForOffer}";
    }

    public string Name { get; }
    public int Interest { get; }
    public string BadgeText { get; }
    public Brush BadgeColor { get; }
    public string GapText { get; }
    public string InterestText { get; }
}
