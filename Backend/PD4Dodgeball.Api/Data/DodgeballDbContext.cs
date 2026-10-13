using Microsoft.EntityFrameworkCore;
using PD4Dodgeball.Api.Models;

namespace PD4Dodgeball.Api.Data;

public class DodgeballDbContext : DbContext
{
    public DodgeballDbContext(DbContextOptions<DodgeballDbContext> options)
        : base(options)
    {
    }

    public DbSet<Player> Players => Set<Player>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<MatchPlayer> MatchPlayers => Set<MatchPlayer>();
    public DbSet<PlayerHit> PlayerHits => Set<PlayerHit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MatchPlayer>()
    .Property(mp => mp.CrownsRemaining)
    .HasDefaultValue(1);

        modelBuilder.Entity<Player>()
            .HasIndex(p => p.PlayFabId)
            .IsUnique();

        modelBuilder.Entity<Player>()
            .Property(p => p.PlayFabId)
            .HasMaxLength(64);

        modelBuilder.Entity<Player>()
            .Property(p => p.DisplayName)
            .HasMaxLength(64);

        modelBuilder.Entity<MatchPlayer>()
            .HasIndex(mp => new { mp.MatchId, mp.PlayerId })
            .IsUnique();

        modelBuilder.Entity<PlayerHit>()
            .HasOne(h => h.AttackerPlayer)
            .WithMany()
            .HasForeignKey(h => h.AttackerPlayerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PlayerHit>()
            .HasOne(h => h.VictimPlayer)
            .WithMany()
            .HasForeignKey(h => h.VictimPlayerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}