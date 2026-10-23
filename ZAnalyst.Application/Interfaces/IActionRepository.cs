using Action = ZAnalyst.Domain.Entities.Action;

namespace ZAnalyst.Application.Interfaces;

public interface IActionRepository
{
    Task AddAsync(Action action, CancellationToken cancellationToken = default);
    Task<Action?> GetByIdAsync(Guid actionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Action>> GetMatchActionsAsync(Guid matchId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Action>> GetPlayerMatchActionsAsync(Guid matchId, Guid playerId, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid actionId, Action action, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid actionId, CancellationToken cancellationToken = default);
}
