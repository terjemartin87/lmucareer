namespace LmuCareerTool.Transfers;

/// <summary>
/// Din aktive avtale med ett merke (eller et privatlag/fri klasse) for en gitt bilklasse.
/// Erstatter den gamle "permanent opplåsing"-modellen - et merke er ikke låst opp for alltid,
/// du kjører for den du til enhver tid har kontrakt med.
/// </summary>
public class Contract
{
    public string Manufacturer { get; set; } = "";
    public string CarClass { get; set; } = "";
    public string Car { get; set; } = "";

    public int SeasonsRemaining { get; set; }
    public int SalaryPerRound { get; set; }

    /// <summary>Alle krav som må innfris for at merket skal fornye deg neste sesong (tom liste = ingen krav).</summary>
    public List<ContractGoal> Goals { get; set; } = new();

    public int SignedInSeasonNumber { get; set; }

    /// <summary>Betalt sete hos et privatlag - ingen lønn, ingen sesongmål, ingen fabrikkstøtte.</summary>
    public bool IsPrivateerSeat { get; set; }

    /// <summary>Klasser uten merke-oppsett ennå (LMP2/LMP3/Hypercar) - ingen reell forhandling, bare fri kjøring.</summary>
    public bool IsFreeAgent { get; set; }
}
