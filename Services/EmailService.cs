using MailKit.Net.Smtp;
using MimeKit;

namespace GestioneFerie.Web.Services;

public class EmailService
{
  private readonly IConfiguration _config;

  public EmailService(IConfiguration config)
  {
    _config = config;
  }

  public async Task InviaAsync(string destinatario, string oggetto, string corpo)
  {
    try
    {
      var email = new MimeMessage();
      email.From.Add(MailboxAddress.Parse(_config["Email:From"]));
      email.To.Add(MailboxAddress.Parse(destinatario));
      email.Subject = oggetto;
      email.Body = new TextPart("html") { Text = corpo };

      using var smtp = new SmtpClient();
      await smtp.ConnectAsync(_config["Email:Host"],
          int.Parse(_config["Email:Port"]!),
          MailKit.Security.SecureSocketOptions.StartTls);
      await smtp.AuthenticateAsync(_config["Email:Username"], _config["Email:Password"]);
      await smtp.SendAsync(email);
      await smtp.DisconnectAsync(true);
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Errore invio email: {ex.Message}");
    }
  }
}