using System.Net;
using System.Net.Mail;

namespace GestioneFerie.Web.Services;

public class EmailService
{
  private readonly IConfiguration _config;
  private readonly ILogger<EmailService> _logger;

  public EmailService(IConfiguration config, ILogger<EmailService> logger)
  {
    _config = config;
    _logger = logger;
  }

  public async Task InviaAsync(string destinatario, string oggetto, string corpo)
  {
    try
    {
      // Convenzione SMTP generica (Email:*), con fallback ai vecchi nomi Brevo:* per compatibilità.
      var host = _config["Email:Host"] ?? _config["Brevo:SmtpHost"] ?? "smtp-relay.brevo.com";
      var port = int.TryParse(_config["Email:Port"] ?? _config["Brevo:SmtpPort"], out var p) ? p : 587;
      var login = _config["Email:Username"] ?? _config["Brevo:SmtpLogin"];
      var password = _config["Email:Password"] ?? _config["Brevo:SmtpKey"] ?? _config["Brevo:ApiKey"];
      var from = _config["Email:From"] ?? "sistema.ferie@gmail.com";

      if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
      {
        _logger.LogWarning("SMTP Brevo non configurato: email a {Destinatario} non inviata", destinatario);
        return;
      }

      _logger.LogInformation("Invio email a {Destinatario} via SMTP Brevo", destinatario);

      using var message = new MailMessage
      {
        From = new MailAddress(from, "Gestione Ferie"),
        Subject = oggetto,
        Body = corpo,
        IsBodyHtml = true
      };
      message.To.Add(destinatario);

      using var client = new SmtpClient(host, port)
      {
        EnableSsl = true, // STARTTLS sulla porta 587
        DeliveryMethod = SmtpDeliveryMethod.Network,
        Credentials = new NetworkCredential(login, password)
      };

      await client.SendMailAsync(message);
      _logger.LogInformation("Email inviata con successo a {Destinatario}", destinatario);
    }
    catch (Exception ex)
    {
      _logger.LogError("Errore invio email a {Destinatario}: {Errore}", destinatario, ex.Message);
    }
  }
}
