namespace ZAnalyst.Application.Exceptions;

public sealed class TemplateInUseException(Guid templateId)
    : InvalidOperationException("This template cannot be deleted while it is used by a match or recorded button data.")
{
    public Guid TemplateId { get; } = templateId;
}
