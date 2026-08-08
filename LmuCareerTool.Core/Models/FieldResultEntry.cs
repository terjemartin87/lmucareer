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

    /// <summary>LMUs egen klassebetegnelse for bilen (f.eks. "GT3", "LMP2") og sjåførens plassering
    /// INNENFOR den klassen - hentet direkte fra resultatfilens &lt;CarClass&gt;/&lt;ClassPosition&gt;.
    /// Karrieremodus bruker ikke disse (én fast klasse per sesong), men Liga modus scorer alltid
    /// per klasse siden et hostet løp fritt kan blande GT3, LMP2, LMP3 og Hypercar i samme race.</summary>
    public string CarClass { get; set; } = "";
    public int ClassPosition { get; set; }
}
