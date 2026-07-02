using GestioneFerie.Web.Models;

namespace GestioneFerie.Web.Services;

public static class EmailTemplates
{
  public static (string Subject, string Body) RichiestaApprovata(RichiestaFerie r) => (
    "La tua richiesta ferie è stata approvata ✅",
    $"""
    <p>Ciao {r.Utente.Nome},</p>
    <p>La tua richiesta di <strong>{r.Tipo}</strong> dal <strong>{r.DataInizio:dd/MM/yyyy}</strong> al <strong>{r.DataFine:dd/MM/yyyy}</strong> è stata <strong>approvata</strong>.</p>
    <p>Ore scalate: <strong>{r.OreRichieste}</strong></p>
    <p>Il team HR</p>
    """
  );

  public static (string Subject, string Body) RichiestaRifiutata(RichiestaFerie r) => (
    "La tua richiesta ferie è stata rifiutata ❌",
    $"""
    <p>Ciao {r.Utente.Nome},</p>
    <p>La tua richiesta di <strong>{r.Tipo}</strong> dal <strong>{r.DataInizio:dd/MM/yyyy}</strong> al <strong>{r.DataFine:dd/MM/yyyy}</strong> è stata <strong>rifiutata</strong>.</p>
    <p>Motivo: <strong>{r.MotivoRifiuto}</strong></p>
    <p>Il team HR</p>
    """
  );

  public static (string Subject, string Body) RichiestaEliminata(RichiestaFerie r) => (
    $"🗑️ {r.Utente.Nome} ha eliminato una richiesta",
    $"""
    <p><strong>{r.Utente.Nome} {r.Utente.Cognome}</strong> ha eliminato la seguente richiesta:</p>
    <p>Tipo: <strong>{r.Tipo}</strong></p>
    <p>Dal: <strong>{r.DataInizio:dd/MM/yyyy}</strong> al <strong>{r.DataFine:dd/MM/yyyy}</strong></p>
    <p>Ore: <strong>{r.OreRichieste}</strong></p>
    <p>Stato precedente: <strong>{r.Stato}</strong></p>
    """
  );

  public static (string Subject, string Body) NuovaRichiestaNotifica(
      string nomeDipendente, string tipo, DateTime dataInizio, DateTime dataFine, decimal ore, string? note, bool preavvisoInsufficiente) => (
    $"📋 Nuova richiesta di {tipo} - {nomeDipendente}",
    $"""
    <p>Il dipendente <strong>{nomeDipendente}</strong> ha inviato una nuova richiesta.</p>
    <p>Tipo: <strong>{tipo}</strong></p>
    <p>Dal: <strong>{dataInizio:dd/MM/yyyy}</strong></p>
    <p>Al: <strong>{dataFine:dd/MM/yyyy}</strong></p>
    <p>Ore: <strong>{ore}</strong></p>
    <p>Note: {(string.IsNullOrWhiteSpace(note) ? "—" : note)}</p>
    {(preavvisoInsufficiente ? $"<p>⚠️ Richiesta con meno di {RichiesteService.GiorniPreavvisoMinimo} giorni di preavviso.</p>" : "")}
    <p>Accedi al pannello per approvare o rifiutare.</p>
    """
  );

  public static (string Subject, string Body) SegnalazioneMalattia(
      string nomeDipendente, string codiceMalattia, DateTime dataInizio, DateTime dataFine, string? note) => (
    $"🤒 Segnalazione malattia - {nomeDipendente}",
    $"""
    <p>Il dipendente <strong>{nomeDipendente}</strong> ha segnalato una malattia.</p>
    <p>Codice malattia: <strong>{codiceMalattia}</strong></p>
    <p>Dal: <strong>{dataInizio:dd/MM/yyyy}</strong></p>
    <p>Al: <strong>{dataFine:dd/MM/yyyy}</strong></p>
    <p>Note: {(string.IsNullOrWhiteSpace(note) ? "—" : note)}</p>
    """
  );

  public static (string Subject, string Body) SaldoAggiornato(string nome, int anno, decimal oreTotali) => (
    $"Saldo ferie aggiornato - {anno}",
    $"""
    <p>Ciao {nome},</p>
    <p>Il tuo saldo ferie per l'anno <strong>{anno}</strong> è stato aggiornato:</p>
    <p>Ore totali: <strong>{oreTotali}</strong></p>
    <p>Il team HR</p>
    """
  );

  public static (string Subject, string Body) SaldoRipristinato(string nome, int anno, decimal orePrecedenti) => (
    $"Saldo ferie ripristinato - {anno}",
    $"""
    <p>Ciao {nome},</p>
    <p>Il tuo saldo ferie per l'anno <strong>{anno}</strong> è stato ripristinato a <strong>{orePrecedenti} ore</strong>.</p>
    <p>Il team HR</p>
    """
  );

  public static (string Subject, string Body) CredenzialiAccesso(string nome, string email, string password) => (
    "Benvenuto! Le tue credenziali di accesso",
    $"""
    <p>Ciao {nome},</p>
    <p>Il tuo account è stato creato. Ecco le tue credenziali:</p>
    <p>Email: <strong>{email}</strong></p>
    <p>Password: <strong>{password}</strong></p>
    <p>Ti consigliamo di cambiare la password al primo accesso.</p>
    <p>Il team HR</p>
    """
  );

  public static (string Subject, string Body) BenvenutoCapo(string nome, string nomeAzienda, string email, string password) => (
    $"Benvenuto su Gestione Ferie — {nomeAzienda}",
    $"""
    <p>Ciao {nome},</p>
    <p>Il tuo account da <strong>capo</strong> per l'azienda <strong>{nomeAzienda}</strong> è stato creato.</p>
    <p>Email: <strong>{email}</strong></p>
    <p>Password: <strong>{password}</strong></p>
    <p>Accedi e aggiungi i tuoi dipendenti dalla sezione <em>Dipendenti</em>.</p>
    """
  );
}
