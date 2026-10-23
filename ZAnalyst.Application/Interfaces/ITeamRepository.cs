using ZAnalyst.Domain.Entities;

namespace ZAnalyst.Application.Interfaces;

public interface ITeamRepository
{
    Task AddAsync(Team team, CancellationToken cancellationToken = default);
    Task<Team?> GetByIdAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<Team?> GetByIdWithPlayersAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<Team?> GetByIdWithMatchesAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid teamId, Team team, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid teamId, CancellationToken cancellationToken = default);
}