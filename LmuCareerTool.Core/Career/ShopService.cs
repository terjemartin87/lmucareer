using LmuCareerTool.Content;
using LmuCareerTool.Models;

namespace LmuCareerTool.Career;

/// <summary>
/// Coaching/manager-butikken: bruk credits på engangsboost til XP/rating, eller ansett en
/// manager for en varig passiv fordel i kontraktsforhandlinger. Speiler ClassUnlockService/
/// AchievementService-mønsteret (data-drevne definisjoner, mutasjon + resultat tilbake).
/// </summary>
public static class ShopService
{
    public static bool CanPurchase(CareerProfile career, ShopItemDefinition item)
    {
        if (career.Credits < item.Cost) return false;
        if (!item.Repeatable && career.PurchasedShopItems.Contains(item.Id)) return false;
        return true;
    }

    public static bool TryPurchase(CareerProfile career, ShopItemDefinition item)
    {
        if (!CanPurchase(career, item)) return false;

        career.Credits -= item.Cost;

        switch (item.Effect)
        {
            case ShopItemEffect.DriverRatingBoost:
                career.DriverRating = Math.Clamp(career.DriverRating + item.EffectValue, 0, 100);
                break;
            case ShopItemEffect.SafetyRatingBoost:
                career.SafetyRating = Math.Clamp(career.SafetyRating + item.EffectValue, 0, 100);
                break;
            case ShopItemEffect.XpBoost:
                career.TotalXp += item.EffectValue;
                career.Level = XpCalculator.LevelFromXp(career.TotalXp);
                break;
            case ShopItemEffect.HireManager:
                career.HasManager = true;
                break;
        }

        if (!item.Repeatable)
            career.PurchasedShopItems.Add(item.Id);

        return true;
    }
}
