using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

public static class Sha256Check
{
    public static bool Matches(Stream data, string expectedHex)
    {
        if(data == null || string.IsNullOrWhiteSpace(expectedHex)) { return false; }
        using(var sha = SHA256.Create())
        {
            var hex = string.Concat(sha.ComputeHash(data).Select(b => b.ToString("x2")));
            return string.Equals(hex, expectedHex.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
