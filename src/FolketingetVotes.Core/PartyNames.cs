namespace FolketingetVotes.Core;

/// <summary>
/// Maps party names as written in biographies and party accounts to the group short names used by oda.ft.dk
/// (<c>gruppenavnkort</c>). Names are matched case-insensitively after trimming; longest alias first.
/// </summary>
public static class PartyNames
{
    private static readonly (string Name, string ShortName)[] Aliases =
    [
        ("Socialdemokratiet", "S"), ("Socialdemokraterne", "S"), ("Socialdemokratiet i Danmark", "S"),
        ("Venstre, Danmarks Liberale Parti", "V"), ("Venstre", "V"),
        ("Det Konservative Folkeparti", "KF"), ("Konservative Folkeparti", "KF"), ("Konservative", "KF"), ("Det Konservative Folkeparti (KF)", "KF"),
        ("Socialistisk Folkeparti", "SF"), ("SF - Socialistisk Folkeparti", "SF"),
        ("Dansk Folkeparti", "DF"),
        ("Det Radikale Venstres Landsforbund", "RV"), ("Radikale Venstres Landsforbund", "RV"), ("Det Radikale Venstre", "RV"), ("Radikale Venstre", "RV"),
        ("Enhedslisten", "EL"), ("Enhedslisten - De Rød-Grønne", "EL"),
        ("Liberal Alliance", "LA"), ("Ny Alliance", "NY"),
        ("Alternativet", "ALT"), ("Moderaterne", "M"),
        ("Danmarksdemokraterne", "DD"), ("Danmarksdemokraterne – Inger Støjberg", "DD"),
        ("Nye Borgerlige", "NB"), ("Kristendemokraterne", "KD"), ("Kristeligt Folkeparti", "KrF"),
        ("Frie Grønne", "FG"), ("Frie Grønne, Danmarks Nye Venstrefløjsparti", "FG"), ("Danmarks Nye Venstrefløjsparti", "FG"),
        ("Venstres Landsorganisation", "V"), ("Dansk Folkepartis Landsorganisation", "DF"), ("Dansk Folkeparti Landsorganisation", "DF"), ("Liberal Alliance Landsorganisation", "LA"),
        ("Borgernes Parti", "BP"), ("Borgernes Parti – Lars Boje Mathiesen", "BP"),
        ("Centrum-Demokraterne", "CD"), ("Fremskridtspartiet", "FP"), ("Venstresocialisterne", "VS"),
        ("Danmarks Retsforbund", "DR"), ("Retsforbundet", "DR"), ("Slesvigsk Parti", "SL"), ("De Uafhængige", "U"),
        ("Uden for folketingsgrupperne", "UFG"), ("Uden for partierne", "UFG"), ("Løsgænger", "UFG"),
        ("Inuit Ataqatigiit", "IA"), ("Siumut", "SIU"), ("Naleraq", "N"), ("Nunatta Qitornai", "NQ"),
        ("Sambandsflokkurin", "SP"), ("Javnaðarflokkurin", "JF"), ("Tjóðveldi", "T"), ("Tjóðveldisflokkurin", "TF"), ("Fólkaflokkurin", "FF"), ("Folkaflokkurin", "FF"),
        ("Danmarks Kommunistiske Parti", "DKP"), ("Liberalt Centrum", "LC"), ("Medlemsgruppen Frihed 2000", "FRI"), ("Frihed 2000", "FRI"),
        ("Fælles Kurs", "FK"), ("Fredspolitisk Folkeparti", "Fredspolitisk folkeparti"), ("Trivselspartiet", "TP"), ("Erhvervspartiet", "EP"), ("Det Liberale Højre", "LH"),
    ];

    private static readonly Dictionary<string, string> Exact = Aliases.ToDictionary(a => Normalize(a.Name), a => a.ShortName, StringComparer.Ordinal);

    private static readonly (string Name, string ShortName)[] ByLength = [.. Aliases.OrderByDescending(a => a.Name.Length)];

    /// <summary>All alias names, longest first (useful for detecting party headings in free text).</summary>
    public static IReadOnlyList<string> Names { get; } = [.. ByLength.Select(a => a.Name).Distinct(StringComparer.OrdinalIgnoreCase)];

    /// <summary>Exact (normalised) match, then the longest alias contained in the text; null when unknown.</summary>
    public static string? ShortNameFor(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        if (Exact.TryGetValue(Normalize(name), out var exact))
        {
            return exact;
        }

        foreach (var (alias, shortName) in ByLength)
        {
            if (name.Contains(alias, StringComparison.OrdinalIgnoreCase))
            {
                return shortName;
            }
        }

        return null;
    }

    private static string Normalize(string value) => value.Trim().TrimEnd('.').ToLowerInvariant();
}
