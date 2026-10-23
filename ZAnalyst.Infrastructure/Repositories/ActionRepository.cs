using Microsoft.EntityFrameworkCore;
using ZAnalyst.Application.Interfaces;
using Action = ZAnalyst.Domain.Entities.Action;
using ZAnalyst.Infrastructure.Persistence;

namespace ZAnalyst.Infrastructure.Repositories;

internal sealed class ActionRepository(ZAnalystDbContext context)
    : EfRepository<Action>(context), IActionRepository
{
    public Task AddAsync(Action action, CancellationToken cancellationToken = default) =>
        base.StageAddAsync(action, cancellationToken);

    public Task<Action?> GetByIdAsync(Guid actionId, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(action => action.Id == actionId, cancellationToken);

    public async Task<IReadOnlyList<Action>> GetMatchActionsAsync(
        Guid matchId,
        CancellationToken cancellationToken = default) =>
        await Set.Where(action => action.MatchId == matchId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Action>> GetPlayerMatchActionsAsync(
        Guid matchId,
        Guid playerId,
        CancellationToken cancellationToken = default) =>
        await Set.Where(action => action.MatchId == matchId && action.PlayerId == playerId)
            .ToListAsync(cancellationToken);

    public Task UpdateAsync(Guid actionId, Action action, CancellationToken cancellationToken = default) =>
        base.StageUpdateAsync(actionId, action, cancellationToken);

    public Task RemoveAsync(Guid actionId, CancellationToken cancellationToken = default) =>
        base.StageRemoveAsync(actionId, cancellationToken);
}
