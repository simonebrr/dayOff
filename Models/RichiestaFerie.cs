namespace GestioneFerie.Web.Models;

public class RichiestaFerie
{
  public int Id { get; set; }
  public int UtenteId { get; set; }
  public Utente Utente { get; set; } = null!;
  public TipoRichiesta Tipo { get; set; }
  public string? CodiceMalattia { get; set; }
  public DateTime DataInizio { get; set; }
  public DateTime DataFine { get; set; }
  public decimal OreRichieste { get; set; }
  public string? Note { get; set; }
  public StatoRichiesta Stato { get; set; } = StatoRichiesta.InAttesa;
  public DateTime DataRichiesta { get; set; } = DateTime.Now;
  public string? MotivoRifiuto { get; set; }
  public string? GoogleCalendarEventId { get; set; }
}

public enum TipoRichiesta
{
  Ferie,
  Permesso,
  Malattia
}
public enum StatoRichiesta
{
  InAttesa,
  Approvata,
  Rifiutata
}