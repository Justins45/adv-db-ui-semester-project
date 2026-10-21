using System.Text.Json.Serialization;
using Google.Cloud.Firestore;
using TheaterApi.Data;

namespace TheaterApi.Models.Logs;

[FirestoreData]
public class PerformanceStatusLog : ILogDocument
{
    [FirestoreProperty("id")] [JsonPropertyName("id")]
    public int Id { get; set; }

    [FirestoreProperty("performance_id")] [JsonPropertyName("performance_id")]
    public int PerformanceId { get; set; }

    [FirestoreProperty("changed_by")] [JsonPropertyName("changed_by")]
    public int? ChangedBy { get; set; }

    [FirestoreProperty("previous")] [JsonPropertyName("previous")]
    public string? PreviousStatus { get; set; }

    [FirestoreProperty("new")] [JsonPropertyName("new")]
    public string NewStatus { get; set; } = "";

    [FirestoreProperty("date_time", ConverterType = typeof(FlexibleDateTimeConverter))]
    [JsonPropertyName("date_time")]
    public DateTime DateTime { get; set; }
}

public class PerformanceStatusLogRequest
{
    [JsonPropertyName("performance_id")]
    public int PerformanceId { get; set; }

    [JsonPropertyName("changed_by")]
    public int? ChangedBy { get; set; }

    [JsonPropertyName("previous")]
    public string? PreviousStatus { get; set; }

    [JsonPropertyName("new")]
    public string NewStatus { get; set; } = "";

    public PerformanceStatusLog ToEntity()
    {
        var log = new PerformanceStatusLog { DateTime = System.DateTime.UtcNow };
        ApplyTo(log);
        return log;
    }

    public void ApplyTo(PerformanceStatusLog log)
    {
        log.PerformanceId = PerformanceId;
        log.ChangedBy = ChangedBy;
        log.PreviousStatus = PreviousStatus;
        log.NewStatus = NewStatus;
    }
}