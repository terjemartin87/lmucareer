using System.Net;
using System.Text;
using LmuCareerTool.Models;

namespace LmuCareerTool.Career;

/// <summary>Bygger en selvstendig HTML-fil av sesongrapporten, til deling med andre.</summary>
public static class SeasonReportHtmlBuilder
{
    public static string Build(SeasonReport report, string driverName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"no\"><head><meta charset=\"utf-8\">");
        sb.AppendLine($"<title>Sesong {report.SeasonNumber} - {Enc(report.CarClass)}</title>");
        sb.AppendLine("<style>" + Css + "</style></head><body>");

        sb.AppendLine("<div class=\"page\">");
        sb.AppendLine($"<h1>🏆 Sesong {report.SeasonNumber} - {Enc(report.CarClass)}</h1>");
        sb.AppendLine($"<p class=\"muted\">{Enc(driverName)}" +
                       (report.Manufacturer != null ? $" &middot; {Enc(report.Manufacturer)}" : "") + "</p>");

        sb.AppendLine("<div class=\"tiles\">");
        AppendTile(sb, "PLASSERING", report.ChampionshipPosition > 0 ? $"P{report.ChampionshipPosition} / {report.TotalDrivers}" : "-");
        AppendTile(sb, "POENG", report.Points.ToString());
        AppendTile(sb, "SEIRE", report.Wins.ToString());
        AppendTile(sb, "PODIER", report.Podiums.ToString());
        AppendTile(sb, "POLE POSITIONS", report.PolePositions.ToString());
        AppendTile(sb, "DNF", report.Dnfs.ToString());
        AppendTile(sb, "SNITTPLASSERING", report.AverageFinish > 0 ? $"P{report.AverageFinish:0.#}" : "-");
        AppendTile(sb, "DISTANSE", $"{report.TotalDistanceKm:0} km");
        sb.AppendLine("</div>");

        sb.AppendLine($"<div class=\"panel\"><strong>Kontraktsoppgjør:</strong> {Enc(report.ContractSummary)}</div>");

        if (report.Awards.Count > 0)
        {
            sb.AppendLine("<h2>Sesongpriser</h2><div class=\"awards\">");
            foreach (var award in report.Awards)
            {
                sb.AppendLine($"<div class=\"award\"><div class=\"award-title\">🏅 {Enc(award.Title)}</div>" +
                               $"<div class=\"muted\">{Enc(award.Description)}</div></div>");
            }
            sb.AppendLine("</div>");
        }

        sb.AppendLine("<h2>Runde for runde</h2>");
        sb.AppendLine("<table><tr><th>Runde</th><th>Bane</th><th>Resultat</th><th>Poeng</th><th>Tabellposisjon</th></tr>");
        foreach (var round in report.Rounds)
        {
            sb.AppendLine($"<tr><td>{round.RoundNumber}</td><td>{Enc(round.TrackVenue)}</td>" +
                           $"<td>{(round.FinishPos > 0 ? $"P{round.FinishPos}" : "-")}</td><td>{round.PointsEarned}</td>" +
                           $"<td>{(round.ChampionshipPositionAfter > 0 ? $"P{round.ChampionshipPositionAfter}" : "-")}</td></tr>");
        }
        sb.AppendLine("</table>");

        sb.AppendLine("<h2>Førermesterskap</h2>");
        sb.AppendLine("<table><tr><th>#</th><th>Fører</th><th>Team</th><th>Poeng</th><th>Seire</th><th>Podier</th></tr>");
        for (var i = 0; i < report.DriverStandings.Count; i++)
        {
            var d = report.DriverStandings[i];
            var rowClass = d.IsPlayer ? " class=\"player\"" : "";
            sb.AppendLine($"<tr{rowClass}><td>{i + 1}</td><td>{Enc(d.Name)}{(d.IsPlayer ? " ★" : "")}</td>" +
                           $"<td>{Enc(d.TeamName)}</td><td>{d.Points}</td><td>{d.Wins}</td><td>{d.Podiums}</td></tr>");
        }
        sb.AppendLine("</table>");

        sb.AppendLine("<h2>Merkemesterskap</h2>");
        sb.AppendLine("<table><tr><th>#</th><th>Merke</th><th>Poeng</th><th>Seire</th></tr>");
        for (var i = 0; i < report.ManufacturerStandings.Count; i++)
        {
            var m = report.ManufacturerStandings[i];
            sb.AppendLine($"<tr><td>{i + 1}</td><td>{Enc(m.Manufacturer)}</td><td>{m.Points}</td><td>{m.Wins}</td></tr>");
        }
        sb.AppendLine("</table>");

        if (report.TrackRecords.Count > 0)
        {
            sb.AppendLine("<h2>Personlige rekorder</h2>");
            sb.AppendLine("<table><tr><th>Bane</th><th>Beste runde</th><th></th></tr>");
            foreach (var r in report.TrackRecords)
            {
                sb.AppendLine($"<tr><td>{Enc(r.TrackVenue)}</td><td>{FormatTime(r.BestLapSeconds)}</td>" +
                               $"<td>{(r.IsCareerBest ? "🏆 Karriererekord" : "")}</td></tr>");
            }
            sb.AppendLine("</table>");
        }

        sb.AppendLine("</div></body></html>");
        return sb.ToString();
    }

    private static void AppendTile(StringBuilder sb, string label, string value) =>
        sb.AppendLine($"<div class=\"tile\"><div class=\"tile-label\">{Enc(label)}</div><div class=\"tile-value\">{Enc(value)}</div></div>");

    private static string Enc(string value) => WebUtility.HtmlEncode(value);

    private static string FormatTime(double seconds)
    {
        if (seconds <= 0) return "-";
        var ts = TimeSpan.FromSeconds(seconds);
        return $"{(int)ts.TotalMinutes}:{ts.Seconds:00}.{ts.Milliseconds:000}";
    }

    private const string Css = """
        body { background:#14161B; color:#EDEDED; font-family:'Segoe UI',Arial,sans-serif; margin:0; }
        .page { max-width:900px; margin:0 auto; padding:32px 24px 64px; }
        h1 { font-size:28px; margin-bottom:4px; }
        h2 { font-size:18px; margin-top:36px; border-bottom:1px solid #2C303A; padding-bottom:8px; }
        .muted { color:#9AA0AC; margin-top:0; }
        .tiles { display:flex; flex-wrap:wrap; gap:12px; margin:20px 0; }
        .tile { background:#1E2129; border-radius:10px; padding:14px 18px; min-width:110px; }
        .tile-label { color:#9AA0AC; font-size:11px; letter-spacing:.04em; }
        .tile-value { font-size:22px; font-weight:600; margin-top:4px; color:#E04B3C; }
        .panel { background:#1E2129; border-radius:10px; padding:16px 18px; margin:16px 0; }
        .awards { display:flex; flex-direction:column; gap:10px; }
        .award { background:#1E2129; border-radius:8px; padding:12px 16px; }
        .award-title { font-weight:600; margin-bottom:2px; }
        table { width:100%; border-collapse:collapse; margin-top:10px; }
        th, td { text-align:left; padding:8px 10px; border-bottom:1px solid #2C303A; font-size:14px; }
        th { color:#9AA0AC; font-weight:600; font-size:12px; }
        tr.player { background:#20232B; font-weight:600; }
        """;
}
