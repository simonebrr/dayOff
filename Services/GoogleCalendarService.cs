using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using GestioneFerie.Web.Models;

namespace GestioneFerie.Web.Services;

/// <summary>
/// Sincronizza le richieste di ferie/permessi/malattia con i calendari Google
/// delle aziende. App multi-azienda: un unico service account (credenziali
/// globali) scrive su tanti calendari, uno per azienda; l'id del calendario è
/// salvato su <c>Azienda.GoogleCalendarId</c> e passato ai metodi di sync.
///
/// Autenticazione tramite service account (server-to-server): non serve alcun
/// login utente. Ogni calendario aziendale va condiviso con l'email del service
/// account concedendo il permesso "Apportare modifiche agli eventi".
///
/// Configurazione globale (appsettings / variabili d'ambiente):
///   GoogleCalendar:ServiceAccountKeyJson   -> contenuto JSON della chiave (opzionale)
///   GoogleCalendar:ServiceAccountKeyPath   -> percorso al file JSON della chiave (opzionale)
///   GoogleCalendar:ServiceAccountEmail     -> email del service account (solo informativa)
///   GoogleCalendar:TimeZone                -> es. "Europe/Rome" (default)
///
/// Se le credenziali non sono configurate (o un'azienda non ha un calendarId),
/// i metodi di sync sono no-op sicuri: l'app funziona comunque.
/// </summary>
public class GoogleCalendarService
{
  private readonly IConfiguration _config;
  private readonly ILogger<GoogleCalendarService> _logger;
  private readonly object _lock = new();
  private CalendarService? _service;

  public GoogleCalendarService(IConfiguration config, ILogger<GoogleCalendarService> logger)
  {
    _config = config;
    _logger = logger;
  }

  private string TimeZone => _config["GoogleCalendar:TimeZone"] ?? "Europe/Rome";

  /// <summary>Calendario pubblico Google delle festività italiane.</summary>
  public const string FestivitaCalendarId = "it.italian#holiday@group.v.calendar.google.com";

  private readonly Dictionary<int, HashSet<DateTime>> _festiviCache = new();

  /// <summary>True se il service account (credenziali globali) è configurato.</summary>
  public bool CredenzialiDisponibili()
  {
    var json = _config["GoogleCalendar:ServiceAccountKeyJson"];
    var path = _config["GoogleCalendar:ServiceAccountKeyPath"];
    return !string.IsNullOrWhiteSpace(json)
        || (!string.IsNullOrWhiteSpace(path) && File.Exists(path));
  }

