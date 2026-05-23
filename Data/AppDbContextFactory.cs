using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GestioneFerie.Web.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
  public AppDbContext CreateDbContext(string[] args)
  {
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlite("Data Source=gestioneferie.db")
        .Options;
    return new AppDbContext(options);
  }
}