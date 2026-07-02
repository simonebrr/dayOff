using System.Globalization;
using GestioneFerie.Web.Data;
using GestioneFerie.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GestioneFerie.Web.Services;

public record DipendenteViewModel(Utente Utente, SaldoFerie? Saldo);
public record CsvRiga(string Nome, string Cognome, string Email, decimal OreFerie);

public class DipendentiService
{
  private readonly IDbContextFactory<AppDbContext> _dbFactory;
  private readonly EmailService _email;

  public DipendentiService(IDbContextFactory<AppDbContext> dbFactory, EmailService email)
  {
    _dbFactory = dbFactory;
    _email = email;
  }

  public async Task<List<DipendenteViewModel>> ListaAsync(int? aziendaId, int anno)
  {
    using var db = _dbFactory.CreateDbContext();
    var utenti = await db.Utenti
        .Where(u => u.Ruolo == Ruolo.Dipendente && u.AziendaId == aziendaId)
        .ToListAsync();

    var utenteIds = utenti.Select(u => u.Id).ToList();
    var saldi = await db.SaldoFerie
        .Where(s => s.Anno == anno && utenteIds.Contains(s.UtenteId))
        .ToListAsync();

    return utenti
        .Select(u => new DipendenteViewModel(u, saldi.FirstOrDefault(s => s.UtenteId == u.Id)))
        .ToList();
  }

  public async Task<(bool Success, string Messaggio)> AggiungiAsync(
      int? aziendaId, string nome, string cognome, string email, decimal oreTotali, int anno)
  {
    using var db = _dbFactory.CreateDbContext();

    if (await db.Utenti.AnyAsync(u => u.Email == email))
      return (false, "Email già in uso.");

    var password = PasswordGenerator.Genera();
    var utente = new Utente
    {
      Nome = nome,
      Cognome = cognome,
      Email = email,
      PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
      Ruolo = Ruolo.Dipendente,
      AziendaId = aziendaId
    };
    db.Utenti.Add(utente);
    await db.SaveChangesAsync();

    db.SaldoFerie.Add(new SaldoFerie { UtenteId = utente.Id, OreTotali = oreTotali, OreUsate = 0, Anno = anno });
    await db.SaveChangesAsync();

    var (subject, body) = EmailTemplates.CredenzialiAccesso(nome, email, password);
    _ = _email.InviaAsync(email, subject, body);

    return (true, $"Dipendente {nome} {cognome} aggiunto! Email con credenziali inviata.");
  }

  public static List<CsvRiga> ParseCsv(string content)
  {
    var righe = new List<CsvRiga>();
    var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

    foreach (var line in lines)
    {
      var trimmed = line.Trim();
      if (string.IsNullOrEmpty(trimmed)) continue;

      var parts = trimmed.Split(',');
      if (parts.Length < 3) continue;

      var nome = parts[0].Trim();
      var cognome = parts[1].Trim();
      var email = parts[2].Trim();

      if (nome.Equals("Nome", StringComparison.OrdinalIgnoreCase)) continue;
      if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(email)) continue;

      var oreFerie = 160m;
      if (parts.Length >= 4 && decimal.TryParse(parts[3].Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var oreFromCsv))
        oreFerie = oreFromCsv;

      righe.Add(new CsvRiga(nome, cognome, email, oreFerie));
    }

