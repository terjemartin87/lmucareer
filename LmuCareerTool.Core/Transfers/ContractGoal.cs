namespace LmuCareerTool.Transfers;

public enum ContractGoalType
{
    MinChampionshipPosition,
    MinPodiums,
    MinWins,
    MinSafetyRating,
}

/// <summary>Ett konkret krav i en kontrakt - en avtale kan ha flere, og ALLE må innfris.</summary>
public class ContractGoal
{
    public ContractGoalType Type { get; set; }
    public int TargetValue { get; set; }
    public string Description { get; set; } = "";
}
