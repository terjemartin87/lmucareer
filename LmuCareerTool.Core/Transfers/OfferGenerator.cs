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
    /// <summary>Terskel for at et merke faktisk sender et tilbud - brukes også av TransferMarketService.</summary>
    public const int MinInterestForOffer = 50;
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
                Goals = new List<ContractGoal>(),
                Reasoning = "Denne klassen har ikke et fullt kontraktssystem ennå. Du kjører fritt uten fabrikkstøtte.",
                IsFreeAgent = true,
            });
            return offers;
        }

        foreach (var manufacturer in classDef.Manufacturers)
        {
            var isCurrent = string.Equals(
                career.CurrentContract?.Manufacturer, manufacturer.Name, StringComparison.OrdinalIgnoreCase);

            if (isCurrent)
            {
                if (WasGoalMet(career, career.CurrentContract!, lastSeason, playerName))
                    offers.Add(BuildRenewalOffer(career.CurrentContract!, manufacturer));

                continue; // enten fornyelse over, eller de er ikke fornøyd - enten vei, ikke også et "nytt" tilbud
            }

            var interest = InterestModel.ComputeInterest(career, manufacturer, lastSeason);
            if (interest < MinInterestForOffer) continue;

            offers.Add(BuildNewOffer(manufacturer, carClass, interest));
        }

        offers.Add(BuildPrivateerOffer(classDef, carClass, career.HasManager));

        return offers
            .OrderByDescending(o => o.IsRenewal)
            .ThenByDescending(o => o.InterestScore)
            .ToList();
    }

    /// <summary>
    /// Sjekker om ALLE krav i kontrakten ble innfridd forrige sesong - posisjon/pallplasser/
    /// seire fra mesterskapstabellen, Safety Rating fra karrieren slik den står nå (som er
    /// verdien rett etter sesongens siste løp, siden rating oppdateres per løp underveis).
    /// </summary>
    public static bool WasGoalMet(CareerProfile career, Contract contract, SeasonModel? lastSeason, string playerName)
    {
        if (lastSeason == null || contract.Goals.Count == 0) return true;

        var standings = ChampionshipTable.ComputeDriverStandings(lastSeason, playerName);
        var playerIndex = standings.FindIndex(s => s.IsPlayer);
        if (playerIndex < 0) return true; // fant deg ikke i tabellen (f.eks. ingen fullførte runder) - ikke straff for det

        var playerEntry = standings[playerIndex];
        var playerPosition = playerIndex + 1;

        return contract.Goals.All(goal => goal.Type switch
        {
            ContractGoalType.MinChampionshipPosition => playerPosition <= goal.TargetValue,
            ContractGoalType.MinPodiums => playerEntry.Podiums >= goal.TargetValue,
            ContractGoalType.MinWins => playerEntry.Wins >= goal.TargetValue,
            ContractGoalType.MinSafetyRating => career.SafetyRating >= goal.TargetValue,
            _ => true,
        });
    }

    /// <summary>
    /// Bygger kravene for et tilbud. Prestisjetunge merker (høy RatingRequired) stiller flere
    /// og strengere krav enn ferske merker - det er her "forskjellige krav per merke" kommer fra.
    /// </summary>
    private static List<ContractGoal> BuildGoals(ManufacturerDefinition manufacturer, int interest)
    {
        var goals = new List<ContractGoal>();

        var goalPosition = interest >= 80 ? 3 : interest >= 65 ? 5 : 8;
        goals.Add(new ContractGoal
        {
            Type = ContractGoalType.MinChampionshipPosition,
            TargetValue = goalPosition,
            Description = $"Topp {goalPosition} sammenlagt i førermesterskapet",
        });

        if (manufacturer.RatingRequired >= 50)
        {
            var minSafety = manufacturer.RatingRequired >= 70 ? 65 : 55;
            goals.Add(new ContractGoal
            {
                Type = ContractGoalType.MinSafetyRating,
                TargetValue = minSafety,
                Description = $"Safety Rating på minst {minSafety}",
            });
        }

        if (interest >= 80)
        {
            goals.Add(new ContractGoal
            {
                Type = ContractGoalType.MinPodiums,
                TargetValue = 1,
                Description = "Minst 1 pallplass i løpet av sesongen",
            });
        }

        return goals;
    }

    private static ContractOffer BuildNewOffer(ManufacturerDefinition manufacturer, string carClass, int interest)
    {
        var length = interest >= 80 ? 3 : interest >= 65 ? 2 : 1;
        var salary = 150 + interest * 6;

        return new ContractOffer
        {
            Manufacturer = manufacturer.Name,
            CarClass = carClass,
            Car = manufacturer.Cars.FirstOrDefault() ?? "",
            InterestScore = interest,
            LengthSeasons = length,
            SalaryPerRound = salary,
            Goals = BuildGoals(manufacturer, interest),
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
        // Fornyelse regnes med toppinteresse (100) for kravstrenghet, men posisjonskravet
        // strammes inn ett hakk fra forrige kontrakt i stedet for å hoppe rett til 3.
        var goals = BuildGoals(manufacturer, interest: 100);
        var previousPositionGoal = current.Goals
            .FirstOrDefault(g => g.Type == ContractGoalType.MinChampionshipPosition)?.TargetValue ?? 8;
        var nextPositionGoal = Math.Max(1, previousPositionGoal - 1);
        var positionGoal = goals.First(g => g.Type == ContractGoalType.MinChampionshipPosition);
        positionGoal.TargetValue = nextPositionGoal;
        positionGoal.Description = $"Topp {nextPositionGoal} sammenlagt i førermesterskapet";

        return new ContractOffer
        {
            Manufacturer = current.Manufacturer,
            CarClass = current.CarClass,
            Car = manufacturer.Cars.FirstOrDefault() ?? current.Car,
            InterestScore = 100,
            LengthSeasons = current.SeasonsRemaining > 0 ? current.SeasonsRemaining : 1,
            SalaryPerRound = current.SalaryPerRound + 25,
            Goals = goals,
            Reasoning = "Fornøyd med samarbeidet forrige sesong - de vil fortsette med deg.",
            IsRenewal = true,
        };
    }

    private static ContractOffer BuildPrivateerOffer(ClassDefinition classDef, string carClass, bool hasManager)
    {
        var car = classDef.Manufacturers.FirstOrDefault()?.Cars.FirstOrDefault() ?? classDef.Cars.FirstOrDefault() ?? "";
        var signingCost = hasManager ? (int)Math.Round(PrivateerSigningCost * 0.75) : PrivateerSigningCost;

        return new ContractOffer
        {
            Manufacturer = "Privatlag",
            CarClass = carClass,
            Car = car,
            InterestScore = 0,
            LengthSeasons = 1,
            SalaryPerRound = 0,
            Goals = new List<ContractGoal>(),
            Reasoning = hasManager
                ? "Ingen fabrikkontrakt i sikte? Din manager har forhandlet frem en billigere avtale hos et privatlag."
                : "Ingen fabrikkontrakt i sikte? Et privatlag tar deg imot mot betaling.",
            IsPrivateerSeat = true,
            SigningCost = signingCost,
        };
    }
}
