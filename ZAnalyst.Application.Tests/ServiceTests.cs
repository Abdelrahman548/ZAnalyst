using ZAnalyst.Application.Interfaces;
using ZAnalyst.Application.Services;
using ZAnalyst.Domain.Entities;
using ZAnalyst.Domain.Enums;
using Xunit;
using Action = ZAnalyst.Domain.Entities.Action;

namespace ZAnalyst.Application.Tests;

public sealed class ServiceTests
{
    private static readonly CancellationToken TestToken = new CancellationTokenSource().Token;

    [Fact]
    public async Task ActionService_ForwardsAllOperationsAndCancellationTokens()
    {
        var repository = new ActionRepositoryFake();
        var unitOfWork = new UnitOfWorkFake();
        var service = new ActionService(repository, unitOfWork);
        var action = CreateAction();
        var matchId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        Assert.Same(action, await service.CreateAsync(action, TestToken));
        Assert.Same(action, repository.LastEntity);
        Assert.Equal(TestToken, repository.LastToken);
        Assert.Equal(1, unitOfWork.SaveCount);

        Assert.Same(repository.Action, await service.GetByIdAsync(action.Id, TestToken));
        Assert.Equal(action.Id, repository.LastId);
        Assert.Equal(TestToken, repository.LastToken);

        Assert.Same(repository.Actions, await service.GetMatchActionsAsync(matchId, TestToken));
        Assert.Equal(matchId, repository.LastId);
        Assert.Equal(TestToken, repository.LastToken);

        Assert.Same(repository.Actions, await service.GetPlayerMatchActionsAsync(matchId, playerId, TestToken));
        Assert.Equal(matchId, repository.LastId);
        Assert.Equal(playerId, repository.LastRelatedId);
        Assert.Equal(TestToken, repository.LastToken);

        await service.UpdateAsync(action.Id, action, TestToken);
        Assert.Equal(action.Id, repository.LastId);
        Assert.Same(action, repository.LastEntity);
        Assert.Equal(TestToken, repository.LastToken);
        Assert.Equal(2, unitOfWork.SaveCount);
        Assert.Equal(TestToken, unitOfWork.LastToken);

        await service.RemoveAsync(action.Id, TestToken);
        Assert.Equal(action.Id, repository.LastId);
        Assert.Equal(TestToken, repository.LastToken);
        Assert.Equal(3, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task ActionService_RejectsInvalidIdsAndMismatchedEntities()
    {
        var repository = new ActionRepositoryFake();
        var service = new ActionService(repository, new UnitOfWorkFake());

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetByIdAsync(Guid.Empty));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetMatchActionsAsync(Guid.Empty));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetPlayerMatchActionsAsync(Guid.Empty, Guid.NewGuid()));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetPlayerMatchActionsAsync(Guid.NewGuid(), Guid.Empty));
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => service.RemoveAsync(Guid.Empty));
        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(Guid.NewGuid(), CreateAction()));

        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task EventService_ForwardsAllOperationsAndCancellationTokens()
    {
        var repository = new EventRepositoryFake();
        var unitOfWork = new UnitOfWorkFake();
        var service = new EventService(repository, unitOfWork);
        var domainEvent = CreateEvent();
        var matchId = Guid.NewGuid();

        Assert.Same(domainEvent, await service.CreateAsync(domainEvent, TestToken));
        Assert.Same(domainEvent, repository.LastEntity);
        Assert.Equal(TestToken, repository.LastToken);
        Assert.Equal(1, unitOfWork.SaveCount);

        Assert.Same(repository.DomainEvent, await service.GetByIdAsync(domainEvent.Id, TestToken));
        Assert.Equal(domainEvent.Id, repository.LastId);
        Assert.Equal(TestToken, repository.LastToken);

        Assert.Same(repository.Events, await service.GetMatchEventsAsync(matchId, TestToken));
        Assert.Equal(matchId, repository.LastId);
        Assert.Equal(TestToken, repository.LastToken);

        await service.UpdateAsync(domainEvent.Id, domainEvent, TestToken);
        Assert.Equal(domainEvent.Id, repository.LastId);
        Assert.Same(domainEvent, repository.LastEntity);
        Assert.Equal(TestToken, repository.LastToken);
        Assert.Equal(2, unitOfWork.SaveCount);

        await service.RemoveAsync(domainEvent.Id, TestToken);
        Assert.Equal(domainEvent.Id, repository.LastId);
        Assert.Equal(TestToken, repository.LastToken);
        Assert.Equal(3, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task EventService_RejectsInvalidIdsAndMismatchedEntities()
    {
        var repository = new EventRepositoryFake();
        var service = new EventService(repository, new UnitOfWorkFake());

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetByIdAsync(Guid.Empty));
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetMatchEventsAsync(Guid.Empty));
        await Assert.ThrowsAsync<ArgumentException>(() => service.RemoveAsync(Guid.Empty));
        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(Guid.NewGuid(), CreateEvent()));

        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task MatchService_ForwardsAllOperationsAndCancellationTokens()
    {
        var repository = new MatchRepositoryFake();
        var unitOfWork = new UnitOfWorkFake();
        var service = new MatchService(repository, unitOfWork);
        var match = CreateMatch();

        Assert.Same(match, await service.CreateAsync(match, TestToken));
        Assert.Same(match, repository.LastEntity);
        Assert.Equal(TestToken, repository.LastToken);
        Assert.Equal(1, unitOfWork.SaveCount);

        Assert.Same(repository.Match, await service.GetByIdAsync(match.Id, TestToken));
        Assert.Equal(match.Id, repository.LastId);
        Assert.Equal(TestToken, repository.LastToken);

        await service.UpdateAsync(match.Id, match, TestToken);
        Assert.Equal(match.Id, repository.LastId);
        Assert.Same(match, repository.LastEntity);
        Assert.Equal(TestToken, repository.LastToken);
        Assert.Equal(2, unitOfWork.SaveCount);

        await service.RemoveAsync(match.Id, TestToken);
        Assert.Equal(match.Id, repository.LastId);
        Assert.Equal(TestToken, repository.LastToken);
        Assert.Equal(3, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task MatchService_RejectsInvalidIdsAndMismatchedEntities()
    {
        var repository = new MatchRepositoryFake();
        var service = new MatchService(repository, new UnitOfWorkFake());

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetByIdAsync(Guid.Empty));
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => service.RemoveAsync(Guid.Empty));
        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(Guid.NewGuid(), CreateMatch()));

        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task PlayerService_ForwardsAllOperationsAndCancellationTokens()
    {
        var repository = new PlayerRepositoryFake();
        var unitOfWork = new UnitOfWorkFake();
        var service = new PlayerService(repository, unitOfWork);
        var player = CreatePlayer();

        Assert.Same(player, await service.CreateAsync(player, TestToken));
        Assert.Same(player, repository.LastEntity);
        Assert.Equal(TestToken, repository.LastToken);
        Assert.Equal(1, unitOfWork.SaveCount);

        Assert.Same(repository.Player, await service.GetByIdAsync(player.Id, TestToken));
        Assert.Equal(player.Id, repository.LastId);
        Assert.Equal(TestToken, repository.LastToken);

        await service.UpdateAsync(player.Id, player, TestToken);
        Assert.Equal(player.Id, repository.LastId);
        Assert.Same(player, repository.LastEntity);
        Assert.Equal(TestToken, repository.LastToken);
        Assert.Equal(2, unitOfWork.SaveCount);

        await service.RemoveAsync(player.Id, TestToken);
        Assert.Equal(player.Id, repository.LastId);
        Assert.Equal(TestToken, repository.LastToken);
        Assert.Equal(3, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task PlayerService_RejectsInvalidIdsAndMismatchedEntities()
    {
        var repository = new PlayerRepositoryFake();
        var service = new PlayerService(repository, new UnitOfWorkFake());

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetByIdAsync(Guid.Empty));
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => service.RemoveAsync(Guid.Empty));
        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(Guid.NewGuid(), CreatePlayer()));

        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task TeamService_ForwardsAllOperationsAndCancellationTokens()
    {
        var repository = new TeamRepositoryFake();
        var unitOfWork = new UnitOfWorkFake();
        var service = new TeamService(repository, unitOfWork);
        var team = new Team("Team");

        Assert.Same(team, await service.CreateAsync(team, TestToken));
        Assert.Same(team, repository.LastEntity);
        Assert.Equal(TestToken, repository.LastToken);
        Assert.Equal(1, unitOfWork.SaveCount);

        Assert.Same(repository.Team, await service.GetByIdAsync(team.Id, TestToken));
        Assert.Equal(team.Id, repository.LastId);
        Assert.Equal(TestToken, repository.LastToken);

        Assert.Same(repository.Team, await service.GetByIdWithPlayersAsync(team.Id, TestToken));
        Assert.Equal(team.Id, repository.LastId);
        Assert.Equal(TestToken, repository.LastToken);

        Assert.Same(repository.Team, await service.GetByIdWithMatchesAsync(team.Id, TestToken));
        Assert.Equal(team.Id, repository.LastId);
        Assert.Equal(TestToken, repository.LastToken);

        await service.UpdateAsync(team.Id, team, TestToken);
        Assert.Equal(team.Id, repository.LastId);
        Assert.Same(team, repository.LastEntity);
        Assert.Equal(TestToken, repository.LastToken);
        Assert.Equal(2, unitOfWork.SaveCount);

        await service.RemoveAsync(team.Id, TestToken);
        Assert.Equal(team.Id, repository.LastId);
        Assert.Equal(TestToken, repository.LastToken);
        Assert.Equal(3, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task TeamService_RejectsInvalidIdsAndMismatchedEntities()
    {
        var repository = new TeamRepositoryFake();
        var service = new TeamService(repository, new UnitOfWorkFake());

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetByIdAsync(Guid.Empty));
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetByIdWithPlayersAsync(Guid.Empty));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetByIdWithMatchesAsync(Guid.Empty));
        await Assert.ThrowsAsync<ArgumentException>(() => service.RemoveAsync(Guid.Empty));
        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(Guid.NewGuid(), new Team("Team")));

        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public void DomainEntities_ValidateRequiredFieldsAndKeepNotes()
    {
        Assert.Throws<ArgumentException>(() => new Team(" "));
        Assert.Throws<ArgumentException>(() => new Match(Guid.Empty, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => new Player(" ", 1, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => new Player("Player", 1, Guid.Empty));
        Assert.Throws<ArgumentException>(() =>
            new Action(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, ActionRate.Good, DateTime.UtcNow, DateTime.UtcNow, null));

        var note = "review";
        var action = new Action(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ActionRate.Good,
            DateTime.UtcNow, DateTime.UtcNow, note);
        var domainEvent = new Event(
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow, note);

        Assert.Equal(note, action.Note);
        Assert.Equal(note, domainEvent.Note);
    }

    private static Action CreateAction() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ActionRate.Good, DateTime.UtcNow, DateTime.UtcNow, null);

    private static Event CreateEvent() =>
        new(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow, null);

    private static Match CreateMatch() => new(Guid.NewGuid(), Guid.NewGuid());

    private static Player CreatePlayer() => new("Player", 1, Guid.NewGuid());

    private abstract class RepositoryFake
    {
        public int CallCount { get; protected set; }
        public Guid LastId { get; protected set; }
        public Guid LastRelatedId { get; protected set; }
        public Entity? LastEntity { get; protected set; }
        public CancellationToken LastToken { get; protected set; }
    }

    private sealed class UnitOfWorkFake : IUnitOfWork
    {
        public int SaveCount { get; private set; }
        public CancellationToken LastToken { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            LastToken = cancellationToken;
            return Task.FromResult(1);
        }
    }

    private sealed class ActionRepositoryFake : RepositoryFake, IActionRepository
    {
        public Action Action { get; } = CreateAction();
        public IReadOnlyList<Action> Actions { get; } = [CreateAction()];

        public Task AddAsync(Action action, CancellationToken cancellationToken = default)
        {
            Record(action.Id, cancellationToken);
            LastEntity = action;
            return Task.CompletedTask;
        }

        public Task<Action?> GetByIdAsync(Guid actionId, CancellationToken cancellationToken = default)
        {
            Record(actionId, cancellationToken);
            return Task.FromResult<Action?>(Action);
        }

        public Task<IReadOnlyList<Action>> GetMatchActionsAsync(Guid matchId, CancellationToken cancellationToken = default)
        {
            Record(matchId, cancellationToken);
            return Task.FromResult(Actions);
        }

        public Task<IReadOnlyList<Action>> GetPlayerMatchActionsAsync(Guid matchId, Guid playerId, CancellationToken cancellationToken = default)
        {
            Record(matchId, cancellationToken);
            LastRelatedId = playerId;
            return Task.FromResult(Actions);
        }

        public Task UpdateAsync(Guid actionId, Action action, CancellationToken cancellationToken = default)
        {
            Record(actionId, cancellationToken);
            LastEntity = action;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Guid actionId, CancellationToken cancellationToken = default)
        {
            Record(actionId, cancellationToken);
            return Task.CompletedTask;
        }

        private void Record(Guid id, CancellationToken token)
        {
            CallCount++;
            LastId = id;
            LastToken = token;
        }
    }

    private sealed class EventRepositoryFake : RepositoryFake, IEventRepository
    {
        public Event DomainEvent { get; } = CreateEvent();
        public IReadOnlyList<Event> Events { get; } = [CreateEvent()];

        public Task AddAsync(Event domainEvent, CancellationToken cancellationToken = default)
        {
            Record(domainEvent.Id, cancellationToken);
            LastEntity = domainEvent;
            return Task.CompletedTask;
        }

        public Task<Event?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken = default)
        {
            Record(eventId, cancellationToken);
            return Task.FromResult<Event?>(DomainEvent);
        }

        public Task<IReadOnlyList<Event>> GetMatchEventsAsync(Guid matchId, CancellationToken cancellationToken = default)
        {
            Record(matchId, cancellationToken);
            return Task.FromResult(Events);
        }

        public Task UpdateAsync(Guid eventId, Event domainEvent, CancellationToken cancellationToken = default)
        {
            Record(eventId, cancellationToken);
            LastEntity = domainEvent;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Guid eventId, CancellationToken cancellationToken = default)
        {
            Record(eventId, cancellationToken);
            return Task.CompletedTask;
        }

        private void Record(Guid id, CancellationToken token)
        {
            CallCount++;
            LastId = id;
            LastToken = token;
        }
    }

    private sealed class MatchRepositoryFake : RepositoryFake, IMatchRepository
    {
        public Match Match { get; } = CreateMatch();

        public Task AddAsync(Match match, CancellationToken cancellationToken = default)
        {
            Record(match.Id, cancellationToken);
            LastEntity = match;
            return Task.CompletedTask;
        }

        public Task<Match?> GetByIdAsync(Guid matchId, CancellationToken cancellationToken = default)
        {
            Record(matchId, cancellationToken);
            return Task.FromResult<Match?>(Match);
        }

        public Task UpdateAsync(Guid matchId, Match match, CancellationToken cancellationToken = default)
        {
            Record(matchId, cancellationToken);
            LastEntity = match;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Guid matchId, CancellationToken cancellationToken = default)
        {
            Record(matchId, cancellationToken);
            return Task.CompletedTask;
        }

        private void Record(Guid id, CancellationToken token)
        {
            CallCount++;
            LastId = id;
            LastToken = token;
        }
    }

    private sealed class PlayerRepositoryFake : RepositoryFake, IPlayerRepository
    {
        public Player Player { get; } = CreatePlayer();

        public Task AddAsync(Player player, CancellationToken cancellationToken = default)
        {
            Record(player.Id, cancellationToken);
            LastEntity = player;
            return Task.CompletedTask;
        }

        public Task<Player?> GetByIdAsync(Guid playerId, CancellationToken cancellationToken = default)
        {
            Record(playerId, cancellationToken);
            return Task.FromResult<Player?>(Player);
        }

        public Task UpdateAsync(Guid playerId, Player player, CancellationToken cancellationToken = default)
        {
            Record(playerId, cancellationToken);
            LastEntity = player;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Guid playerId, CancellationToken cancellationToken = default)
        {
            Record(playerId, cancellationToken);
            return Task.CompletedTask;
        }

        private void Record(Guid id, CancellationToken token)
        {
            CallCount++;
            LastId = id;
            LastToken = token;
        }
    }

    private sealed class TeamRepositoryFake : RepositoryFake, ITeamRepository
    {
        public Team Team { get; } = new("Team");

        public Task AddAsync(Team team, CancellationToken cancellationToken = default)
        {
            Record(team.Id, cancellationToken);
            LastEntity = team;
            return Task.CompletedTask;
        }

        public Task<Team?> GetByIdAsync(Guid teamId, CancellationToken cancellationToken = default)
        {
            Record(teamId, cancellationToken);
            return Task.FromResult<Team?>(Team);
        }

        public Task<Team?> GetByIdWithPlayersAsync(Guid teamId, CancellationToken cancellationToken = default)
        {
            Record(teamId, cancellationToken);
            return Task.FromResult<Team?>(Team);
        }

        public Task<Team?> GetByIdWithMatchesAsync(Guid teamId, CancellationToken cancellationToken = default)
        {
            Record(teamId, cancellationToken);
            return Task.FromResult<Team?>(Team);
        }

        public Task UpdateAsync(Guid teamId, Team team, CancellationToken cancellationToken = default)
        {
            Record(teamId, cancellationToken);
            LastEntity = team;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Guid teamId, CancellationToken cancellationToken = default)
        {
            Record(teamId, cancellationToken);
            return Task.CompletedTask;
        }

        private void Record(Guid id, CancellationToken token)
        {
            CallCount++;
            LastId = id;
            LastToken = token;
        }
    }
}
