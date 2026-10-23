using ZAnalyst.Domain.Entities;

namespace ZAnalyst.Application.Interfaces;

public interface IButtonRepository
{
    Task AddAsync(Button button, CancellationToken cancellationToken = default);
    Task<Button?> GetByIdAsync(Guid buttonId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Button>> GetTemplateButtonsAsync(
        Guid templateId,
        bool includeArchived = false,
        CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid buttonId, Button button, CancellationToken cancellationToken = default);
    Task ArchiveAsync(Guid buttonId, CancellationToken cancellationToken = default);
    Task UnarchiveAsync(Guid buttonId, CancellationToken cancellationToken = default);
}
