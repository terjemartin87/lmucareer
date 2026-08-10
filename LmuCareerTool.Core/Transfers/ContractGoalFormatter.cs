namespace LmuCareerTool.Transfers;

public static class ContractGoalFormatter
{
    public static string Describe(IReadOnlyList<ContractGoal> goals) =>
        goals.Count == 0 ? "Ingen krav" : string.Join(" · ", goals.Select(g => g.Description));
}
