using System.Net;
using System.Text;
using LmuCareerTool.Content;

namespace LmuCareerTool.League;

/// <summary>
/// Bygger et selvstendig, statisk HTML-øyeblikksbilde av ligastillingen - dette ER hele
/// delingsmekanismen for League-modus. Verten trykker "Publiser", får en enkelt HTML-fil han
/// kan sende/laste opp hvor han vil, og alle andre kan åpne den for å SE stillingen. Ingen
/// innlogging, ingen server, ingen skriverettigheter for mottakere - en statisk fil er per
/// definisjon read-only, som var akkurat problemet vi løste ved å velge denne tilnærmingen
/// fremfor en live/hostet løsning.
/// </summary>
public static class LeagueReportHtmlBuilder
{
    public static string Build(LeagueProfile league, GameContent content)
    {
        var season = league.CurrentSeason ?? league.SeasonHistory.LastOrDefault();

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"no\"><head><meta charset=\"utf-8\">");
        sb.AppendLine($"<title>{Enc(league.LeagueName)} - Ligastilling</title>");
        sb.AppendLine("<style>" + Css + "</style></head><body>");
        sb.AppendLine("<div class=\"page\">");

        sb.AppendLine($"<h1>🏁 {Enc(league.LeagueName)}</h1>");
        sb.AppendLine($"<p class=\"muted\">Vert: {Enc(league.HostDisplayName)}" +
                       (season != null ? $" &middot; Sesong {season.SeasonNumber} &middot; {Enc(season.CarClass)}" : "") +
                       $" &middot; Publisert {DateTime.Now:dd.MM.yyyy HH:mm}</p>");

        if (season == null)
        {
            sb.AppendLine("<div class=\"panel\">Ingen sesong er startet ennå.</div>");
            sb.AppendLine("</div></body></html>");
            return sb.ToString();
        }

        // Et hostet løp kan blande flere klasser i samme race (GT3, LMP2, LMP3, Hypercar osv,
        // akkurat som ekte WEC) - hver klasse får derfor sin egen mesterskapstabell, siden poeng
        // i én klasse ikke er sammenlignbare med poeng i en annen. En enkeltklasse-liga (det
        // vanlige tilfellet) får rett og slett bare ett sett tabeller uten klasseoverskrift.
        var classes = LeagueStandingsCalculator.GetClassesInSeason(season);
        var multiClass = classes.Count > 1;

        sb.AppendLine("<div class=\"tiles\">");
        AppendTile(sb, "RUNDER", $"{season.CompletedCount} / {season.Rounds.Count}");
        AppendTile(sb, "STATUS", season.IsComplete ? "Fullført" : "Pågående");
        AppendTile(sb, "KLASSER", classes.Count > 0 ? string.Join(", ", classes) : season.CarClass);
        sb.AppendLine("</div>");

        var classFilters = multiClass ? classes.Cast<string?>().ToList() : new List<string?> { null };

        foreach (var classFilter in classFilters)
        {
            var heading = multiClass ? $" - {Enc(classFilter!)}" : "";

            sb.AppendLine($"<h2>Førermesterskap{heading}</h2>");
            sb.AppendLine("<table><tr><th>#</th><th>Fører</th><th>Team</th><th>Poeng</th><th>Seire</th><th>Podier</th><th>Top 5</th><th>Top 10</th><th>Straffepoeng</th></tr>");
            var driverStandings = LeagueStandingsCalculator.ComputeDriverStandings(season, classFilter);
            for (var i = 0; i < driverStandings.Count; i++)
            {
                var d = driverStandings[i];
                sb.AppendLine($"<tr><td>{i + 1}</td><td>{Enc(d.Name)}</td><td>{Enc(d.TeamName)}</td>" +
                               $"<td>{d.Points}</td><td>{d.Wins}</td><td>{d.Podiums}</td><td>{d.Top5}</td><td>{d.Top10}</td>" +
                               $"<td>{(d.PenaltyPointsTotal > 0 ? $"-{d.PenaltyPointsTotal}" : "-")}</td></tr>");
            }
            sb.AppendLine("</table>");

            sb.AppendLine($"<h2>Merkemesterskap{heading}</h2>");
            sb.AppendLine("<table><tr><th>#</th><th>Merke</th><th>Poeng</th><th>Seire</th></tr>");
            var makeStandings = LeagueStandingsCalculator.ComputeManufacturerStandings(season, content, classFilter);
            for (var i = 0; i < makeStandings.Count; i++)
            {
                var m = makeStandings[i];
                sb.AppendLine($"<tr><td>{i + 1}</td><td>{Enc(m.Manufacturer)}</td><td>{m.Points}</td><td>{m.Wins}</td></tr>");
            }
            sb.AppendLine("</table>");
        }

