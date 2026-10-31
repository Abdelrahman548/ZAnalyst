using ZAnalyst.Application.Interfaces;
using ZAnalyst.Domain.Entities;

namespace ZAnalyst.Application.Services;

public sealed class TemplateService(ITemplateRepository repository, IUnitOfWork unitOfWork) : ITemplateService
{
    private readonly ITemplateRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    public async Task<Template> CreateAsync(Template template, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstInvalidNewEntity(template, nameof(template));
        await _repository.AddAsync(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return template;
    }

    public Task<IReadOnlyList<Template>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);

    public Task<Template?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstEmptyId(templateId, nameof(templateId));
        return _repository.GetByIdAsync(templateId, cancellationToken);
    }

    public Task<Template?> GetByIdWithButtonsAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstEmptyId(templateId, nameof(templateId));
        return _repository.GetByIdWithButtonsAsync(templateId, cancellationToken);
    }

    public Task UpdateAsync(Guid templateId, Template template, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstMismatchedEntityId(templateId, template, nameof(template));
        return UpdateAndSaveAsync(templateId, template, cancellationToken);
    }

    public async Task RemoveAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        ServiceGuard.AgainstEmptyId(templateId, nameof(templateId));
        await _repository.RemoveAsync(templateId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task UpdateAndSaveAsync(Guid templateId, Template template, CancellationToken cancellationToken)
    {
        await _repository.UpdateAsync(templateId, template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
