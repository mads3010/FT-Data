namespace FolketingetVotes.Web.Services;

/// <summary>
/// Conventional party colours as used by Danish media, for identity swatches only. Never used to encode a
/// vote outcome: ballots use the validated blue/orange/aqua/gray scheme in app.css.
/// </summary>
public static class PartyColors
{
    private static readonly Dictionary<string, string> Colors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["S"] = "#c4161c",
        ["V"] = "#1e4c8b",
        ["M"] = "#7b3f9e",
        ["SF"] = "#d9006c",
        ["DD"] = "#1d5f8a",
        ["LA"] = "#3fb2ce",
        ["KF"] = "#3b7d22",
        ["EL"] = "#e6801a",
        ["RV"] = "#e5007d",
        ["DF"] = "#e9c22e",
        ["ALT"] = "#2b8c3a",
        ["NB"] = "#12345f",
        ["KD"] = "#7e6f2c",
        ["FG"] = "#5aa02c",
        ["BP"] = "#8a6d3b",
        ["IA"] = "#c9302c",
        ["SIU"] = "#b04e2a",
        ["SP"] = "#c9a227",
        ["JF"] = "#3e6f9e",
        ["T"] = "#7a7a7a",
        ["UFG"] = "#9a9a94",
    };

    public static string For(string? shortName) =>
        shortName is not null && Colors.TryGetValue(shortName, out var color) ? color : "#9a9a94";
}
