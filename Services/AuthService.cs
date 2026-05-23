using GestioneFerie.Web.Data;
using GestioneFerie.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GestioneFerie.Web.Services;

public class AuthService
{
  private readonly IDbContextFactory<AppDbContext> _dbFactory;
  private const int MaxTentativiFalliti = 5;
  private const int BloccoDurataPeriodoMinuti = 15;

  public int? UtenteId { get; private set; }
  public string? UtenteNome { get; private set; }
  public string? UtenteRuolo { get; private set; }
  public int? AziendaId { get; private set; }
  public bool IsAuthenticated => UtenteId != null;
  public bool IsCapo => UtenteRuolo == "Capo";
  public bool IsSuperAdmin => UtenteRuolo == "SuperAdmin";

  public event Action? OnChange;

  public AuthService(IDbContextFactory<AppDbContext> dbFactory)
  {
    _dbFactory = dbFactory;
  }

  public async Task<(bool successo, string messaggio)> LoginAsync(string email, string password)
  {
    using var db = _dbFactory.CreateDbContext();

    var tentativiFalliti = await db.LoginAttempts
        .Where(a => a.Email == email && !a.Successo &&
                    a.Timestamp > DateTime.UtcNow.AddMinutes(-BloccoDurataPeriodoMinuti))
        .CountAsync();

    if (tentativiFalliti >= MaxTentativiFalliti)
      return (false, "Account bloccato per troppi tentativi. Riprova tra 15 minuti.");

    var utente = await db.Utenti.FirstOrDefaultAsync(u => u.Email == email);

    if (utente == null || !BCrypt.Net.BCrypt.Verify(password, utente.PasswordHash))
    {
      db.LoginAttempts.Add(new LoginAttempt { Email = email, IpAddress = "unknown", Successo = false });
      await db.SaveChangesAsync();
      return (false, "Email o password non corretti.");
    }

    db.LoginAttempts.Add(new LoginAttempt { Email = email, IpAddress = "unknown", Successo = true });
    await db.SaveChangesAsync();

    UtenteId = utente.Id;
    UtenteNome = utente.Nome;
    UtenteRuolo = utente.Ruolo.ToString();
    AziendaId = utente.AziendaId;
    OnChange?.Invoke();

    return (true, "Login effettuato con successo.");
  }

  public void Logout()
  {
    UtenteId = null;
    UtenteNome = null;
    UtenteRuolo = null;
    AziendaId = null;
    OnChange?.Invoke();
  }
}
