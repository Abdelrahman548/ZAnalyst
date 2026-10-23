using Microsoft.EntityFrameworkCore;
using ZAnalyst.Application.Interfaces;
using ZAnalyst.Domain.Entities;
using ZAnalyst.Infrastructure.Persistence;

namespace ZAnalyst.Infrastructure.Repositories;

internal sealed class PlayerRepository(ZAnalystDbContext context)
    : EfRepository<Player>(context), IPlayerRepository
{
    public Task AddAsync(Player player, CancellationToken cancellationToken = default) =>
        base.StageAddAsync(player, cancellationToken);

    public Task<Player?> GetByIdAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(player => player.Id == playerId, cancellationToken);

    public Task UpdateAsync(Guid playerId, Player player, CancellationToken cancellationToken = default) =>
        base.StageUpdateAsync(playerId, player, cancellationToken);

    public Task RemoveAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        base.StageRemoveAsync(playerId, cancellationToken);
}