  private CalendarService? GetService()
  {
    if (!CredenzialiDisponibili()) return null;
    if (_service != null) return _service;

    lock (_lock)
    {
      if (_service != null) return _service;
      try
      {
        GoogleCredential credential;
        var json = _config["GoogleCalendar:ServiceAccountKeyJson"];
        if (!string.IsNullOrWhiteSpace(json))
          credential = GoogleCredential.FromJson(json);
        else
          credential = GoogleCredential.FromFile(_config["GoogleCalendar:ServiceAccountKeyPath"]!);

        credential = credential.CreateScoped(CalendarService.Scope.Calendar);

        _service = new CalendarService(new BaseClientService.Initializer
        {
          HttpClientInitializer = credential,
          ApplicationName = "Gestione Ferie"
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Impossibile inizializzare il client Google Calendar");
        _service = null;
      }
      return _service;
    }
  }

  /// <summary>
  /// Crea (o aggiorna, se già collegato) l'evento su Google Calendar.
  /// Ritorna l'id dell'evento da salvare in <see cref="RichiestaFerie.GoogleCalendarEventId"/>,
  /// oppure il valore esistente in caso di errore o servizio non configurato.
  /// </summary>
  public async Task<string?> CreaOAggiornaEventoAsync(RichiestaFerie r, string nome, string? calendarId)
  {
    if (string.IsNullOrWhiteSpace(calendarId)) return r.GoogleCalendarEventId;
    var svc = GetService();
    if (svc == null) return r.GoogleCalendarEventId;

    var ev = CostruisciEvento(r, nome);
    try
    {
      if (!string.IsNullOrWhiteSpace(r.GoogleCalendarEventId))
      {
        try
        {
          var aggiornato = await svc.Events.Update(ev, calendarId, r.GoogleCalendarEventId).ExecuteAsync();
          return aggiornato.Id;
        }
        catch (GoogleApiException)
        {
          // L'evento non esiste più: lo ricreiamo.
          var ricreato = await svc.Events.Insert(ev, calendarId).ExecuteAsync();
          return ricreato.Id;
        }
      }

      var creato = await svc.Events.Insert(ev, calendarId).ExecuteAsync();
      return creato.Id;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Errore nella sincronizzazione dell'evento Google Calendar per la richiesta {Id}", r.Id);
      return r.GoogleCalendarEventId;
    }
  }

  /// <summary>Rimuove l'evento dal calendario dell'azienda (no-op se non collegato).</summary>
  public async Task RimuoviEventoAsync(string? eventId, string? calendarId)
  {
    if (string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(calendarId)) return;
    var svc = GetService();
    if (svc == null) return;

    try
    {
      await svc.Events.Delete(calendarId, eventId).ExecuteAsync();
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Impossibile eliminare l'evento Google Calendar {EventId}", eventId);
    }
  }

  /// <summary>
  /// Restituisce le date festive dell'anno lette dal calendario pubblico Google
  /// delle festività italiane. Se Google non è configurato o la lettura fallisce,
  /// ricade sul calcolo locale (<see cref="HolidayCalculator"/>).
  /// </summary>
  public async Task<HashSet<DateTime>> GetFestiviAsync(int anno)
  {
    lock (_lock)
    {
      if (_festiviCache.TryGetValue(anno, out var cached)) return cached;
    }

    var risultato = await LeggiFestiviDaGoogleAsync(anno)
                    ?? HolidayCalculator.FestivitaItaliane(anno);

    lock (_lock)
    {
      _festiviCache[anno] = risultato;
    }
    return risultato;
  }

  private async Task<HashSet<DateTime>?> LeggiFestiviDaGoogleAsync(int anno)
  {
    var svc = GetService();
    if (svc == null) return null;

    try
    {
      var req = svc.Events.List(FestivitaCalendarId);
      req.TimeMinDateTimeOffset = new DateTimeOffset(new DateTime(anno, 1, 1, 0, 0, 0, DateTimeKind.Utc));
      req.TimeMaxDateTimeOffset = new DateTimeOffset(new DateTime(anno + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc));
      req.SingleEvents = true;

      var res = await req.ExecuteAsync();
      var set = new HashSet<DateTime>();
      if (res.Items != null)
      {
        foreach (var ev in res.Items)
        {
          if (DateTime.TryParse(ev.Start?.Date, out var d))
            set.Add(d.Date);
        }
      }
      return set.Count > 0 ? set : null;
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Impossibile leggere le festività da Google Calendar per l'anno {Anno}", anno);
      return null;
    }
  }

  private Event CostruisciEvento(RichiestaFerie r, string nome)
  {
    var titolo = $"{r.Tipo} – {nome}";

    // Colori Google Calendar: 10=verde (Basil), 9=blu (Blueberry), 3=viola (Grape)
    var colore = r.Tipo switch
    {
      TipoRichiesta.Ferie => "10",
      TipoRichiesta.Permesso => "9",
      _ => "3"
    };

    var righe = new List<string> { $"Stato: {r.Stato}" };
    if (r.Tipo != TipoRichiesta.Malattia)
      righe.Add($"Ore: {r.OreRichieste}");
    if (!string.IsNullOrWhiteSpace(r.CodiceMalattia))
      righe.Add($"Codice malattia: {r.CodiceMalattia}");
    if (!string.IsNullOrWhiteSpace(r.Note))
      righe.Add($"Note: {r.Note}");

    return new Event
    {
      Summary = titolo,
      Description = string.Join("\n", righe),
      ColorId = colore,
      // Eventi "tutto il giorno": la data di fine è esclusiva, quindi +1 giorno.
      Start = new EventDateTime { Date = r.DataInizio.ToString("yyyy-MM-dd"), TimeZone = TimeZone },
      End = new EventDateTime { Date = r.DataFine.AddDays(1).ToString("yyyy-MM-dd"), TimeZone = TimeZone }
    };
  }
}
