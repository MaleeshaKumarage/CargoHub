using System.Net;

namespace CargoHub.Application.AdminEmail;

/// <summary>
/// Converts plain-text release notes to safe HTML that preserves line breaks and spacing (pre-wrap), matching typical email client plain-text behavior.
/// </summary>
public static class ReleaseNotesEmailBodyFormatter
{
    public const int MaxBodyLength = 100_000;

    public static string ToHtml(string plainText)
    {
        var encoded = WebUtility.HtmlEncode(plainText ?? "");
        return $"<div style=\"white-space: pre-wrap; font-family: sans-serif;\">{encoded}</div>";
    }
}
