using Microsoft.EntityFrameworkCore;
using ZAnalyst.Application.Interfaces;
using ZAnalyst.Domain.Entities;
using ZAnalyst.Infrastructure.Persistence;

namespace ZAnalyst.Infrastructure.Repositories;

internal sealed class EventRepository(ZAnalystDbContext context)
    : EfRepository<Event>(context), IEventRepository
{
    public Task AddAsync(Event domainEvent, CancellationToken cancellationToken = default) =>
        base.StageAddAsync(domainEvent, cancellationToken);

    public Task<Event?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(domainEvent => domainEvent.Id == eventId, cancellationToken);

    public async Task<IReadOnlyList<Event>> GetMatchEventsAsync(
        Guid matchId,
        CancellationToken cancellationToken = default) =>
        await Set.Where(domainEvent => domainEvent.MatchId == matchId)
            .ToListAsync(cancellationToken);

    public Task UpdateAsync(Guid eventId, Event domainEvent, CancellationToken cancellationToken = default) =>
        base.StageUpdateAsync(eventId, domainEvent, cancellationToken);

    public Task RemoveAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        base.StageRemoveAsync(eventId, cancellationToken);
}
