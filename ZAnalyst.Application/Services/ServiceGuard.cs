using ZAnalyst.Domain.Entities;

namespace ZAnalyst.Application.Services;

internal static class ServiceGuard
{
    public static void AgainstInvalidNewEntity<T>(T entity, string parameterName)
        where T : Entity
    {
        ArgumentNullException.ThrowIfNull(entity, parameterName);
        AgainstEmptyId(entity.Id, parameterName);
    }

    public static void AgainstEmptyId(Guid id, string parameterName)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty.", parameterName);
    }

    public static void AgainstMismatchedEntityId<T>(Guid id, T entity, string parameterName)
        where T : Entity
    {
        ArgumentNullException.ThrowIfNull(entity, parameterName);
        AgainstEmptyId(id, parameterName);

        if (entity.Id != id)
            throw new ArgumentException("Entity id must match the supplied id.", parameterName);
    }
}
