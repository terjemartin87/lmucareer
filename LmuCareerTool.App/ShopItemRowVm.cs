using LmuCareerTool.Content;

namespace LmuCareerTool.App;

public class ShopItemRowVm
{
    public ShopItemRowVm(ShopItemDefinition item, bool canBuy, bool alreadyOwned)
    {
        Item = item;
        Icon = item.Icon;
        Name = item.Name;
        Description = item.Description;
        CostText = $"{item.Cost} cr";
        AlreadyOwned = alreadyOwned;

        ButtonText = alreadyOwned ? "Ansatt" : "Kjøp";
        CanBuy = canBuy && !alreadyOwned;
    }

    public ShopItemDefinition Item { get; }
    public string Icon { get; }
    public string Name { get; }
    public string Description { get; }
    public string CostText { get; }
    public string ButtonText { get; }
    public bool CanBuy { get; }
    public bool AlreadyOwned { get; }
}
