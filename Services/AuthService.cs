using GestioneFerie.Web.Data;
using GestioneFerie.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GestioneFerie.Web.Services;

public class AuthService
{
  private readonly IDbContextFactory<AppDbContext> _dbFactory;
  private readonly SessionService _session;

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

  public Task LoadSessionAsync()
  {
    return _session.LoadAsync();
  }
}