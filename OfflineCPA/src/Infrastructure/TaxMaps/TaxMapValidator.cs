using System.Text.Json;
using System.Text.RegularExpressions;

namespace Infrastructure.TaxMaps;

/// <summary>
/// Structural check for a versioned tax map file (see tax-maps/*.json).
/// Returns one message per problem; an empty list means the file passes.
/// </summary>
public static partial class TaxMapValidator
{
    private const string NoVerifiedSource = "Data not available.";

    [GeneratedRegex(@"^(\[(High|Moderate|Low) \d+/\d+\]|\[Inferred\])$")]
    private static partial Regex ConfidenceTag();

    public static IReadOnlyList<string> Validate(JsonElement map)
    {
        var errors = new List<string>();

        RequireString(map, "form", errors);
        RequireString(map, "chart", errors);
        if (!map.TryGetProperty("taxYear", out var year) || year.ValueKind != JsonValueKind.Number)
        {
            errors.Add("taxYear must be a number.");
        }

        if (!map.TryGetProperty("sources", out var sources) || sources.ValueKind != JsonValueKind.Array || sources.GetArrayLength() == 0)
        {
            errors.Add("sources must be a non-empty array.");
        }

        var lineIds = new HashSet<string>();
        if (!map.TryGetProperty("lines", out var lines) || lines.ValueKind != JsonValueKind.Array || lines.GetArrayLength() == 0)
        {
            errors.Add("lines must be a non-empty array.");
        }
        else
        {
            foreach (var line in lines.EnumerateArray())
            {
                ValidateLine(line, lineIds, errors);
            }
        }

        if (!map.TryGetProperty("accounts", out var accounts) || accounts.ValueKind != JsonValueKind.Object)
        {
            errors.Add("accounts must be an object.");
        }
        else
        {
            foreach (var entry in accounts.EnumerateObject())
            {
                if (!Regex.IsMatch(entry.Name, @"^\d{4}$"))
                {
                    errors.Add($"account code '{entry.Name}' must be four digits.");
                }

                var target = entry.Value.GetString();
                if (target is null || !lineIds.Contains(target))
                {
                    errors.Add($"account {entry.Name} points at unknown line '{target}'.");
                }
            }
        }

        return errors;
    }

    private static void ValidateLine(JsonElement line, HashSet<string> lineIds, List<string> errors)
    {
        var id = line.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(id))
        {
            errors.Add("a line is missing its id.");
            return;
        }

        if (!lineIds.Add(id))
        {
            errors.Add($"line id {id} is repeated.");
        }

        if (!line.TryGetProperty("label", out var label) || string.IsNullOrWhiteSpace(label.GetString()))
        {
            errors.Add($"line {id} needs a label.");
        }

        var confidence = line.TryGetProperty("confidence", out var conf) ? conf.GetString() : null;
        if (confidence is null || (confidence != NoVerifiedSource && !ConfidenceTag().IsMatch(confidence)))
        {
            errors.Add($"line {id} needs a confidence tag such as [Moderate 1/1] or \"{NoVerifiedSource}\".");
        }
    }

    private static void RequireString(JsonElement map, string name, List<string> errors)
    {
        if (!map.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
        {
            errors.Add($"{name} must be a non-empty string.");
        }
    }
}
