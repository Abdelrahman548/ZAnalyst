using ZAnalyst.Domain.Entities;

namespace ZAnalyst.Application.Interfaces;

public interface ITemplateRepository
{
    Task AddAsync(Template template, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Template>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Template?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task<Template?> GetByIdWithButtonsAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid templateId, Template template, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid templateId, CancellationToken cancellationToken = default);
}
