using LmuCareerTool.Championship;
using LmuCareerTool.Content;
using LmuCareerTool.Models;
using LmuCareerTool.Parsing;
using LmuCareerTool.Season;
using LmuCareerTool.Transfers;
using LmuCareerTool.Validation;

namespace LmuCareerTool.Career;

/// <summary>Resultatet av at en fullført løpshelg er behandlet, klar til visning i UI/konsoll.</summary>
public class WeekendProcessingOutcome
{
    public RaceWeekendResult Weekend { get; set; } = null!;
    public int XpEarned { get; set; }
    public int PointsEarned { get; set; }
    public int RatingDelta { get; set; }
    public int CreditsEarned { get; set; }

    /// <summary>Kontraktlønn utbetalt for denne runden (0 hvis ingen aktiv kontrakt med lønn).</summary>
    public int ContractSalaryEarned { get; set; }

    /// <summary>Runden som faktisk ble kreditert - null hvis ingen sesongrunde ble fullført.</summary>
    public SeasonEvent? MatchedEvent { get; set; }

    /// <summary>Neste ikke-fullførte runde i sesongen da dette løpet ble behandlet - brukes til
    /// å tilby "godkjenn likevel" selv om oppsettet (bane og/eller bil) ikke stemte.</summary>
    public SeasonEvent? CandidateEvent { get; set; }

    public bool CarMismatch { get; set; }
    public bool TrackMismatch { get; set; }

    /// <summary>Menneskelesbare avviksmeldinger fra SetupValidator.</summary>
    public List<string> Issues { get; set; } = new();

    /// <summary>True hvis spilleren kan tvinge gjennom denne runden manuelt til tross for avvik.</summary>
    public bool CanApproveAnyway =>
        (CarMismatch || TrackMismatch) && CandidateEvent != null && Weekend.RaceResult != null;

    public List<string> NewUnlocks { get; set; } = new();

    /// <summary>Satt hvis dette løpet var siste runde i sesongen - UI bør da vise sesongoppsummering + overgangsvindu.</summary>
    public bool SeasonJustCompleted { get; set; }
    public SeasonModel? CompletedSeason { get; set; }

    /// <summary>Merket sa deg opp fordi sesongmålet ikke ble innfridd.</summary>
    public bool DroppedByManufacturer { get; set; }

    /// <summary>Kontrakten løp naturlig ut (siste sesong av avtalt lengde), uten oppsigelse.</summary>
    public bool ContractExpired { get; set; }
}

/// <summary>
/// Samler all karriere-logikk (parsing, gruppering, XP, poeng, rating, credits, sesong, bilkrav,
/// mesterskapstabell, kontrakter) på ett sted, slik at både konsoll-testverktøyet og WPF-UI-en
/// bruker nøyaktig samme logikk.
/// </summary>
public class CareerEngine
{
    public const int RacesPerSeason = 9;
    public const double EnduranceRatio = 0.35;

    private readonly WeekendGrouper _grouper = new();
    private readonly CareerStore _store;
    private readonly PendingWeekendStore _pendingStore;
    private readonly string _playerName;

    public GameContent Content { get; }
    public CareerProfile Career { get; private set; }

    public event Action<SessionResult>? SessionParsed;

    /// <summary>Fyres for økter som ikke telles mot karrieren (f.eks. Multiplayer) - se SessionModeFilter.</summary>
    public event Action<SessionResult>? SessionIgnored;
    public event Action<WeekendProcessingOutcome>? WeekendCompleted;

    public CareerEngine(string playerName, string careerFilePath, string contentFilePath)
    {
        _playerName = playerName;
        _store = new CareerStore(careerFilePath);
        Content = ContentLoader.Load(contentFilePath);
        Career = _store.LoadOrCreate(playerName);

        var careerDir = Path.GetDirectoryName(Path.GetFullPath(careerFilePath))!;
        var careerFileStem = Path.GetFileNameWithoutExtension(careerFilePath);
        _pendingStore = new PendingWeekendStore(Path.Combine(careerDir, $"pending_{careerFileStem}.json"));

        var pendingState = _pendingStore.LoadOrEmpty();
        _grouper.RestoreState(pendingState.Practice, pendingState.Qualifying);

        // Merk: vi genererer IKKE en sesong automatisk her. UI-en (overgangsvinduet) må alltid
        // be spilleren signere et tilbud eksplisitt - både ved aller første oppstart og hver
        // gang en sesong er fullført. Dette holder "signering"-flyten konsistent overalt.
    }

