using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Components;

namespace FolketingetVotes.Web.Services;

/// <summary>SVG helpers for Razor: <c>&lt;text&gt;</c> is a Razor keyword, so text elements are emitted as markup.</summary>
public static class Svg
{
    public static string F(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);

    public static MarkupString Text(double x, double y, string cssClass, string anchor, string content) =>
        new($"<text x=\"{F(x)}\" y=\"{F(y)}\" class=\"{cssClass}\" text-anchor=\"{anchor}\">{WebUtility.HtmlEncode(content)}</text>");
}
