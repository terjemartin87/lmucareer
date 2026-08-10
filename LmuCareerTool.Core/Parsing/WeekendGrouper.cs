using LmuCareerTool.Models;

namespace LmuCareerTool.Parsing;

/// <summary>
/// Holder styr på de siste Practice/Qualifying-resultatene per bane, slik at vi
/// kan sette dem sammen med Race-resultatet når det kommer inn.
///
/// Filnavnenes egne "sesjons-ID"-tall (f.eks. "25" i 25R1.xml) er IKKE delt mellom
/// P/Q/R for samme helg, så vi kan ikke gruppere på det tallet. I stedet grupperer
/// vi på bane (TrackVenue) + at sesjonene ligger innenfor et rimelig tidsvindu.
/// </summary>
public class WeekendGrouper
{
    private readonly TimeSpan _weekendWindow;
    private readonly Dictionary<string, SessionResult> _lastQualifyingByTrack = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SessionResult> _lastPracticeByTrack = new(StringComparer.OrdinalIgnoreCase);

    public WeekendGrouper(TimeSpan? weekendWindow = null)
    {
        _weekendWindow = weekendWindow ?? TimeSpan.FromHours(6);
    }

    /// <summary>Ventende Practice-økter per bane, til persistering (PendingWeekendStore).</summary>
    public IReadOnlyDictionary<string, SessionResult> PendingPractice => _lastPracticeByTrack;

    /// <summary>Ventende Qualifying-økter per bane, til persistering (PendingWeekendStore).</summary>
    public IReadOnlyDictionary<string, SessionResult> PendingQualifying => _lastQualifyingByTrack;

    /// <summary>Forkaster ventende Practice/Qualifying-data for en bane - til bruk når spilleren
    /// starter en helg men bestemmer seg for ikke å fullføre løpet.</summary>
    public void ClearPending(string trackVenue)
    {
        _lastPracticeByTrack.Remove(trackVenue);
        _lastQualifyingByTrack.Remove(trackVenue);
    }

    /// <summary>Gjenoppretter ventende P/Q-økter fra disk, slik at en helg overlever at appen restartes.</summary>
    public void RestoreState(
        IDictionary<string, SessionResult> practice,
        IDictionary<string, SessionResult> qualifying)
    {
        _lastPracticeByTrack.Clear();
        foreach (var kv in practice) _lastPracticeByTrack[kv.Key] = kv.Value;

        _lastQualifyingByTrack.Clear();
        foreach (var kv in qualifying) _lastQualifyingByTrack[kv.Key] = kv.Value;
    }

    /// <summary>
    /// Mater inn en nylig parset sesjon. Returnerer en komplett RaceWeekendResult
    /// hvis dette var Race-sesjonen som fullfører en helg for spilleren, ellers null.
    /// </summary>
    public RaceWeekendResult? Feed(SessionResult session, string playerName)
    {
        switch (session.SessionType)
        {
            case SessionType.Practice:
                _lastPracticeByTrack[session.TrackVenue] = session;
                return null;

            case SessionType.Qualifying:
                _lastQualifyingByTrack[session.TrackVenue] = session;
                return null;

            case SessionType.Race:
                var weekend = BuildWeekend(session, playerName);
                // Nullstill for banen slik at neste helg på samme bane starter friskt
                _lastPracticeByTrack.Remove(session.TrackVenue);
                _lastQualifyingByTrack.Remove(session.TrackVenue);
                return weekend;

            default:
                return null;
        }
    }

    private RaceWeekendResult BuildWeekend(SessionResult race, string playerName)
    {
        var weekend = new RaceWeekendResult
        {
            TrackVenue = race.TrackVenue,
            CompletedAtUtc = race.SessionTimeUtc,
            TrackLength = race.TrackLength,
            RaceResult = race.FindPlayer(playerName),
            FullRaceField = race.Drivers,
            TotalParticipants = race.Drivers.Count
        };

        if (_lastQualifyingByTrack.TryGetValue(race.TrackVenue, out var quali) &&
            race.SessionTimeUtc - quali.SessionTimeUtc <= _weekendWindow)
        {
            weekend.QualifyingResult = quali.FindPlayer(playerName);
        }

        if (_lastPracticeByTrack.TryGetValue(race.TrackVenue, out var practice) &&
            race.SessionTimeUtc - practice.SessionTimeUtc <= _weekendWindow)
        {
            weekend.BestPracticeResult = practice.FindPlayer(playerName);
        }

        return weekend;
    }
}
