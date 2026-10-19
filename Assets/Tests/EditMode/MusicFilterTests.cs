using System.Linq;
using NUnit.Framework;

public class MusicFilterTests
{
    private static MusicEntry F(string n) { return new MusicEntry { Id = n, Name = n, Kind = MusicEntryKind.File }; }
    private static MusicEntry D(string n) { return new MusicEntry { Id = n, Name = n, Kind = MusicEntryKind.Folder }; }

    private static MusicTree Build()
    {
        var tree = new MusicTree();
        tree.SetChildren(tree.Root, new[] { D("Anime"), D("Games"), F("root_song.ogg") });
        tree.SetChildren(tree.Root.Children[0], new[] { F("Bebop_Theme.ogg"), F("Naruto_Blue.ogg") });
        tree.SetChildren(tree.Root.Children[1], new[] { F("Zelda_Main.ogg") });
        return tree;
    }

    [Test]
    public void Empty_query_shows_everything()
    {
        var tree = Build();
        var visible = MusicFilter.Visible(tree.Root, "  ");
        Assert.AreEqual(7, visible.Count);
    }

    [Test]
    public void Match_keeps_file_and_ancestors_only()
    {
        var tree = Build();
        var visible = MusicFilter.Visible(tree.Root, "bebop");
        var names = visible.Select(n => n.Entry.Name).OrderBy(n => n).ToArray();
        CollectionAssert.AreEqual(new[] { "", "Anime", "Bebop_Theme.ogg" }.OrderBy(n => n).ToArray(), names);
    }

    [Test]
    public void No_match_returns_empty()
    {
        Assert.AreEqual(0, MusicFilter.Visible(Build().Root, "zzz").Count);
    }
}
