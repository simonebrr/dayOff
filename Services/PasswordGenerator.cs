using System.Security.Cryptography;

namespace GestioneFerie.Web.Services;

public static class PasswordGenerator
{
  private const string Caratteri = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$";

  public static string Genera(int lunghezza = 12)
  {
    var bytes = RandomNumberGenerator.GetBytes(lunghezza);
    var chars = new char[lunghezza];
    for (var i = 0; i < lunghezza; i++)
      chars[i] = Caratteri[bytes[i] % Caratteri.Length];
    return new string(chars);
  }
}
