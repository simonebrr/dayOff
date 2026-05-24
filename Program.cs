using GestioneFerie.Web.Components;
using GestioneFerie.Web.Data;
using GestioneFerie.Web.Services;
using Microsoft.EntityFrameworkCore;
using ApexCharts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite("Data Source=gestioneferie.db"));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddHttpClient();
builder.Services.AddApexCharts();
builder.Services.AddScoped<EmailService>();

builder.Services.AddServerSideBlazor(options =>
{
  options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(30);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Error");
  app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// Endpoint login/logout via cookie HTTP
app.MapPost("/auth/login", async (HttpContext ctx,
    IDbContextFactory<AppDbContext> dbFactory) =>
{
  var form = await ctx.Request.ReadFormAsync();
  var email = form["email"].ToString();
  var password = form["password"].ToString();

  using var db = dbFactory.CreateDbContext();
  var utente = await db.Utenti.FirstOrDefaultAsync(u => u.Email == email);
  if (utente == null || !BCrypt.Net.BCrypt.Verify(password, utente.PasswordHash))
    return Results.Redirect("/login?error=1");

  ctx.Response.Cookies.Append("uid", utente.Id.ToString(),
      new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Strict, MaxAge = TimeSpan.FromDays(7) });
  ctx.Response.Cookies.Append("unome", utente.Nome,
      new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Strict, MaxAge = TimeSpan.FromDays(7) });
  ctx.Response.Cookies.Append("uruolo", utente.Ruolo.ToString(),
      new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Strict, MaxAge = TimeSpan.FromDays(7) });
  ctx.Response.Cookies.Append("uazienda", (utente.AziendaId ?? 0).ToString(),
      new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Strict, MaxAge = TimeSpan.FromDays(7) });

  var redirect = utente.Ruolo.ToString() == "SuperAdmin" ? "/admin" : "/dashboard";
  return Results.Redirect(redirect);
}).DisableAntiforgery();

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