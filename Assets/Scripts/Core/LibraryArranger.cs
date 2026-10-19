using System;
using System.Collections.Generic;
using System.Linq;

public enum SortMode { Sheet, Newest, Oldest }

public sealed class LibraryGroup
{
    public string Heading;
    public List<LibraryEntry> Entries = new List<LibraryEntry>();
}

public static class LibraryArranger
{
    public const string UnknownDate = "Unknown date";

    //Sheet: keep order, grouped by consecutive category. Newest/Oldest: grouped by year, undated last.
    public static IReadOnlyList<LibraryGroup> Arrange(IReadOnlyList<LibraryEntry> entries, SortMode mode)
    {
        return mode == SortMode.Sheet ? BySheet(entries) : ByYear(entries, mode == SortMode.Newest);
    }

    private static IReadOnlyList<LibraryGroup> BySheet(IReadOnlyList<LibraryEntry> entries)
    {
        var groups = new List<LibraryGroup>();
        foreach(var entry in entries)
        {
            var heading = entry.Category ?? string.Empty;
            if(groups.Count == 0 || groups[groups.Count - 1].Heading != heading) { groups.Add(new LibraryGroup { Heading = heading }); }
            groups[groups.Count - 1].Entries.Add(entry);
        }
        return groups;
    }

    private static IReadOnlyList<LibraryGroup> ByYear(IReadOnlyList<LibraryEntry> entries, bool newestFirst)
    {
        var dated = entries.Where(e => e.Updated.HasValue);
        var years = dated.GroupBy(e => e.Updated.Value.Year);
        years = newestFirst ? years.OrderByDescending(g => g.Key) : years.OrderBy(g => g.Key);

        var groups = new List<LibraryGroup>();
        foreach(var year in years)
        {
            var ordered = newestFirst ? year.OrderByDescending(e => e.Updated.Value) : year.OrderBy(e => e.Updated.Value);
            groups.Add(new LibraryGroup { Heading = year.Key.ToString(), Entries = ordered.ThenBy(e => e.Series, StringComparer.OrdinalIgnoreCase).ToList() });
        }

        var undated = entries.Where(e => !e.Updated.HasValue).OrderBy(e => e.Series, StringComparer.OrdinalIgnoreCase).ToList();
        if(undated.Count > 0) { groups.Add(new LibraryGroup { Heading = UnknownDate, Entries = undated }); }
        return groups;
    }
}
