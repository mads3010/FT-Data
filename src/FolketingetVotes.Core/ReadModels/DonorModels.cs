using System.Globalization;
using System.Text;

namespace FolketingetVotes.Core.ReadModels;

public sealed record DonorFilter(string? Query = null, string? PartyShortName = null, int? Year = null);

public sealed record DonorListItem(
    string Key,
    string DisplayName,
    IReadOnlyList<string> Parties,
    int FirstYear,
    int LastYear,
    int RowCount,
    decimal? StatedTotal,
    int? ActorId);

public sealed record DonorContributionRow(
    int Year,
    string PartyName,
    string? PartyShortName,
    string? DonorAddress,
    decimal? Amount,
    string? Note,
    int? SourcePage,
    string SourceFile,
    string RawText);

public sealed record DonorDetail(string Key, string DisplayName, int? ActorId, IReadOnlyList<DonorContributionRow> Rows);

/// <summary>Groups OCR'd donor names that differ only in case, punctuation or spacing.</summary>
public static class DonorNames
{
    public static string Normalize(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var sb = new StringBuilder(name.Length);
        var lastWasSpace = true;
        foreach (var raw in name.Normalize(NormalizationForm.FormC))
        {
            var c = char.ToLowerInvariant(raw);
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
                lastWasSpace = false;
            }
            else if (!lastWasSpace)
            {
                sb.Append(' ');
                lastWasSpace = true;
            }
        }

        return sb.ToString().Trim();
    }

    public static string Slug(string key) => Uri.EscapeDataString(key.Replace(' ', '-'));

    public static string FromSlug(string slug) => Uri.UnescapeDataString(slug).Replace('-', ' ');
}
