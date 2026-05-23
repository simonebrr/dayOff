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

builder.Services.AddSingleton<AuthService>();
builder.Services.AddHttpClient();
builder.Services.AddApexCharts();
builder.Services.AddScoped<EmailService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Error");
  app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

using (var scope = app.Services.CreateScope())
{
  var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
  using var db = dbFactory.CreateDbContext();
  db.Database.Migrate();
  await DbSeeder.SeedAsync(db);
}

app.MapGet("/logout", (HttpContext ctx) =>
{
  return Results.Redirect("/login");
});

app.Run();