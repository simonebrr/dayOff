using GestioneFerie.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GestioneFerie.Web.Data;

public class AppDbContext : DbContext
{
  public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
  public DbSet<LogModificaSaldo> LogModificheSaldo { get; set; }
  public DbSet<Utente> Utenti { get; set; }
  public DbSet<RichiestaFerie> RichiestaFerie { get; set; }
  public DbSet<SaldoFerie> SaldoFerie { get; set; }

  public DbSet<LoginAttempt> LoginAttempts { get; set; }
  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<Utente>()
      .HasIndex(u => u.Email)
      .IsUnique();

    modelBuilder.Entity<RichiestaFerie>()
      .HasOne(r => r.Utente)
      .WithMany(u => u.Richieste)
      .HasForeignKey(r => r.UtenteId);

    modelBuilder.Entity<SaldoFerie>()
      .HasOne(s => s.Utente)
      .WithOne(u => u.SaldoFerie)
      .HasForeignKey<SaldoFerie>(s => s.UtenteId);
  }
}