    /// <summary>Konkrete kontraktstilbud for en gitt klasse, til visning i overgangsvinduet.
    /// Vurderer form/renhet mot forrige fullførte sesong (om noen), og fornyelse hvis du
    /// allerede har kontrakt med et merke i klassen og innfridde forrige sesongmål.</summary>
    public List<ContractOffer> GetContractOffers(string carClass) =>
        OfferGenerator.GenerateOffers(Career, Content, carClass, Career.SeasonHistory.LastOrDefault(), _playerName);

    /// <summary>Signerer et tilbud og starter sesongen umiddelbart. Returnerer false hvis du ikke
    /// hadde råd til engangskostnaden (kun privatlag-setet har en).</summary>
    public bool SignContract(ContractOffer offer)
    {
        var seasonNumber = Career.SeasonHistory.Count + 1;
        if (!ContractService.TrySign(Career, offer, seasonNumber)) return false;

        var manufacturerForCarPool = offer.IsPrivateerSeat || offer.IsFreeAgent ? null : offer.Manufacturer;
        var explicitCar = offer.IsPrivateerSeat ? offer.Car : null;

        Career.CurrentSeason = SeasonGenerator.Generate(
            Content, offer.CarClass, manufacturerForCarPool, seasonNumber, RacesPerSeason, EnduranceRatio,
            explicitCar: explicitCar);

        _store.Save(Career);
        return true;
    }

    /// <summary>Sier opp gjeldende kontrakt mot en bruddsum i credits. Returnerer false hvis du
    /// ikke har råd, eller hvis du ikke har en oppsigelig kontrakt (privatlag/fri klasse/ingen).</summary>
    public bool TryTerminateContract()
    {
        var success = ContractService.TryTerminateEarly(Career);
        if (success) _store.Save(Career);
        return success;
    }

    /// <summary>Fører- og merkemesterskapstabellen for gjeldende (eller angitt) sesong.</summary>
    public List<DriverStandingEntry> GetDriverStandings(SeasonModel? season = null, int? throughRound = null) =>
        ChampionshipTable.ComputeDriverStandings(season ?? Career.CurrentSeason, _playerName, throughRound);

    public List<ManufacturerStandingEntry> GetManufacturerStandings(SeasonModel? season = null, int? throughRound = null) =>
        ChampionshipTable.ComputeManufacturerStandings(season ?? Career.CurrentSeason, Content, throughRound);

    /// <summary>Leser en fil kun for å fylle Practice/Qualifying-cache - trigger ikke XP.</summary>
    public void IndexExistingFile(string path)
    {
        var session = ResultXmlParser.Parse(path);
        SessionParsed?.Invoke(session);

        if (!SessionModeFilter.IsCareerEligible(session))
        {
            SessionIgnored?.Invoke(session);
            return;
        }

        if (session.SessionType != SessionType.Race)
        {
            _grouper.Feed(session, _playerName);
            PersistPendingState();
        }
    }

    /// <summary>Behandler en ny resultatfil. Returnerer et utfall hvis dette fullførte en løpshelg (Race-fil), ellers null.</summary>
    public WeekendProcessingOutcome? ProcessFile(string path)
    {
        var session = ResultXmlParser.Parse(path);
        SessionParsed?.Invoke(session);

        if (!SessionModeFilter.IsCareerEligible(session))
        {
            SessionIgnored?.Invoke(session);
            return null;
        }

        var weekend = _grouper.Feed(session, _playerName);
        PersistPendingState();
        if (weekend == null) return null;

        return HandleCompletedWeekend(weekend);
    }

