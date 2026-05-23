namespace GestioneFerie.Web.Models;

public class Utente
{
  public int Id { get; set; }
  public string Nome { get; set; } = "";
  public string Cognome { get; set; } = "";
  public string Email { get; set; } = "";
  public string PasswordHash { get; set; } = "";
  public Ruolo Ruolo { get; set; }
  public ICollection<RichiestaFerie> Richieste { get; set; } = new List<RichiestaFerie>();
  public SaldoFerie? SaldoFerie { get; set; }
}

public enum Ruolo
{
  Dipendente,
  Capo
}