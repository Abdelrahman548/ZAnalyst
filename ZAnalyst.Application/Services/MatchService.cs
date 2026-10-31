using ZAnalyst.Application.Interfaces;
using ZAnalyst.Domain.Entities;

namespace ZAnalyst.Application.Services;

public sealed class MatchService(IMatchRepository repository, IUnitOfWork unitOfWork) : IMatchService
{
    private readonly IMatchRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    public async Task<Match> CreateAsync(Match match, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstInvalidNewEntity(match, nameof(match));
        await _repository.AddAsync(match, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return match;
    }

    public Task<IReadOnlyList<Match>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);

    public Task<Match?> GetByIdAsync(Guid matchId, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstEmptyId(matchId, nameof(matchId));
        return _repository.GetByIdAsync(matchId, cancellationToken);
    }

    public Task UpdateAsync(Guid matchId, Match match, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstMismatchedEntityId(matchId, match, nameof(match));
        return UpdateAndSaveAsync(matchId, match, cancellationToken);
    }

    public async Task RemoveAsync(Guid matchId, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstEmptyId(matchId, nameof(matchId));
        await _repository.RemoveAsync(matchId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task UpdateAndSaveAsync(Guid matchId, Match match, CancellationToken cancellationToken)
    {
        await _repository.UpdateAsync(matchId, match, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
