using System.IO;
using System.Linq;
using NUnit.Framework;

public class PackLocatorTests
{
    private string root;

    [SetUp]
    public void SetUp() { root = Path.Combine(Path.GetTempPath(), "s4p_loc_" + System.Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root); }

    [TearDown]
    public void TearDown() { if(Directory.Exists(root)) { Directory.Delete(root, true); } }

    private void Dir(string rel) { Directory.CreateDirectory(Path.Combine(root, rel)); }

    [Test]
    public void Finds_CHAR_dir_at_depth_0()
    {
        Dir("CHAR_Alpha");
        CollectionAssert.AreEqual(new[] { Path.Combine(root, "CHAR_Alpha") }, PackLocator.FindCharDirs(root));
    }

    [Test]
    public void Finds_CHAR_dirs_inside_wrapper_folders()
    {
        Dir("Wrapper_V1.0/CHAR_Alpha");
        Dir("Wrapper_V1.0/Sub/CHAR_Beta");
        var found = PackLocator.FindCharDirs(root).Select(p => Path.GetFileName(p)).OrderBy(x => x).ToArray();
        CollectionAssert.AreEqual(new[] { "CHAR_Alpha", "CHAR_Beta" }, found);
    }

    [Test]
    public void Returns_empty_when_none()
    {
        Dir("Wrapper/music");
        Assert.AreEqual(0, PackLocator.FindCharDirs(root).Count);
        Assert.AreEqual(0, PackLocator.FindCharDirs(Path.Combine(root, "missing")).Count);
    }

    [Test]
    public void Does_not_descend_into_a_found_CHAR_dir()
    {
        Dir("CHAR_Outer/CHAR_Inner");
        var found = PackLocator.FindCharDirs(root);
        Assert.AreEqual(1, found.Count);
        Assert.AreEqual("CHAR_Outer", Path.GetFileName(found[0]));
    }

    [Test]
    public void Match_is_case_sensitive_prefix_CHAR_underscore()
    {
        Dir("char_lower");
        Dir("CHARX");
        Assert.AreEqual(0, PackLocator.FindCharDirs(root).Count);
    }
}
