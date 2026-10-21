using TheaterApi.Models.Logs;

namespace TheaterApi.Data;

public interface ILogRepository<T> where T : class, ILogDocument
{
    Task<IReadOnlyList<T>> GetRecentAsync(int limit, CancellationToken ct = default);
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<T> CreateAsync(T entity, CancellationToken ct = default);
    Task<T?> UpdateAsync(int id, Action<T> apply, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}