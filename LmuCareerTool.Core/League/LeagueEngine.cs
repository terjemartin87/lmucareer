using LmuCareerTool.Championship;
using LmuCareerTool.Content;
using LmuCareerTool.Models;
using LmuCareerTool.Parsing;

namespace LmuCareerTool.League;

/// <summary>Resultatet av at en fullført løpshelg er behandlet mot en ligasesong.</summary>
public class LeagueRoundOutcome
{
    public RaceWeekendResult Weekend { get; set; } = null!;

    /// <summary>Runden som faktisk ble kreditert - null hvis banen ikke matchet neste runde.</summary>
    public LeagueRound? MatchedRound { get; set; }

    /// <summary>Neste ikke-fullførte runde da dette løpet ble behandlet.</summary>
    public LeagueRound? CandidateRound { get; set; }

    public bool TrackMismatch { get; set; }

    public bool SeasonJustCompleted { get; set; }
    public LeagueSeason? CompletedSeason { get; set; }
}

/// <summary>
/// Samler ligalogikken (resultat-innhenting, rundematching, sesonglivssyklus) på samme måte
/// som CareerEngine gjør for karrieremodus - men helt uavhengig av den. Ingen XP, Rating,
/// Credits eller kontrakter; bare et poengsystem for et ekte hostet felt av mennesker.
/// Kun "Multiplayer"-økter teller (motsatt av karrieremodus, som kun teller "Race Weekend").
/// </summary>
public class LeagueEngine
{
    // WeekendGrouper krever et "spillernavn" for å finne DEN ene sjåførens resultat, men i
    // ligamodus bryr vi oss kun om HELE feltet (FullRaceField) - denne matcher bevisst ingen,
    // slik at RaceResult/QualifyingResult/BestPracticeResult (som vi ikke bruker) blir null.
    private const string NoSinglePlayer = "\0__league__\0";

    private readonly WeekendGrouper _grouper = new();
    private readonly LeagueStore _store;

    public GameContent Content { get; }
    public LeagueProfile League { get; private set; }

    public event Action<SessionResult>? SessionIgnored;
    public event Action<LeagueRoundOutcome>? RoundCompleted;

    public LeagueEngine(string leagueName, string hostDisplayName, string leagueFilePath, string contentFilePath)
    {
        _store = new LeagueStore(leagueFilePath);
        Content = ContentLoader.Load(contentFilePath);
        League = _store.LoadOrCreate(leagueName, hostDisplayName);
    }

    /// <summary>Genererer og starter en ny ligasesong. Kalles av verten fra UI-en.</summary>
    public void GenerateNewSeason(string carClass, int roundCount, LeagueFormatPreference formatPreference)
    {
        var seasonNumber = League.SeasonHistory.Count + 1;
        var avoidOpeningTrack = League.SeasonHistory.LastOrDefault()?.Rounds.LastOrDefault()?.TrackVenue;

        League.CurrentSeason = LeagueSeasonGenerator.Generate(
            Content, carClass, seasonNumber, roundCount, formatPreference, avoidOpeningTrack);

        _store.Save(League);
    }

    /// <summary>Leser en fil kun for å fylle Practice/Qualifying-cache.</summary>
    public void IndexExistingFile(string path)
    {
        var session = ResultXmlParser.Parse(path);

        if (!SessionModeFilter.IsLeagueEligible(session))
        {
            SessionIgnored?.Invoke(session);
            return;
        }

        if (session.SessionType != SessionType.Race)
            _grouper.Feed(session, NoSinglePlayer);
    }

    /// <summary>Behandler en ny resultatfil. Returnerer et utfall hvis dette fullførte en løpshelg, ellers null.</summary>
    public LeagueRoundOutcome? ProcessFile(string path)
    {
        var session = ResultXmlParser.Parse(path);

        if (!SessionModeFilter.IsLeagueEligible(session))
        {
            SessionIgnored?.Invoke(session);
            return null;
        }

        var weekend = _grouper.Feed(session, NoSinglePlayer);
        if (weekend == null) return null;

        return HandleCompletedWeekend(weekend);
    }

