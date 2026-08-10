using LmuCareerTool.Content;
using LmuCareerTool.Models;

namespace LmuCareerTool.Career;

/// <summary>
/// Sjekker karrierens statistikk mot prestasjonsdefinisjonene i innholdsdatabasen og låser opp
/// nye permanent. Speiler mønsteret fra ClassUnlockService - mutasjon + liste over det som er
/// nytt akkurat nå, til bruk i en "Prestasjon låst opp!"-melding.
/// </summary>
public static class AchievementService
{
    public static List<AchievementDefinition> CheckForNewUnlocks(CareerProfile career, GameContent content)
    {
        var newlyUnlocked = new List<AchievementDefinition>();

        foreach (var achievement in content.Achievements)
        {
            if (career.UnlockedAchievements.Contains(achievement.Id)) continue;

            var met = achievement.Metric == AchievementMetric.SpecificSpecialEvent
                ? career.SpecialEventHistory.Any(r => r.EventId == achievement.RequiredEventId)
                : GetMetricValue(career, achievement.Metric) >= achievement.Threshold;

            if (met)
            {
                career.UnlockedAchievements.Add(achievement.Id);
                newlyUnlocked.Add(achievement);
            }
        }

        return newlyUnlocked;
    }

    private static int GetMetricValue(CareerProfile career, AchievementMetric metric) => metric switch
    {
        AchievementMetric.TotalWins => career.RaceHistory.Count(r => r.FinishPos == 1),
        AchievementMetric.TotalPodiums => career.RaceHistory.Count(r => r.FinishPos is >= 1 and <= 3),
        AchievementMetric.TotalPoles => career.RaceHistory.Count(r => r.QualifyingPos == 1),
        AchievementMetric.TotalRacesFinished => career.RaceHistory.Count(r => r.FinishPos > 0),
        AchievementMetric.TotalCleanRaces => career.RaceHistory.Count(r => r.IncidentCount == 0 && r.PenaltyCount == 0),
        AchievementMetric.TotalXp => career.TotalXp,
        AchievementMetric.DriverRating => career.DriverRating,
        AchievementMetric.SafetyRating => career.SafetyRating,
        AchievementMetric.SeasonsCompleted => career.SeasonHistory.Count,
        AchievementMetric.ClassesUnlocked => career.UnlockedClasses.Count,
        AchievementMetric.TotalDistanceKm => (int)(career.RaceHistory.Sum(r => r.Laps * r.TrackLength) / 1000.0),
        AchievementMetric.SpecialEventsCompleted => career.SpecialEventHistory.Count,
        _ => 0,
    };
}
