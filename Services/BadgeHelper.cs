using GestioneFerie.Web.Models;

namespace GestioneFerie.Web.Services;

public static class BadgeHelper
{
  public static string StatoBadgeClass(StatoRichiesta stato) => stato switch
  {
    StatoRichiesta.Approvata => "badge-success",
    StatoRichiesta.Rifiutata => "badge-danger",
    _ => "badge-warning"
  };

  public static string TipoBadgeClass(TipoRichiesta tipo) => tipo switch
  {
    TipoRichiesta.Ferie => "badge-ferie",
    TipoRichiesta.Permesso => "badge-permesso",
    _ => "badge-malattia"
  };
}
