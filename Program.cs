using GestioneFerie.Web.Components;
using GestioneFerie.Web.Data;
using GestioneFerie.Web.Services;
using Microsoft.EntityFrameworkCore;
using ApexCharts;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;

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
builder.Services.AddScoped<LoginAttemptService>();
builder.Services.AddScoped<RichiesteService>();
builder.Services.AddScoped<DipendentiService>();
builder.Services.AddScoped<AziendeService>();

builder.Services.AddServerSideBlazor(options =>
{
  options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(30);
});

builder.Services.Configure<CookiePolicyOptions>(options =>
{
  options.MinimumSameSitePolicy = SameSiteMode.None;
  options.Secure = CookieSecurePolicy.Always;
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
  options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
  options.KnownNetworks.Clear();
  options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseWebSockets();
app.UseStaticFiles();
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