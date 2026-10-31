namespace ZAnalyst.Application.Exceptions;

public sealed class ButtonInUseException(Guid buttonId)
    : InvalidOperationException("This button cannot be deleted while it is used by recorded events or actions.")
{
    public Guid ButtonId { get; } = buttonId;
}
