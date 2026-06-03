using System.Text;
using System.Text.Json;

namespace GestioneFerie.Web.Services;

public class EmailService
{
  private readonly IConfiguration _config;
  private readonly ILogger<EmailService> _logger;
  private readonly HttpClient _http;

  public EmailService(IConfiguration config, ILogger<EmailService> logger, IHttpClientFactory httpFactory)
  {
    _config = config;
    _logger = logger;
    _http = httpFactory.CreateClient();
  }

  public async Task InviaAsync(string destinatario, string oggetto, string corpo)
  {
    try
    {
      var apiKey = _config["Brevo:ApiKey"];
      var from = _config["Email:From"] ?? "sistema.ferie@gmail.com";

      _logger.LogInformation("Invio email a {Destinatario} via Brevo API", destinatario);

      var payload = new
      {
        sender = new { email = from, name = "Gestione Ferie" },
        to = new[] { new { email = destinatario } },
        subject = oggetto,
        htmlContent = corpo
      };

      var json = JsonSerializer.Serialize(payload);
      var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
      request.Headers.Add("api-key", apiKey);
      request.Content = new StringContent(json, Encoding.UTF8, "application/json");

      var response = await _http.SendAsync(request);
      var responseBody = await response.Content.ReadAsStringAsync();

      if (response.IsSuccessStatusCode)
        _logger.LogInformation("Email inviata con successo a {Destinatario}", destinatario);
      else
        _logger.LogError("Errore Brevo API: {Status} - {Body}", response.StatusCode, responseBody);
    }
    catch (Exception ex)
    {
      _logger.LogError("Errore invio email a {Destinatario}: {Errore}", destinatario, ex.Message);
    }
  }
}