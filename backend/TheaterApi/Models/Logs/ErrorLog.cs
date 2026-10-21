using System.Text.Json.Serialization;
using Google.Cloud.Firestore;

namespace TheaterApi.Models.Logs;

[FirestoreData]
public class ErrorLog : ILogDocument
{
    [FirestoreProperty("id")] [JsonPropertyName("id")]
    public int Id { get; set; }

    [FirestoreProperty("person_id")] [JsonPropertyName("person_id")]
    public int? PersonId { get; set; }

    [FirestoreProperty("end_point")] [JsonPropertyName("end_point")]
    public string EndPoint { get; set; } = "";

    [FirestoreProperty("error_message")] [JsonPropertyName("error_message")]
    public string ErrorMessage { get; set; } = "";

    [FirestoreProperty("stack_trace")] [JsonPropertyName("stack_trace")]
    public string? StackTrace { get; set; }
}

public class ErrorLogRequest
{
    [JsonPropertyName("person_id")]
    public int? PersonId { get; set; }

    [JsonPropertyName("end_point")]
    public string EndPoint { get; set; } = "";

    [JsonPropertyName("error_message")]
    public string ErrorMessage { get; set; } = "";

    [JsonPropertyName("stack_trace")]
    public string? StackTrace { get; set; }

    public ErrorLog ToEntity()
    {
        var log = new ErrorLog();
        ApplyTo(log);
        return log;
    }

    public void ApplyTo(ErrorLog log)
    {
        log.PersonId = PersonId;
        log.EndPoint = EndPoint;
        log.ErrorMessage = ErrorMessage;
        log.StackTrace = StackTrace;
    }
}