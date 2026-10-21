using System.Text.Json;

namespace TheaterApi.Models.Logs;

/// <summary>
/// System.Text.Json reads untyped values (like the "changes" maps) as JsonElement,
/// which Firestore can't store. This converts them to plain .NET values.
/// </summary>
internal static class JsonValues
{
    public static Dictionary<string, object?>? NormalizeMap(Dictionary<string, object?>? map) =>
        map?.ToDictionary(pair => pair.Key, pair => Normalize(pair.Value));

    private static object? Normalize(object? value)
    {
        if (value is not JsonElement element) return value;

        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return element.GetString();
            case JsonValueKind.Number:
                if (element.TryGetInt64(out var whole)) return whole;
                return element.GetDouble();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Object:
                return element.EnumerateObject()
                    .ToDictionary(p => p.Name, p => Normalize(p.Value));
            case JsonValueKind.Array:
                return element.EnumerateArray().Select(item => Normalize(item)).ToList();
            default:
                return null;
        }
    }
}