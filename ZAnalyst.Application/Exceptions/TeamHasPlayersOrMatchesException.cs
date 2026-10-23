namespace ZAnalyst.Application.Exceptions;

public sealed class TeamHasPlayersOrMatchesException(Guid teamId)
    : InvalidOperationException("This team cannot be deleted while it has players or matches.")
{
    public Guid TeamId { get; } = teamId;
}
