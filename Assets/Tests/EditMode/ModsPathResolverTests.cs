using System;
using NUnit.Framework;

public class ModsPathResolverTests
{
    private static readonly Func<string, bool> Exists = p => p == "/data/mods" || p == "/Users/x/My Mods/музыка";
    private static readonly Func<string, bool> Never = p => false;

    [Test]
    public void Resolve_null_uses_default()
    {
        var r = ModsPathResolver.Resolve(null, "/app/mods", Never);
        Assert.AreEqual("/app/mods", r.Mods);
        Assert.IsFalse(r.UsedFallback);
    }

    [Test]
    public void Resolve_empty_and_whitespace_use_default()
    {
        Assert.AreEqual("/app/mods", ModsPathResolver.Resolve("", "/app/mods", Never).Mods);
        Assert.AreEqual("/app/mods", ModsPathResolver.Resolve("   ", "/app/mods", Never).Mods);
    }

    [Test]
    public void Resolve_existing_configured_is_used_and_not_fallback()
    {
        var r = ModsPathResolver.Resolve("/data/mods", "/app/mods", Exists);
        Assert.AreEqual("/data/mods", r.Mods);
        Assert.IsFalse(r.UsedFallback);
    }

    [Test]
    public void Resolve_missing_configured_falls_back_with_flag()
    {
        var r = ModsPathResolver.Resolve("/gone/mods", "/app/mods", Never);
        Assert.AreEqual("/app/mods", r.Mods);
        Assert.IsTrue(r.UsedFallback);
    }

    [Test]
    public void Normalize_strips_trailing_slashes_and_converts_backslashes()
    {
        Assert.AreEqual("C:/a/mods", ModsPathResolver.Normalize("C:\\a\\mods\\"));
        Assert.AreEqual("/data/mods", ModsPathResolver.Normalize("/data/mods//"));
        Assert.AreEqual("/", ModsPathResolver.Normalize("/"));
    }

    [Test]
    public void Music_is_mods_plus_music()
    {
        Assert.AreEqual("/data/mods/music", ModsPathResolver.Resolve("/data/mods/", "/app/mods", Exists).Music);
    }

    [Test]
    public void Resolve_keeps_spaces_and_unicode()
    {
        var r = ModsPathResolver.Resolve("/Users/x/My Mods/музыка", "/app/mods", Exists);
        Assert.AreEqual("/Users/x/My Mods/музыка", r.Mods);
        Assert.IsFalse(r.UsedFallback);
    }
}
