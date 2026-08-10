using LmuCareerTool.Championship;
using LmuCareerTool.Content;
using LmuCareerTool.Models;
using LmuCareerTool.Parsing;
using LmuCareerTool.Scoring;
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
    public int SafetyRatingDelta { get; set; }
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
    public List<AchievementDefinition> NewAchievements { get; set; } = new();

    /// <summary>Satt hvis dette løpet var siste runde i sesongen - UI bør da vise sesongoppsummering + overgangsvindu.</summary>
    public bool SeasonJustCompleted { get; set; }
    public SeasonModel? CompletedSeason { get; set; }

    /// <summary>Merket sa deg opp fordi sesongmålet ikke ble innfridd.</summary>
    public bool DroppedByManufacturer { get; set; }

    /// <summary>Kontrakten løp naturlig ut (siste sesong av avtalt lengde), uten oppsigelse.</summary>
    public bool ContractExpired { get; set; }

    /// <summary>Satt hvis dette løpet fullførte en påmeldt spesialarrangement (Fase 7) i stedet for
    /// en sesongrunde - navnet på arrangementet, til bruk i en egen "Fullført!"-melding.</summary>
    public string? CompletedSpecialEventName { get; set; }
}

/// <summary>
/// Samler all karriere-logikk (parsing, gruppering, XP, poeng, rating, credits, sesong, bilkrav,
/// mesterskapstabell, kontrakter) på ett sted, slik at både konsoll-testverktøyet og WPF-UI-en
/// bruker nøyaktig samme logikk.
/// </summary>
public class CareerEngine
{
    // MIDLERTIDIG TESTOPPSETT: 1 rundes sesong, ren Endurance (60 min), for å teste
    // sesong->sesong-overgangen raskt. Normalverdier (kommentert ut under) var 9 / 0.35 -
    // bytt tilbake når testingen er ferdig.
    public const int RacesPerSeason = 1;
    public const double EnduranceRatio = 1.0;
    // public const int RacesPerSeason = 9;
    // public const double EnduranceRatio = 0.35;

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

    /// <summary>Full oversikt over alle merker i en klasse for transfer-markedet (ikke bare de som
    /// faktisk sender tilbud akkurat nå) - oppdaterer og lagrer merkenes "hukommelse" av deg.</summary>
    public List<ManufacturerInterestEntry> GetManufacturerInterest(string carClass)
    {
        var entries = TransferMarketService.BuildEntries(Career, Content, carClass, Career.SeasonHistory.LastOrDefault());
        _store.Save(Career);
        return entries;
    }

    /// <summary>Alle prestasjoner i spillet, med opplåst-status, til bruk i prestasjonsvinduet.</summary>
    public List<(AchievementDefinition Definition, bool Unlocked)> GetAchievementStatus() =>
        Content.Achievements
            .Select(a => (Definition: a, Unlocked: Career.UnlockedAchievements.Contains(a.Id)))
            .OrderByDescending(a => a.Unlocked)
            .ThenBy(a => a.Definition.Threshold)
            .ToList();

    /// <summary>Alle varer i coaching/manager-butikken, til bruk i butikkvinduet.</summary>
    public List<ShopItemDefinition> GetShopItems() => Content.ShopItems;

    /// <summary>Forsøker å kjøpe en butikkvare. Returnerer false hvis du ikke har råd, eller
    /// (for engangsvarer som manager) allerede har kjøpt den.</summary>
    public bool TryPurchaseShopItem(ShopItemDefinition item)
    {
        var success = ShopService.TryPurchase(Career, item);
        if (success) _store.Save(Career);
        return success;
    }

