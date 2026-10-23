using Microsoft.EntityFrameworkCore;
using ZAnalyst.Domain.Entities;
using ZAnalyst.Infrastructure.Persistence;

namespace ZAnalyst.Infrastructure.Repositories;

internal abstract class EfRepository<TEntity>(ZAnalystDbContext context)
    where TEntity : Entity
{
    protected ZAnalystDbContext Context { get; } =
        context ?? throw new ArgumentNullException(nameof(context));

    protected DbSet<TEntity> Set => Context.Set<TEntity>();

    protected async Task StageAddAsync(TEntity entity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (entity.Id == Guid.Empty)
            throw new ArgumentException("Entity id cannot be empty.", nameof(entity));

        await Set.AddAsync(entity, cancellationToken);
    }

    protected Task<TEntity?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Set.FindAsync([id], cancellationToken).AsTask();

    protected async Task StageUpdateAsync(Guid id, TEntity entity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entity);
        EnsureMatchingId(id, entity.Id);

        var existingEntity = await FindAsync(id, cancellationToken);
        if (existingEntity is null)
            throw new KeyNotFoundException($"{typeof(TEntity).Name} with id '{id}' was not found.");

        Context.Entry(existingEntity).CurrentValues.SetValues(entity);
    }

    protected async Task StageRemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await FindAsync(id, cancellationToken);
        if (entity is null)
            throw new KeyNotFoundException($"{typeof(TEntity).Name} with id '{id}' was not found.");

        Set.Remove(entity);
    }

    protected static void EnsureMatchingId(Guid id, Guid entityId)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (entityId != id)
            throw new ArgumentException("Entity id must match the supplied id.", nameof(entityId));
    }
}
