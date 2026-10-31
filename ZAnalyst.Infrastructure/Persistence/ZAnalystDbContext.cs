using System.Drawing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ZAnalyst.Application.Interfaces;
using ZAnalyst.Domain.Entities;
using Action = ZAnalyst.Domain.Entities.Action;

namespace ZAnalyst.Infrastructure.Persistence;

public sealed class ZAnalystDbContext(DbContextOptions<ZAnalystDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Template> Templates => Set<Template>();
    public DbSet<Button> Buttons => Set<Button>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Action> Actions => Set<Action>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(team => team.Id);
            entity.Property(team => team.Name).IsRequired();
            entity.HasMany(team => team.Players)
                .WithOne(player => player.Team)
                .HasForeignKey(player => player.TeamId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(team => team.Matches)
                .WithOne(match => match.Team)
                .HasForeignKey(match => match.TeamId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasKey(player => player.Id);
            entity.Property(player => player.Name).IsRequired();
        });

        modelBuilder.Entity<Match>(entity =>
        {
            entity.HasKey(match => match.Id);
            entity.Property(match => match.Title).IsRequired();
            entity.HasOne(match => match.Template)
                .WithMany(template => template.Matches)
                .HasForeignKey(match => match.TemplateId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Template>(entity =>
        {
            entity.HasKey(template => template.Id);
            entity.Property(template => template.Name).IsRequired();
            entity.HasMany(template => template.Buttons)
                .WithOne()
                .HasForeignKey(button => button.TemplateId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Button>(entity =>
        {
            entity.HasKey(button => button.Id);
            entity.Property(button => button.Color)
                .HasConversion(new ValueConverter<Color, int>(
                    color => color.ToArgb(),
                    argb => Color.FromArgb(argb)));
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(domainEvent => domainEvent.Id);
            entity.HasOne(domainEvent => domainEvent.Match)
                .WithMany()
                .HasForeignKey(domainEvent => domainEvent.MatchId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(domainEvent => domainEvent.Button)
                .WithMany()
                .HasForeignKey(domainEvent => domainEvent.ButtonId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Action>(entity =>
        {
            entity.HasKey(action => action.Id);
            entity.HasOne(action => action.Match)
                .WithMany()
                .HasForeignKey(action => action.MatchId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(action => action.Button)
                .WithMany()
                .HasForeignKey(action => action.ButtonId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(action => action.Player)
                .WithMany(player => player.Actions)
                .HasForeignKey(action => action.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
