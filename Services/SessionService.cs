using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace GestioneFerie.Web.Services;

public class SessionService
{
  private readonly ProtectedSessionStorage _storage;

  public int? UtenteId { get; private set; }
  public string? UtenteNome { get; private set; }
  public string? UtenteRuolo { get; private set; }
  public int? AziendaId { get; private set; }

  public bool IsAuthenticated => UtenteId != null;
  public bool IsCapo => UtenteRuolo == "Capo";
  public bool IsSuperAdmin => UtenteRuolo == "SuperAdmin";

  public event Action? OnChange;

  public SessionService(ProtectedSessionStorage storage)
  {
    _storage = storage;
  }

  public async Task SetAsync(int id, string nome, string ruolo, int? aziendaId)
  {
    UtenteId = id;
    UtenteNome = nome;
    UtenteRuolo = ruolo;
    AziendaId = aziendaId;

    await _storage.SetAsync("uid", id);
    await _storage.SetAsync("unome", nome);
    await _storage.SetAsync("uruolo", ruolo);
    await _storage.SetAsync("uazienda", aziendaId ?? 0);

    OnChange?.Invoke();
  }

  public async Task LoadAsync()
  {
    try
    {
      var id = await _storage.GetAsync<int>("uid");
      var nome = await _storage.GetAsync<string>("unome");
      var ruolo = await _storage.GetAsync<string>("uruolo");
      var azienda = await _storage.GetAsync<int>("uazienda");

      if (id.Success && id.Value != 0)
      {
        UtenteId = id.Value;
        UtenteNome = nome.Success ? nome.Value : null;
        UtenteRuolo = ruolo.Success ? ruolo.Value : null;
        AziendaId = azienda.Success && azienda.Value != 0 ? azienda.Value : null;
      }
    }
    catch { /* prima navigazione, storage vuoto */ }
  }

  public async Task ClearAsync()
  {
    UtenteId = null;
    UtenteNome = null;
    UtenteRuolo = null;
    AziendaId = null;

    await _storage.DeleteAsync("uid");
    await _storage.DeleteAsync("unome");
    await _storage.DeleteAsync("uruolo");
    await _storage.DeleteAsync("uazienda");

    OnChange?.Invoke();
  }

  public void NotifyChange() => OnChange?.Invoke();
}