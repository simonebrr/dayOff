namespace GestioneFerie.Web.Models;

public class LogModificaSaldo
{
  public int Id { get; set; }
  public int UtenteId { get; set; }
  public Utente Utente { get; set; } = null!;
  public decimal OrePrecedenti { get; set; }
  public decimal OreNuove { get; set; }
  public DateTime DataModifica { get; set; } = DateTime.Now;
  public string ModificatoDa { get; set; } = "";
  public int Anno { get; set; }
}