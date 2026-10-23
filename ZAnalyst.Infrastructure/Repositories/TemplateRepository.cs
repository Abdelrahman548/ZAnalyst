using Microsoft.EntityFrameworkCore;
using ZAnalyst.Application.Interfaces;
using ZAnalyst.Domain.Entities;
using ZAnalyst.Infrastructure.Persistence;

namespace ZAnalyst.Infrastructure.Repositories;

internal sealed class TemplateRepository(ZAnalystDbContext context)
    : EfRepository<Template>(context), ITemplateRepository
{
    public Task AddAsync(Template template, CancellationToken cancellationToken = default) =>
        base.StageAddAsync(template, cancellationToken);

    public Task<Template?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(template => template.Id == templateId, cancellationToken);

    public Task<Template?> GetByIdWithButtonsAsync(Guid templateId, CancellationToken cancellationToken = default) =>
        Set.Include(template => template.Buttons)
            .FirstOrDefaultAsync(template => template.Id == templateId, cancellationToken);

    public Task UpdateAsync(Guid templateId, Template template, CancellationToken cancellationToken = default) =>
        base.StageUpdateAsync(templateId, template, cancellationToken);

    public Task RemoveAsync(Guid templateId, CancellationToken cancellationToken = default) =>
        base.StageRemoveAsync(templateId, cancellationToken);
}
