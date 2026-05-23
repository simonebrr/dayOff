namespace GestioneFerie.Web.Models;

public class SaldoFerie
{
  public int Id { get; set; }
  public int UtenteId { get; set; }
  public Utente Utente { get; set; } = null!;
  public decimal OreTotali { get; set; }
  public decimal OreUsate { get; set; }
  public decimal OreDisponibili => OreTotali - OreUsate;
  public int Anno { get; set; } = DateTime.Now.Year;
}