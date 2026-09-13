using GestioneFerie.Web.Components;
using GestioneFerie.Web.Data;
using GestioneFerie.Web.Services;
using Microsoft.EntityFrameworkCore;
using ApexCharts;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// --- MODIFICA 1: path DB configurabile via variabile d'ambiente ---
// In locale usa "gestioneferie.db" (default), su Fly imposta DB_PATH=/data/gestioneferie.db
var dbPath = Environment.GetEnvironmentVariable("DB_PATH") ?? "gestioneferie.db";

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// --- MODIFICA 2: Data Protection Keys persistite su volume ---
// In locale usa la cartella "keys" già esistente nel progetto, su Fly usa /data/keys
var keysPath = Environment.GetEnvironmentVariable("KEYS_PATH") ?? "keys";
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddHttpClient();
builder.Services.AddApexCharts();
builder.Services.AddScoped<EmailService>();
builder.Services.AddSingleton<GoogleCalendarService>();
builder.Services.AddScoped<LoginAttemptService>();
builder.Services.AddScoped<RichiesteService>();
builder.Services.AddScoped<DipendentiService>();
builder.Services.AddScoped<AziendeService>();

builder.Services.AddServerSideBlazor(options =>
{
  options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(30);
});

// Rate limiter globale: max 200 richieste/minuto per IP (gli asset statici sono
// esentati perché serviti prima nel pipeline). Protegge da abusi/DoS di base.
builder.Services.AddRateLimiter(options =>
{
  options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
  options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
      RateLimitPartition.GetFixedWindowLimiter(
          http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
          _ => new FixedWindowRateLimiterOptions
          {
            PermitLimit = 200,
            Window = TimeSpan.FromMinutes(1)
          }));
});

builder.Services.Configure<CookiePolicyOptions>(options =>
{
  options.MinimumSameSitePolicy = SameSiteMode.None;
  options.Secure = CookieSecurePolicy.Always;
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
  options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
  options.KnownIPNetworks.Clear();
  options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseWebSockets();
app.UseStaticFiles();
app.UseRateLimiter();
app.UseCookiePolicy();
app.UseAntiforgery();

app.MapGet("/", () => Results.Redirect("/login"));

app.MapPost("/auth/login", async (HttpContext ctx,
    IDbContextFactory<AppDbContext> dbFactory,
    LoginAttemptService loginAttempts) =>
{
  var form = await ctx.Request.ReadFormAsync();
  var email = form["email"].ToString();
  var password = form["password"].ToString();
  var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "sconosciuto";

  if (await loginAttempts.IsBloccatoAsync(email, ip))
    return Results.Redirect("/login?error=blocked");

  using var db = dbFactory.CreateDbContext();
  var utente = await db.Utenti.FirstOrDefaultAsync(u => u.Email == email);

  if (utente == null || !BCrypt.Net.BCrypt.Verify(password, utente.PasswordHash))
  {
    await loginAttempts.RegistraAsync(email, ip, successo: false);
    return Results.Redirect("/login?error=1");
  }

  await loginAttempts.RegistraAsync(email, ip, successo: true);

  var cookieOptions = new CookieOptions
  {
    HttpOnly = true,
    SameSite = SameSiteMode.None,
    Secure = true,
    MaxAge = TimeSpan.FromDays(7)
  };

  ctx.Response.Cookies.Append("uid", utente.Id.ToString(), cookieOptions);
  ctx.Response.Cookies.Append("unome", utente.Nome, cookieOptions);
  ctx.Response.Cookies.Append("uruolo", utente.Ruolo.ToString(), cookieOptions);
  ctx.Response.Cookies.Append("uazienda", (utente.AziendaId ?? 0).ToString(), cookieOptions);

  var redirect = utente.Ruolo.ToString() == "SuperAdmin" ? "/admin" : "/dashboard";
  return Results.Redirect(redirect);
});

app.MapGet("/auth/logout", (HttpContext ctx) =>
{
  ctx.Response.Cookies.Delete("uid");
  ctx.Response.Cookies.Delete("unome");
  ctx.Response.Cookies.Delete("uruolo");
  ctx.Response.Cookies.Delete("uazienda");
  return Results.Redirect("/login");
});

// --- OAuth Google Calendar (collegamento account del capo) ---
app.MapGet("/google/connect", (HttpContext ctx, GoogleCalendarService gcal) =>
{
  if (ctx.Request.Cookies["uruolo"] != "Capo") return Results.Redirect("/login");
  if (!gcal.OAuthConfigurato) return Results.Redirect("/profilo?gerror=config");

  var redirectUri = $"{ctx.Request.Scheme}://{ctx.Request.Host}/google/callback";
  var url = gcal.BuildAuthUrl(redirectUri, null);
  return string.IsNullOrEmpty(url) ? Results.Redirect("/profilo?gerror=config") : Results.Redirect(url);
});

app.MapGet("/google/callback", async (HttpContext ctx,
    GoogleCalendarService gcal, IDbContextFactory<AppDbContext> dbFactory) =>
{
  if (ctx.Request.Cookies["uruolo"] != "Capo" ||
      !int.TryParse(ctx.Request.Cookies["uazienda"], out var aziendaId) || aziendaId == 0)
    return Results.Redirect("/login");

  var code = ctx.Request.Query["code"].ToString();
  if (string.IsNullOrEmpty(code)) return Results.Redirect("/profilo?gerror=1");

  var redirectUri = $"{ctx.Request.Scheme}://{ctx.Request.Host}/google/callback";
  var refresh = await gcal.ScambiaCodiceAsync(code, redirectUri);
  if (string.IsNullOrEmpty(refresh)) return Results.Redirect("/profilo?gerror=1");

  using var db = dbFactory.CreateDbContext();
  var azienda = await db.Aziende.FindAsync(aziendaId);
  if (azienda != null)
  {
    azienda.GoogleRefreshToken = refresh;
    await db.SaveChangesAsync();
  }
  return Results.Redirect("/profilo?gok=1");
});

app.MapGet("/google/disconnect", async (HttpContext ctx, IDbContextFactory<AppDbContext> dbFactory) =>
{
  if (ctx.Request.Cookies["uruolo"] != "Capo" ||
      !int.TryParse(ctx.Request.Cookies["uazienda"], out var aziendaId) || aziendaId == 0)
    return Results.Redirect("/login");

  using var db = dbFactory.CreateDbContext();
  var azienda = await db.Aziende.FindAsync(aziendaId);
  if (azienda != null)
  {
    azienda.GoogleRefreshToken = null;
    await db.SaveChangesAsync();
  }
  return Results.Redirect("/profilo?gdisc=1");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

using (var scope = app.Services.CreateScope())
{
  var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
  using var db = dbFactory.CreateDbContext();
  db.Database.Migrate();
  await DbSeeder.SeedAsync(db);
}

app.Run();