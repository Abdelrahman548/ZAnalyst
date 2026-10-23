using ZAnalyst.Application.Interfaces;
using Action = ZAnalyst.Domain.Entities.Action;

namespace ZAnalyst.Application.Services;

public sealed class ActionService(IActionRepository repository, IUnitOfWork unitOfWork) : IActionService
{
    private readonly IActionRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    public async Task<Action> CreateAsync(Action action, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstInvalidNewEntity(action, nameof(action));
        await _repository.AddAsync(action, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return action;
    }

    public Task<Action?> GetByIdAsync(Guid actionId, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstEmptyId(actionId, nameof(actionId));
        return _repository.GetByIdAsync(actionId, cancellationToken);
    }

    public Task<IReadOnlyList<Action>> GetMatchActionsAsync(Guid matchId, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstEmptyId(matchId, nameof(matchId));
        return _repository.GetMatchActionsAsync(matchId, cancellationToken);
    }

    public Task<IReadOnlyList<Action>> GetPlayerMatchActionsAsync(Guid matchId, Guid playerId, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstEmptyId(matchId, nameof(matchId));
        ServiceGuard.AgainstEmptyId(playerId, nameof(playerId));
        return _repository.GetPlayerMatchActionsAsync(matchId, playerId, cancellationToken);
    }

    public Task UpdateAsync(Guid actionId, Action action, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstMismatchedEntityId(actionId, action, nameof(action));
        return UpdateAndSaveAsync(actionId, action, cancellationToken);
    }

    public async Task RemoveAsync(Guid actionId, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstEmptyId(actionId, nameof(actionId));
        await _repository.RemoveAsync(actionId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task UpdateAndSaveAsync(Guid actionId, Action action, CancellationToken cancellationToken)
    {
        await _repository.UpdateAsync(actionId, action, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
