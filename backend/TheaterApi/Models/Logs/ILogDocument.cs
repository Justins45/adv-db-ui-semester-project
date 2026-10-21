namespace TheaterApi.Models.Logs;

/// <summary>Every log document has an int id, which is also its Firestore document ID.</summary>
public interface ILogDocument
{
    int Id { get; set; }
}