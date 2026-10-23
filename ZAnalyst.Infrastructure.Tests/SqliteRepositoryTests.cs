using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ZAnalyst.Application.Interfaces;
using ZAnalyst.Infrastructure;
using ZAnalyst.Application.Services;
using ZAnalyst.Domain.Entities;
using ZAnalyst.Domain.Enums;
using ZAnalyst.Infrastructure.Persistence;
using ZAnalyst.Infrastructure.Repositories;

namespace ZAnalyst.Infrastructure.Tests;

public sealed class SqliteRepositoryTests
{
    [Fact]
    public async Task TemplateAndButtonServicesPersistTemplateAndArchiveState()
    {
        await using var connection = await OpenDatabaseAsync();
        var options = CreateOptions(connection);
        Guid templateId;
        Guid buttonId;

        await using (var context = new ZAnalystDbContext(options))
        {
            var templateService = new TemplateService(new TemplateRepository(context), context);
            var buttonService = new ButtonService(new ButtonRepository(context), context);
            var template = await templateService.CreateAsync(new Template("Soccer"));
            var button = await buttonService.CreateAsync(
                new Button(template.Id, "Goal", System.Drawing.Color.Green, ButtonType.Event, PressMode.Single));

            await buttonService.ArchiveAsync(button.Id);
            templateId = template.Id;
            buttonId = button.Id;
        }

        await using var verifyContext = new ZAnalystDbContext(options);
        var templateWithButtons = await verifyContext.Templates
            .Include(template => template.Buttons)
            .SingleAsync(template => template.Id == templateId);
        Assert.Equal("Soccer", templateWithButtons.Name);
        Assert.Contains(templateWithButtons.Buttons, button => button.Id == buttonId && button.Archived);
    }

    [Fact]
    public async Task TeamAndPlayerServicesCreateRecordsInSqlite()
    {
        await using var connection = await OpenDatabaseAsync();
        var options = CreateOptions(connection);
        Guid teamId;
        Guid playerId;

        await using (var context = new ZAnalystDbContext(options))
        {
            var teamService = new TeamService(new TeamRepository(context), context);
            var playerService = new PlayerService(new PlayerRepository(context), context);

            var team = await teamService.CreateAsync(new Team("Home"));
            var player = await playerService.CreateAsync(new Player("Player", 7, team.Id));
            teamId = team.Id;
            playerId = player.Id;
        }

        await using var verifyContext = new ZAnalystDbContext(options);
        Assert.Equal("Home", (await verifyContext.Teams.SingleAsync(team => team.Id == teamId)).Name);
        Assert.Equal("Player", (await verifyContext.Players.SingleAsync(player => player.Id == playerId)).Name);
    }

    [Fact]
    public async Task DependencyInjectionInitializesDatabaseAndResolvesServices()
    {
        var databaseName = $"ZAnalyst-{Guid.NewGuid():N}";
        var connectionString = $"Data Source={databaseName};Mode=Memory;Cache=Shared";
        await using var keepAlive = new SqliteConnection(connectionString);
        await keepAlive.OpenAsync();

        var services = new ServiceCollection()
            .AddZAnalystInfrastructure(connectionString);
        await using var provider = services.BuildServiceProvider();

        await provider.InitializeZAnalystDatabaseAsync();

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ZAnalystDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var playerService = scope.ServiceProvider.GetRequiredService<ZAnalyst.Application.Interfaces.IPlayerService>();

        Assert.Same(context, unitOfWork);
        Assert.NotNull(playerService);
        Assert.True(await context.Database.CanConnectAsync());
        Assert.Contains("20261023094704_InitialCreate", await context.Database.GetAppliedMigrationsAsync());
    }

