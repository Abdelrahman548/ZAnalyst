using ZAnalyst.Application.Interfaces;
using ZAnalyst.Domain.Entities;

namespace ZAnalyst.Application.Services;

public sealed class EventService(IEventRepository repository, IUnitOfWork unitOfWork) : IEventService
{
    private readonly IEventRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    public async Task<Event> CreateAsync(Event domainEvent, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstInvalidNewEntity(domainEvent, nameof(domainEvent));
        await _repository.AddAsync(domainEvent, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return domainEvent;
    }

    public Task<Event?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstEmptyId(eventId, nameof(eventId));
        return _repository.GetByIdAsync(eventId, cancellationToken);
    }

    public Task<IReadOnlyList<Event>> GetMatchEventsAsync(Guid matchId, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstEmptyId(matchId, nameof(matchId));
        return _repository.GetMatchEventsAsync(matchId, cancellationToken);
    }

    public Task UpdateAsync(Guid eventId, Event domainEvent, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstMismatchedEntityId(eventId, domainEvent, nameof(domainEvent));
        return UpdateAndSaveAsync(eventId, domainEvent, cancellationToken);
    }

    public async Task RemoveAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstEmptyId(eventId, nameof(eventId));
        await _repository.RemoveAsync(eventId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task UpdateAndSaveAsync(Guid eventId, Event domainEvent, CancellationToken cancellationToken)
    {
        await _repository.UpdateAsync(eventId, domainEvent, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
