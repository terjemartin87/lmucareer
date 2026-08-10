namespace LmuCareerTool.Transfers;

/// <summary>Én rad i transfer-markedet: hvor interessert et merke er i deg akkurat nå, uansett
/// om det er nok til å faktisk sende et tilbud ved sesongslutt.</summary>
public class ManufacturerInterestEntry
{
    public string Name { get; set; } = "";
    public int RatingRequired { get; set; }
    public int Interest { get; set; }
    public int RatingGap { get; set; }
    public bool IsCurrentManufacturer { get; set; }
    public bool WouldSendOffer { get; set; }
}
