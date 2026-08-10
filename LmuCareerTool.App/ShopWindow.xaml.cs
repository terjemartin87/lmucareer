using System.Windows;
using System.Windows.Controls;
using LmuCareerTool.Career;

namespace LmuCareerTool.App;

/// <summary>Coaching/manager-butikken: bruk credits på engangsboost til rating/XP, eller
/// ansett en manager for varig bedre kontraktsforhandlinger.</summary>
public partial class ShopWindow : Window
{
    private readonly CareerEngine _engine;

    public ShopWindow(CareerEngine engine)
    {
        InitializeComponent();
        DarkTitleBarHelper.Apply(this);
        _engine = engine;

        Refresh();
    }

    private void Refresh()
    {
        var career = _engine.Career;
        SubTitleText.Text = $"Credits: {career.Credits}   ·   Driver: {career.DriverRating}   ·   Safety: {career.SafetyRating}" +
                             (career.HasManager ? "   ·   Manager ansatt ✔" : "");

        ShopList.ItemsSource = _engine.GetShopItems()
            .Select(item => new ShopItemRowVm(
                item,
                canBuy: ShopService.CanPurchase(career, item),
                alreadyOwned: !item.Repeatable && career.PurchasedShopItems.Contains(item.Id)))
            .ToList();
    }

    private void BuyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ShopItemRowVm row }) return;

        if (_engine.TryPurchaseShopItem(row.Item))
        {
            Refresh();
        }
        else
        {
            MessageBox.Show(this, "Ikke nok credits til å kjøpe dette.", "Kjøp feilet",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
