using LmuCareerTool.Championship;
using LmuCareerTool.Content;
using LmuCareerTool.Models;
using LmuCareerTool.Season;

namespace LmuCareerTool.Career;

/// <summary>Bygger den fullstendige sesongrapporten som vises når en sesong er fullført.</summary>
public static class SeasonSummaryBuilder
{
    public static SeasonReport Build(
        CareerProfile career, SeasonModel season, GameContent content, string playerName,
        bool droppedByManufacturer, bool contractExpired)
    {
        var report = new SeasonReport
        {
            SeasonNumber = season.SeasonNumber,
            CarClass = season.CarClass,
            Manufacturer = career.CurrentContract?.Manufacturer
                           ?? career.ContractHistory.LastOrDefault(c => c.SignedInSeasonNumber == season.SeasonNumber)?.Manufacturer,
            RoundsCompleted = season.CompletedCount,
        };

        var driverStandings = ChampionshipTable.ComputeDriverStandings(season, playerName);
        report.DriverStandings = driverStandings;
        report.ManufacturerStandings = ChampionshipTable.ComputeManufacturerStandings(season, content);
        report.TotalDrivers = driverStandings.Count;

        var playerIndex = driverStandings.FindIndex(d => d.IsPlayer);
        if (playerIndex >= 0)
        {
            report.ChampionshipPosition = playerIndex + 1;
            report.Points = driverStandings[playerIndex].Points;
            report.Wins = driverStandings[playerIndex].Wins;
            report.Podiums = driverStandings[playerIndex].Podiums;
        }

        var seasonRaces = career.RaceHistory
            .Where(r => r.SeasonNumber == season.SeasonNumber)
            .OrderBy(r => r.RoundNumber ?? 0)
            .ToList();

        if (seasonRaces.Count > 0)
        {
            report.Dnfs = seasonRaces.Count(r => !r.FinishStatus.Equals("Finished Normally", StringComparison.OrdinalIgnoreCase));
            report.AverageFinish = seasonRaces.Where(r => r.FinishPos > 0).Select(r => (double)r.FinishPos).DefaultIfEmpty(0).Average();
            report.TotalDistanceKm = seasonRaces.Sum(r => r.Laps * r.TrackLength) / 1000.0;
            report.PolePositions = seasonRaces.Count(r => r.QualifyingPos == 1);
        }

        foreach (var ev in season.Events.Where(e => e.Completed))
        {
            var standingsThroughRound = ChampionshipTable.ComputeDriverStandings(season, playerName, ev.RoundNumber);
            var positionAfter = standingsThroughRound.FindIndex(d => d.IsPlayer);

            report.Rounds.Add(new SeasonReportRound
            {
                RoundNumber = ev.RoundNumber,
                TrackVenue = ev.TrackVenue,
                FinishPos = ev.FinishPos,
                PointsEarned = ev.PointsEarned ?? 0,
                ChampionshipPositionAfter = positionAfter >= 0 ? positionAfter + 1 : 0,
            });
        }

        report.TrackRecords = BuildTrackRecords(career, seasonRaces);
        report.Awards = AwardService.ComputeAwards(seasonRaces);
        report.ContractSummary = BuildContractSummary(career, droppedByManufacturer, contractExpired);

        return report;
    }

    private static List<PersonalTrackRecord> BuildTrackRecords(CareerProfile career, List<CareerRaceEntry> seasonRaces)
    {
        var careerBestByTrack = career.RaceHistory
            .Where(r => r.RaceBestLap is > 0)
            .GroupBy(r => r.TrackVenue, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Min(r => r.RaceBestLap!.Value), StringComparer.OrdinalIgnoreCase);

        var records = new List<PersonalTrackRecord>();
        foreach (var trackGroup in seasonRaces.Where(r => r.RaceBestLap is > 0).GroupBy(r => r.TrackVenue, StringComparer.OrdinalIgnoreCase))
        {
            var bestThisSeason = trackGroup.Min(r => r.RaceBestLap!.Value);
            var careerBest = careerBestByTrack.TryGetValue(trackGroup.Key, out var cb) ? cb : bestThisSeason;

            records.Add(new PersonalTrackRecord
            {
                TrackVenue = trackGroup.Key,
                BestLapSeconds = bestThisSeason,
                IsCareerBest = bestThisSeason <= careerBest + 0.0001,
            });
        }

        return records.OrderBy(r => r.TrackVenue, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string BuildContractSummary(CareerProfile career, bool dropped, bool expired)
    {
        if (dropped)
            return "Merket var ikke fornøyd med resultatene og har sagt opp kontrakten din.";
        if (expired)
            return "Kontrakten din har løpt ut som avtalt - tid for et nytt tilbud.";
        if (career.CurrentContract is { IsPrivateerSeat: false, IsFreeAgent: false } contract)
            return $"Kontrakten med {contract.Manufacturer} fortsetter - {contract.SeasonsRemaining} sesong(er) igjen, {contract.SalaryPerRound} cr/runde.";
        if (career.CurrentContract is { IsPrivateerSeat: true })
            return "Du kjører fortsatt for et privatlag - ingen fabrikkontrakt.";
        return "Ingen aktiv kontrakt.";
    }
}
