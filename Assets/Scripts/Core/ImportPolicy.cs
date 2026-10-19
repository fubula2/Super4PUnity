public static class ImportPolicy
{
    //Import without asking only when the pack holds exactly one character and the sheet names its characters
    public static bool NeedsPrompt(int charCount, bool namesKnown)
    {
        return charCount != 1 || !namesKnown;
    }
}

public static class CharDisplayName
{
    private const string Prefix = "CHAR_";

    //Same text the Chars list shows: the folder name without its CHAR_ prefix
    public static string From(string dirName)
    {
        if(string.IsNullOrEmpty(dirName)) { return string.Empty; }
        return dirName.StartsWith(Prefix, System.StringComparison.Ordinal) ? dirName.Substring(Prefix.Length) : dirName;
    }
}
