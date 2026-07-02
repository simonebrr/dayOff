namespace GestioneFerie.Web.Services;

public static class HolidayCalculator
{
  public static HashSet<DateTime> FestivitaItaliane(int anno)
  {
    var festivi = new HashSet<DateTime>
    {
      new DateTime(anno, 1, 1),
      new DateTime(anno, 1, 6),
      new DateTime(anno, 4, 25),
      new DateTime(anno, 5, 1),
      new DateTime(anno, 6, 2),
      new DateTime(anno, 8, 15),
      new DateTime(anno, 11, 1),
      new DateTime(anno, 12, 8),
      new DateTime(anno, 12, 25),
      new DateTime(anno, 12, 26),
    };

    var pasqua = CalcolaPasqua(anno);
    festivi.Add(pasqua);
    festivi.Add(pasqua.AddDays(1));
    return festivi;
  }

  public static int GiorniLavorativi(DateTime dataInizio, DateTime dataFine)
  {
    if (dataFine.Date < dataInizio.Date) return 0;

    var festivi = FestivitaItaliane(dataInizio.Year);
    if (dataFine.Year != dataInizio.Year)
      festivi.UnionWith(FestivitaItaliane(dataFine.Year));

    var giorni = 0;
    for (var d = dataInizio.Date; d <= dataFine.Date; d = d.AddDays(1))
    {
      var isWeekend = d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday;
      if (!isWeekend && !festivi.Contains(d.Date)) giorni++;
    }
    return giorni;
  }

  public static DateTime CalcolaPasqua(int anno)
  {
    int a = anno % 19, b = anno / 100, c = anno % 100;
    int d = b / 4, e = b % 4, f = (b + 8) / 25;
    int g = (b - f + 1) / 3;
    int h = (19 * a + b - d - g + 15) % 30;
    int i = c / 4, k = c % 4;
    int l = (32 + 2 * e + 2 * i - h - k) % 7;
    int m = (a + 11 * h + 22 * l) / 451;
    int mese = (h + l - 7 * m + 114) / 31;
    int giorno = ((h + l - 7 * m + 114) % 31) + 1;
    return new DateTime(anno, mese, giorno);
  }
}
