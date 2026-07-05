using System.Security.Cryptography;
using System.Text;

namespace Stalika.Web.Services;

public static class PasswordHasher
{
    public static string HashPassword(string input)
    {
        using var sha512 = SHA512.Create();
        byte[] data = sha512.ComputeHash(Encoding.UTF8.GetBytes(input));

        StringBuilder sb = new StringBuilder();
        foreach (byte b in data)
        {
            sb.Append(b.ToString("x2"));
        }

        return sb.ToString();
    }

    public static bool VerifyPassword(string input, string storedHash)
    {
        var inputHash = HashPassword(input);
        return string.Equals(inputHash, storedHash, StringComparison.OrdinalIgnoreCase);
    }
}