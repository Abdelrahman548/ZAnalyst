using Microsoft.EntityFrameworkCore;
using ZAnalyst.Application.Interfaces;
using ZAnalyst.Domain.Entities;
using ZAnalyst.Infrastructure.Persistence;

namespace ZAnalyst.Infrastructure.Repositories;

internal sealed class TeamRepository(ZAnalystDbContext context)
    : EfRepository<Team>(context), ITeamRepository
{
    public Task AddAsync(Team team, CancellationToken cancellationToken = default) =>
        base.StageAddAsync(team, cancellationToken);

    public Task<Team?> GetByIdAsync(Guid teamId, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(team => team.Id == teamId, cancellationToken);

    public Task<Team?> GetByIdWithPlayersAsync(Guid teamId, CancellationToken cancellationToken = default) =>
        Set.Include(team => team.Players)
            .FirstOrDefaultAsync(team => team.Id == teamId, cancellationToken);

    public Task<Team?> GetByIdWithMatchesAsync(Guid teamId, CancellationToken cancellationToken = default) =>
        Set.Include(team => team.Matches)
            .FirstOrDefaultAsync(team => team.Id == teamId, cancellationToken);

    public Task UpdateAsync(Guid teamId, Team team, CancellationToken cancellationToken = default) =>
        base.StageUpdateAsync(teamId, team, cancellationToken);

    public Task RemoveAsync(Guid teamId, CancellationToken cancellationToken = default) =>
        base.StageRemoveAsync(teamId, cancellationToken);
}
