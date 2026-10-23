namespace ZAnalyst.Application.Exceptions;

public sealed class PlayerHasRecordedActionsException(Guid playerId)
    : InvalidOperationException("This player cannot be deleted because recorded actions refer to them.")
{
    public Guid PlayerId { get; } = playerId;
}
