using System.Globalization;
using Google.Cloud.Firestore;

namespace TheaterApi.Data;

/// <summary>
/// Reads a date stored either as a Firestore Timestamp or as an ISO string
/// (what you get if the seed JSON was imported as-is). Always writes Timestamps.
/// </summary>
public class FlexibleDateTimeConverter : IFirestoreConverter<DateTime>
{
    public object ToFirestore(DateTime value) =>
        Timestamp.FromDateTime(value.ToUniversalTime());

    public DateTime FromFirestore(object value) => value switch
    {
        Timestamp ts => ts.ToDateTime(),
        string s => DateTime.Parse(s, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal),
        _ => throw new ArgumentException($"Can't convert {value.GetType()} to DateTime.")
    };
}