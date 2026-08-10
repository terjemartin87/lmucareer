using LmuCareerTool.Models;

namespace LmuCareerTool.Transfers;

/// <summary>
/// Gir merke-interesse litt "hukommelse" i stedet for å regnes helt på nytt fra bunnen hver
/// gang: lagret rykte beveger seg 25% av veien mot fersk beregnet interesse per visning.
/// </summary>
public static class ReputationService
{
    private const double DriftFactor = 0.25;

    public static int UpdateAndGet(CareerProfile career, string manufacturerName, int freshInterest)
    {
        if (career.ManufacturerReputation.TryGetValue(manufacturerName, out var current))
        {
            var updated = current + (int)Math.Round((freshInterest - current) * DriftFactor);
            career.ManufacturerReputation[manufacturerName] = updated;
            return updated;
        }

        career.ManufacturerReputation[manufacturerName] = freshInterest;
        return freshInterest;
    }
}
