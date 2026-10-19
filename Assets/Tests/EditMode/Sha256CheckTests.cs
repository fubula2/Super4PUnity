using System.IO;
using System.Text;
using NUnit.Framework;

public class Sha256CheckTests
{
    private const string Abc = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";

    private static Stream S(string t) { return new MemoryStream(Encoding.ASCII.GetBytes(t)); }

    [Test]
    public void Matches_known_vector_case_insensitively()
    {
        Assert.IsTrue(Sha256Check.Matches(S("abc"), Abc));
        Assert.IsTrue(Sha256Check.Matches(S("abc"), Abc.ToUpperInvariant()));
    }

    [Test]
    public void Rejects_wrong_or_missing_hash()
    {
        Assert.IsFalse(Sha256Check.Matches(S("abd"), Abc));
        Assert.IsFalse(Sha256Check.Matches(S("abc"), ""));
        Assert.IsFalse(Sha256Check.Matches(null, Abc));
    }
}
