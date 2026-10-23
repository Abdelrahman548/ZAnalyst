using ZAnalyst.Domain.Entities;

namespace ZAnalyst.Application.Interfaces;

public interface ITemplateService
{
    Task<Template> CreateAsync(Template template, CancellationToken cancellationToken = default);
    Task<Template?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task<Template?> GetByIdWithButtonsAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid templateId, Template template, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid templateId, CancellationToken cancellationToken = default);
}
