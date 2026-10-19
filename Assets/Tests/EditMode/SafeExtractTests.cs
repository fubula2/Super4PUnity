using System.IO;
using NUnit.Framework;

public class SafeExtractTests
{
    private string root;

    [SetUp]
    public void SetUp() { root = Path.Combine(Path.GetTempPath(), "s4p_safe_" + System.Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root); }

    [TearDown]
    public void TearDown() { if(Directory.Exists(root)) { Directory.Delete(root, true); } }

    [Test]
    public void Accepts_normal_relative_entries()
    {
        Assert.IsTrue(SafeExtract.TryResolve(root, "a/b.txt", out var full));
        Assert.AreEqual(Path.GetFullPath(Path.Combine(root, "a", "b.txt")), full);
        Assert.IsTrue(SafeExtract.TryResolve(root, "CHAR_X/clip.mp4", out _));
        Assert.IsTrue(SafeExtract.TryResolve(root, "folder/", out _));
        Assert.IsTrue(SafeExtract.TryResolve(root, "a/./b.txt", out _));
    }

    [Test]
    public void Accepts_dots_inside_a_segment()
    {
        Assert.IsTrue(SafeExtract.TryResolve(root, "a..b/c", out _));
        Assert.IsTrue(SafeExtract.TryResolve(root, "..hidden/c", out _));
    }

    [TestCase("../evil.txt")]
    [TestCase("a/../../evil.txt")]
    [TestCase("a\\..\\..\\evil.txt")]
    [TestCase("..\\evil.txt")]
    [TestCase("/abs/evil.txt")]
    [TestCase("\\abs\\evil.txt")]
    [TestCase("C:\\evil.txt")]
    [TestCase("C:/evil.txt")]
    [TestCase("")]
    [TestCase(null)]
    public void Rejects_unsafe_entries(string entry)
    {
        Assert.IsFalse(SafeExtract.TryResolve(root, entry, out var full));
        Assert.IsNull(full);
    }

    [Test]
    public void Sibling_directory_with_same_prefix_is_not_inside_root()
    {
        //root is ".../s4p_safe_x"; ".../s4p_safe_x_evil" must not count as inside it
        Assert.IsFalse(SafeExtract.TryResolve(root, "../" + Path.GetFileName(root) + "_evil/x", out _));
    }
}
