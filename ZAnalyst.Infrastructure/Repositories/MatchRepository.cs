using Microsoft.EntityFrameworkCore;
using ZAnalyst.Application.Interfaces;
using ZAnalyst.Domain.Entities;
using ZAnalyst.Infrastructure.Persistence;

namespace ZAnalyst.Infrastructure.Repositories;

internal sealed class MatchRepository(ZAnalystDbContext context)
    : EfRepository<Match>(context), IMatchRepository
{
    public Task AddAsync(Match match, CancellationToken cancellationToken = default) =>
        base.StageAddAsync(match, cancellationToken);

    public async Task<IReadOnlyList<Match>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await Set
            .Include(match => match.Team)
            .Include(match => match.Template)
            .OrderByDescending(match => match.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<Match?> GetByIdAsync(Guid matchId, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(match => match.Id == matchId, cancellationToken);

    public Task UpdateAsync(Guid matchId, Match match, CancellationToken cancellationToken = default) =>
        base.StageUpdateAsync(matchId, match, cancellationToken);

    public async Task RemoveAsync(Guid matchId, CancellationToken cancellationToken = default)
    {
        var events = await Context.Events
            .Where(domainEvent => domainEvent.MatchId == matchId)
            .ToListAsync(cancellationToken);
        var actions = await Context.Actions
            .Where(action => action.MatchId == matchId)
            .ToListAsync(cancellationToken);

        Context.Events.RemoveRange(events);
        Context.Actions.RemoveRange(actions);
        await base.StageRemoveAsync(matchId, cancellationToken);
    }
}
