using ZAnalyst.Domain.Entities;

namespace ZAnalyst.Application.Interfaces;

public interface IPlayerRepository
{
    Task AddAsync(Player player, CancellationToken cancellationToken = default);
    Task<Player?> GetByIdAsync(Guid playerId, CancellationToken cancellationToken = default);
    Task<bool> HasRecordedActionsAsync(Guid playerId, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid playerId, Player player, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid playerId, CancellationToken cancellationToken = default);
}
