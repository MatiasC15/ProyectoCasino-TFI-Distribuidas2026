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

        // --- Configuración de Transaction ---
        modelBuilder.Entity<Transaction>()
            .Property(t => t.Monto)
            .HasColumnType("decimal(18,2)");
    }
}