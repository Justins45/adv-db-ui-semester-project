using System.Text.Json;
using Google.Cloud.Firestore;

public class FirestoreSeeder
{
    // Stored as ISO strings in the JSON; converted to real Firestore Timestamps
    private static readonly HashSet<string> TimestampFields = new() { "date_time", "changed_at" };

    private readonly FirestoreDb _db;
    public FirestoreSeeder(FirestoreDb db) => _db = db;

    public async Task SeedAsync(string jsonPath)
    {
        var json = await File.ReadAllTextAsync(jsonPath);
        using var doc = JsonDocument.Parse(json);

        var batch = _db.StartBatch(); // limit is 500 writes; 50 fits easily

        foreach (var collection in doc.RootElement.EnumerateObject())
        {
            foreach (var item in collection.Value.EnumerateArray())
            {
                var data = new Dictionary<string, object>();
                foreach (var prop in item.EnumerateObject())
                    data[prop.Name] = ConvertValue(prop);

                var id = item.GetProperty("id").GetInt32().ToString();
                batch.Set(_db.Collection(collection.Name).Document(id), data);
            }
        }

        await batch.CommitAsync();
    }

    private static object ConvertValue(JsonProperty prop)
    {
        if (TimestampFields.Contains(prop.Name))
            return Timestamp.FromDateTime(prop.Value.GetDateTimeOffset().UtcDateTime);

        return prop.Value.ValueKind switch
        {
            JsonValueKind.Number => prop.Value.GetInt64(),
            JsonValueKind.String => prop.Value.GetString()!,
            _ => prop.Value.ToString()
        };
    }
}