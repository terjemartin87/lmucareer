namespace LmuCareerTool.Transfers;

/// <summary>Et konkret tilbud fra et merke (eller privatlag/fri klasse), vist i overgangsvinduet.</summary>
public class ContractOffer
{
    public string Manufacturer { get; set; } = "";
    public string CarClass { get; set; } = "";
    public string Car { get; set; } = "";

    /// <summary>0-100, kun til visning/sortering - ikke lagret på selve kontrakten.</summary>
    public int InterestScore { get; set; }

    public int LengthSeasons { get; set; }
    public int SalaryPerRound { get; set; }

    public int GoalTargetPosition { get; set; }
    public string GoalDescription { get; set; } = "";

    public string Reasoning { get; set; } = "";

    /// <summary>True hvis dette er en fornyelse fra merket du allerede kjører for.</summary>
    public bool IsRenewal { get; set; }

    /// <summary>True for det alltid-tilgjengelige betalte privatlag-setet.</summary>
    public bool IsPrivateerSeat { get; set; }

    /// <summary>True for klasser uten merke-oppsett ennå - ingen reell forhandling.</summary>
    public bool IsFreeAgent { get; set; }

    /// <summary>Engangskostnad i credits for å signere (kun privatlag-setet har dette > 0).</summary>
    public int SigningCost { get; set; }
}