    /// <summary>Tvinger gjennom en runde manuelt til tross for at banen ikke matchet neste runde.</summary>
    /// <summary>Verten gir en manuell straff for en fullført runde.</summary>
    public void ApplyPenalty(int roundNumber, string driverName, int pointsDeducted, bool disqualified, string reason)
    {
        var round = League.CurrentSeason?.Rounds.FirstOrDefault(r => r.RoundNumber == roundNumber)
            ?? League.SeasonHistory.SelectMany(s => s.Rounds).FirstOrDefault(r => r.RoundNumber == roundNumber);
        if (round == null) throw new InvalidOperationException("Fant ikke runden.");

        round.Penalties.Add(new LeaguePenalty
        {
            DriverName = RosterMatcher.Normalize(driverName),
            PointsDeducted = pointsDeducted,
            Disqualified = disqualified,
            Reason = reason,
            AppliedAtUtc = DateTime.UtcNow,
        });

        _store.Save(League);
    }

    /// <summary>Publiserer et statisk HTML-øyeblikksbilde av ligastillingen til valgt fil - hele delingsmekanismen.</summary>
    public void PublishSnapshot(string filePath)
    {
        var html = LeagueReportHtmlBuilder.Build(League, Content);
        File.WriteAllText(filePath, html);
    }

    public LeagueRoundOutcome ApproveDespiteMismatch(LeagueRoundOutcome previous)
    {
        if (previous.CandidateRound == null || previous.MatchedRound != null)
            throw new InvalidOperationException("Ingenting å godkjenne for dette resultatet.");

        var round = previous.CandidateRound;
        CompleteRound(round, previous.Weekend);

        var outcome = new LeagueRoundOutcome { Weekend = previous.Weekend, MatchedRound = round, CandidateRound = round };
        CompleteSeasonIfFinished(outcome);

        _store.Save(League);
        RoundCompleted?.Invoke(outcome);
        return outcome;
    }

    private LeagueRoundOutcome HandleCompletedWeekend(RaceWeekendResult weekend)
    {
        var outcome = new LeagueRoundOutcome { Weekend = weekend };

        var nextRound = League.CurrentSeason?.NextRound;
        outcome.CandidateRound = nextRound;

        var trackMismatch = nextRound != null &&
            !nextRound.TrackVenue.Equals(weekend.TrackVenue, StringComparison.OrdinalIgnoreCase);
        var matchedRound = nextRound != null && !trackMismatch ? nextRound : null;
        outcome.MatchedRound = matchedRound;
        outcome.TrackMismatch = trackMismatch;

        if (matchedRound != null)
            CompleteRound(matchedRound, weekend);

        CompleteSeasonIfFinished(outcome);

        _store.Save(League);
        RoundCompleted?.Invoke(outcome);
        return outcome;
    }

    private void CompleteRound(LeagueRound round, RaceWeekendResult weekend)
    {
        round.Completed = true;
        round.CompletedAtUtc = weekend.CompletedAtUtc;
        round.FieldResults = weekend.FullRaceField
            .Where(d => d.Position > 0)
            .Select(d => new FieldResultEntry
            {
                Name = RosterMatcher.Normalize(d.Name),
                TeamName = d.TeamName,
                CarType = d.CarType,
                Position = d.Position,
                FinishStatus = d.FinishStatus,
                CarClass = d.CarClass,
                // LMU setter ikke alltid ClassPosition i enklasses løp - fall tilbake til den
                // overordnede plasseringen, som da ER klasseplasseringen siden alle er i samme klasse.
                ClassPosition = d.ClassPosition > 0 ? d.ClassPosition : d.Position,
            })
            .ToList();

        if (League.CurrentSeason != null && League.CurrentSeason.LockedRosterNames.Count == 0)
        {
            League.CurrentSeason.LockedRosterNames = round.FieldResults
                .Select(r => r.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    private void CompleteSeasonIfFinished(LeagueRoundOutcome outcome)
    {
        if (League.CurrentSeason?.IsComplete != true) return;

        outcome.SeasonJustCompleted = true;
        outcome.CompletedSeason = League.CurrentSeason;
        League.SeasonHistory.Add(League.CurrentSeason);
        League.CurrentSeason = null;
    }
}
