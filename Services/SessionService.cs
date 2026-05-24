using Microsoft.AspNetCore.Http;

namespace GestioneFerie.Web.Services;

public class SessionService
{
  private readonly IHttpContextAccessor _http;

  public int? UtenteId { get; private set; }
  public string? UtenteNome { get; private set; }
  public string? UtenteRuolo { get; private set; }
  public int? AziendaId { get; private set; }

  public bool IsAuthenticated => UtenteId != null && UtenteId != 0;
  public bool IsCapo => UtenteRuolo == "Capo";
  public bool IsSuperAdmin => UtenteRuolo == "SuperAdmin";

  public event Action? OnChange;

  public SessionService(IHttpContextAccessor http)
  {
    _http = http;
    CaricaDaCookie();
  }

  private void CaricaDaCookie()
  {
    var cookies = _http.HttpContext?.Request.Cookies;
    if (cookies == null) return;

    if (cookies.TryGetValue("uid", out var uid) && int.TryParse(uid, out var id) && id != 0)
    {
      UtenteId = id;
      UtenteNome = cookies.TryGetValue("unome", out var nome) ? nome : null;
      UtenteRuolo = cookies.TryGetValue("uruolo", out var ruolo) ? ruolo : null;
      AziendaId = cookies.TryGetValue("uazienda", out var az) && int.TryParse(az, out var azId) && azId != 0
          ? azId : null;
    }
  }

  public Task LoadAsync()
  {
    CaricaDaCookie();
    OnChange?.Invoke();
    return Task.CompletedTask;
  }

  public Task SetAsync(int id, string nome, string ruolo, int? aziendaId)
  {
    UtenteId = id;
    UtenteNome = nome;
    UtenteRuolo = ruolo;
    AziendaId = aziendaId;
    OnChange?.Invoke();
    return Task.CompletedTask;
  }

  public Task ClearAsync()
  {
    UtenteId = null;
    UtenteNome = null;
    UtenteRuolo = null;
    AziendaId = null;
    OnChange?.Invoke();
    return Task.CompletedTask;
  }

  public void NotifyChange() => OnChange?.Invoke();
}