    [Fact]
    public async Task TeamRepository_LoadsPlayersFromSqlite()
    {
        await using var connection = await OpenDatabaseAsync();
        var options = CreateOptions(connection);
        Guid teamId;
        Guid playerId;

        await using (var seedContext = new ZAnalystDbContext(options))
        {
            var team = new Team("Team");
            var player = new Player("Initial", 10, team.Id);
            seedContext.Teams.Add(team);
            seedContext.Players.Add(player);
            await seedContext.SaveChangesAsync();
            teamId = team.Id;
            playerId = player.Id;
        }

        await using var context = new ZAnalystDbContext(options);
        var repository = new TeamRepository(context);
        var result = await repository.GetByIdWithPlayersAsync(teamId);

        Assert.NotNull(result);
        Assert.Contains(result.Players, player => player.Id == playerId);
    }

    [Fact]
    public async Task PlayerRepository_UpdateIsStagedUntilUnitOfWorkSaves()
    {
        await using var connection = await OpenDatabaseAsync();
        var options = CreateOptions(connection);
        var team = new Team("Team");
        var player = new Player("Before", 10, team.Id);

        await using (var seedContext = new ZAnalystDbContext(options))
        {
            seedContext.Teams.Add(team);
            seedContext.Players.Add(player);
            await seedContext.SaveChangesAsync();
        }

        await using (var updateContext = new ZAnalystDbContext(options))
        {
            var repository = new PlayerRepository(updateContext);
            var update = await repository.GetByIdAsync(player.Id);
            Assert.NotNull(update);
            update.Update("After", 11, team.Id);

            await repository.UpdateAsync(player.Id, update);

            await using var readBeforeSave = new ZAnalystDbContext(options);
            var unchanged = await readBeforeSave.Players.SingleAsync(item => item.Id == player.Id);
            Assert.Equal("Before", unchanged.Name);
            Assert.Equal(10, unchanged.Number);

            await updateContext.SaveChangesAsync();
        }

        await using var verifyContext = new ZAnalystDbContext(options);
        var updated = await verifyContext.Players.SingleAsync(item => item.Id == player.Id);
        Assert.Equal("After", updated.Name);
        Assert.Equal(11, updated.Number);
    }

    [Fact]
    public async Task PlayerServiceWithSqliteRepositoryPersistsUpdate()
    {
        await using var connection = await OpenDatabaseAsync();
        var options = CreateOptions(connection);
        var team = new Team("Team");
        var player = new Player("Before", 10, team.Id);

        await using (var seedContext = new ZAnalystDbContext(options))
        {
            seedContext.Teams.Add(team);
            seedContext.Players.Add(player);
            await seedContext.SaveChangesAsync();
        }

        await using (var serviceContext = new ZAnalystDbContext(options))
        {
            var update = await serviceContext.Players.SingleAsync(item => item.Id == player.Id);
            var service = new PlayerService(
                new PlayerRepository(serviceContext),
                serviceContext);

            update.Update("Saved", 12, team.Id);
            await service.UpdateAsync(update.Id, update);
        }

        await using var verifyContext = new ZAnalystDbContext(options);
        var saved = await verifyContext.Players.SingleAsync(item => item.Id == player.Id);
        Assert.Equal("Saved", saved.Name);
        Assert.Equal(12, saved.Number);
    }

    [Fact]
    public async Task RepositoryUpdateThrowsWhenEntityDoesNotExist()
    {
        await using var connection = await OpenDatabaseAsync();
        var options = CreateOptions(connection);
        await using var context = new ZAnalystDbContext(options);
        var repository = new PlayerRepository(context);
        var player = new Player("Missing", 1, Guid.NewGuid());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => repository.UpdateAsync(player.Id, player));
    }

    private static async Task<SqliteConnection> OpenDatabaseAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using var context = new ZAnalystDbContext(CreateOptions(connection));
        await context.Database.MigrateAsync();
        return connection;
    }

    private static DbContextOptions<ZAnalystDbContext> CreateOptions(SqliteConnection connection) =>
        new DbContextOptionsBuilder<ZAnalystDbContext>()
            .UseSqlite(connection)
            .Options;
}
