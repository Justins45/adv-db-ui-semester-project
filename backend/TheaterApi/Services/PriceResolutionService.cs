using TheaterApi.Models;

namespace TheaterApi.Services;

public interface IPriceResolutionService
{
    decimal Resolve(PerformanceSeat seat, IEnumerable<PriceTier> tiers);
}

public sealed class PriceResolutionService : IPriceResolutionService
{
    public decimal Resolve(PerformanceSeat seat, IEnumerable<PriceTier> tiers)
    {
        var performance = seat.Performance;
        var section = seat.Seat.Section;
        var applicable = tiers.Where(t =>
            (!t.SectionId.HasValue || t.SectionId == section.Id) &&
            (!t.PerformanceId.HasValue || t.PerformanceId == performance.Id) &&
            (!t.StageId.HasValue || t.StageId == performance.StageId) &&
            (!t.EventId.HasValue || t.EventId == performance.EventId));
        return applicable.OrderByDescending(t => (t.SectionId.HasValue ? 1 : 0) +
                                                  (t.PerformanceId.HasValue ? 1 : 0) +
                                                  (t.StageId.HasValue ? 1 : 0) +
                                                  (t.EventId.HasValue ? 1 : 0))
                         .ThenByDescending(t => t.Price)
                         .Select(t => t.Price).FirstOrDefault();
    }
}
