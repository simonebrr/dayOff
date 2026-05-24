using GestioneFerie.Web.Models;
namespace GestioneFerie.Web.Data;

public static class DbSeeder
{
  public static async Task SeedAsync(AppDbContext db)
  {
    // SuperAdmin (system owner)
    if (!db.Utenti.Any(u => u.Email == "purp7epi3@gmail.com"))
    {
      db.Utenti.Add(new Utente
      {
        Nome = "Mario",
        Cognome = "Rossi",
        Email = "purp7epi3@gmail.com",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
        Ruolo = Ruolo.SuperAdmin
      });
      await db.SaveChangesAsync();
    }
    else
    {
      // Migrate existing user to SuperAdmin
      var mario = db.Utenti.First(u => u.Email == "purp7epi3@gmail.com");
      if (mario.Ruolo != Ruolo.SuperAdmin)
      {
        mario.Ruolo = Ruolo.SuperAdmin;
        mario.AziendaId = null;
        await db.SaveChangesAsync();
      }
    }

    // Demo company boss
    if (!db.Utenti.Any(u => u.Email == "capo@demo.it"))
    {
      db.Utenti.Add(new Utente
      {
        Nome = "Giulia",
        Cognome = "Ferrari",
        Email = "capo@demo.it",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
        Ruolo = Ruolo.Capo
      });
      await db.SaveChangesAsync();
    }

    if (!db.Utenti.Any(u => u.Email == "brunelli01@gmail.com"))
    {
      db.Utenti.Add(new Utente
      {
        Nome = "Luca",
        Cognome = "Bianchi",
        Email = "brunelli01@gmail.com",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
        Ruolo = Ruolo.Dipendente
      });
      await db.SaveChangesAsync();
    }

    if (!db.Utenti.Any(u => u.Email == "dip2@azienda.it"))
    {
      db.Utenti.Add(new Utente
      {
        Nome = "Anna",
        Cognome = "Verdi",
        Email = "dip2@azienda.it",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
        Ruolo = Ruolo.Dipendente
      });
      await db.SaveChangesAsync();
    }

    var capo = db.Utenti.First(u => u.Email == "capo@demo.it");
    var luca = db.Utenti.First(u => u.Email == "brunelli01@gmail.com");
    var anna = db.Utenti.First(u => u.Email == "dip2@azienda.it");

    // Create default demo company if not exists
    if (!db.Aziende.Any())
    {
      var azienda = new Azienda { Nome = "Azienda Demo", CapoId = capo.Id };
      db.Aziende.Add(azienda);
      await db.SaveChangesAsync();

      capo.AziendaId = azienda.Id;
      luca.AziendaId = azienda.Id;
      anna.AziendaId = azienda.Id;
      await db.SaveChangesAsync();
    }
    else
    {
      // If demo company exists but has Mario as capo (old seed), fix it
      var demoAzienda = db.Aziende.First();
      var mario = db.Utenti.First(u => u.Email == "purp7epi3@gmail.com");
      if (demoAzienda.CapoId == mario.Id)
      {
        demoAzienda.CapoId = capo.Id;
        capo.AziendaId = demoAzienda.Id;
        if (luca.AziendaId == null) luca.AziendaId = demoAzienda.Id;
        if (anna.AziendaId == null) anna.AziendaId = demoAzienda.Id;
        await db.SaveChangesAsync();
      }
    }

    if (!db.SaldoFerie.Any(s => s.UtenteId == luca.Id))
      db.SaldoFerie.Add(new SaldoFerie { UtenteId = luca.Id, OreTotali = 160, OreUsate = 0, Anno = DateTime.Now.Year });

    if (!db.SaldoFerie.Any(s => s.UtenteId == anna.Id))
      db.SaldoFerie.Add(new SaldoFerie { UtenteId = anna.Id, OreTotali = 160, OreUsate = 0, Anno = DateTime.Now.Year });

    if (!db.RichiestaFerie.Any())
    {
      db.RichiestaFerie.Add(new RichiestaFerie
      {
        UtenteId = luca.Id, Tipo = TipoRichiesta.Ferie,
        DataInizio = new DateTime(DateTime.Now.Year, 6, 1),
        DataFine = new DateTime(DateTime.Now.Year, 6, 5),
        OreRichieste = 40, Stato = StatoRichiesta.InAttesa, DataRichiesta = DateTime.Now
      });
      db.RichiestaFerie.Add(new RichiestaFerie
      {
        UtenteId = anna.Id, Tipo = TipoRichiesta.Permesso,
        DataInizio = new DateTime(DateTime.Now.Year, 5, 20),
        DataFine = new DateTime(DateTime.Now.Year, 5, 20),
        OreRichieste = 4, Stato = StatoRichiesta.InAttesa, DataRichiesta = DateTime.Now
      });
    }

    await db.SaveChangesAsync();
  }
}
