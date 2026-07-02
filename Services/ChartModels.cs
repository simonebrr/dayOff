namespace GestioneFerie.Web.Services;

public record SaldoItem(string Label = "", double Valore = 0);
public record OreDipendente(string Nome = "", double OreUsate = 0, double OreDisponibili = 0);
public record RichiestaMese(string Mese = "", int Conteggio = 0);
