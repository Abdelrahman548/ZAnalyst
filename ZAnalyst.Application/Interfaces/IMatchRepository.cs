using ZAnalyst.Domain.Entities;

namespace ZAnalyst.Application.Interfaces;

public interface IMatchRepository
{
    Task AddAsync(Match match, CancellationToken cancellationToken = default);
    Task<Match?> GetByIdAsync(Guid matchId, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid matchId, Match match, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid matchId, CancellationToken cancellationToken = default);
}
