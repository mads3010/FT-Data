namespace FolketingetVotes.Web.Services;

/// <summary>Builds relative URLs with only the non-empty query parameters.</summary>
public static class QueryString
{
    public static string Build(string path, params (string Key, string? Value)[] parameters)
    {
        var parts = parameters
            .Where(p => !string.IsNullOrEmpty(p.Value))
            .Select(p => Uri.EscapeDataString(p.Key) + "=" + Uri.EscapeDataString(p.Value!))
            .ToList();
        return parts.Count == 0 ? path : path + "?" + string.Join('&', parts);
    }
}
