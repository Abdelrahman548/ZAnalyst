using System.Drawing;
using Xunit;
using ZAnalyst.Application.Interfaces;
using ZAnalyst.Application.Services;
using ZAnalyst.Domain.Entities;
using ZAnalyst.Domain.Enums;

namespace ZAnalyst.Application.Tests;

public sealed class TemplateButtonServiceTests
{
    [Fact]
    public async Task TemplateService_ForwardsOperationsAndSavesWrites()
    {
        var repository = new TemplateRepositoryFake();
        var unitOfWork = new UnitOfWorkFake();
        var service = new TemplateService(repository, unitOfWork);
        var template = new Template("Match");

        Assert.Same(template, await service.CreateAsync(template));
        Assert.Equal(1, unitOfWork.SaveCount);
        Assert.Same(template, repository.LastTemplate);

        Assert.Same(repository.Template, await service.GetByIdAsync(template.Id));
        Assert.Same(repository.Template, await service.GetByIdWithButtonsAsync(template.Id));
        Assert.Same(repository.Templates, await service.GetAllAsync());

        await service.UpdateAsync(template.Id, template);
        Assert.Same(template, repository.LastTemplate);
        Assert.Equal(2, unitOfWork.SaveCount);

        await service.RemoveAsync(template.Id);
        Assert.Equal(template.Id, repository.LastId);
        Assert.Equal(3, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task TemplateService_RejectsInvalidInputsBeforeRepositoryCalls()
    {
        var repository = new TemplateRepositoryFake();
        var service = new TemplateService(repository, new UnitOfWorkFake());

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetByIdAsync(Guid.Empty));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetByIdWithButtonsAsync(Guid.Empty));
        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(Guid.NewGuid(), new Template("Mismatch")));
        await Assert.ThrowsAsync<ArgumentException>(() => service.RemoveAsync(Guid.Empty));
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task ButtonServiceForwardsOperationsSavesWritesAndSupportsArchiving()
    {
        var repository = new ButtonRepositoryFake();
        var unitOfWork = new UnitOfWorkFake();
        var service = new ButtonService(repository, unitOfWork);
        var button = CreateButton();

        Assert.Same(button, await service.CreateAsync(button));
        Assert.Equal(1, unitOfWork.SaveCount);
        Assert.Same(button, repository.LastButton);

        Assert.Same(repository.Button, await service.GetByIdAsync(button.Id));
        Assert.Same(repository.Buttons, await service.GetTemplateButtonsAsync(button.TemplateId));
        Assert.Same(repository.Buttons, await service.GetTemplateButtonsAsync(button.TemplateId, includeArchived: true));
        Assert.True(repository.LastIncludeArchived);

        await service.UpdateAsync(button.Id, button);
        Assert.Same(button, repository.LastButton);
        Assert.Equal(2, unitOfWork.SaveCount);

        await service.ArchiveAsync(button.Id);
        Assert.Equal(button.Id, repository.LastId);
        Assert.Equal(3, unitOfWork.SaveCount);

        await service.RemoveAsync(button.Id);
        Assert.Equal(button.Id, repository.LastId);
        Assert.Equal(4, unitOfWork.SaveCount);

        await service.UnarchiveAsync(button.Id);
        Assert.Equal(button.Id, repository.LastId);
        Assert.Equal(5, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task ButtonService_RejectsInvalidInputsBeforeRepositoryCalls()
    {
        var repository = new ButtonRepositoryFake();
        var service = new ButtonService(repository, new UnitOfWorkFake());

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetByIdAsync(Guid.Empty));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetTemplateButtonsAsync(Guid.Empty));
        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(Guid.NewGuid(), CreateButton()));
        await Assert.ThrowsAsync<ArgumentException>(() => service.RemoveAsync(Guid.Empty));
        await Assert.ThrowsAsync<ArgumentException>(() => service.ArchiveAsync(Guid.Empty));
        await Assert.ThrowsAsync<ArgumentException>(() => service.UnarchiveAsync(Guid.Empty));
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public void TemplateAndButtonValidateNamesAndDoublePressDurations()
    {
        Assert.Throws<ArgumentException>(() => new Template(" "));
        Assert.Throws<ArgumentException>(() => CreateButton(name: " "));

        var button = CreateButton();
        button.ToSinglePressMode(0.5f, 1.5f);

        Assert.Equal(PressMode.Single, button.Mode);
        Assert.Equal(0.5f, button.PreSec);
        Assert.Equal(1.5f, button.PostSec);

        button.ToDoublePressMode();

        Assert.Equal(PressMode.Double, button.Mode);
        Assert.Equal(0, button.PreSec);
        Assert.Equal(0, button.PostSec);
        Assert.Throws<ArgumentOutOfRangeException>(() => button.ToSinglePressMode(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => button.ToSinglePressMode(float.NaN, 0));
    }

    [Fact]
    public void ButtonUpdateConfigurationUpdatesAllEditableSettingsAndValidatesDurations()
    {
        var button = CreateButton();

        button.UpdateConfiguration(
            "  Assist  ",
            Color.Blue,
            ButtonType.Action,
            PressMode.Double,
            "F2",
            "  Note  ",
            0.25f,
            0.75f);

        Assert.Equal("Assist", button.Name);
        Assert.Equal(Color.Blue, button.Color);
        Assert.Equal(ButtonType.Action, button.Type);
        Assert.Equal(PressMode.Double, button.Mode);
        Assert.Equal("F2", button.HotKey);
        Assert.Equal("  Note  ", button.Note);
        Assert.Equal(0, button.PreSec);
        Assert.Equal(0, button.PostSec);

        button.UpdateConfiguration(
            "Assist",
            Color.Blue,
            ButtonType.Action,
            PressMode.Single,
            "F2",
            "Note",
            0.25f,
            0.75f);
        Assert.Equal(0.25f, button.PreSec);
        Assert.Equal(0.75f, button.PostSec);

        Assert.Throws<ArgumentException>(() => button.UpdateConfiguration(
            " ", Color.Red, ButtonType.Event, PressMode.Single, null, null, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => button.UpdateConfiguration(
            "Valid", Color.Red, ButtonType.Event, PressMode.Single, null, null, -1, 0));
    }

    private static Button CreateButton(string name = "Goal") =>
        new(Guid.NewGuid(), name, Color.Red, ButtonType.Event, PressMode.Single);

    private sealed class UnitOfWorkFake : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class TemplateRepositoryFake : ITemplateRepository
    {
        public int CallCount { get; private set; }
        public Guid LastId { get; private set; }
        public Template Template { get; } = new("Template");
        public IReadOnlyList<Template> Templates { get; } = [new Template("Template")];
        public Template? LastTemplate { get; private set; }

        public Task AddAsync(Template template, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastTemplate = template;
            return Task.CompletedTask;
        }

        public Task<Template?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastId = templateId;
            return Task.FromResult<Template?>(Template);
        }

        public Task<IReadOnlyList<Template>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(Templates);
        }

        public Task<Template?> GetByIdWithButtonsAsync(Guid templateId, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastId = templateId;
            return Task.FromResult<Template?>(Template);
        }

        public Task UpdateAsync(Guid templateId, Template template, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastId = templateId;
            LastTemplate = template;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Guid templateId, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastId = templateId;
            return Task.CompletedTask;
        }
    }

