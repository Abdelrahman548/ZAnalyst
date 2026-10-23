using ZAnalyst.Application.Interfaces;
using ZAnalyst.Domain.Entities;

namespace ZAnalyst.Application.Services;

public sealed class ButtonService(IButtonRepository repository, IUnitOfWork unitOfWork) : IButtonService
{
    private readonly IButtonRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    public async Task<Button> CreateAsync(Button button, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstInvalidNewEntity(button, nameof(button));
        await _repository.AddAsync(button, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return button;
    }

    public Task<Button?> GetByIdAsync(Guid buttonId, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstEmptyId(buttonId, nameof(buttonId));
        return _repository.GetByIdAsync(buttonId, cancellationToken);
    }

    public Task<IReadOnlyList<Button>> GetTemplateButtonsAsync(
        Guid templateId,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstEmptyId(templateId, nameof(templateId));
        return _repository.GetTemplateButtonsAsync(templateId, includeArchived, cancellationToken);
    }

    public Task UpdateAsync(Guid buttonId, Button button, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstMismatchedEntityId(buttonId, button, nameof(button));
        return UpdateAndSaveAsync(buttonId, button, cancellationToken);
    }

    public async Task ArchiveAsync(Guid buttonId, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstEmptyId(buttonId, nameof(buttonId));
        await _repository.ArchiveAsync(buttonId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UnarchiveAsync(Guid buttonId, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstEmptyId(buttonId, nameof(buttonId));
        await _repository.UnarchiveAsync(buttonId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task UpdateAndSaveAsync(Guid buttonId, Button button, CancellationToken cancellationToken)
    {
        await _repository.UpdateAsync(buttonId, button, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
