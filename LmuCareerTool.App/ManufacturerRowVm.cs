using LmuCareerTool.Career;

namespace LmuCareerTool.App;

public class ManufacturerRowVm
{
    public ManufacturerRowVm(ManufacturerStatus status, bool canAfford)
    {
        Name = status.Name;
        Unlocked = status.Unlocked;
        StatusText = status.Unlocked
            ? "Opplåst"
            : $"Krever Rating {status.RatingRequired}";
        BuyText = status.Unlocked ? "" : $"Kjøp for {status.UnlockCost} cr";
        UnlockCost = status.UnlockCost;
        CanAfford = canAfford;
    }

    public string Name { get; }
    public bool Unlocked { get; }
    public string StatusText { get; }
    public string BuyText { get; }
    public int UnlockCost { get; }
    public bool CanAfford { get; }
    public bool ShowBuyButton => !Unlocked;
}
