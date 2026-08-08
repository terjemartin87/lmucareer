namespace LmuCareerTool.Models;

/// <summary>
/// Et slankt øyeblikksbilde av én sjåførs resultat i en fullført sesongrunde - lagres på
/// SeasonEvent slik at mesterskapstabellen kan regnes ut på nytt fra sesongens egne data,
/// uten å måtte dra med hele DriverResult (rundetider osv.) for alle i feltet.
/// </summary>
public class FieldResultEntry
{
    public string Name { get; set; } = "";
    public string TeamName { get; set; } = "";
    public string CarType { get; set; } = "";
    public int Position { get; set; }
    public string FinishStatus { get; set; } = "";
    public bool IsPlayer { get; set; }
}
