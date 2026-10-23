using ZAnalyst.Domain.Entities;

namespace ZAnalyst.Application.Interfaces;

public interface IEventService
{
    Task<Event> CreateAsync(Event domainEvent, CancellationToken cancellationToken = default);
    Task<Event?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Event>> GetMatchEventsAsync(Guid matchId, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid eventId, Event domainEvent, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid eventId, CancellationToken cancellationToken = default);
}