    /// <summary>
    /// Tvinger gjennom en runde manuelt til tross for at bane og/eller bil ikke stemte
    /// (WeekendProcessingOutcome.CanApproveAnyway). Din egen karriere, dine egne regler.
    /// </summary>
    public WeekendProcessingOutcome ApproveDespiteMismatch(WeekendProcessingOutcome previous)
    {
        if (!previous.CanApproveAnyway || previous.CandidateEvent == null || previous.Weekend.RaceResult == null)
            throw new InvalidOperationException("Ingenting å godkjenne for dette resultatet.");

        var weekend = previous.Weekend;
        var matchedEvent = previous.CandidateEvent;

        var (xp, points, ratingDelta, creditsEarned, salaryEarned) = CompleteRound(matchedEvent, weekend);

        // Oppdater den siste historikk-oppføringen for denne helgen i stedet for å legge til en ny.
        var historyEntry = Career.RaceHistory.LastOrDefault(h =>
            h.CompletedAtUtc == weekend.CompletedAtUtc &&
            h.TrackVenue.Equals(weekend.TrackVenue, StringComparison.OrdinalIgnoreCase));
        if (historyEntry != null)
        {
            historyEntry.RoundNumber = matchedEvent.RoundNumber;
            historyEntry.SeasonNumber = Career.CurrentSeason?.SeasonNumber;
            historyEntry.XpEarned = xp;
            historyEntry.PointsEarned = points;
        }

        // Ved avvik ble ikke xp/rating/credits lagt til forrige gang (alt sto på 0) - legg dem til nå.
        Career.TotalXp += xp;
        Career.Level = XpCalculator.LevelFromXp(Career.TotalXp);
        Career.DriverRating = RatingCalculator.ApplyDelta(Career.DriverRating, ratingDelta);
        Career.Credits += creditsEarned;

        var outcome = new WeekendProcessingOutcome
        {
            Weekend = weekend,
            MatchedEvent = matchedEvent,
            CandidateEvent = matchedEvent,
            XpEarned = xp,
            PointsEarned = points,
            RatingDelta = ratingDelta,
            CreditsEarned = creditsEarned,
            ContractSalaryEarned = salaryEarned,
        };

        outcome.NewUnlocks = ClassUnlockService.CheckForNewUnlocks(Career, Content);

        CompleteSeasonIfFinished(outcome);

        _store.Save(Career);
        WeekendCompleted?.Invoke(outcome);
        return outcome;
    }

    private WeekendProcessingOutcome HandleCompletedWeekend(RaceWeekendResult weekend)
    {
        var outcome = new WeekendProcessingOutcome { Weekend = weekend };

        if (weekend.RaceResult == null)
        {
            WeekendCompleted?.Invoke(outcome);
            return outcome;
        }

        var race = weekend.RaceResult;

        // F7-fiks: match KUN mot neste ikke-fullførte runde (ikke et vilkårlig banesøk), slik at
        // du ikke kan fullføre runder ute av rekkefølge eller kreditere feil runde ved dupliserte baner.
        var nextEvent = Career.CurrentSeason?.NextEvent;
        outcome.CandidateEvent = nextEvent;

        var trackMismatch = nextEvent != null &&
            !nextEvent.TrackVenue.Equals(weekend.TrackVenue, StringComparison.OrdinalIgnoreCase);
        var matchedEvent = nextEvent != null && !trackMismatch ? nextEvent : null;
        outcome.MatchedEvent = matchedEvent;
        outcome.TrackMismatch = trackMismatch;

        var xp = XpCalculator.Calculate(weekend);
        var points = 0;
        var ratingDelta = 0;
        var creditsEarned = 0;
        var salaryEarned = 0;

        if (matchedEvent != null)
        {
            var carMatches = string.Equals(
                race.CarType.Trim(), matchedEvent.AssignedCar.Trim(), StringComparison.OrdinalIgnoreCase);
            outcome.CarMismatch = !carMatches;

            if (!carMatches)
            {
                xp = 0; // ingen XP/poeng/rating/credits hvis feil bil ble brukt til en sesongrunde
            }
            else
            {
                (xp, points, ratingDelta, creditsEarned, salaryEarned) = CompleteRound(matchedEvent, weekend);
            }
        }

        outcome.Issues = SetupValidator.Validate(matchedEvent, trackMismatch, nextEvent?.TrackVenue, weekend);

        outcome.XpEarned = xp;
        outcome.PointsEarned = points;
        outcome.RatingDelta = ratingDelta;
        outcome.CreditsEarned = creditsEarned;
        outcome.ContractSalaryEarned = salaryEarned;

        Career.RaceHistory.Add(new CareerRaceEntry
        {
            CompletedAtUtc = weekend.CompletedAtUtc,
            TrackVenue = weekend.TrackVenue,
            CarType = race.CarType,
            RoundNumber = matchedEvent?.RoundNumber,
            SeasonNumber = Career.CurrentSeason?.SeasonNumber,
            GridPos = race.GridPos,
            FinishPos = race.Position,
            TotalParticipants = weekend.TotalParticipants,
            FinishStatus = race.FinishStatus,
            IncidentCount = race.IncidentCount,
            PenaltyCount = race.PenaltyCount,
            XpEarned = xp,
            PointsEarned = points,

            PracticeBestLap = weekend.BestPracticeResult?.BestLapTime,
            PracticeS1 = weekend.BestPracticeResult?.BestSector1,
            PracticeS2 = weekend.BestPracticeResult?.BestSector2,
            PracticeS3 = weekend.BestPracticeResult?.BestSector3,

            QualifyingPos = weekend.QualifyingResult?.Position,
            QualifyingBestLap = weekend.QualifyingResult?.BestLapTime,
            QualifyingS1 = weekend.QualifyingResult?.BestSector1,
            QualifyingS2 = weekend.QualifyingResult?.BestSector2,
            QualifyingS3 = weekend.QualifyingResult?.BestSector3,

            RaceBestLap = race.BestLapTime,
            RaceS1 = race.BestSector1,
            RaceS2 = race.BestSector2,
            RaceS3 = race.BestSector3,
        });

        Career.TotalXp += xp;
        Career.Level = XpCalculator.LevelFromXp(Career.TotalXp);
        Career.DriverRating = RatingCalculator.ApplyDelta(Career.DriverRating, ratingDelta);
        Career.Credits += creditsEarned;

        outcome.NewUnlocks = ClassUnlockService.CheckForNewUnlocks(Career, Content);

        CompleteSeasonIfFinished(outcome);

        _store.Save(Career);
        WeekendCompleted?.Invoke(outcome);
        return outcome;
    }

