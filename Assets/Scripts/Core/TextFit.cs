using System;

//Keeps long names readable in fixed-size UI: shortens the middle and keeps both ends (a pack's version is at its end)
public static class TextFit
{
    private const string Ellipsis = "...";

    public static string MiddleEllipsis(string text, int maxChars)
    {
        if(string.IsNullOrEmpty(text)) { return string.Empty; }
        if(text.Length <= maxChars || maxChars <= Ellipsis.Length + 1) { return text; }

        var keep = maxChars - Ellipsis.Length;
        var head = (keep + 1) / 2;
        var tail = keep / 2;
        return text.Substring(0, head) + Ellipsis + text.Substring(text.Length - tail);
    }

    //A pack file name as shown to the player: no .zip/.rar, shortened in the middle when long
    public static string PackLabel(string fileName, int maxChars = 44)
    {
        if(string.IsNullOrEmpty(fileName)) { return string.Empty; }

        var name = fileName;
        if(name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".rar", StringComparison.OrdinalIgnoreCase))
        {
            name = name.Substring(0, name.Length - 4);
        }
        return MiddleEllipsis(name, maxChars);
    }
}
