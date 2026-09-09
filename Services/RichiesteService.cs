using GestioneFerie.Web.Data;
using GestioneFerie.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GestioneFerie.Web.Services;

public class RichiesteService
{
  public const int GiorniPreavvisoMinimo = 4;

  private readonly IDbContextFactory<AppDbContext> _dbFactory;
  private readonly EmailService _email;
  private readonly GoogleCalendarService _gcal;

  public RichiesteService(IDbContextFactory<AppDbContext> dbFactory, EmailService email, GoogleCalendarService gcal)
  {
    _dbFactory = dbFactory;
    _email = email;
    _gcal = gcal;
  }

  public static bool PreavvisoInsufficiente(DateTime dataInizio) =>
      (dataInizio.Date - DateTime.Today.Date).TotalDays < GiorniPreavvisoMinimo;

  public async Task<RichiestaFerie?> ApprovaAsync(int id)
  {
    using var db = _dbFactory.CreateDbContext();
    var richiesta = await db.RichiestaFerie.Include(r => r.Utente).FirstOrDefaultAsync(r => r.Id == id);
    if (richiesta == null) return null;

    richiesta.Stato = StatoRichiesta.Approvata;
    var saldo = await db.SaldoFerie.FirstOrDefaultAsync(s => s.UtenteId == richiesta.UtenteId && s.Anno == DateTime.Now.Year);
    if (saldo != null) saldo.OreUsate += richiesta.OreRichieste;
    await db.SaveChangesAsync();

    // Sincronizza sul calendario Google dell'azienda (solo richieste approvate).
    var azienda = await GetAziendaAsync(db, richiesta.Utente.AziendaId);
    var nome = $"{richiesta.Utente.Nome} {richiesta.Utente.Cognome}";
    var eventId = await _gcal.CreaOAggiornaEventoAsync(richiesta, nome, azienda);
    if (eventId != richiesta.GoogleCalendarEventId)
    {
      richiesta.GoogleCalendarEventId = eventId;
      await db.SaveChangesAsync();
    }

    var (subject, body) = EmailTemplates.RichiestaApprovata(richiesta);
    await _email.InviaAsync(richiesta.Utente.Email, subject, body);

    return richiesta;
  }

  public async Task<RichiestaFerie?> RifiutaAsync(int id, string motivo)
  {
    using var db = _dbFactory.CreateDbContext();
    var richiesta = await db.RichiestaFerie.Include(r => r.Utente).FirstOrDefaultAsync(r => r.Id == id);
    if (richiesta == null) return null;

    richiesta.Stato = StatoRichiesta.Rifiutata;
    richiesta.MotivoRifiuto = motivo;

    // Se era già a calendario (es. approvata e poi rifiutata), rimuovi l'evento.
    if (!string.IsNullOrWhiteSpace(richiesta.GoogleCalendarEventId))
    {
      var azienda = await GetAziendaAsync(db, richiesta.Utente.AziendaId);
      await _gcal.RimuoviEventoAsync(richiesta.GoogleCalendarEventId, azienda);
      richiesta.GoogleCalendarEventId = null;
    }
    await db.SaveChangesAsync();

    var (subject, body) = EmailTemplates.RichiestaRifiutata(richiesta);
    await _email.InviaAsync(richiesta.Utente.Email, subject, body);

    return richiesta;
  }

  public async Task<RichiestaFerie?> EliminaAsync(int id, string? emailNotificaHR)
  {
    using var db = _dbFactory.CreateDbContext();
    var richiesta = await db.RichiestaFerie.Include(r => r.Utente).FirstOrDefaultAsync(r => r.Id == id);
    if (richiesta == null) return null;

    if (richiesta.Stato == StatoRichiesta.Approvata)
    {
      var saldo = await db.SaldoFerie.FirstOrDefaultAsync(s =>
          s.UtenteId == richiesta.UtenteId && s.Anno == DateTime.Now.Year);
      if (saldo != null) saldo.OreUsate -= richiesta.OreRichieste;
    }

    var eventId = richiesta.GoogleCalendarEventId;
    var azienda = await GetAziendaAsync(db, richiesta.Utente.AziendaId);
    db.RichiestaFerie.Remove(richiesta);
    await db.SaveChangesAsync();

    // Rimuovi l'eventuale evento dal calendario Google dell'azienda.
    await _gcal.RimuoviEventoAsync(eventId, azienda);

    if (!string.IsNullOrEmpty(emailNotificaHR))
    {
      var (subject, body) = EmailTemplates.RichiestaEliminata(richiesta);
      _ = _email.InviaAsync(emailNotificaHR, subject, body);
    }

    return richiesta;
  }

