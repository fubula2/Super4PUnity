using System.Linq;
using NUnit.Framework;

public class MusicTreeTests
{
    private static MusicEntry F(string name) { return new MusicEntry { Id = name, Name = name, Kind = MusicEntryKind.File }; }
    private static MusicEntry D(string name) { return new MusicEntry { Id = name, Name = name, Kind = MusicEntryKind.Folder }; }

    [Test]
    public void SetChildren_sorts_folders_first_and_drops_non_ogg()
    {
        var tree = new MusicTree();
        tree.SetChildren(tree.Root, new[] { F("b.ogg"), F("notes.txt"), D("Zed"), F("A.ogg"), D("alpha") });
        CollectionAssert.AreEqual(new[] { "alpha", "Zed", "A.ogg", "b.ogg" }, tree.Root.Children.Select(c => c.Entry.Name).ToArray());
        Assert.IsTrue(tree.Root.Loaded);
    }

    [Test]
    public void FileCount_counts_loaded_descendant_files()
    {
        var tree = new MusicTree();
        tree.SetChildren(tree.Root, new[] { D("One"), F("x.ogg") });
        var one = tree.Root.Children[0];
        tree.SetChildren(one, new[] { F("a.ogg"), F("b.ogg") });
        Assert.AreEqual(2, one.FileCount);
        Assert.AreEqual(3, tree.Root.FileCount);
    }

    [Test]
    public void Empty_folder_loads_with_no_children()
    {
        var tree = new MusicTree();
        tree.SetChildren(tree.Root, new MusicEntry[0]);
        Assert.IsTrue(tree.Root.Loaded);
        Assert.AreEqual(0, tree.Root.Children.Count);
    }
}
