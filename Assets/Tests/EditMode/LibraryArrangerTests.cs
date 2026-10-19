using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public class LibraryArrangerTests
{
    private static LibraryEntry E(string series, string category, int? y = null, int m = 1)
    {
        return new LibraryEntry { Series = series, Category = category, Updated = y.HasValue ? new DateTime(y.Value, m, 1) : (DateTime?)null, Url = "u" };
    }

    private static List<LibraryEntry> Sample()
    {
        return new List<LibraryEntry> { E("Zeta", "Games", 2022, 5), E("Alpha", "Games", 2024, 1), E("Mid", "Anime", 2022, 9), E("NoDate", "Anime"), E("Beta", "Anime", 2024, 1) };
    }

    [Test]
    public void Sheet_mode_keeps_order_and_groups_by_consecutive_category()
    {
        var groups = LibraryArranger.Arrange(Sample(), SortMode.Sheet);
        CollectionAssert.AreEqual(new[] { "Games", "Anime" }, groups.Select(g => g.Heading).ToArray());
        CollectionAssert.AreEqual(new[] { "Zeta", "Alpha" }, groups[0].Entries.Select(e => e.Series).ToArray());
        CollectionAssert.AreEqual(new[] { "Mid", "NoDate", "Beta" }, groups[1].Entries.Select(e => e.Series).ToArray());
    }

    [Test]
    public void Newest_groups_by_year_descending_with_unknown_last()
    {
        var groups = LibraryArranger.Arrange(Sample(), SortMode.Newest);
        CollectionAssert.AreEqual(new[] { "2024", "2022", "Unknown date" }, groups.Select(g => g.Heading).ToArray());
        //Same date: ties broken by series name
        CollectionAssert.AreEqual(new[] { "Alpha", "Beta" }, groups[0].Entries.Select(e => e.Series).ToArray());
        CollectionAssert.AreEqual(new[] { "Mid", "Zeta" }, groups[1].Entries.Select(e => e.Series).ToArray());
    }

    [Test]
    public void Oldest_groups_by_year_ascending_with_unknown_last()
    {
        var groups = LibraryArranger.Arrange(Sample(), SortMode.Oldest);
        CollectionAssert.AreEqual(new[] { "2022", "2024", "Unknown date" }, groups.Select(g => g.Heading).ToArray());
        CollectionAssert.AreEqual(new[] { "Zeta", "Mid" }, groups[0].Entries.Select(e => e.Series).ToArray());
    }

    [Test]
    public void Empty_and_null_category_have_empty_heading()
    {
        var groups = LibraryArranger.Arrange(new List<LibraryEntry> { E("A", null), E("B", "") }, SortMode.Sheet);
        Assert.AreEqual(1, groups.Count);
        Assert.AreEqual("", groups[0].Heading);
        Assert.AreEqual(2, groups[0].Entries.Count);
    }

    [Test]
    public void No_entries_gives_no_groups()
    {
        Assert.AreEqual(0, LibraryArranger.Arrange(new List<LibraryEntry>(), SortMode.Newest).Count);
    }

    [Test]
    public void Year_boundary_goes_to_the_right_group()
    {
        var entries = new List<LibraryEntry> { new LibraryEntry { Series = "A", Updated = new DateTime(2023, 12, 31), Url = "u" }, new LibraryEntry { Series = "B", Updated = new DateTime(2024, 1, 1), Url = "u" } };
        var groups = LibraryArranger.Arrange(entries, SortMode.Newest);
        CollectionAssert.AreEqual(new[] { "2024", "2023" }, groups.Select(g => g.Heading).ToArray());
    }
}
