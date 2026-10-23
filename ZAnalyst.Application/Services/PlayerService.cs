using ZAnalyst.Application.Interfaces;
using ZAnalyst.Application.Exceptions;
using ZAnalyst.Domain.Entities;

namespace ZAnalyst.Application.Services;

public sealed class PlayerService(IPlayerRepository repository, IUnitOfWork unitOfWork) : IPlayerService
{
    private readonly IPlayerRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    public async Task<Player> CreateAsync(Player player, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstInvalidNewEntity(player, nameof(player));
        await _repository.AddAsync(player, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return player;
    }

    public Task<Player?> GetByIdAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstEmptyId(playerId, nameof(playerId));
        return _repository.GetByIdAsync(playerId, cancellationToken);
    }

    public Task UpdateAsync(Guid playerId, Player player, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstMismatchedEntityId(playerId, player, nameof(player));
        return UpdateAndSaveAsync(playerId, player, cancellationToken);
    }

    public async Task RemoveAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstEmptyId(playerId, nameof(playerId));
        if (await _repository.HasRecordedActionsAsync(playerId, cancellationToken))
            throw new PlayerHasRecordedActionsException(playerId);

        await _repository.RemoveAsync(playerId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task UpdateAndSaveAsync(Guid playerId, Player player, CancellationToken cancellationToken)
    {
        await _repository.UpdateAsync(playerId, player, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
