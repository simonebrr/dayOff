using System.Text;
using System.Text.Json;

namespace GestioneFerie.Web.Services;

public class EmailService
{
  private readonly IConfiguration _config;
  private readonly ILogger<EmailService> _logger;
  private readonly IHttpClientFactory _httpFactory;

  public EmailService(IConfiguration config, ILogger<EmailService> logger, IHttpClientFactory httpFactory)
  {
    _config = config;
    _logger = logger;
    _httpFactory = httpFactory;
  }

  public async Task InviaAsync(string destinatario, string oggetto, string corpo)
  {
    try
    {
      // API HTTP di Brevo (HTTPS/443): Railway blocca le porte SMTP → timeout.
      // Serve una API key Brevo "xkeysib-..." (NON la chiave SMTP).
      var apiKey = _config["Brevo:ApiKey"] ?? _config["Email:Password"];
      var from = _config["Email:From"] ?? "sistema.ferie@gmail.com";

      if (string.IsNullOrWhiteSpace(apiKey))
      {
        _logger.LogWarning("Brevo API key mancante: email a {Destinatario} non inviata", destinatario);
        return;
      }

      var payload = new
      {
        sender = new { email = from, name = "Gestione Ferie" },
        to = new[] { new { email = destinatario } },
        subject = oggetto,
        htmlContent = corpo
      };

      var http = _httpFactory.CreateClient();
      http.Timeout = TimeSpan.FromSeconds(20);

      using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
      request.Headers.Add("api-key", apiKey);
      request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

      var response = await http.SendAsync(request);
      var body = await response.Content.ReadAsStringAsync();

      if (response.IsSuccessStatusCode)
        _logger.LogInformation("Email inviata con successo a {Destinatario}", destinatario);
      else
        _logger.LogError("Errore Brevo API: {Status} - {Body}", response.StatusCode, body);
    }
    catch (Exception ex)
    {
      _logger.LogError("Errore invio email a {Destinatario}: {Errore}", destinatario, ex.Message);
    }
  }
}