namespace LmuCareerTool.Career;

public static class PointsCalculator
{
    // Klassisk F1-poengskala. Justér fritt om du vil ha en annen fordeling.
    private static readonly int[] PointsByPosition = { 25, 18, 15, 12, 10, 8, 6, 4, 2, 1 };

    public static int PointsForPosition(int position)
    {
        if (position < 1 || position > PointsByPosition.Length) return 0;
        return PointsByPosition[position - 1];
    }
}