  public async Task RimuoviScaduteInAttesaAsync(int? aziendaId, int? utenteId)
  {
    using var db = _dbFactory.CreateDbContext();
    var oggi = DateTime.Today;

    IQueryable<RichiestaFerie> query = db.RichiestaFerie
        .Include(r => r.Utente)
        .Where(r => r.Stato == StatoRichiesta.InAttesa && r.DataInizio < oggi);

    query = utenteId != null
        ? query.Where(r => r.UtenteId == utenteId)
        : query.Where(r => r.Utente.AziendaId == aziendaId);

    var scadute = await query.ToListAsync();
    if (scadute.Count == 0) return;

    db.RichiestaFerie.RemoveRange(scadute);
    await db.SaveChangesAsync();
  }

  public async Task<RichiestaFerie> InviaFeriePermessoAsync(
      int utenteId, string nomeUtente, int? aziendaId, TipoRichiesta tipo,
      DateTime dataInizio, DateTime dataFine, decimal ore, string? note)
  {
    using var db = _dbFactory.CreateDbContext();
    var richiesta = new RichiestaFerie
    {
      UtenteId = utenteId,
      Tipo = tipo,
      DataInizio = dataInizio,
      DataFine = dataFine,
      OreRichieste = ore,
      Note = string.IsNullOrWhiteSpace(note) ? null : note,
      Stato = StatoRichiesta.InAttesa,
      DataRichiesta = DateTime.Now
    };
    db.RichiestaFerie.Add(richiesta);
    await db.SaveChangesAsync();

    var capo = await db.Utenti.FirstOrDefaultAsync(u => u.Ruolo == Ruolo.Capo && u.AziendaId == aziendaId);
    if (capo != null)
    {
      var (subject, body) = EmailTemplates.NuovaRichiestaNotifica(
          nomeUtente, tipo.ToString(), dataInizio, dataFine, ore, note, PreavvisoInsufficiente(dataInizio));
      await _email.InviaAsync(capo.Email, subject, body);
    }

    return richiesta;
  }

  public async Task<RichiestaFerie> InviaMalattiaAsync(
      int utenteId, string nomeUtente, DateTime dataInizio, DateTime dataFine, string codiceMalattia, string? note)
  {
    using var db = _dbFactory.CreateDbContext();
    var richiesta = new RichiestaFerie
    {
      UtenteId = utenteId,
      Tipo = TipoRichiesta.Malattia,
      DataInizio = dataInizio,
      DataFine = dataFine,
      OreRichieste = 0,
      Note = string.IsNullOrWhiteSpace(note) ? null : note,
      CodiceMalattia = codiceMalattia,
      Stato = StatoRichiesta.InAttesa,
      DataRichiesta = DateTime.Now
    };
    db.RichiestaFerie.Add(richiesta);
    await db.SaveChangesAsync();

    var utente = await db.Utenti
        .Include(u => u.Azienda)
        .ThenInclude(a => a!.Capo)
        .FirstOrDefaultAsync(u => u.Id == utenteId);
    var emailDestinatario = utente?.Azienda?.EmailMalattia ?? utente?.Azienda?.Capo?.Email ?? "";

    // La malattia non ha un'approvazione: la mettiamo subito a calendario dell'azienda.
    var gcalEventId = await _gcal.CreaOAggiornaEventoAsync(richiesta, nomeUtente, utente?.Azienda);
    if (gcalEventId != richiesta.GoogleCalendarEventId)
    {
      richiesta.GoogleCalendarEventId = gcalEventId;
      await db.SaveChangesAsync();
    }

    if (!string.IsNullOrEmpty(emailDestinatario))
    {
      var (subject, body) = EmailTemplates.SegnalazioneMalattia(nomeUtente, codiceMalattia, dataInizio, dataFine, note);
      await _email.InviaAsync(emailDestinatario, subject, body);
    }

    return richiesta;
  }

  private static async Task<Azienda?> GetAziendaAsync(AppDbContext db, int? aziendaId)
  {
    if (aziendaId == null) return null;
    return await db.Aziende.FindAsync(aziendaId.Value);
  }

  public async Task<SaldoFerie?> GetSaldoAsync(int utenteId, int anno)
  {
    using var db = _dbFactory.CreateDbContext();
    return await db.SaldoFerie.FirstOrDefaultAsync(s => s.UtenteId == utenteId && s.Anno == anno);
  }

  public async Task<string> GetNomeCapoAsync(int? aziendaId)
  {
    using var db = _dbFactory.CreateDbContext();
    var capo = await db.Utenti.FirstOrDefaultAsync(u => u.Ruolo == Ruolo.Capo && u.AziendaId == aziendaId);
    return capo != null ? $"{capo.Nome} {capo.Cognome}" : "il tuo responsabile";
  }
}
