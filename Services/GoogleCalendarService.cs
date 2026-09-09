using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
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
/// Due modalità di scrittura:
///  - OAuth del capo (consigliata): il capo collega il proprio account Google con
///    un clic; gli eventi vanno nel suo calendario personale ("primary"). Ha
///    precedenza se l'azienda ha un refresh token.
///  - Service account (server-to-server): scrive su un calendario condiviso
///    (per-azienda o globale) condiviso con l'email del service account con
///    permesso "Apportare modifiche agli eventi".
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

  private GoogleAuthorizationCodeFlow? _flow;
  private readonly Dictionary<string, CalendarService> _oauthServices = new();

  public GoogleCalendarService(IConfiguration config, ILogger<GoogleCalendarService> logger)
  {
    _config = config;
    _logger = logger;
  }

  private string TimeZone => _config["GoogleCalendar:TimeZone"] ?? "Europe/Rome";

  /// <summary>Calendario globale di fallback (owner), usato se l'azienda non ne ha uno proprio.</summary>
  private string? CalendarIdGlobale => _config["GoogleCalendar:CalendarId"];

  /// <summary>Id azienda se presente, altrimenti il calendario globale.</summary>
  private string? RisolviCalendarId(string? calendarId) =>
      !string.IsNullOrWhiteSpace(calendarId) ? calendarId : CalendarIdGlobale;

  // ===================== OAuth "Connetti Google" (per il capo) =====================

  private string? OAuthClientId => _config["GoogleCalendar:OAuthClientId"];
  private string? OAuthClientSecret => _config["GoogleCalendar:OAuthClientSecret"];

  /// <summary>True se sono configurate le credenziali OAuth (client id/secret).</summary>
  public bool OAuthConfigurato =>
      !string.IsNullOrWhiteSpace(OAuthClientId) && !string.IsNullOrWhiteSpace(OAuthClientSecret);

  private GoogleAuthorizationCodeFlow? GetFlow()
  {
    if (!OAuthConfigurato) return null;
    if (_flow != null) return _flow;
    lock (_lock)
    {
      _flow ??= new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
      {
        ClientSecrets = new ClientSecrets { ClientId = OAuthClientId, ClientSecret = OAuthClientSecret },
        Scopes = new[] { CalendarService.Scope.Calendar }
      });
      return _flow;
    }
  }

  /// <summary>URL a cui reindirizzare il capo per il consenso Google (null se OAuth non configurato).</summary>
  public string? BuildAuthUrl(string redirectUri, string? state)
  {
    if (!OAuthConfigurato) return null;
    var req = new GoogleAuthorizationCodeRequestUrl(new Uri(GoogleAuthConsts.AuthorizationUrl))
    {
      ClientId = OAuthClientId,
      Scope = CalendarService.Scope.Calendar,
      RedirectUri = redirectUri,
      AccessType = "offline",
      Prompt = "consent",
      State = state
    };
    return req.Build().ToString();
  }

  /// <summary>Scambia il codice di autorizzazione con un refresh token (da salvare sull'azienda).</summary>
  public async Task<string?> ScambiaCodiceAsync(string code, string redirectUri)
  {
    var flow = GetFlow();
    if (flow == null) return null;
    try
    {
      var token = await flow.ExchangeCodeForTokenAsync("user", code, redirectUri, CancellationToken.None);
      return token.RefreshToken;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Errore nello scambio del codice OAuth Google");
      return null;
    }
  }

  private CalendarService? GetServiceOAuth(string refreshToken)
  {
    var flow = GetFlow();
    if (flow == null) return null;

    lock (_lock)
    {
      if (_oauthServices.TryGetValue(refreshToken, out var cached)) return cached;
      try
      {
        var credential = new UserCredential(flow, "user", new TokenResponse { RefreshToken = refreshToken });
        var svc = new CalendarService(new BaseClientService.Initializer
        {
          HttpClientInitializer = credential,
          ApplicationName = "Gestione Ferie"
        });
        _oauthServices[refreshToken] = svc;
        return svc;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Errore nella creazione della credenziale OAuth Google");
        return null;
      }
    }
  }

  /// <summary>
  /// Determina dove scrivere gli eventi per un'azienda:
  /// 1) se il capo ha collegato Google (refresh token) → il suo calendario "primary";
  /// 2) altrimenti il calendario condiviso (azienda o globale) via service account.
  /// </summary>
  private (CalendarService? svc, string? calendarId) RisolviTarget(Azienda? azienda)
  {
    if (azienda != null && !string.IsNullOrWhiteSpace(azienda.GoogleRefreshToken) && OAuthConfigurato)
    {
      var svc = GetServiceOAuth(azienda.GoogleRefreshToken!);
      if (svc != null) return (svc, "primary");
    }

    var calId = RisolviCalendarId(azienda?.GoogleCalendarId);
    if (!string.IsNullOrWhiteSpace(calId))
    {
      var svc = GetService();
      if (svc != null) return (svc, calId);
    }

    return (null, null);
  }

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
  public async Task<string?> CreaOAggiornaEventoAsync(RichiestaFerie r, string nome, Azienda? azienda)
  {
    var (svc, calendarId) = RisolviTarget(azienda);
    if (svc == null || string.IsNullOrWhiteSpace(calendarId)) return r.GoogleCalendarEventId;

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
  public async Task RimuoviEventoAsync(string? eventId, Azienda? azienda)
  {
    if (string.IsNullOrWhiteSpace(eventId)) return;
    var (svc, calendarId) = RisolviTarget(azienda);
    if (svc == null || string.IsNullOrWhiteSpace(calendarId)) return;

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
