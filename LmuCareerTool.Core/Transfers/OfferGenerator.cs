using LmuCareerTool.Championship;
using LmuCareerTool.Content;
using LmuCareerTool.Models;
using LmuCareerTool.Season;

namespace LmuCareerTool.Transfers;

/// <summary>
/// Bygger de konkrete tilbudene som vises i overgangsvinduet ved sesongslutt (og ved
/// førstegangsoppsett, hvor alt blir "nye" tilbud siden det ikke finnes noen kontrakt ennå).
/// </summary>
public static class OfferGenerator
{
    private const int MinInterestForOffer = 50;
    private const int PrivateerSigningCost = 1500;

    public static List<ContractOffer> GenerateOffers(
        CareerProfile career, GameContent content, string carClass, SeasonModel? lastSeason, string playerName)
    {
        var offers = new List<ContractOffer>();

        var classDef = content.Classes.FirstOrDefault(c => c.Name.Equals(carClass, StringComparison.OrdinalIgnoreCase));
        if (classDef == null) return offers;

        if (classDef.Manufacturers.Count == 0)
        {
            // Ingen merke-oppsett ennå i denne klassen (LMP2/LMP3/Hypercar) - ingen reell
            // forhandling, bare fri kjøring med tilfeldig bil fra klassens billiste.
            offers.Add(new ContractOffer
            {
                Manufacturer = "",
                CarClass = carClass,
                InterestScore = 100,
                LengthSeasons = 1,
                GoalDescription = "Ingen merke-oppsett i denne klassen ennå - ingen krav.",
                Reasoning = "Denne klassen har ikke et fullt kontraktssystem ennå. Du kjører fritt uten fabrikkstøtte.",
                IsFreeAgent = true,
            });
            return offers;
        }

        var lastSeasonRaces = lastSeason == null
            ? new List<CareerRaceEntry>()
            : career.RaceHistory.Where(r => r.SeasonNumber == lastSeason.SeasonNumber).ToList();

        foreach (var manufacturer in classDef.Manufacturers)
        {
            var isCurrent = string.Equals(
                career.CurrentContract?.Manufacturer, manufacturer.Name, StringComparison.OrdinalIgnoreCase);

            if (isCurrent)
            {
                if (WasGoalMet(career.CurrentContract!, lastSeason, playerName))
                    offers.Add(BuildRenewalOffer(career.CurrentContract!, manufacturer));

                continue; // enten fornyelse over, eller de er ikke fornøyd - enten vei, ikke også et "nytt" tilbud
            }

            var interest = InterestModel.ComputeInterest(career, manufacturer, lastSeason, lastSeasonRaces);
            if (interest < MinInterestForOffer) continue;

            offers.Add(BuildNewOffer(manufacturer, carClass, interest));
        }

        offers.Add(BuildPrivateerOffer(classDef, carClass));

        return offers
            .OrderByDescending(o => o.IsRenewal)
            .ThenByDescending(o => o.InterestScore)
            .ToList();
    }

    /// <summary>
    /// Sjekker om forrige sesongs sesongmål ble innfridd, ved å se hvor du faktisk endte i
    /// førermesterskapstabellen (Fase 2) den sesongen.
    /// </summary>
    public static bool WasGoalMet(Contract contract, SeasonModel? lastSeason, string playerName)
    {
        if (lastSeason == null || contract.GoalTargetPosition <= 0) return true;

        var standings = ChampionshipTable.ComputeDriverStandings(lastSeason, playerName);
        var playerIndex = standings.FindIndex(s => s.IsPlayer);
        if (playerIndex < 0) return true; // fant deg ikke i tabellen (f.eks. ingen fullførte runder) - ikke straff for det

        return playerIndex + 1 <= contract.GoalTargetPosition;
    }

    private static ContractOffer BuildNewOffer(ManufacturerDefinition manufacturer, string carClass, int interest)
    {
        var length = interest >= 80 ? 3 : interest >= 65 ? 2 : 1;
        var salary = 150 + interest * 6;
        var goalPosition = interest >= 80 ? 3 : interest >= 65 ? 5 : 8;

        return new ContractOffer
        {
            Manufacturer = manufacturer.Name,
            CarClass = carClass,
            Car = manufacturer.Cars.FirstOrDefault() ?? "",
            InterestScore = interest,
            LengthSeasons = length,
            SalaryPerRound = salary,
            GoalTargetPosition = goalPosition,
            GoalDescription = $"Topp {goalPosition} sammenlagt i førermesterskapet",
            Reasoning = interest switch
            {
                >= 85 => "De har fulgt deg tett og vil ha deg som fast fører.",
                >= 70 => "Solide resultater har gjort dem interessert i et samarbeid.",
                _ => "De ser potensial og tilbyr deg en sjanse til å bevise deg."
            },
        };
    }

    private static ContractOffer BuildRenewalOffer(Contract current, ManufacturerDefinition manufacturer)
    {
        var nextGoal = Math.Max(1, current.GoalTargetPosition - 1);

        return new ContractOffer
        {
            Manufacturer = current.Manufacturer,
            CarClass = current.CarClass,
            Car = manufacturer.Cars.FirstOrDefault() ?? current.Car,
            InterestScore = 100,
            LengthSeasons = current.SeasonsRemaining > 0 ? current.SeasonsRemaining : 1,
            SalaryPerRound = current.SalaryPerRound + 25,
            GoalTargetPosition = nextGoal,
            GoalDescription = $"Topp {nextGoal} sammenlagt i førermesterskapet",
            Reasoning = "Fornøyd med samarbeidet forrige sesong - de vil fortsette med deg.",
            IsRenewal = true,
        };
    }

    private static ContractOffer BuildPrivateerOffer(ClassDefinition classDef, string carClass)
    {
        var car = classDef.Manufacturers.FirstOrDefault()?.Cars.FirstOrDefault() ?? classDef.Cars.FirstOrDefault() ?? "";

        return new ContractOffer
        {
            Manufacturer = "Privatlag",
            CarClass = carClass,
            Car = car,
            InterestScore = 0,
            LengthSeasons = 1,
            SalaryPerRound = 0,
            GoalTargetPosition = 0,
            GoalDescription = "Ingen krav - dette er et betalt sete uten fabrikkstøtte.",
            Reasoning = "Ingen fabrikkontrakt i sikte? Et privatlag tar deg imot mot betaling.",
            IsPrivateerSeat = true,
            SigningCost = PrivateerSigningCost,
        };
    }
}
