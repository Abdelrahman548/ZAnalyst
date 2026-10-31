using Microsoft.EntityFrameworkCore;
using ZAnalyst.Application.Exceptions;
using ZAnalyst.Application.Interfaces;
using ZAnalyst.Domain.Entities;
using ZAnalyst.Infrastructure.Persistence;

namespace ZAnalyst.Infrastructure.Repositories;

internal sealed class TemplateRepository(ZAnalystDbContext context)
    : EfRepository<Template>(context), ITemplateRepository
{
    public Task AddAsync(Template template, CancellationToken cancellationToken = default) =>
        base.StageAddAsync(template, cancellationToken);

    public async Task<IReadOnlyList<Template>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await Set.OrderBy(template => template.Name).ToListAsync(cancellationToken);

    public Task<Template?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(template => template.Id == templateId, cancellationToken);

    public Task<Template?> GetByIdWithButtonsAsync(Guid templateId, CancellationToken cancellationToken = default) =>
        Set.Include(template => template.Buttons)
            .FirstOrDefaultAsync(template => template.Id == templateId, cancellationToken);

    public Task UpdateAsync(Guid templateId, Template template, CancellationToken cancellationToken = default) =>
        base.StageUpdateAsync(templateId, template, cancellationToken);

    public async Task RemoveAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var template = await Set
            .Include(item => item.Buttons)
            .FirstOrDefaultAsync(item => item.Id == templateId, cancellationToken)
            ?? throw new KeyNotFoundException($"Template with id '{templateId}' was not found.");

        var buttonIds = template.Buttons.Select(button => button.Id).ToArray();
        if (await Context.Matches.AnyAsync(match => match.TemplateId == templateId, cancellationToken)
            || await Context.Events.AnyAsync(domainEvent => buttonIds.Contains(domainEvent.ButtonId), cancellationToken)
            || await Context.Actions.AnyAsync(action => buttonIds.Contains(action.ButtonId), cancellationToken))
        {
            throw new TemplateInUseException(templateId);
        }

        Context.Buttons.RemoveRange(template.Buttons);
        Context.Templates.Remove(template);
    }
}
