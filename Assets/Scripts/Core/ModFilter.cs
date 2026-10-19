using System;

public static class ModFilter
{
    //Case-insensitive substring match; an empty query matches everything
    public static bool Matches(string name, string query)
    {
        if(string.IsNullOrWhiteSpace(query)) { return true; }
        if(name == null) { return false; }
        return name.IndexOf(query.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