    /// <summary>Regner ut belønninger for en fullført, matchende runde og oppdaterer SeasonEvent
    /// (inkl. mesterskapsfeltet, en evt. rosterlåsing, og kontraktlønn). Delt mellom normal- og
    /// godkjenn-likevel-flyten.</summary>
    private (int xp, int points, int ratingDelta, int creditsEarned, int salaryEarned) CompleteRound(
        SeasonEvent matchedEvent, RaceWeekendResult weekend)
    {
        var race = weekend.RaceResult!;
        var xp = XpCalculator.Calculate(weekend);
        var points = PointsCalculator.PointsForPosition(race.Position);
        var ratingDelta = RatingCalculator.CalculateDelta(weekend);
        var creditsEarned = CreditsCalculator.Calculate(weekend);
        var salaryEarned = ContractService.PaySalaryForRound(Career);

        matchedEvent.CarMatched = true;
        matchedEvent.Completed = true;
        matchedEvent.CompletedAtUtc = weekend.CompletedAtUtc;
        matchedEvent.FinishPos = race.Position;
        matchedEvent.XpEarned = xp;
        matchedEvent.PointsEarned = points;

        matchedEvent.FieldResults = weekend.FullRaceField
            .Where(d => d.Position > 0)
            .Select(d => new FieldResultEntry
            {
                Name = RosterMatcher.Normalize(d.Name),
                TeamName = d.TeamName,
                CarType = d.CarType,
                Position = d.Position,
                FinishStatus = d.FinishStatus,
                IsPlayer = d.IsPlayer
            })
            .ToList();

        if (Career.CurrentSeason != null)
            FieldRoster.LockIfNeeded(Career.CurrentSeason, matchedEvent.FieldResults);

        return (xp, points, ratingDelta, creditsEarned, salaryEarned);
    }

    private void CompleteSeasonIfFinished(WeekendProcessingOutcome outcome)
    {
        if (Career.CurrentSeason?.IsComplete != true) return;

        outcome.SeasonJustCompleted = true;
        outcome.CompletedSeason = Career.CurrentSeason;

        if (Career.CurrentContract != null)
        {
            var dropped = ContractService.ApplySeasonResult(Career, Career.CurrentSeason, _playerName);
            outcome.DroppedByManufacturer = dropped;
            outcome.ContractExpired = !dropped && Career.CurrentContract == null;
        }

        Career.SeasonHistory.Add(Career.CurrentSeason);
        Career.CurrentSeason = null; // UI må be spilleren signere et nytt tilbud før neste runde vises
    }

    private void PersistPendingState()
    {
        _pendingStore.Save(new PendingWeekendState
        {
            Practice = _grouper.PendingPractice.ToDictionary(kv => kv.Key, kv => kv.Value),
            Qualifying = _grouper.PendingQualifying.ToDictionary(kv => kv.Key, kv => kv.Value),
        });
    }
}
