namespace FolketingetVotes.Core.ReadModels;

public sealed record CompositionRow(string PartyShortName, string PartyName, int Members, int Women, int Men, int Unknown, double? MedianAge, double? MedianSeniorityYears)
{
    public double? WomenShare => Women + Men == 0 ? null : Women / (double)(Women + Men);
}

public sealed record CompositionReport(DateOnly AsOf, IReadOnlyList<CompositionRow> Parties, CompositionRow Total);

public static class Median
{
    public static double? Of(IEnumerable<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        if (sorted.Count == 0)
        {
            return null;
        }

        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2.0;
    }
}
