namespace LmuCareerTool.Content;

public class ManufacturerDefinition
{
    public string Name { get; set; } = "";
    public List<string> Cars { get; set; } = new();
    public bool StartUnlocked { get; set; }
    public int RatingRequired { get; set; }
    public int UnlockCost { get; set; }
}

public class ClassDefinition
{
    public string Name { get; set; } = "";
    public int Order { get; set; }
    public int XpThreshold { get; set; }
    public List<string> Cars { get; set; } = new();
    public List<ManufacturerDefinition> Manufacturers { get; set; } = new();
}

public class GameContent
{
    public List<ClassDefinition> Classes { get; set; } = new();
    public List<string> Tracks { get; set; } = new();
    public List<string> WeatherOptions { get; set; } = new();
}