    return righe;
  }

  public async Task<(int Importati, int Saltati)> ImportaCsvAsync(int? aziendaId, List<CsvRiga> righe, int anno)
  {
    using var db = _dbFactory.CreateDbContext();
    var importati = 0;
    var saltati = 0;
    var emailsDaInviare = new List<(string Email, string Nome, string Password)>();

    foreach (var riga in righe)
    {
      if (await db.Utenti.AnyAsync(u => u.Email == riga.Email))
      {
        saltati++;
        continue;
      }

      var password = PasswordGenerator.Genera();
      var utente = new Utente
      {
        Nome = riga.Nome,
        Cognome = riga.Cognome,
        Email = riga.Email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        Ruolo = Ruolo.Dipendente,
        AziendaId = aziendaId
      };
      db.Utenti.Add(utente);
      await db.SaveChangesAsync();

      db.SaldoFerie.Add(new SaldoFerie { UtenteId = utente.Id, OreTotali = riga.OreFerie, OreUsate = 0, Anno = anno });
      await db.SaveChangesAsync();

      emailsDaInviare.Add((riga.Email, riga.Nome, password));
      importati++;
    }

    _ = Task.Run(async () =>
    {
      foreach (var (email, nome, pwd) in emailsDaInviare)
      {
        var (subject, body) = EmailTemplates.CredenzialiAccesso(nome, email, pwd);
        await _email.InviaAsync(email, subject, body);
      }
    });

    return (importati, saltati);
  }

  private static void RegistraLog(AppDbContext db, int utenteId, decimal orePrecedenti, decimal oreNuove, string modificatoDa, int anno)
  {
    db.LogModificheSaldo.Add(new LogModificaSaldo
    {
      UtenteId = utenteId,
      OrePrecedenti = orePrecedenti,
      OreNuove = oreNuove,
      DataModifica = DateTime.Now,
      ModificatoDa = modificatoDa,
      Anno = anno
    });
  }

  public async Task SalvaSaldoAsync(int utenteId, decimal oreTotali, int anno, string modificatoDa)
  {
    using var db = _dbFactory.CreateDbContext();
    var saldo = await db.SaldoFerie.FirstOrDefaultAsync(s => s.UtenteId == utenteId && s.Anno == anno);
    var utente = await db.Utenti.FindAsync(utenteId);

    RegistraLog(db, utenteId, saldo?.OreTotali ?? 0, oreTotali, modificatoDa, anno);

    if (saldo == null)
      db.SaldoFerie.Add(new SaldoFerie { UtenteId = utenteId, OreTotali = oreTotali, OreUsate = 0, Anno = anno });
    else
      saldo.OreTotali = oreTotali;

    await db.SaveChangesAsync();

    if (utente != null && !string.IsNullOrWhiteSpace(utente.Email))
    {
      var (subject, body) = EmailTemplates.SaldoAggiornato(utente.Nome, anno, oreTotali);
      await _email.InviaAsync(utente.Email, subject, body);
    }
  }

  public async Task SalvaTuttiAsync(Dictionary<int, decimal> modifiche, int anno, string modificatoDa)
  {
    using var db = _dbFactory.CreateDbContext();
    var utenteIds = modifiche.Keys.ToList();

    var saldi = await db.SaldoFerie.Where(s => s.Anno == anno && utenteIds.Contains(s.UtenteId)).ToListAsync();
    var utenti = await db.Utenti.Where(u => utenteIds.Contains(u.Id)).ToListAsync();

    foreach (var (utenteId, oreTotali) in modifiche)
    {
      var saldo = saldi.FirstOrDefault(s => s.UtenteId == utenteId);
      RegistraLog(db, utenteId, saldo?.OreTotali ?? 0, oreTotali, modificatoDa, anno);

      if (saldo == null)
        db.SaldoFerie.Add(new SaldoFerie { UtenteId = utenteId, OreTotali = oreTotali, OreUsate = 0, Anno = anno });
      else
        saldo.OreTotali = oreTotali;
    }

    await db.SaveChangesAsync();

    var emailTasks = utenti
        .Where(u => !string.IsNullOrWhiteSpace(u.Email))
        .Select(u =>
        {
          var (subject, body) = EmailTemplates.SaldoAggiornato(u.Nome, anno, modifiche[u.Id]);
          return _email.InviaAsync(u.Email, subject, body);
        });
    await Task.WhenAll(emailTasks);
  }

  public async Task AssegnaTuttiAsync(int? aziendaId, decimal ore, int anno, string modificatoDa)
  {
    using var db = _dbFactory.CreateDbContext();
    var dipendenti = await db.Utenti
        .Where(u => u.Ruolo == Ruolo.Dipendente && u.AziendaId == aziendaId)
        .ToListAsync();
    var utenteIds = dipendenti.Select(d => d.Id).ToList();

    var saldi = await db.SaldoFerie.Where(s => s.Anno == anno && utenteIds.Contains(s.UtenteId)).ToListAsync();

    foreach (var d in dipendenti)
    {
      var saldo = saldi.FirstOrDefault(s => s.UtenteId == d.Id);
      RegistraLog(db, d.Id, saldo?.OreTotali ?? 0, ore, modificatoDa, anno);

      if (saldo == null)
        db.SaldoFerie.Add(new SaldoFerie { UtenteId = d.Id, OreTotali = ore, OreUsate = 0, Anno = anno });
      else
        saldo.OreTotali = ore;
    }

    await db.SaveChangesAsync();

    var destinatari = dipendenti
        .Where(d => !string.IsNullOrWhiteSpace(d.Email))
        .Select(d => (d.Email, d.Nome))
        .ToList();

    _ = Task.Run(async () =>
    {
      foreach (var (email, nome) in destinatari)
      {
        var (subject, body) = EmailTemplates.SaldoAggiornato(nome, anno, ore);
        await _email.InviaAsync(email, subject, body);
      }
    });
  }

  public async Task RipristinaSaldoAsync(int utenteId, LogModificaSaldo log, string modificatoDa)
  {
    using var db = _dbFactory.CreateDbContext();
    var saldo = await db.SaldoFerie.FirstOrDefaultAsync(s => s.UtenteId == utenteId && s.Anno == log.Anno);
    if (saldo == null) return;

    RegistraLog(db, utenteId, saldo.OreTotali, log.OrePrecedenti, modificatoDa, log.Anno);
    saldo.OreTotali = log.OrePrecedenti;
    await db.SaveChangesAsync();

    var utente = await db.Utenti.FindAsync(utenteId);
    if (utente != null && !string.IsNullOrWhiteSpace(utente.Email))
    {
      var (subject, body) = EmailTemplates.SaldoRipristinato(utente.Nome, log.Anno, log.OrePrecedenti);
      await _email.InviaAsync(utente.Email, subject, body);
    }
  }

  public async Task EliminaDipendenteAsync(int utenteId)
  {
    using var db = _dbFactory.CreateDbContext();

    var richieste = await db.RichiestaFerie.Where(r => r.UtenteId == utenteId).ToListAsync();
    db.RichiestaFerie.RemoveRange(richieste);

    var saldi = await db.SaldoFerie.Where(s => s.UtenteId == utenteId).ToListAsync();
    db.SaldoFerie.RemoveRange(saldi);

    var log = await db.LogModificheSaldo.Where(l => l.UtenteId == utenteId).ToListAsync();
    db.LogModificheSaldo.RemoveRange(log);

    var utente = await db.Utenti.FindAsync(utenteId);
    if (utente != null) db.Utenti.Remove(utente);

    await db.SaveChangesAsync();
  }
}