    /// <summary>Sletter et løp fra historikken og reverserer XP/poeng/rating/safety/credits det ga.
    /// Hvis løpet var en runde i den PÅGÅENDE sesongen, nullstilles runden slik at den kan kjøres på
    /// nytt. Løp fra allerede avsluttede sesonger fjernes fra historikken og statistikken reverseres,
    /// men selve sesongoppsummeringen for den gamle sesongen endres ikke (den er et fastfrosset
    /// øyeblikksbilde). Prestasjoner og klasser du har låst opp underveis forblir låst opp.</summary>
    public bool DeleteRaceHistoryEntry(CareerRaceEntry entry)
    {
        if (!Career.RaceHistory.Remove(entry)) return false;

        Career.TotalXp = Math.Max(0, Career.TotalXp - entry.XpEarned);
        Career.Level = XpCalculator.LevelFromXp(Career.TotalXp);
        Career.DriverRating = RatingCalculator.ApplyDelta(Career.DriverRating, -entry.RatingDelta);
        Career.SafetyRating = RatingCalculator.ApplyDelta(Career.SafetyRating, -entry.SafetyRatingDelta);
        Career.Credits = Math.Max(0, Career.Credits - entry.CreditsEarned - entry.ContractSalaryEarned);

        if (Career.CurrentSeason != null && entry.SeasonNumber == Career.CurrentSeason.SeasonNumber)
        {
            var seasonEvent = Career.CurrentSeason.Events.FirstOrDefault(e => e.RoundNumber == entry.RoundNumber);
            if (seasonEvent != null)
            {
                seasonEvent.Completed = false;
                seasonEvent.CompletedAtUtc = null;
                seasonEvent.FinishPos = null;
                seasonEvent.XpEarned = null;
                seasonEvent.PointsEarned = null;
                seasonEvent.CarMatched = null;
                seasonEvent.FieldResults = new List<FieldResultEntry>();
            }
        }

        _store.Save(Career);
        return true;
    }

    /// <summary>Praksis-/kvalifiseringsøkter som venter på en Race-fil for å bli en fullført
    /// løpshelg - f.eks. et enduranceløp du startet på men ikke fullførte.</summary>
    public List<SessionResult> GetPendingSessions() =>
        _grouper.PendingPractice.Values.Concat(_grouper.PendingQualifying.Values)
            .OrderByDescending(s => s.SessionTimeUtc)
            .ToList();

    /// <summary>Forkaster ventende Practice/Qualifying-data for en bane uten å påvirke karrieren -
    /// til bruk når du starter en helg men ikke fullfører løpet.</summary>
    public void ClearPendingSession(string trackVenue)
    {
        _grouper.ClearPending(trackVenue);
        PersistPendingState();
    }

    /// <summary>Alle spesialarrangementer (24h Le Mans osv), til bruk i arrangement-vinduet.</summary>
    public List<SpecialEventDefinition> GetSpecialEvents() => Content.SpecialEvents;

    /// <summary>Alle biler tilgjengelig for en klasse (uansett merke), til bruk i bilvelgeren
    /// ved påmelding til et spesialarrangement - fri bilvalg, ikke bundet til sesongkontrakten.</summary>
    public List<string> GetCarsForClass(string carClass)
    {
        var classDef = Content.Classes.FirstOrDefault(c => c.Name.Equals(carClass, StringComparison.OrdinalIgnoreCase));
        if (classDef == null) return new List<string>();

        return classDef.Manufacturers.Count > 0
            ? classDef.Manufacturers.SelectMany(m => m.Cars).Distinct().ToList()
            : classDef.Cars;
    }

    /// <summary>Melder deg på et spesialarrangement. Trekker inngangspenger og tildeler tilfeldig
    /// vær. Returnerer false hvis du ikke har råd eller allerede har en aktiv påmelding.</summary>
    public bool TryEnrollSpecialEvent(SpecialEventDefinition definition, string carClass, string car)
    {
        var weather = Content.WeatherOptions.Count > 0
            ? Content.WeatherOptions[Random.Shared.Next(Content.WeatherOptions.Count)]
            : "Clear";

        var success = SpecialEventService.TryEnroll(Career, definition, carClass, car, weather);
        if (success) _store.Save(Career);
        return success;
    }

    /// <summary>Trekker deg fra din aktive påmelding uten refusjon av inngangspengene.</summary>
    public void WithdrawSpecialEvent()
    {
        SpecialEventService.Withdraw(Career);
        _store.Save(Career);
    }

