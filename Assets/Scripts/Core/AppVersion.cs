using System;
using System.Linq;
using System.Text.RegularExpressions;

public static class AppVersion
{
    private static readonly Regex Dotted = new Regex(@"^\d+(\.\d+){0,3}$");

    //"v1.2.3-beta+5" -> "1.2.3"; null when it is not a dotted number
    public static string Normalize(string raw)
    {
        if(string.IsNullOrWhiteSpace(raw)) { return null; }
        var text = raw.Trim();
        if(text.StartsWith("v") || text.StartsWith("V")) { text = text.Substring(1); }
        var cut = text.IndexOfAny(new[] { '-', '+' });
        if(cut >= 0) { text = text.Substring(0, cut); }
        return Dotted.IsMatch(text) ? text : null;
    }

    //True only when both are valid and latest is strictly newer; missing parts count as 0 ("1.2" == "1.2.0")
    public static bool IsNewer(string latest, string current)
    {
        var a = Parts(latest);
        var b = Parts(current);
        if(a == null || b == null) { return false; }

        for(var i = 0; i < 4; i++)
        {
            if(a[i] != b[i]) { return a[i] > b[i]; }
        }
        return false;
    }

    private static int[] Parts(string version)
    {
        var normalized = Normalize(version);
        if(normalized == null) { return null; }

        var parts = normalized.Split('.').Select(p => int.TryParse(p, out var n) ? n : -1).ToList();
        if(parts.Any(n => n < 0)) { return null; }
        while(parts.Count < 4) { parts.Add(0); }
        return parts.ToArray();
    }
}
