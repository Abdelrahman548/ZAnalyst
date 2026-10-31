using Microsoft.EntityFrameworkCore;
using ZAnalyst.Application.Exceptions;
using ZAnalyst.Application.Interfaces;
using ZAnalyst.Domain.Entities;
using ZAnalyst.Infrastructure.Persistence;

namespace ZAnalyst.Infrastructure.Repositories;

internal sealed class ButtonRepository(ZAnalystDbContext context)
    : EfRepository<Button>(context), IButtonRepository
{
    public Task AddAsync(Button button, CancellationToken cancellationToken = default) =>
        base.StageAddAsync(button, cancellationToken);

    public Task<Button?> GetByIdAsync(Guid buttonId, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(button => button.Id == buttonId, cancellationToken);

    public async Task<IReadOnlyList<Button>> GetTemplateButtonsAsync(
        Guid templateId,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var query = Set.Where(button => button.TemplateId == templateId);
        if (!includeArchived)
            query = query.Where(button => !button.Archived);

        return await query.ToListAsync(cancellationToken);
    }

    public Task UpdateAsync(Guid buttonId, Button button, CancellationToken cancellationToken = default) =>
        base.StageUpdateAsync(buttonId, button, cancellationToken);

    public async Task RemoveAsync(Guid buttonId, CancellationToken cancellationToken = default)
    {
        if (await Context.Events.AnyAsync(domainEvent => domainEvent.ButtonId == buttonId, cancellationToken)
            || await Context.Actions.AnyAsync(action => action.ButtonId == buttonId, cancellationToken))
        {
            throw new ButtonInUseException(buttonId);
        }

        await base.StageRemoveAsync(buttonId, cancellationToken);
    }

    public Task ArchiveAsync(Guid buttonId, CancellationToken cancellationToken = default) =>
        UpdateArchiveStateAsync(buttonId, archive: true, cancellationToken);

    public Task UnarchiveAsync(Guid buttonId, CancellationToken cancellationToken = default) =>
        UpdateArchiveStateAsync(buttonId, archive: false, cancellationToken);

    private async Task UpdateArchiveStateAsync(Guid buttonId, bool archive, CancellationToken cancellationToken)
    {
        var button = await FindAsync(buttonId, cancellationToken);
        if (button is null)
            throw new KeyNotFoundException($"Button with id '{buttonId}' was not found.");

        if (archive)
            button.Archive();
        else
            button.UnArchive();
    }
}
