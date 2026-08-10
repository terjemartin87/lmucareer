using LmuCareerTool.Transfers;

namespace LmuCareerTool.App;

public class ContractOfferRowVm
{
    public ContractOfferRowVm(ContractOffer offer, bool canAfford)
    {
        Offer = offer;

        Title = offer.IsPrivateerSeat ? "Privatlag"
            : offer.IsFreeAgent ? "Fri kjøring"
            : offer.Manufacturer;

        Subtitle = offer.IsFreeAgent ? "Ingen merke-oppsett i denne klassen ennå" : offer.Car;

        BadgeText = offer.IsRenewal ? "FORNYELSE"
            : offer.IsPrivateerSeat ? "BETALT SETE"
            : offer.IsFreeAgent ? "FRI KLASSE"
            : $"INTERESSE {offer.InterestScore}";

        LengthText = offer.IsFreeAgent ? "" : $"{offer.LengthSeasons} sesong{(offer.LengthSeasons != 1 ? "er" : "")}";
        SalaryText = offer.SalaryPerRound > 0 ? $"{offer.SalaryPerRound} cr/runde" : "Ingen lønn";
        GoalText = ContractGoalFormatter.Describe(offer.Goals);
        ReasoningText = offer.Reasoning;

        SignButtonText = offer.SigningCost > 0 ? $"Signer ({offer.SigningCost} cr)" : "Signer";
        CanAfford = canAfford;
    }

    public ContractOffer Offer { get; }
    public string Title { get; }
    public string Subtitle { get; }
    public string BadgeText { get; }
    public string LengthText { get; }
    public string SalaryText { get; }
    public string GoalText { get; }
    public string ReasoningText { get; }
    public string SignButtonText { get; }
    public bool CanAfford { get; }
}
