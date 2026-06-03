using MailKit.Net.Smtp;
using MimeKit;

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
      var host = _config["Email:Host"];
      var port = int.Parse(_config["Email:Port"]!);
      var username = _config["Email:Username"];
      var password = _config["Email:Password"];
      var from = _config["Email:From"];

      _logger.LogInformation("Invio email a {Destinatario} via {Host}:{Port}", destinatario, host, port);

      var email = new MimeMessage();
      email.From.Add(MailboxAddress.Parse(from));
      email.To.Add(MailboxAddress.Parse(destinatario));
      email.Subject = oggetto;
      email.Body = new TextPart("html") { Text = corpo };

      using var smtp = new SmtpClient();
      await smtp.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTls);
      await smtp.AuthenticateAsync(username, password);
      await smtp.SendAsync(email);
      await smtp.DisconnectAsync(true);

      _logger.LogInformation("Email inviata con successo a {Destinatario}", destinatario);
    }
    catch (Exception ex)
    {
      _logger.LogError("Errore invio email a {Destinatario}: {Errore}", destinatario, ex.Message);
    }
  }
}