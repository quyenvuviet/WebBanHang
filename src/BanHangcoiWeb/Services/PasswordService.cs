using System.Security.Cryptography;
using System.Text;

namespace BanHangcoiWeb.Services;

public static class PasswordService
{
    public static string Hash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes("BanHangcoi:" + password));
        return Convert.ToHexString(bytes);
    }

    public static bool Verify(string password, string? hash)
    {
        return string.Equals(Hash(password), hash, StringComparison.OrdinalIgnoreCase);
    }
}