    private sealed class ButtonRepositoryFake : IButtonRepository
    {
        public int CallCount { get; private set; }
        public Guid LastId { get; private set; }
        public Button Button { get; } = CreateButton();
        public Button LastButton { get; private set; } = CreateButton();
        public IReadOnlyList<Button> Buttons { get; } = [CreateButton()];
        public bool LastIncludeArchived { get; private set; }

        public Task AddAsync(Button button, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastButton = button;
            return Task.CompletedTask;
        }

        public Task<Button?> GetByIdAsync(Guid buttonId, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastId = buttonId;
            return Task.FromResult<Button?>(Button);
        }

        public Task<IReadOnlyList<Button>> GetTemplateButtonsAsync(
            Guid templateId,
            bool includeArchived = false,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastId = templateId;
            LastIncludeArchived = includeArchived;
            return Task.FromResult(Buttons);
        }

        public Task UpdateAsync(Guid buttonId, Button button, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastId = buttonId;
            LastButton = button;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Guid buttonId, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastId = buttonId;
            return Task.CompletedTask;
        }

        public Task ArchiveAsync(Guid buttonId, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastId = buttonId;
            return Task.CompletedTask;
        }

        public Task UnarchiveAsync(Guid buttonId, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastId = buttonId;
            return Task.CompletedTask;
        }
    }
}
