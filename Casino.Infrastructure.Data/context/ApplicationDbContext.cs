using Casino.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Casino.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

   
    public DbSet<User> Users { get; set; }
    public DbSet<Game> Games { get; set; }
    public DbSet<Bet> Bets { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<RouletteRound> RouletteRounds { get; set; }
    public DbSet<RouletteConfiguration> RouletteConfigurations { get; set; }
    public DbSet<AdminAuditLog> AdminAuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Configuración de User ---
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique(); 

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Dni)
            .IsUnique(); // DNI único

        modelBuilder.Entity<User>()
            .Property(u => u.Saldo)
            .HasColumnType("decimal(18,2)"); 

        // --- Configuración de Game ---
        modelBuilder.Entity<Game>()
            .Property(g => g.ApuestaMinima)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Game>()
            .Property(g => g.ApuestaMaxima)
            .HasColumnType("decimal(18,2)");

        // --- Configuración de Bet ---
        modelBuilder.Entity<Bet>()
            .Property(b => b.MontoApostado)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Bet>()
            .Property(b => b.MontoGanado)
            .HasColumnType("decimal(18,2)");

    
        modelBuilder.Entity<Bet>()
            .Property(b => b.DetallesJson)
            .HasColumnType("jsonb");

        modelBuilder.Entity<RouletteRound>()
            .HasIndex(r => new { r.State, r.BettingClosesAtUtc });

        modelBuilder.Entity<RouletteRound>()
            .HasIndex(r => r.State)
            .IsUnique()
            .HasFilter("\"State\" = 1")
            .HasDatabaseName("IX_RouletteRounds_OnlyOneOpen");

        modelBuilder.Entity<RouletteConfiguration>()
            .Property(c => c.Id)
            .ValueGeneratedNever();

        modelBuilder.Entity<RouletteConfiguration>()
            .Property(c => c.ChipValuesJson)
            .HasColumnType("jsonb");

        modelBuilder.Entity<AdminAuditLog>()
            .HasIndex(a => new { a.CreatedAtUtc, a.Action });

        modelBuilder.Entity<AdminAuditLog>()
            .Property(a => a.DetailsJson)
            .HasColumnType("jsonb");

        modelBuilder.Entity<AdminAuditLog>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AdminAuditLog>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.TargetUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Bet>()
            .HasIndex(b => b.RouletteRoundId);

        modelBuilder.Entity<Bet>()
            .HasIndex(b => new { b.UserId, b.IdempotencyKey })
            .IsUnique();

        modelBuilder.Entity<Bet>()
            .HasOne(b => b.RouletteRound)
            .WithMany(r => r.Bets)
            .HasForeignKey(b => b.RouletteRoundId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- Configuración de Transaction ---
        modelBuilder.Entity<Transaction>()
            .Property(t => t.Monto)
            .HasColumnType("decimal(18,2)");
    }
}
