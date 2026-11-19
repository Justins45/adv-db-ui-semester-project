using System.Text.Json;
using Google.Cloud.Firestore;

public class FirestoreSeeder
{
    private readonly FirestoreDb _db;
    public FirestoreSeeder(FirestoreDb db) => _db = db;

    public async Task SeedAsync(string jsonPath)
    {
        var json = await File.ReadAllTextAsync(jsonPath);
        using var doc = JsonDocument.Parse(json);

        // Batch limit is 500 writes; the seed is ~100 documents, so one batch is fine.
        var writes = new List<(DocumentReference Reference, Dictionary<string, object?> Data)>();

        foreach (var collection in doc.RootElement.EnumerateObject())
        {
            foreach (var item in collection.Value.EnumerateArray())
            {
                var data = ConvertObject(item);
                var id = item.GetProperty("id").GetInt32().ToString();
                writes.Add((_db.Collection(collection.Name).Document(id), data));
            }
        }

        foreach (var chunk in writes.Chunk(500))
        {
            var batch = _db.StartBatch();
            foreach (var write in chunk) batch.Set(write.Reference, write.Data);
            await batch.CommitAsync();
        }
    }

    // JSON object -> Firestore map (recurses into nested maps and arrays)
    private static Dictionary<string, object?> ConvertObject(JsonElement element)
    {
        var map = new Dictionary<string, object?>();
        foreach (var prop in element.EnumerateObject())
            map[prop.Name] = ConvertValue(prop.Value, prop.Name);
        return map;
    }

    private static object? ConvertValue(JsonElement value, string? propertyName = null)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Object => ConvertObject(value),
            JsonValueKind.Array  => value.EnumerateArray()
                                         .Select(v => ConvertValue(v))
                                         .ToList(),
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.TryGetInt64(out var l) ? l : value.GetDouble(),
            JsonValueKind.True   => true,
            JsonValueKind.False  => false,
            JsonValueKind.Null   => null,
            _ => value.ToString()
        };
    }
}
