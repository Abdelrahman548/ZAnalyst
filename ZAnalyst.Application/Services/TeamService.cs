using ZAnalyst.Application.Interfaces;
using ZAnalyst.Domain.Entities;

namespace ZAnalyst.Application.Services;

public sealed class TeamService(ITeamRepository repository, IUnitOfWork unitOfWork) : ITeamService
{
    private readonly ITeamRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    public async Task<Team> CreateAsync(Team team, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstInvalidNewEntity(team, nameof(team));
        await _repository.AddAsync(team, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return team;
    }

    public Task<Team?> GetByIdAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstEmptyId(teamId, nameof(teamId));
        return _repository.GetByIdAsync(teamId, cancellationToken);
    }

    public Task<Team?> GetByIdWithPlayersAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstEmptyId(teamId, nameof(teamId));
        return _repository.GetByIdWithPlayersAsync(teamId, cancellationToken);
    }

    public Task<Team?> GetByIdWithMatchesAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstEmptyId(teamId, nameof(teamId));
        return _repository.GetByIdWithMatchesAsync(teamId, cancellationToken);
    }

    public Task UpdateAsync(Guid teamId, Team team, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstMismatchedEntityId(teamId, team, nameof(team));
        return UpdateAndSaveAsync(teamId, team, cancellationToken);
    }

    public async Task RemoveAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstEmptyId(teamId, nameof(teamId));
        await _repository.RemoveAsync(teamId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task UpdateAndSaveAsync(Guid teamId, Team team, CancellationToken cancellationToken)
    {
        await _repository.UpdateAsync(teamId, team, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
