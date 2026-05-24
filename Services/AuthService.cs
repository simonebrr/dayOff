using GestioneFerie.Web.Data;
using GestioneFerie.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GestioneFerie.Web.Services;

public class AuthService
{
  private readonly IDbContextFactory<AppDbContext> _dbFactory;
  private readonly SessionService _session;
  private const int MaxTentativiFalliti = 5;
  private const int BloccoDurataPeriodoMinuti = 15;

  public int? UtenteId => _session.UtenteId;
  public string? UtenteNome => _session.UtenteNome;
  public string? UtenteRuolo => _session.UtenteRuolo;
  public int? AziendaId => _session.AziendaId;
  public bool IsAuthenticated => _session.IsAuthenticated;
  public bool IsCapo => _session.IsCapo;
  public bool IsSuperAdmin => _session.IsSuperAdmin;

  public event Action? OnChange
  {
    add => _session.OnChange += value;
    remove => _session.OnChange -= value;
  }

  public AuthService(IDbContextFactory<AppDbContext> dbFactory, SessionService session)
  {
    _dbFactory = dbFactory;
    _session = session;
  }

  public async Task<(bool successo, string messaggio)> LoginAsync(string email, string password)
  {
    try
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

      await _session.SetAsync(utente.Id, utente.Nome, utente.Ruolo.ToString(), utente.AziendaId);

      return (true, "Login effettuato con successo.");
    }
    catch (Exception ex)
    {
      return (false, $"Errore durante il login: {ex.Message}");
    }
  }

  public async Task LogoutAsync()
  {
    await _session.ClearAsync();
  }

  public async Task LoadSessionAsync()
  {
    await _session.LoadAsync();
  }
}