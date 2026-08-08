using LmuCareerTool.Content;
using LmuCareerTool.Models;

namespace LmuCareerTool.Career;

public static class ManufacturerUnlockService
{
    /// <summary>Sørger for at startmerkene i en klasse er markert som opplåst (kalles lazy, trenger ikke sesong).</summary>
    public static void EnsureStartingManufacturersUnlocked(CareerProfile career, GameContent content, string carClass)
    {
        var classDef = content.Classes.FirstOrDefault(c => c.Name.Equals(carClass, StringComparison.OrdinalIgnoreCase));
        if (classDef == null || classDef.Manufacturers.Count == 0) return;

        if (!career.UnlockedManufacturers.TryGetValue(carClass, out var unlocked))
        {
            unlocked = new List<string>();
            career.UnlockedManufacturers[carClass] = unlocked;
        }

        foreach (var m in classDef.Manufacturers.Where(m => m.StartUnlocked))
        {
            if (!unlocked.Contains(m.Name)) unlocked.Add(m.Name);
        }
    }

    /// <summary>
    /// Sjekker om Driver Rating nå er høy nok til at flere merker "legger merke til deg" automatisk.
    /// Returnerer navnene på merker som akkurat ble låst opp (for varsling i UI).
    /// </summary>
    public static List<string> CheckForNewOffers(CareerProfile career, GameContent content, string carClass)
    {
        var classDef = content.Classes.FirstOrDefault(c => c.Name.Equals(carClass, StringComparison.OrdinalIgnoreCase));
        if (classDef == null || classDef.Manufacturers.Count == 0) return new List<string>();

        if (!career.UnlockedManufacturers.TryGetValue(carClass, out var unlocked))
        {
            unlocked = new List<string>();
            career.UnlockedManufacturers[carClass] = unlocked;
        }

        var newOffers = new List<string>();
        foreach (var m in classDef.Manufacturers)
        {
            if (!unlocked.Contains(m.Name) && career.DriverRating >= m.RatingRequired)
            {
                unlocked.Add(m.Name);
                newOffers.Add(m.Name);
            }
        }

        return newOffers;
    }

    /// <summary>Forsøker å kjøpe deg inn hos et merke direkte med credits, uansett Rating. Returnerer true ved suksess.</summary>
    public static bool TryBuyManufacturer(CareerProfile career, GameContent content, string carClass, string manufacturerName)
    {
        var classDef = content.Classes.FirstOrDefault(c => c.Name.Equals(carClass, StringComparison.OrdinalIgnoreCase));
        var manufacturer = classDef?.Manufacturers.FirstOrDefault(m => m.Name.Equals(manufacturerName, StringComparison.OrdinalIgnoreCase));
        if (manufacturer == null) return false;

        if (!career.UnlockedManufacturers.TryGetValue(carClass, out var unlocked))
        {
            unlocked = new List<string>();
            career.UnlockedManufacturers[carClass] = unlocked;
        }

        if (unlocked.Contains(manufacturer.Name)) return true; // allerede opplåst
        if (career.Credits < manufacturer.UnlockCost) return false;

        career.Credits -= manufacturer.UnlockCost;
        unlocked.Add(manufacturer.Name);
        return true;
    }
}