    /// <summary>Signerer et tilbud og starter sesongen umiddelbart. Returnerer false hvis du ikke
    /// hadde råd til engangskostnaden (kun privatlag-setet har en).</summary>
    public bool SignContract(ContractOffer offer)
    {
        var seasonNumber = Career.SeasonHistory.Count + 1;
        if (!ContractService.TrySign(Career, offer, seasonNumber)) return false;

        var manufacturerForCarPool = offer.IsPrivateerSeat || offer.IsFreeAgent ? null : offer.Manufacturer;
        var explicitCar = offer.IsPrivateerSeat ? offer.Car : null;
        var avoidOpeningTrack = Career.SeasonHistory.LastOrDefault()?.Events.LastOrDefault()?.TrackVenue;

        Career.CurrentSeason = SeasonGenerator.Generate(
            Content, offer.CarClass, manufacturerForCarPool, seasonNumber, RacesPerSeason, EnduranceRatio,
            explicitCar: explicitCar, avoidOpeningTrack: avoidOpeningTrack);

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

    /// <summary>Rivalen din akkurat nå: føreren rett foran deg i mesterskapet (eller rett bak,
    /// hvis du leder). Null hvis det ikke er nok førere i tabellen ennå.</summary>
    public (DriverStandingEntry Player, DriverStandingEntry Rival)? GetRivalComparison(SeasonModel? season = null)
    {
        var standings = GetDriverStandings(season);
        var player = standings.FirstOrDefault(s => s.IsPlayer);
        var rival = RivalService.FindRival(standings);
        return player != null && rival != null ? (player, rival) : null;
    }

    /// <summary>Bygger den fulle sesongrapporten (sammendrag, runde-for-runde, priser, kontraktsoppgjør) for en fullført sesong.</summary>
    public SeasonReport BuildSeasonReport(SeasonModel season, bool droppedByManufacturer = false, bool contractExpired = false) =>
        SeasonSummaryBuilder.Build(Career, season, Content, _playerName, droppedByManufacturer, contractExpired);

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

        var (xp, points, ratingDelta, safetyRatingDelta, creditsEarned, salaryEarned) = CompleteRound(matchedEvent, weekend);

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
            historyEntry.RatingDelta = ratingDelta;
            historyEntry.SafetyRatingDelta = safetyRatingDelta;
            historyEntry.CreditsEarned = creditsEarned;
            historyEntry.ContractSalaryEarned = salaryEarned;
        }

        // Ved avvik ble ikke xp/rating/credits lagt til forrige gang (alt sto på 0) - legg dem til nå.
        Career.TotalXp += xp;
        Career.Level = XpCalculator.LevelFromXp(Career.TotalXp);
        Career.DriverRating = RatingCalculator.ApplyDelta(Career.DriverRating, ratingDelta);
        Career.SafetyRating = RatingCalculator.ApplyDelta(Career.SafetyRating, safetyRatingDelta);
        Career.Credits += creditsEarned;

        var outcome = new WeekendProcessingOutcome
        {
            Weekend = weekend,
            MatchedEvent = matchedEvent,
            CandidateEvent = matchedEvent,
            XpEarned = xp,
            PointsEarned = points,
            RatingDelta = ratingDelta,
            SafetyRatingDelta = safetyRatingDelta,
            CreditsEarned = creditsEarned,
            ContractSalaryEarned = salaryEarned,
        };

        outcome.NewUnlocks = ClassUnlockService.CheckForNewUnlocks(Career, Content);
        outcome.NewAchievements = AchievementService.CheckForNewUnlocks(Career, Content);

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

