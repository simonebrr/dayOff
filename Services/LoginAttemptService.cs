using GestioneFerie.Web.Data;
using GestioneFerie.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GestioneFerie.Web.Services;

public class LoginAttemptService
{
  private const int MaxTentativiFalliti = 5;
  private static readonly TimeSpan FinestraBlocco = TimeSpan.FromMinutes(15);

  private readonly IDbContextFactory<AppDbContext> _dbFactory;

  public LoginAttemptService(IDbContextFactory<AppDbContext> dbFactory)
  {
    _dbFactory = dbFactory;
  }

  public async Task<bool> IsBloccatoAsync(string email, string ipAddress)
  {
    using var db = _dbFactory.CreateDbContext();
    var da = DateTime.UtcNow - FinestraBlocco;

    var falliti = await db.LoginAttempts.CountAsync(a =>
        !a.Successo && a.Timestamp >= da && (a.Email == email || a.IpAddress == ipAddress));

    return falliti >= MaxTentativiFalliti;
  }

  public async Task RegistraAsync(string email, string ipAddress, bool successo)
  {
    using var db = _dbFactory.CreateDbContext();
    db.LoginAttempts.Add(new LoginAttempt
    {
      Email = email,
      IpAddress = ipAddress,
      Timestamp = DateTime.UtcNow,
      Successo = successo
    });
    await db.SaveChangesAsync();

    if (successo)
    {
      var vecchiFalliti = await db.LoginAttempts
          .Where(a => a.Email == email && !a.Successo)
          .ToListAsync();
      if (vecchiFalliti.Count > 0)
      {
        db.LoginAttempts.RemoveRange(vecchiFalliti);
        await db.SaveChangesAsync();
      }
    }
  }
}