        sb.AppendLine("<h2>Løpskalender</h2>");
        sb.AppendLine("<table><tr><th>Runde</th><th>Bane</th><th>Format</th><th>Status</th><th>Vinner (totalt)</th></tr>");
        foreach (var round in season.Rounds)
        {
            var winner = round.FieldResults.FirstOrDefault(f => f.Position == 1);
            sb.AppendLine($"<tr><td>{round.RoundNumber}</td><td>{Enc(round.TrackVenue)}</td>" +
                           $"<td>{Enc(round.Format.ToString())}</td>" +
                           $"<td>{(round.Completed ? "Fullført" : "Ikke kjørt")}</td>" +
                           $"<td>{(winner != null ? Enc(winner.Name) : "-")}</td></tr>");
        }
        sb.AppendLine("</table>");

        var allPenalties = season.Rounds.SelectMany(r => r.Penalties.Select(p => (round: r, penalty: p))).ToList();
        if (allPenalties.Count > 0)
        {
            sb.AppendLine("<h2>Straffer</h2>");
            sb.AppendLine("<table><tr><th>Runde</th><th>Fører</th><th>Konsekvens</th><th>Begrunnelse</th></tr>");
            foreach (var (round, penalty) in allPenalties)
            {
                var consequence = penalty.Disqualified ? "Diskvalifisert" : $"-{penalty.PointsDeducted} poeng";
                sb.AppendLine($"<tr><td>{round.RoundNumber}</td><td>{Enc(penalty.DriverName)}</td>" +
                               $"<td>{Enc(consequence)}</td><td>{Enc(penalty.Reason)}</td></tr>");
            }
            sb.AppendLine("</table>");
        }

        sb.AppendLine("</div></body></html>");
        return sb.ToString();
    }

    private static void AppendTile(StringBuilder sb, string label, string value) =>
        sb.AppendLine($"<div class=\"tile\"><div class=\"tile-label\">{Enc(label)}</div><div class=\"tile-value\">{Enc(value)}</div></div>");

    private static string Enc(string value) => WebUtility.HtmlEncode(value);

    private const string Css = """
        body { background:#14161B; color:#EDEDED; font-family:'Segoe UI',Arial,sans-serif; margin:0; }
        .page { max-width:900px; margin:0 auto; padding:32px 24px 64px; }
        h1 { font-size:28px; margin-bottom:4px; }
        h2 { font-size:18px; margin-top:36px; border-bottom:1px solid #2C303A; padding-bottom:8px; }
        .muted { color:#9AA0AC; margin-top:0; }
        .tiles { display:flex; flex-wrap:wrap; gap:12px; margin:20px 0; }
        .tile { background:#1E2129; border-radius:10px; padding:14px 18px; min-width:110px; }
        .tile-label { color:#9AA0AC; font-size:11px; letter-spacing:.04em; }
        .tile-value { font-size:22px; font-weight:600; margin-top:4px; color:#4BA3E0; }
        .panel { background:#1E2129; border-radius:10px; padding:16px 18px; margin:16px 0; }
        table { width:100%; border-collapse:collapse; margin-top:10px; }
        th, td { text-align:left; padding:8px 10px; border-bottom:1px solid #2C303A; font-size:14px; }
        th { color:#9AA0AC; font-weight:600; font-size:12px; }
        """;
}
