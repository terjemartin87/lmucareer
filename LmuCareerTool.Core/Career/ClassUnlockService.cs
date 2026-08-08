using LmuCareerTool.Content;
using LmuCareerTool.Models;

namespace LmuCareerTool.Career;

public static class ClassUnlockService
{
    /// <summary>
    /// Sjekker karrierens totale XP mot terskelverdiene i innholdsdatabasen og
    /// låser opp nye klasser hvis grensen er nådd. Returnerer navnene på klasser
    /// som ble låst opp akkurat nå (for å kunne vise en "Ny klasse låst opp!"-melding).
    /// </summary>
    public static List<string> CheckForNewUnlocks(CareerProfile career, GameContent content)
    {
        var newlyUnlocked = new List<string>();

        foreach (var classDef in content.Classes.OrderBy(c => c.Order))
        {
            if (career.TotalXp >= classDef.XpThreshold && !career.UnlockedClasses.Contains(classDef.Name))
            {
                career.UnlockedClasses.Add(classDef.Name);
                newlyUnlocked.Add(classDef.Name);
            }
        }

        return newlyUnlocked;
    }
}
