using TheaterApi.Models;

namespace TheaterApi.Data;

public interface ILogStore
{
    Task WriteAsync(LogEntry entry);
    Task<IReadOnlyList<LogEntry>> GetRecentAsync(int count = 20);
}