using GestioneFerie.Web.Data;
using GestioneFerie.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GestioneFerie.Web.Services;

public class AziendeService
{
  private readonly IDbContextFactory<AppDbContext> _dbFactory;
  private readonly EmailService _email;

  public AziendeService(IDbContextFactory<AppDbContext> dbFactory, EmailService email)
  {
    _dbFactory = dbFactory;
    _email = email;
  }

  public async Task<List<Azienda>> ListaAsync()
  {
    using var db = _dbFactory.CreateDbContext();
    return await db.Aziende.Include(a => a.Capo).OrderBy(a => a.Nome).ToListAsync();
  }

  public async Task<Dictionary<int, int>> ConteggioDipendentiAsync(List<int> aziendeIds)
  {
    using var db = _dbFactory.CreateDbContext();
    return await db.Utenti
        .Where(u => u.AziendaId != null && aziendeIds.Contains(u.AziendaId!.Value) && u.Ruolo == Ruolo.Dipendente)
        .GroupBy(u => u.AziendaId!.Value)
        .ToDictionaryAsync(g => g.Key, g => g.Count());
  }

  public async Task<(bool Success, string Messaggio)> CreaAsync(
      string nomeAzienda, string? emailMalattia, string nomeCapo, string cognomeCapo, string emailCapo,
      string? googleCalendarId = null)
  {
    using var db = _dbFactory.CreateDbContext();

    if (await db.Utenti.AnyAsync(u => u.Email == emailCapo))
      return (false, "Email capo già in uso.");

    var password = PasswordGenerator.Genera();

    var capo = new Utente
    {
      Nome = nomeCapo,
      Cognome = cognomeCapo,
      Email = emailCapo,
      PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
      Ruolo = Ruolo.Capo
    };
    db.Utenti.Add(capo);
    await db.SaveChangesAsync();

    var azienda = new Azienda
    {
      Nome = nomeAzienda,
      EmailMalattia = string.IsNullOrWhiteSpace(emailMalattia) ? null : emailMalattia,
      GoogleCalendarId = string.IsNullOrWhiteSpace(googleCalendarId) ? null : googleCalendarId.Trim(),
      CapoId = capo.Id
    };
    db.Aziende.Add(azienda);
    await db.SaveChangesAsync();

    capo.AziendaId = azienda.Id;
    await db.SaveChangesAsync();

    var (subject, body) = EmailTemplates.BenvenutoCapo(nomeCapo, nomeAzienda, emailCapo, password);
    _ = _email.InviaAsync(emailCapo, subject, body);

    return (true, $"Azienda \"{nomeAzienda}\" creata! Credenziali inviate a {emailCapo}.");
  }

  public async Task<bool> SalvaModificaAsync(int aziendaId, string nome, string? emailMalattia,
      string? googleCalendarId = null)
  {
    using var db = _dbFactory.CreateDbContext();
    var azienda = await db.Aziende.FindAsync(aziendaId);
    if (azienda == null) return false;

    azienda.Nome = nome;
    azienda.EmailMalattia = string.IsNullOrWhiteSpace(emailMalattia) ? null : emailMalattia;
    azienda.GoogleCalendarId = string.IsNullOrWhiteSpace(googleCalendarId) ? null : googleCalendarId.Trim();
    await db.SaveChangesAsync();
    return true;
  }

  /// <summary>Aggiorna solo l'ID del Google Calendar dell'azienda (usato dal capo).</summary>
  public async Task<bool> SalvaGoogleCalendarIdAsync(int aziendaId, string? googleCalendarId)
  {
    using var db = _dbFactory.CreateDbContext();
    var azienda = await db.Aziende.FindAsync(aziendaId);
    if (azienda == null) return false;

    azienda.GoogleCalendarId = string.IsNullOrWhiteSpace(googleCalendarId) ? null : googleCalendarId.Trim();
    await db.SaveChangesAsync();
    return true;
  }

  public async Task EliminaAsync(int aziendaId)
  {
    using var db = _dbFactory.CreateDbContext();

    var utenti = await db.Utenti.Where(u => u.AziendaId == aziendaId).ToListAsync();
    var utenteIds = utenti.Select(u => u.Id).ToList();

    var richieste = await db.RichiestaFerie.Where(r => utenteIds.Contains(r.UtenteId)).ToListAsync();
    db.RichiestaFerie.RemoveRange(richieste);
    await db.SaveChangesAsync();

    var saldi = await db.SaldoFerie.Where(s => utenteIds.Contains(s.UtenteId)).ToListAsync();
    db.SaldoFerie.RemoveRange(saldi);
    await db.SaveChangesAsync();

    var log = await db.LogModificheSaldo.Where(l => utenteIds.Contains(l.UtenteId)).ToListAsync();
    db.LogModificheSaldo.RemoveRange(log);
    await db.SaveChangesAsync();

    // L'azienda va eliminata prima degli utenti: Azienda.CapoId -> Utente.Id ha OnDelete Restrict,
    // quindi il capo non puo' essere rimosso finche' l'azienda lo referenzia come CapoId.
    var azienda = await db.Aziende.FindAsync(aziendaId);
    if (azienda != null)
    {
      db.Aziende.Remove(azienda);
      await db.SaveChangesAsync();
    }

    db.Utenti.RemoveRange(utenti);
    await db.SaveChangesAsync();
  }
}