        if (TryCompleteSpecialEvent(weekend, race, outcome))
        {
            _store.Save(Career);
            WeekendCompleted?.Invoke(outcome);
            return outcome;
        }

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
        var safetyRatingDelta = 0;
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
                (xp, points, ratingDelta, safetyRatingDelta, creditsEarned, salaryEarned) = CompleteRound(matchedEvent, weekend);
            }
        }

        outcome.Issues = SetupValidator.Validate(matchedEvent, trackMismatch, nextEvent?.TrackVenue, weekend);

        outcome.XpEarned = xp;
        outcome.PointsEarned = points;
        outcome.RatingDelta = ratingDelta;
        outcome.SafetyRatingDelta = safetyRatingDelta;
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
            Laps = race.Laps,
            TrackLength = weekend.TrackLength,
            XpEarned = xp,
            PointsEarned = points,
            RatingDelta = ratingDelta,
            SafetyRatingDelta = safetyRatingDelta,
            CreditsEarned = creditsEarned,
            ContractSalaryEarned = salaryEarned,

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
        Career.SafetyRating = RatingCalculator.ApplyDelta(Career.SafetyRating, safetyRatingDelta);
        Career.Credits += creditsEarned;

        outcome.NewUnlocks = ClassUnlockService.CheckForNewUnlocks(Career, Content);
        outcome.NewAchievements = AchievementService.CheckForNewUnlocks(Career, Content);

        CompleteSeasonIfFinished(outcome);

        _store.Save(Career);
        WeekendCompleted?.Invoke(outcome);
        return outcome;
    }

    /// <summary>
    /// Fullfører en påmeldt spesialarrangement hvis dette løpet matcher den (samme bane OG bil
    /// som ble valgt ved påmelding). Helt separat fra sesongen - matches ALDRI mot
    /// Career.CurrentSeason, legges ALDRI i RaceHistory, og gir ingen PointsEarned - påvirker
    /// derfor aldri mesterskapstabellen. XP og credits regnes med en egen varighetsskalert
    /// formel (SpecialEventRewardCalculator) i stedet for de vanlige sesong-kalkulatorene,
    /// siden et 24-timersløp skal betale seg vesentlig bedre enn en vanlig sesongrunde.
    /// </summary>
    private bool TryCompleteSpecialEvent(RaceWeekendResult weekend, DriverResult race, WeekendProcessingOutcome outcome)
    {
        var active = Career.ActiveSpecialEvent;
        if (active == null) return false;
        if (!active.TrackVenue.Equals(weekend.TrackVenue, StringComparison.OrdinalIgnoreCase)) return false;
        if (!active.AssignedCar.Trim().Equals(race.CarType.Trim(), StringComparison.OrdinalIgnoreCase)) return false;

        var xp = SpecialEventRewardCalculator.CalculateXp(weekend, active);
        var creditsEarned = SpecialEventRewardCalculator.CalculateCredits(weekend, active);
        var ratingDelta = RatingCalculator.CalculateDriverRatingDelta(weekend);
        var safetyRatingDelta = RatingCalculator.CalculateSafetyRatingDelta(weekend);

        Career.SpecialEventHistory.Add(new SpecialEventResult
        {
            EventId = active.EventId,
            EventName = active.EventName,
            TrackVenue = active.TrackVenue,
            CompletedAtUtc = weekend.CompletedAtUtc,
            FinishPos = race.Position,
            TotalParticipants = weekend.TotalParticipants,
            FinishStatus = race.FinishStatus,
            XpEarned = xp,
            CreditsEarned = creditsEarned,
            RatingDelta = ratingDelta,
            SafetyRatingDelta = safetyRatingDelta,
        });

        Career.TotalXp += xp;
        Career.Level = XpCalculator.LevelFromXp(Career.TotalXp);
        Career.DriverRating = RatingCalculator.ApplyDelta(Career.DriverRating, ratingDelta);
        Career.SafetyRating = RatingCalculator.ApplyDelta(Career.SafetyRating, safetyRatingDelta);
        Career.Credits += creditsEarned;
        Career.ActiveSpecialEvent = null;

        outcome.XpEarned = xp;
        outcome.RatingDelta = ratingDelta;
        outcome.SafetyRatingDelta = safetyRatingDelta;
        outcome.CreditsEarned = creditsEarned;
        outcome.CompletedSpecialEventName = active.EventName;
        outcome.NewAchievements = AchievementService.CheckForNewUnlocks(Career, Content);

        return true;
    }

    /// <summary>Regner ut belønninger for en fullført, matchende runde og oppdaterer SeasonEvent
    /// (inkl. mesterskapsfeltet, en evt. rosterlåsing, og kontraktlønn). Delt mellom normal- og
    /// godkjenn-likevel-flyten.</summary>
    private (int xp, int points, int ratingDelta, int safetyRatingDelta, int creditsEarned, int salaryEarned) CompleteRound(
        SeasonEvent matchedEvent, RaceWeekendResult weekend)
    {
        var race = weekend.RaceResult!;
        var xp = XpCalculator.Calculate(weekend);
        var points = PointsCalculator.PointsForPosition(race.Position);
        var ratingDelta = RatingCalculator.CalculateDriverRatingDelta(weekend);
        var safetyRatingDelta = RatingCalculator.CalculateSafetyRatingDelta(weekend);
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

        return (xp, points, ratingDelta, safetyRatingDelta, creditsEarned, salaryEarned);
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
