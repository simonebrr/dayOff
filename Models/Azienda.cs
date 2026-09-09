namespace GestioneFerie.Web.Models;

public class Azienda
{
  public int Id { get; set; }
  public string Nome { get; set; } = "";
  public string? EmailMalattia { get; set; }
  public string? GoogleCalendarId { get; set; }
  public int? CapoId { get; set; }
  public Utente? Capo { get; set; }
  public ICollection<Utente> Utenti { get; set; } = new List<Utente>();
}
