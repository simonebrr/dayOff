namespace GestioneFerie.Web.Models;

public class LoginAttempt
{
  public int Id { get; set; }
  public string Email { get; set; } = "";
  public string IpAddress { get; set; } = "";
  public DateTime Timestamp { get; set; } = DateTime.UtcNow;
  public bool Successo { get; set; }
}