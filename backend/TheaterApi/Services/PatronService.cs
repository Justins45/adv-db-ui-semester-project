using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TheaterApi.Data;
using TheaterApi.Models;

namespace TheaterApi.Services;

public sealed class InvalidPreferencesException(string message) : Exception(message);
public sealed class PointsChangeException(string message) : Exception(message);

public sealed class PatronService(AppDbContext db)
{
    public static string ValidatePreferences(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) throw new InvalidPreferencesException("Preferences must be a JSON object.");
            if (root.GetRawText() == "{}") return "{}";
            var required = new[] { "accessibility", "notifications", "content_advisories" };
            if (root.EnumerateObject().Count() != required.Length || required.Any(x => !root.TryGetProperty(x, out _))) throw new InvalidPreferencesException("Preferences must contain the required fields.");
            var access = root.GetProperty("accessibility");
            if (access.ValueKind != JsonValueKind.Object || !IsBool(access, "wheelchair_accessible") || !IsBool(access, "companion_seat_required") || access.EnumerateObject().Count() != 2) throw new InvalidPreferencesException("Invalid accessibility preferences.");
            var notifications = root.GetProperty("notifications");
            if (notifications.ValueKind != JsonValueKind.Object || !IsBool(notifications, "event_reminders") || !IsBool(notifications, "price_drop_alerts") || !notifications.TryGetProperty("reminder_hours_before", out var hours) || !hours.TryGetInt32(out var h) || h is < 1 or > 168 || notifications.EnumerateObject().Count() != 3) throw new InvalidPreferencesException("Invalid notification preferences.");
            var advisories = root.GetProperty("content_advisories");
            if (advisories.ValueKind != JsonValueKind.Array || advisories.EnumerateArray().Any(x => x.ValueKind != JsonValueKind.String)) throw new InvalidPreferencesException("Content advisories must be an array of strings.");
            return root.GetRawText();
        }
        catch (JsonException) { throw new InvalidPreferencesException("Preferences must be valid JSON."); }
    }

    public async Task<string?> GetPreferencesAsync(int personId, CancellationToken ct) => await db.Patrons.Where(x => x.Id == personId).Select(x => x.Preferences).SingleOrDefaultAsync(ct);
    public async Task<bool> SetPreferencesAsync(int personId, string json, CancellationToken ct) { var preferences = ValidatePreferences(json); var patron = await db.Patrons.FindAsync([personId], ct); if (patron is null) return false; patron.Preferences = preferences; await db.SaveChangesAsync(ct); return true; }
    public async Task<bool> ChangePointsAsync(int personId, int amount, CancellationToken ct)
    {
        var updated = await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE patron SET points_balance = points_balance + {amount} WHERE id = {personId} AND points_balance + {amount} >= 0", ct);
        return updated == 1;
    }
    private static bool IsBool(JsonElement element, string name) => element.TryGetProperty(name, out var value) && (value.ValueKind is JsonValueKind.True or JsonValueKind.False);
}
