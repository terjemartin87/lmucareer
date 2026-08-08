using LmuCareerTool.Models;
using LmuCareerTool.Season;

namespace LmuCareerTool.Transfers;

/// <summary>Håndterer hele livssyklusen til en kontrakt: signering, lønn, oppsigelse.</summary>
public static class ContractService
{
    private const int PrivateerSigningRatingPenalty = 3;

    /// <summary>Forsøker å signere et tilbud. Returnerer false hvis du ikke har råd til
    /// engangskostnaden (kun privatlag-setet har en).</summary>
    public static bool TrySign(CareerProfile career, ContractOffer offer, int seasonNumber)
    {
        if (offer.SigningCost > 0)
        {
            if (career.Credits < offer.SigningCost) return false;
            career.Credits -= offer.SigningCost;
        }

        if (career.CurrentContract != null)
            career.ContractHistory.Add(career.CurrentContract);

        career.CurrentContract = new Contract
        {
            Manufacturer = offer.Manufacturer,
            CarClass = offer.CarClass,
            Car = offer.Car,
            SeasonsRemaining = offer.LengthSeasons,
            SalaryPerRound = offer.SalaryPerRound,
            GoalTargetPosition = offer.GoalTargetPosition,
            GoalDescription = offer.GoalDescription,
            SignedInSeasonNumber = seasonNumber,
            IsPrivateerSeat = offer.IsPrivateerSeat,
            IsFreeAgent = offer.IsFreeAgent,
        };
        career.CurrentManufacturer = string.IsNullOrEmpty(offer.Manufacturer) ? null : offer.Manufacturer;
        career.CurrentClass = offer.CarClass;

        if (offer.IsPrivateerSeat)
            career.DriverRating = RatingCalculatorClamp(career.DriverRating - PrivateerSigningRatingPenalty);

        return true;
    }

    /// <summary>Betaler kontraktlønn for én fullført sesongrunde. Returnerer beløpet som ble betalt.</summary>
    public static int PaySalaryForRound(CareerProfile career)
    {
        var salary = career.CurrentContract?.SalaryPerRound ?? 0;
        if (salary > 0) career.Credits += salary;
        return salary;
    }

    /// <summary>Bruddsum for å si opp kontrakten før tiden - dyrere jo flere sesonger som gjenstår.</summary>
    public static int CalculateBreakFee(Contract contract) => Math.Max(contract.SeasonsRemaining, 1) * 1500;

    public static bool TryTerminateEarly(CareerProfile career)
    {
        if (career.CurrentContract is not { IsPrivateerSeat: false, IsFreeAgent: false } contract) return false;

        var fee = CalculateBreakFee(contract);
        if (career.Credits < fee) return false;

        career.Credits -= fee;
        career.ContractHistory.Add(contract);
        career.CurrentContract = null;
        career.CurrentManufacturer = null;
        return true;
    }

    /// <summary>
    /// Kalles når en sesong fullføres mens en kontrakt er aktiv. Trekker ned gjenværende lengde,
    /// og returnerer true hvis merket sier deg opp fordi sesongmålet ikke ble innfridd.
    /// </summary>
    public static bool ApplySeasonResult(CareerProfile career, SeasonModel completedSeason, string playerName)
    {
        var contract = career.CurrentContract;
        if (contract == null) return false;

        contract.SeasonsRemaining = Math.Max(0, contract.SeasonsRemaining - 1);

        var goalMet = OfferGenerator.WasGoalMet(contract, completedSeason, playerName);
        var droppedForMissedGoal = !goalMet && !contract.IsPrivateerSeat && !contract.IsFreeAgent;

        if (contract.SeasonsRemaining <= 0 || droppedForMissedGoal)
        {
            career.ContractHistory.Add(contract);
            career.CurrentContract = null;
            career.CurrentManufacturer = null;
        }

        return droppedForMissedGoal;
    }

    private static int RatingCalculatorClamp(int value) => Math.Clamp(value, 0, 100);
}
