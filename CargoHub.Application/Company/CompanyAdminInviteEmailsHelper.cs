using System.Text.Json;

namespace CargoHub.Application.Company;

/// <summary>Parses and stores the list of explicit admin invite emails for company admin invites.</summary>
public static class CompanyAdminInviteEmailsHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static List<string> NormalizeList(IEnumerable<string>? source)
    {
        if (source == null)
            return new List<string>();
        return source
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string? SerializeJson(IReadOnlyList<string> emails)
    {
        var list = NormalizeList(emails);
        return list.Count == 0 ? null : JsonSerializer.Serialize(list, JsonOptions);
    }

    public static IReadOnlyList<string> DeserializeJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<string>();
        try
        {
            var parsed = JsonSerializer.Deserialize<List<string>>(json, JsonOptions);
            return NormalizeList(parsed);
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    /// <summary>Explicit targets for (re)sending invites: JSON list, else legacy single column.</summary>
    public static IReadOnlyList<string> GetExplicitTargets(string? json, string? legacySingleEmail)
    {
        var fromJson = DeserializeJson(json);
        if (fromJson.Count > 0)
            return fromJson;
        if (!string.IsNullOrWhiteSpace(legacySingleEmail))
            return new[] { legacySingleEmail.Trim() };
        return Array.Empty<string>();
    }
}
