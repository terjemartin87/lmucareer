using LmuCareerTool.Content;
using LmuCareerTool.Models;
using LmuCareerTool.Season;

namespace LmuCareerTool.Transfers;

/// <summary>
/// Bygger den fulle oversikten over alle merker i en klasse for transfer-markedet - i motsetning
/// til OfferGenerator viser denne ALLE merker (også de under tilbudsterskelen), slik at spilleren
/// ser hva som mangler for å bli attraktiv.
/// </summary>
public static class TransferMarketService
{
    public static List<ManufacturerInterestEntry> BuildEntries(
        CareerProfile career, GameContent content, string carClass, SeasonModel? lastSeason)
    {
        var classDef = content.Classes.FirstOrDefault(c => c.Name.Equals(carClass, StringComparison.OrdinalIgnoreCase));
        if (classDef == null) return new List<ManufacturerInterestEntry>();

        var entries = classDef.Manufacturers.Select(manufacturer =>
        {
            var freshInterest = InterestModel.ComputeInterest(career, manufacturer, lastSeason);
            var remembered = ReputationService.UpdateAndGet(career, manufacturer.Name, freshInterest);

            return new ManufacturerInterestEntry
            {
                Name = manufacturer.Name,
                RatingRequired = manufacturer.RatingRequired,
                Interest = remembered,
                RatingGap = career.DriverRating - manufacturer.RatingRequired,
                IsCurrentManufacturer = string.Equals(
                    career.CurrentManufacturer, manufacturer.Name, StringComparison.OrdinalIgnoreCase),
                WouldSendOffer = remembered >= OfferGenerator.MinInterestForOffer,
            };
        });

        return entries
            .OrderByDescending(e => e.IsCurrentManufacturer)
            .ThenByDescending(e => e.Interest)
            .ToList();
    }
}
